# Flow Developers

This is the **conceptual** guide to writing Flows and Steps. For catalog items and schemas, see [catalog-developers.md](catalog-developers.md). For stack-specific step hosts and formats, see [extensions.md](extensions.md).

## Mental model

A **Flow** is a DAG of **Steps** wired between **catalog items**. A Step is a pure function `input → output`; the Flow declares which catalog items feed each Step's input and which receive its output. Flowthru builds the DAG from that wiring, validates it, and — at run time — reads inputs, runs each Step, and writes outputs, scheduling Steps as their inputs become available.

You write two kinds of code:

- **Step logic** — the transform itself, as plain C# (LINQ, loops, whatever). This is where your domain logic lives.
- **Flow wiring** — a declaration of how Steps connect to catalog items. No logic here; just typed plumbing.

The split matters because the wiring is checked at **compile time**: passing a catalog item of the wrong schema type to a Step input is a C# type error, caught before anything runs.

## Steps

A Step is a `[FlowthruStep] public static class` exposing a `Create` method that returns the transform as a `Func<TIn, TOut>`. Dependencies (loggers, services) are parameters to `Create`, captured by the returned closure — the transform itself only ever sees its typed input.

```csharp
[FlowthruStep]
public static class SplitAndEncodeStep
{
    // Step-specific options travel as an ordinary input (see catalog-developers.md
    // for how a ConfigurationItem binds these from appsettings.json).
    public record Options
    {
        public double TestDataRatio { get; init; } = 0.2;
    }

    // Canonical shape: static Func<TIn, TOut> Create(deps) => input => { ... };
    // Multiple inputs/outputs are carried as tuples; name the output elements.
    public static Func<
        (IEnumerable<IrisRawSchema>, Options),
        (
            IEnumerable<IrisFeatureSchema> Features,
            IEnumerable<FeatureVectorSchema> TrainX,
            IEnumerable<TargetLabelSchema> TrainY
        )
    > Create(ILogger logger) =>
        input =>
        {
            var (rawData, options) = input;
            // ... transform ...
            return (features, trainX, trainY);
        };
}
```

Key points:

- **`Create(deps) => input => …`** is the canonical authoring shape. Inject services as `Create` parameters; keep the transform body a pure function of `input`.
- **Tuples carry arity.** A single `(A, B)` input or `(X, Y, Z)` output tuple is how a Step declares more than one input/output. Name the output tuple elements — the flow wiring and diagnostics read those names.
- **Options-as-input.** A Step's configuration is just another typed input (often a nested `Options` record), sourced from the catalog. A config change then invalidates the step's cached output like any other input change — don't reach for ambient/global config.

## Flows

A Flow is a `public static class` with a `Create` method returning `BuiltFlow`. It takes the `Catalog` (and any services the wiring needs, e.g. `ILogger`) and calls `FlowBuilder.CreateFlow(label, pipeline => …)`, adding one `AddStep` per Step.

```csharp
public static class DataEngineeringFlow
{
    public static BuiltFlow Create(Catalog catalog, ILogger logger) =>
        FlowBuilder.CreateFlow("DataEngineering", pipeline =>
        {
            pipeline.AddStep<
                IEnumerable<IrisRawSchema>, SplitAndEncodeStep.Options,   // input types
                IEnumerable<IrisFeatureSchema>,                            // output types
                IEnumerable<FeatureVectorSchema>,
                IEnumerable<TargetLabelSchema>
            >(
                label: "SplitAndEncode",
                transform: SplitAndEncodeStep.Create(logger),
                inputs: (catalog.IrisRaw, catalog.SplitOptions),
                outputs: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY)
            );
        });
}
```

- **`AddStep<…>` is fully typed.** The generic parameters are the Step's input types followed by its output types; `inputs`/`outputs` are tuples of catalog items. The compiler checks that each catalog item's schema type matches the Step's signature — wrong type, wrong arity, or swapped input/output all fail to compile.
- **`BuiltFlow` is immutable.** `CreateFlow` returns a fully-built, validated flow; there is no mutate-after-build.
- **The DAG is inferred from the wiring.** You don't declare edges; a Step that consumes an item another Step produces is automatically downstream of it.

## Registration & running

Flows are registered in `Program.cs`. The entrypoint hands off to the Flowthru CLI runner, which discovers and executes registered flows:

```csharp
public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(args, ConfigureServices);

static void ConfigureServices(IServiceCollection services)
{
    services.AddFlowthru(b =>
    {
        b.UseConfiguration(configuration);                 // if the catalog binds config items
        b.RegisterCatalog(sp => new Catalog(basePath, sp.GetRequiredService<IConfiguration>()));

        // Second generic = the service type the flow's Create() second parameter needs.
        b.RegisterFlow<Catalog, ILogger>("DataEngineering", DataEngineeringFlow.Create)
         .WithDescription("Splits iris data into training and test sets");

        // Each consumed extension is enabled with its own b.UseXxx() — see extensions.md.
    });
}
```

Run with **`dotnet run`** (no args) and confirm the output. `RegisterFlow<Catalog, TService>` names the flow and the DI service its factory's second parameter needs; chain `.WithDescription(...)` for the CLI listing.

## Effects & errors

I/O is wrapped in `FlowIO<T>` (from `Flowthru.Prelude`). Failures are **typed values**, not exceptions: `RuntimeError` and `PreFlightError` are closed sums that you'd match on, and nothing throws across the `FlowIO` boundary. Most step bodies never touch this directly — the framework wraps your `Func<TIn, TOut>` for you. You mainly care that a failing read/write surfaces as a typed pre-flight or runtime error the runner reports, rather than an unhandled throw.

## Testing steps (FUnit)

Because a Step is a plain function, it unit-tests without any pipeline. Projects using FUnit put a nested test class behind `#if FUNIT_ENABLED`:

```csharp
#if FUNIT_ENABLED
public class Tests : FUnitContext
{
    [FUnitStepTest(typeof(SplitAndEncodeStep))]
    public void Splits20Percent()
    {
        var raw = Samples.Generate(10, i => new IrisRawSchema { /* ... */ });
        var (features, trainX, trainY) = Invoke(
            Create(NullLogger.Instance),
            (raw, new Options { TestDataRatio = 0.2 }));
        Assert.That(trainX.Count(), Is.EqualTo(8));
    }
}
#endif
```

`Invoke(Create(deps), input)` runs the transform; `Samples.Generate`/`Samples.Of` build typed input rows. These are fast design-time checks — the earliest, best place to catch a logic error.
