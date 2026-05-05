using DiarizationExample.Data;
using DiarizationExample.Flows.Alignment;
using DiarizationExample.Flows.AudioPreparation;
using DiarizationExample.Flows.Diarization;
using DiarizationExample.Flows.Reporting;
using DiarizationExample.Flows.Transcription;
using Flowthru.Core.Cli;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiarizationExample;

public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services =>
        ConfigureServices(
          services,
          Directory.GetCurrentDirectory(),
          AppDomain.CurrentDomain.BaseDirectory
        )
    );

  public static IServiceProvider ConfigureServices(
    string? basePath = null,
    string? outputPath = null
  )
  {
    var services = new ServiceCollection();
    ConfigureServices(
      services,
      basePath ?? Directory.GetCurrentDirectory(),
      outputPath ?? AppDomain.CurrentDomain.BaseDirectory
    );
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string outputPath
  )
  {
    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
              json.WithOutputDirectory(metadataPath)
            )
            .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
              mermaid.WithOutputDirectory(metadataPath)
            );
        });

        flowthru.UsePython(python =>
        {
          python.ModuleSearchPaths.Add(basePath);                 // Flows/, Services/
          python.ModuleSearchPaths.Add(outputPath);               // generated flowthru pkg
          python.VenvPath = outputPath;

          // ── IConfiguration → env-var bridge ───────────────────────────────
          // The Python extension flattens the named IConfiguration section
          // into env vars (using .NET's native `:` → `__` rule) at subprocess
          // spawn time. `flowthru.config` re-nests those env vars on the
          // Python side and exposes typed accessors that mirror
          // IConfiguration.GetValue<T>(...). Both services and inspectors
          // consume config via the shared DiarizationConfig dataclass — see
          // Services/diarization_config.py.
          python.ConfigurationSection = "Diarization";

          // ── Service ↔ sidecar-inspector linkage ───────────────────────────
          // Mirrors the .NET pattern of registering an IFlowthruInspector<T>
          // for a service. The runtime class lives in `Services/<name>.py`
          // (Flowthru-free); the matching validator lives in
          // `Services/<name>_inspector.py` and exports an `inspect(svc)`
          // function that returns a ValidationResult.
          //
          // The class path matches what the Python @step(services=[...])
          // decorator emits — `cls.__module__ + "." + cls.__qualname__` —
          // which always points at the *defining* module (e.g.
          // `Services.pyannote_diarizer.PyannoteDiarizer`), not any
          // re-export path.
          python.RegisterService(
            "Services.pyannote_diarizer.PyannoteDiarizer",
            svc => svc.WithInspector("Services.pyannote_diarizer_inspector")
          );

          python.RegisterService(
            "Services.whisper_transcriber.WhisperTranscriber",
            svc => svc.WithInspector("Services.whisper_transcriber_inspector")
          );

          python.RegisterService(
            "Services.ffmpeg_normalizer.FfmpegNormalizer",
            svc => svc.WithInspector("Services.ffmpeg_normalizer_inspector")
          );
        });

        var tempProvider = flowthru.Services.BuildServiceProvider();
        var executor =
          tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

        flowthru
          .RegisterFlow(label: "AudioPreparation", flow: AudioPreparationFlow.Create)
          .WithDescription("Resample raw audio to 16kHz mono PCM (uses FfmpegNormalizer).");

        flowthru
          .RegisterFlow(label: "Transcription", flow: TranscriptionFlow.Create)
          .WithDescription("Whisper transcription (uses WhisperTranscriber).");

        flowthru
          .RegisterFlow(label: "Diarization", flow: DiarizationFlow.Create)
          .WithDescription("pyannote speaker diarization (uses PyannoteDiarizer).");

        flowthru
          .RegisterFlow(label: "Alignment", flow: AlignmentFlow.Create)
          .WithDescription("Join transcripts to speaker turns by maximal overlap (C#).");

        flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription("Render one Markdown transcript per clip.");
      }
    );
  }
}
