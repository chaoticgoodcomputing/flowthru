using System.Collections.Concurrent;
using NUnit.Framework;

namespace MagicAST.Tests.Infrastructure;

/// <summary>
/// Singleton tracker for ratchet test results.
/// Tests record their results here, then summary is printed at the end.
/// </summary>
public sealed class RatchetTestTracker
{
  private const string BaselineFileName = "test-baseline.json";
  private static readonly Lazy<RatchetTestTracker> _instance = new(() => new RatchetTestTracker());

  public static RatchetTestTracker Instance => _instance.Value;

  private readonly string _baselinePath;
  private readonly TestBaseline _baseline;
  private readonly ConcurrentDictionary<string, bool> _currentResults = new();
  private readonly bool _updateBaseline;
  private bool _summaryPrinted = false;

  private RatchetTestTracker()
  {
    // Determine baseline path - in project root
    var testAssemblyPath = typeof(RatchetTestTracker).Assembly.Location;
    var projectRoot = Path.GetDirectoryName(testAssemblyPath)!;
    // Navigate up from bin/Debug/net10.0 to project root
    projectRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", ".."));
    _baselinePath = Path.Combine(projectRoot, BaselineFileName);

    // Check if --update-baseline flag was passed via environment variable
    _updateBaseline = Environment.GetEnvironmentVariable("UPDATE_BASELINE") == "true";

    // Load baseline
    _baseline = TestBaseline.LoadOrDefault(_baselinePath);

    // Register for process exit to ensure summary gets printed
    AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
    {
      if (!_summaryPrinted)
      {
        PrintSummaryAndSetExitCode();
      }
    };
  }

  /// <summary>
  /// Record a test result.
  /// </summary>
  public void RecordResult(string testName, bool passed)
  {
    _currentResults[testName] = passed;
  }

  /// <summary>
  /// Print summary and set exit code based on regressions.
  /// </summary>
  public void PrintSummaryAndSetExitCode()
  {
    if (_summaryPrinted)
    {
      return;
    }
    _summaryPrinted = true;

    var comparison = BaselineComparer.Compare(
      _baseline,
      _currentResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
    );

    // Print summary
    PrintSummary(comparison);

    // Update baseline if requested
    if (_updateBaseline)
    {
      var newBaseline = new TestBaseline
      {
        Timestamp = DateTime.UtcNow,
        TestResults = _currentResults
          .Select(kvp => new KeyValuePair<string, TestResult>(
            kvp.Key,
            new TestResult { Passed = kvp.Value }
          ))
          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
      };
      newBaseline.Save(_baselinePath);
      TestContext.Progress.WriteLine($"\n✓ Baseline updated: {_baselinePath}");
    }

    // Fail the test run ONLY if there are regressions
    if (comparison.HasRegressions && !_updateBaseline)
    {
      throw new Exception(
        $"Ratchet test failure: {comparison.Regressions.Count} regression(s) detected. "
          + "Run with UPDATE_BASELINE=true to accept these changes."
      );
    }
  }

  private void PrintSummary(BaselineComparison comparison)
  {
    var writer = TestContext.Progress;

    writer.WriteLine("\n" + new string('=', 80));
    writer.WriteLine("RATCHET TEST SUMMARY");
    writer.WriteLine(new string('=', 80));

    // Progressions (celebrated)
    if (comparison.Progressions.Count > 0)
    {
      writer.WriteLine($"\n✓ PROGRESSIONS ({comparison.Progressions.Count}):");
      foreach (var test in comparison.Progressions)
      {
        writer.WriteLine($"  ✓ {test}");
      }
    }

    // Regressions (highlighted)
    if (comparison.Regressions.Count > 0)
    {
      writer.WriteLine($"\n✗ REGRESSIONS ({comparison.Regressions.Count}):");
      foreach (var test in comparison.Regressions)
      {
        writer.WriteLine($"  ✗ {test}");
      }
    }

    // Stable failures (noted)
    if (comparison.StableFailures.Count > 0)
    {
      writer.WriteLine($"\n⚠ STABLE FAILURES ({comparison.StableFailures.Count}):");
      writer.WriteLine($"  (These tests were already failing in the baseline)");
      foreach (var test in comparison.StableFailures)
      {
        writer.WriteLine($"  - {test}");
      }
    }

    // New tests
    if (comparison.NewTests.Count > 0)
    {
      writer.WriteLine($"\n+ NEW TESTS ({comparison.NewTests.Count}):");
      foreach (var test in comparison.NewTests)
      {
        var passed = _currentResults[test];
        var symbol = passed ? "✓" : "✗";
        writer.WriteLine($"  {symbol} {test}");
      }
    }

    // Final verdict
    writer.WriteLine($"\n{new string('=', 80)}");
    if (comparison.HasRegressions)
    {
      writer.WriteLine($"RESULT: FAILED - {comparison.Regressions.Count} regression(s) detected");
      writer.WriteLine("Run with UPDATE_BASELINE=true to accept these changes.");
    }
    else
    {
      writer.WriteLine("RESULT: PASSED - No regressions detected");
    }
    writer.WriteLine(new string('=', 80));
  }
}
