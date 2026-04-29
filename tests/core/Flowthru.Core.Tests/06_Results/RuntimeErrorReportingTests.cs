using Flowthru.Core.Flows;
using Flowthru.Core.Services;
using Flowthru.Core.Services.Models;
using Flowthru.Core.Tests.Fixtures;
using Flowthru.Core.Tests.Fixtures.TestCatalogs;
using Flowthru.Core.Tests.Fixtures.TestSteps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Results;

/// <summary>
/// End-to-end tests verifying that the runtime-error UX (the
/// "this may indicate a bug in Flowthru, please file an issue" framing emitted
/// by <see cref="ConsoleResultFormatter"/>) reliably reaches the user when an
/// unexpected exception escapes a step. These tests pair an in-memory pipeline
/// with a recording logger so we can assert on the actual log output the user
/// would see in production.
/// </summary>
[TestFixture]
[Category("Results")]
[Category("RuntimeErrorReporting")]
public class RuntimeErrorReportingTests
{
  private const string IssueUrlMarker = "github.com/chaoticgoodcomputing/flowthru/issues/new";

  private (IFlowthruService service, RecordingLogger logger) CreateServiceWithRecordingLogger(
    SimpleThreeStepCatalog catalog,
    Dictionary<string, Flow> pipelines
  )
  {
    var loggerProvider = new RecordingLoggerProvider();

    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
      builder.AddProvider(loggerProvider);
      builder.SetMinimumLevel(LogLevel.Trace);
    });
    services.AddFlowthru(
      new ConfigurationBuilder().Build(),
      flowthru =>
      {
        flowthru.RegisterCatalog(catalog);
        flowthru.RegisterFlows(sp => pipelines);
      }
    );

    var serviceProvider = services.BuildServiceProvider();
    var service = serviceProvider.GetRequiredService<IFlowthruService>();
    return (service, loggerProvider.SharedLogger);
  }

  [Test]
  public async Task ExecuteFlowAsync_StepThrows_EmitsFrameworkBugFraming()
  {
    // Arrange: a single-step pipeline whose only step throws a non-allowlisted
    // exception (InvalidOperationException). This is the smallest reproducer
    // for the downstream user's NpgsqlException scenario — both share the
    // critical property of "not in RuntimeErrorClassifier.ExternalExceptionTypes,"
    // so both should classify as PossibleFrameworkBug and surface the issue URL.
    var catalog = new SimpleThreeStepCatalog();
    await catalog
      .Input.Save(
        new[]
        {
          new TestData
          {
            Id = 1,
            Name = "x",
            Value = 1.0,
          },
        }
      )
      .Run();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "WillThrow",
        transform: FailingStep.Create("boom"),
        input: catalog.Input,
        output: catalog.Output
      );
    });
    pipeline.Name = "test_pipeline";
    var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

    var (service, logger) = CreateServiceWithRecordingLogger(catalog, pipelines);

    // Act
    FlowResult? result = null;
    Exception? thrown = null;
    try
    {
      result = await service.ExecuteFlowAsync(options: null, exportMetadata: false);
    }
    catch (Exception ex)
    {
      thrown = ex;
    }

    // Diagnostics on failure: dump everything we recorded so the assertion
    // failure message points at what actually happened, not just "missing URL."
    var rendered = string.Join("\n", logger.Messages);

    // Assert: ExecuteFlowAsync should NOT throw past the formatter.
    Assert.That(
      thrown,
      Is.Null,
      $"ExecuteFlowAsync threw past the formatter, so the runtime-error UX never had a chance "
        + $"to fire. Captured exception: {thrown?.GetType().Name}: {thrown?.Message}\n"
        + $"Recorded log:\n{rendered}"
    );

    // Assert: the FlowResult was returned with the original exception preserved.
    Assert.That(result, Is.Not.Null);
    Assert.That(result!.Success, Is.False);
    Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());

    // Assert: the "please file an issue" framing reached the user.
    Assert.That(
      logger.Messages,
      Has.Some.Contains(IssueUrlMarker),
      $"Expected the runtime-error UX to emit a GitHub issue URL, but it never appeared in logs. "
        + $"Recorded log:\n{rendered}"
    );
  }

  [Test]
  public async Task ExecuteFlowAsync_StepThrowsWithStopOnFirstError_EmitsFrameworkBugFraming()
  {
    // Same as the above, but with multiple steps and StopOnFirstError = true.
    // Mirrors the downstream user's stack: 22-step migration, step 15 fails,
    // executor cascades cancellation to siblings. The framing must still fire.
    var catalog = new SimpleThreeStepCatalog();
    await catalog
      .Input.Save(
        new[]
        {
          new TestData
          {
            Id = 1,
            Name = "x",
            Value = 1.0,
          },
        }
      )
      .Run();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Step1",
        transform: PassthroughStep.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );
      builder.AddStep(
        label: "Step2_Fails",
        transform: FailingStep.Create("boom mid-flow"),
        input: catalog.StepOne,
        output: catalog.StepTwo
      );
      builder.AddStep(
        label: "Step3_Skipped",
        transform: PassthroughStep.Create(),
        input: catalog.StepTwo,
        output: catalog.Output
      );
    });
    pipeline.Name = "multi_step";
    var pipelines = new Dictionary<string, Flow> { ["multi_step"] = pipeline };

    var (service, logger) = CreateServiceWithRecordingLogger(catalog, pipelines);

    FlowResult? result = null;
    Exception? thrown = null;
    try
    {
      result = await service.ExecuteFlowAsync(
        options: new ExecutionOptions { StopOnFirstError = true },
        exportMetadata: false
      );
    }
    catch (Exception ex)
    {
      thrown = ex;
    }

    var rendered = string.Join("\n", logger.Messages);

    Assert.That(
      thrown,
      Is.Null,
      $"Stop-on-first-error path threw past the formatter. Captured: "
        + $"{thrown?.GetType().Name}: {thrown?.Message}\nRecorded log:\n{rendered}"
    );
    Assert.That(result, Is.Not.Null);
    Assert.That(result!.Success, Is.False);
    Assert.That(
      logger.Messages,
      Has.Some.Contains(IssueUrlMarker),
      $"Framing missing on stop-on-first-error path.\nRecorded log:\n{rendered}"
    );
  }

  [Test]
  public async Task ExecuteFlowAsync_CallerCancelsToken_PropagatesWithoutFraming()
  {
    // Regression guard: the service-level safety net must not swallow
    // caller-initiated cancellation. A user pressing Ctrl-C (or a host
    // applying its own deadline) should propagate as a clean
    // OperationCanceledException, with NO "please file an issue" framing —
    // that's a user-requested abort, not a Flowthru bug.
    var catalog = new SimpleThreeStepCatalog();
    await catalog
      .Input.Save(
        new[]
        {
          new TestData
          {
            Id = 1,
            Name = "x",
            Value = 1.0,
          },
        }
      )
      .Run();

    // A step that observes the token and yields. Cancellation will fire
    // before the step has a chance to complete.
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Slow",
        transform: DelayedStep.Create(TimeSpan.FromSeconds(5)),
        input: catalog.Input,
        output: catalog.Output
      );
    });
    pipeline.Name = "cancel_pipeline";
    var pipelines = new Dictionary<string, Flow> { ["cancel_pipeline"] = pipeline };

    var (service, logger) = CreateServiceWithRecordingLogger(catalog, pipelines);

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

    Exception? thrown = null;
    try
    {
      await service.ExecuteFlowAsync(
        options: null,
        exportMetadata: false,
        cancellationToken: cts.Token
      );
    }
    catch (Exception ex)
    {
      thrown = ex;
    }

    Assert.That(thrown, Is.InstanceOf<OperationCanceledException>());
    Assert.That(
      logger.Messages,
      Has.None.Contains(IssueUrlMarker),
      "Caller-initiated cancellation must NOT trigger the file-an-issue framing"
    );
  }

  /// <summary>
  /// Logger provider that hands out a single shared <see cref="RecordingLogger"/>
  /// for every category, so a test can assert on the union of all log output.
  /// </summary>
  private sealed class RecordingLoggerProvider : ILoggerProvider
  {
    public RecordingLogger SharedLogger { get; } = new();

    public ILogger CreateLogger(string categoryName) => SharedLogger;

    public void Dispose() { }
  }
}
