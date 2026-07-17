---
name: flowthru-funit
description: "Deep skill for Flowthru.FUnit — the inline step-test framework where tests live beside the Step they exercise, as a nested `Tests : FUnitContext` class behind `#if FUNIT_ENABLED`. Use when writing or modifying step tests in a Flowthru (.NET) project, when a step file contains a `Tests` class or `[FUnitStepTest]`, or when adding test coverage to a Flow. Pairs with the umbrella `flowthru` skill."
metadata:
  flowthru:
    extension: Flowthru.FUnit
    surface: testing
    capability: Inline step tests beside the Step they exercise — typed Invoke + sample-data helpers; `dotnet test` discovers them via source-generated runners.
    register: "— (tests compile behind FUNIT_ENABLED)"
---

# flowthru-funit

An inline step-test framework: tests live **in the same file as the Step they exercise** — a nested `Tests : FUnitContext` class behind an `#if FUNIT_ENABLED` guard — so a Step's transform and its assertions never drift apart. Because a Flowthru Step is a plain function (`Create(deps) => input => output`), it tests without any pipeline: build typed input rows, invoke the transform, assert on the result.

A bundled source generator reads each `[FUnitStepTest]` and emits the runner classes, so `dotnet test` discovers the tests under NUnit, xUnit, or MSTest with no framework attribute in your code.

## Install

```bash
dotnet add package Flowthru.FUnit
```

The `#if FUNIT_ENABLED` guard keeps tests out of release builds — define the constant in your test configuration alongside your chosen runner (e.g. `NUnit` + `NUnit3TestAdapter`).

## Write a test

Nest a `Tests : FUnitContext` class inside the Step's own class, and mark each test with `[FUnitStepTest(typeof(TheStep))]`:

<!-- flowthru:snippet:docs:step-funit-test:start -->
```csharp
[FUnitStepTest(typeof(PredictStep))]
public void ReturnsOnePredictionPerInputRow()
{
  // Arrange
  var testX = Samples.Generate(
    5,
    i => new FeatureVectorSchema
    {
      SepalLength = 5.0 + i,
      SepalWidth = 3.0,
      PetalLength = 1.5,
      PetalWidth = 0.3,
    }
  );

  // Apply
  var predictions = Invoke(Create(), (ZeroModel(), testX)).ToList();

  // Assert
  Assert.That(predictions, Has.Count.EqualTo(5));
}
```
_(source: [`IrisFUnit/PredictStep.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/IrisFUnit/Flows/DataScience/Steps/PredictStep.cs))_
<!-- flowthru:snippet:docs:step-funit-test:end -->

- **`Invoke(Create(deps), input)`** runs the transform exactly as the pipeline would, with typed input and output.
- **`Samples.Of(row)` / `Samples.Generate(n, i => row)`** build typed input rows — one-off or parameterized batches.
- Step dependencies are ordinary `Create` parameters — pass test doubles directly (e.g. `Create(NullLogger.Instance)`).

## Notes

- **These are design-time checks** — the earliest, cheapest place to catch a logic error in Flowthru's fail-fast model: a step test fails in `dotnet test` long before a flow ever launches.
- **Co-location is the point.** The test class sits inside the step class, so editing a transform without its assertions in view doesn't happen.
- **Worked examples:** the [IrisFUnit](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/IrisFUnit) and [SpaceflightsFUnit](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/SpaceflightsFUnit) starters are FUnit-tested end to end; the [package README](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/core/Flowthru.FUnit/README.md) carries the full reference.
