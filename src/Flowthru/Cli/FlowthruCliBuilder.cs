using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Cli;

/// <summary>
/// Builder for configuring and creating a FlowthruCli instance.
/// </summary>
/// <remarks>
/// <para>
/// Flowthru CliBuilder provides a fluent API for configuring the CLI.
/// It wraps the service layer configuration and adds CLI-specific options.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// public static async Task&lt;int&gt; Main(string[] args)
/// {
///     var cli = FlowthruCliBuilder.Create(builder =>
///     {
///         builder.UseCatalog&lt;MyCatalog&gt;();
///         builder.UsePipelines(catalog => new Dictionary&lt;string, Pipeline&gt;
///         {
///             ["pipeline1"] = Pipeline1.Create(catalog)
///         });
///     })
///     .ConfigureLogging(logging => logging.AddConsole())
///     .Build();
///
///     return await cli.RunAsync(args);
/// }
/// </code>
/// </remarks>
public sealed class FlowthruCliBuilder
{
  private readonly IServiceCollection _services;
  private Action<ILoggingBuilder>? _configureLogging;
  private TextWriter? _output;

  /// <summary>
  /// Initializes a new CLI builder.
  /// </summary>
  private FlowthruCliBuilder()
  {
    _services = new ServiceCollection();
  }

  /// <summary>
  /// Creates a new CLI builder with the specified configuration.
  /// </summary>
  /// <param name="configure">Configuration action</param>
  /// <returns>Builder instance for chaining configuration</returns>
  public static FlowthruCliBuilder Create(Action<FlowthruServiceBuilder> configure)
  {
    var builder = new FlowthruCliBuilder();
    builder._services.AddFlowthru(configure);
    return builder;
  }

  /// <summary>
  /// Configures logging for the CLI.
  /// </summary>
  /// <param name="configure">Logging configuration action</param>
  /// <returns>This builder for chaining</returns>
  public FlowthruCliBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
  {
    _configureLogging = configure;
    return this;
  }

  /// <summary>
  /// Sets the output writer for CLI messages.
  /// </summary>
  /// <param name="output">Output writer (defaults to Console.Out)</param>
  /// <returns>This builder for chaining</returns>
  public FlowthruCliBuilder UseOutput(TextWriter output)
  {
    _output = output;
    return this;
  }

  /// <summary>
  /// Builds the CLI instance.
  /// </summary>
  /// <returns>Configured CLI instance</returns>
  public FlowthruCli Build()
  {
    // Add logging if configured
    if (_configureLogging != null)
    {
      _services.AddLogging(_configureLogging);
    }
    else
    {
      // Default console logging
      _services.AddLogging(logging =>
      {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
      });
    }

    // Build service provider
    var provider = _services.BuildServiceProvider();

    // Resolve dependencies
    var service = provider.GetRequiredService<IFlowthruService>();
    var logger = provider.GetRequiredService<ILogger<FlowthruCli>>();

    return new FlowthruCli(service, logger, _output);
  }
}
