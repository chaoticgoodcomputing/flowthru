# Flowthru.FUnit

An inline step-test framework for Flowthru pipelines. Tests live in the same file as the Step
they exercise — a nested `Tests : FUnitContext` class behind a `#if FUNIT_ENABLED` guard — so a
Step's transform and its assertions never drift apart. `FUnitContext` gives you typed Step
invocation, sample-data helpers, and a per-test DI container; a bundled source generator reads
each `[FUnitStepTest]` and emits the runner classes so `dotnet test` discovers the tests under
NUnit, xUnit, or MSTest with no framework attribute in your code.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_funit)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Install

```bash
dotnet add package Flowthru.FUnit
```

Nest a `Tests` class inside the Step, invoke its `Create()` transform, and assert on the result:

```csharp
[FlowthruStep]
public static class PreprocessCompaniesStep
{
    public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create(
        ILogger logger) => input => /* … */;

#if FUNIT_ENABLED
    public class Tests : FUnitContext
    {
        [FUnitStepTest(typeof(PreprocessCompaniesStep))]
        public void ValidRecord_ParsesCorrectly()
        {
            var input = Samples.Of(new CompanySchema { Id = "C1", CompanyRating = "90%" });

            var result = Invoke(Create(NullLogger.Instance), input).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CompanyRating, Is.EqualTo(0.90m));
        }
    }
#endif
}
```

The `#if FUNIT_ENABLED` guard keeps tests out of release builds; define the constant in your
test configuration alongside your chosen runner (e.g. `NUnit` + `NUnit3TestAdapter`).
