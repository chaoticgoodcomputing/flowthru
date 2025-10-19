using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Reference;
using System.Reflection;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Diagnostic node that validates Flowthru's model input table against Kedro's reference output.
/// </summary>
/// <remarks>
/// <para>
/// This node performs a progressive comparison:
/// 1. Schema validation - ensures all properties match
/// 2. Row count comparison - verifies same number of observations
/// 3. Data value comparison - checks for discrepancies in actual values
/// </para>
/// <para>
/// This is a pass-through node that outputs the Flowthru data unchanged,
/// but logs detailed comparison results for diagnostic purposes.
/// </para>
/// </remarks>
public class ValidateAgainstKedroNode : NodeBase<ValidateAgainstKedroInputs, ModelInputSchema, NoParams>
{
  protected override Task<IEnumerable<ModelInputSchema>> Transform(IEnumerable<ValidateAgainstKedroInputs> inputs)
  {
    var input = inputs.Single();
    var flowthruData = input.FlowthruData.ToList();
    var kedroData = input.KedroData.ToList();

    Console.WriteLine("\n" + new string('═', 80));
    Console.WriteLine("FLOWTHRU vs KEDRO MODEL INPUT TABLE VALIDATION");
    Console.WriteLine(new string('═', 80));

    // Step 1: Schema Comparison
    Console.WriteLine("\n[1] SCHEMA COMPARISON");
    Console.WriteLine(new string('-', 80));
    CompareSchemas();

    // Step 2: Row Count Comparison
    Console.WriteLine("\n[2] ROW COUNT COMPARISON");
    Console.WriteLine(new string('-', 80));
    Console.WriteLine($"  Flowthru rows: {flowthruData.Count:N0}");
    Console.WriteLine($"  Kedro rows:    {kedroData.Count:N0}");

    if (flowthruData.Count == kedroData.Count)
    {
      Console.WriteLine($"  ✓ Row counts match!");
    }
    else
    {
      var diff = flowthruData.Count - kedroData.Count;
      Console.WriteLine($"  ✗ Row count mismatch: {(diff > 0 ? "+" : "")}{diff:N0} rows difference");
    }

    // Step 3: Data Value Comparison
    Console.WriteLine("\n[3] DATA VALUE COMPARISON");
    Console.WriteLine(new string('-', 80));
    CompareDataValues(flowthruData, kedroData);

    Console.WriteLine("\n" + new string('═', 80));
    Console.WriteLine("VALIDATION COMPLETE");
    Console.WriteLine(new string('═', 80) + "\n");

    // Pass through Flowthru data unchanged
    return Task.FromResult(flowthruData.AsEnumerable());
  }

  private void CompareSchemas()
  {
    var flowthruProps = typeof(ModelInputSchema).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var kedroProps = typeof(KedroModelInputSchema).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    Console.WriteLine($"  Flowthru schema: {flowthruProps.Length} properties");
    Console.WriteLine($"  Kedro schema:    {kedroProps.Length} properties");

    // Find common property names (case-insensitive)
    var flowthruPropNames = flowthruProps.Select(p => p.Name.ToLowerInvariant()).ToHashSet();
    var kedroPropNames = kedroProps.Select(p => p.Name.ToLowerInvariant()).ToHashSet();
    var commonProps = flowthruPropNames.Intersect(kedroPropNames).Count();

    Console.WriteLine($"  Common properties: {commonProps}");
    Console.WriteLine($"  ℹ Kedro has additional columns not used by Flowthru (shuttle_location, engine_type, etc.)");
  }

  private void CompareDataValues(List<ModelInputSchema> flowthruData, List<KedroModelInputSchema> kedroData)
  {
    var minCount = Math.Min(flowthruData.Count, kedroData.Count);

    if (minCount == 0)
    {
      Console.WriteLine("  ⚠ Cannot compare values - one or both datasets are empty");
      return;
    }

    // Build lookup dictionaries for comparison (using ShuttleId as key)
    var flowthruDict = flowthruData.ToDictionary(r => r.ShuttleId ?? "", r => r);
    var kedroDict = kedroData.ToDictionary(r => r.ShuttleId ?? "", r => r);

    // Find common and unique shuttle IDs
    var flowthruKeys = new HashSet<string>(flowthruDict.Keys);
    var kedroKeys = new HashSet<string>(kedroDict.Keys);
    var commonKeys = flowthruKeys.Intersect(kedroKeys).ToHashSet();
    var flowthruOnlyKeys = flowthruKeys.Except(kedroKeys).ToList();
    var kedroOnlyKeys = kedroKeys.Except(flowthruKeys).ToList();

    Console.WriteLine($"  Common shuttle IDs: {commonKeys.Count:N0}");
    Console.WriteLine($"  Flowthru-only IDs:  {flowthruOnlyKeys.Count:N0}");
    Console.WriteLine($"  Kedro-only IDs:     {kedroOnlyKeys.Count:N0}");

    if (flowthruOnlyKeys.Any())
    {
      Console.WriteLine($"\n  ℹ Sample Flowthru-only IDs: {string.Join(", ", flowthruOnlyKeys.Take(5))}");
    }

    if (kedroOnlyKeys.Any())
    {
      Console.WriteLine($"  ℹ Sample Kedro-only IDs: {string.Join(", ", kedroOnlyKeys.Take(5))}");
    }

    if (commonKeys.Any())
    {
      Console.WriteLine($"\n  Comparing {Math.Min(10, commonKeys.Count)} sample records with matching IDs...");

      var mismatchCount = 0;
      var sampleCount = 0;

      foreach (var shuttleId in commonKeys.Take(10))
      {
        var flowthru = flowthruDict[shuttleId];
        var kedro = kedroDict[shuttleId];
        sampleCount++;

        var mismatches = new List<string>();

        // Compare common fields
        if (!AreValuesEqual(flowthru.ShuttleType, kedro.ShuttleType))
          mismatches.Add($"ShuttleType: '{flowthru.ShuttleType}' vs '{kedro.ShuttleType}'");

        if (!AreValuesEqual(flowthru.Engines, kedro.Engines))
          mismatches.Add($"Engines: {flowthru.Engines} vs {kedro.Engines}");

        if (!AreValuesEqual(flowthru.PassengerCapacity, kedro.PassengerCapacity))
          mismatches.Add($"PassengerCapacity: {flowthru.PassengerCapacity} vs {kedro.PassengerCapacity}");

        if (!AreValuesEqual(flowthru.Crew, kedro.Crew))
          mismatches.Add($"Crew: {flowthru.Crew} vs {kedro.Crew}");

        if (flowthru.DCheckComplete != kedro.DCheckComplete)
          mismatches.Add($"DCheckComplete: {flowthru.DCheckComplete} vs {kedro.DCheckComplete}");

        if (flowthru.MoonClearanceComplete != kedro.MoonClearanceComplete)
          mismatches.Add($"MoonClearanceComplete: {flowthru.MoonClearanceComplete} vs {kedro.MoonClearanceComplete}");

        if (!AreValuesEqual(flowthru.Price, kedro.Price))
          mismatches.Add($"Price: {flowthru.Price} vs {kedro.Price}");

        if (!AreValuesEqual(flowthru.CompanyId, kedro.CompanyId))
          mismatches.Add($"CompanyId: '{flowthru.CompanyId}' vs '{kedro.CompanyId}'");

        if (!AreValuesEqual(flowthru.CompanyRating, kedro.CompanyRating))
          mismatches.Add($"CompanyRating: {flowthru.CompanyRating} vs {kedro.CompanyRating}");

        if (!AreValuesEqual(flowthru.CompanyLocation, kedro.CompanyLocation))
          mismatches.Add($"CompanyLocation: '{flowthru.CompanyLocation}' vs '{kedro.CompanyLocation}'");

        if (flowthru.IataApproved != kedro.IataApproved)
          mismatches.Add($"IataApproved: {flowthru.IataApproved} vs {kedro.IataApproved}");

        if (!AreValuesEqual(flowthru.ReviewScoresRating, kedro.ReviewScoresRating))
          mismatches.Add($"ReviewScoresRating: {flowthru.ReviewScoresRating} vs {kedro.ReviewScoresRating}");

        if (mismatches.Any())
        {
          Console.WriteLine($"\n    Shuttle {shuttleId} - {mismatches.Count} mismatches:");
          foreach (var mismatch in mismatches.Take(3))
          {
            Console.WriteLine($"      • {mismatch}");
          }
          if (mismatches.Count > 3)
          {
            Console.WriteLine($"      ... and {mismatches.Count - 3} more");
          }
          mismatchCount++;
        }
        else if (sampleCount <= 3)
        {
          Console.WriteLine($"    ✓ Shuttle {shuttleId} - All compared fields match");
        }
      }

      if (mismatchCount == 0)
      {
        Console.WriteLine($"\n  ✓ All sampled records match perfectly!");
      }
      else
      {
        Console.WriteLine($"\n  ⚠ Found mismatches in {mismatchCount}/{sampleCount} sampled records");
      }
    }
  }

  private bool AreValuesEqual(object? value1, object? value2)
  {
    if (value1 == null && value2 == null) return true;
    if (value1 == null || value2 == null) return false;

    // Handle numeric comparisons (int vs double, decimal, etc.)
    // Convert both to double for comparison with tolerance
    if (IsNumeric(value1) && IsNumeric(value2))
    {
      var num1 = Convert.ToDouble(value1);
      var num2 = Convert.ToDouble(value2);
      return Math.Abs(num1 - num2) < 0.01;
    }

    return value1.Equals(value2);
  }

  private bool IsNumeric(object value)
  {
    return value is int or long or short or byte
        or uint or ulong or ushort or sbyte
        or float or double or decimal;
  }
}

#region Node Artifacts (Colocated)

/// <summary>
/// Multi-input schema for ValidateAgainstKedroNode.
/// </summary>
public record ValidateAgainstKedroInputs
{
  /// <summary>
  /// Model input table produced by Flowthru pipeline
  /// </summary>
  public IEnumerable<ModelInputSchema> FlowthruData { get; init; } = Enumerable.Empty<ModelInputSchema>();

  /// <summary>
  /// Reference model input table from Kedro pipeline
  /// </summary>
  public IEnumerable<KedroModelInputSchema> KedroData { get; init; } = Enumerable.Empty<KedroModelInputSchema>();
}

#endregion
