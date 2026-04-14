using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Flowthru.Spark.Interop;
using Flowthru.Spark.Services;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Spark.Runtime;

/// <summary>
/// Manages the lifecycle of the local Spark JVM backend process.
/// </summary>
/// <remarks>
/// <para>
/// On <see cref="Initialize"/>, spawns <c>spark-submit</c> with the Flowthru JVM bridge JAR
/// in <c>debug</c> mode, which starts <c>DotnetBackend</c> listening on a TCP port.
/// <see cref="Flowthru.Spark.Interop.SparkEnvironment.JvmBridge"/> connects to that port on
/// its first use — so the backend must be running before any Spark API call.
/// </para>
/// <para>
/// Registered as a singleton. <see cref="Initialize"/> is idempotent — safe to call multiple times.
/// </para>
/// </remarks>
public sealed class SparkRuntime : IDisposable
{
    private static readonly object _lock = new();

    private readonly SparkRuntimeOptions _options;
    private readonly ILogger<SparkRuntime> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private Process? _backendProcess;
    private int _backendPort;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SparkRuntime"/>.
    /// </summary>
    public SparkRuntime(
        SparkRuntimeOptions options,
        ILogger<SparkRuntime> logger,
        ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Starts the Spark JVM backend process and waits for it to accept connections.
    /// </summary>
    /// <remarks>
    /// Idempotent — subsequent calls return immediately if already initialized.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the backend does not become ready within the timeout.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this instance has been disposed.
    /// </exception>
    public void Initialize()
    {
        if (_initialized)
            return;

        if (_disposed)
            throw new ObjectDisposedException(nameof(SparkRuntime));

        lock (_lock)
        {
            if (_initialized)
                return;

            // Route all Spark library internal logs through the MEL pipeline so they
            // respect the application's configured log levels and sinks instead of
            // writing directly to stdout via ConsoleLoggerService.
            LoggerServiceFactory.SetLoggerService(new MelLoggerService(_loggerFactory));

            var sparkHome = _options.GetResolvedSparkHome();
            var jarPath = _options.GetResolvedJarPath();
            var master = _options.Master;

            _logger.LogInformation("Spark home: {SparkHome}", sparkHome);
            _logger.LogInformation("Bridge JAR: {JarPath}", jarPath);
            _logger.LogInformation("Master: {Master}", master);

            _backendPort = FindFreePort();
            Environment.SetEnvironmentVariable("DOTNETBACKEND_PORT", _backendPort.ToString());
            _logger.LogInformation("DotnetBackend port: {Port}", _backendPort);

            var sparkSubmit = Path.Combine(sparkHome, "bin", "spark-submit");
            var args = $"--class org.apache.spark.deploy.dotnet.DotnetRunner "
                + $"--master {master} "
                + $"\"{jarPath}\" debug {_backendPort}";

            _logger.LogDebug("Launching: {SparkSubmit} {Args}", sparkSubmit, args);

            var psi = new ProcessStartInfo
            {
                FileName = sparkSubmit,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // Pass the chosen port to DotnetRunner so it binds to the same port we will poll.
            psi.Environment["DOTNETBACKEND_PORT"] = _backendPort.ToString();

            _backendProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // Forward JVM output to the .NET logger at debug level
            _backendProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logger.LogDebug("[Spark JVM] {Line}", e.Data);
            };
            _backendProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logger.LogDebug("[Spark JVM stderr] {Line}", e.Data);
            };

            _backendProcess.Start();
            _backendProcess.BeginOutputReadLine();
            _backendProcess.BeginErrorReadLine();

            WaitForPort(_backendPort, timeoutSeconds: _options.BackendStartupTimeoutSeconds);

            _initialized = true;
            _logger.LogInformation("Spark JVM backend ready on port {Port}", _backendPort);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_backendProcess is { HasExited: false })
        {
            try
            {
                _logger.LogInformation("Stopping Spark JVM backend (pid {Pid})", _backendProcess.Id);

                // Kill the process directly. In local[*] debug mode the .NET side does not
                // hold a SparkContext reference, so stopActiveSparkContext would reliably
                // fail. The session teardown responsibility belongs to whoever holds the
                // SparkSession (e.g., the user's Catalog). In a future cluster-aware refactor
                // SparkRuntime should accept an optional SparkSession and call session.Stop()
                // here before killing when running locally, and skip the Kill() entirely
                // when connected to an external cluster.
                _backendProcess.Kill(entireProcessTree: true);
                _backendProcess.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while stopping Spark JVM backend");
            }
            finally
            {
                _backendProcess.Dispose();
                _backendProcess = null;
            }
        }
    }

    private static int FindFreePort()
    {
        // Bind on port 0 to let the OS assign a free port, then release it.
        // There is a small TOCTOU window, but it's negligible for local dev use.
        var envPort = Environment.GetEnvironmentVariable("DOTNETBACKEND_PORT");
        if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var configured))
            return configured;

        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void WaitForPort(int port, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                client.Connect("127.0.0.1", port);
                return;
            }
            catch (SocketException)
            {
                if (_backendProcess is { HasExited: true })
                    throw new InvalidOperationException(
                        $"Spark JVM backend process exited unexpectedly (exit code {_backendProcess.ExitCode}). "
                            + "Check that SPARK_HOME is set correctly and the bridge JAR is valid."
                    );

                Thread.Sleep(500);
            }
        }

        throw new InvalidOperationException(
            $"Spark JVM backend did not become ready on port {port} within {timeoutSeconds}s."
        );
    }
}
