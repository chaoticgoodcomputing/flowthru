using Flowthru.Step;

namespace Flowthru.FUnit.Tests.Fixtures;

// ---------------------------------------------------------------------------
// Schemas
// ---------------------------------------------------------------------------

public record NumberRow(double Value);

public record StringRow(string Text);

// ---------------------------------------------------------------------------
// Step fixtures
//
// These steps receive FU001 suppressions, as this suite is where the actual FU001 detection occurs.
// ---------------------------------------------------------------------------

[FlowthruStep]
#pragma warning disable FU001 // Step has no tests
public static class DoubleStep
{
  public static Func<IEnumerable<NumberRow>, IEnumerable<NumberRow>> Create() =>
    rows => rows.Select(r => r with { Value = r.Value * 2 });
}

[FlowthruStep]
public static class AsyncDoubleStep
{
  public static Func<IEnumerable<NumberRow>, Task<IEnumerable<NumberRow>>> Create() =>
    rows => Task.FromResult(rows.Select(r => r with { Value = r.Value * 2 }));
}

[FlowthruStep]
public static class CancellableDoubleStep
{
  public static Func<
    IEnumerable<NumberRow>,
    CancellationToken,
    Task<IEnumerable<NumberRow>>
  > Create() =>
    async (rows, ct) =>
    {
      await Task.Delay(0, ct);
      return rows.Select(r => r with { Value = r.Value * 2 });
    };
}

/// <summary>Step with NO [StepTest] methods — exists to verify FU001 fires in the registry.</summary>
[FlowthruStep]
public static class UntestedStep
{
  public static Func<IEnumerable<NumberRow>, IEnumerable<NumberRow>> Create() => rows => rows;
}
#pragma warning restore FU001 // Step has no tests
