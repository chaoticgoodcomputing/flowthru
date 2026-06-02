namespace Flowthru.Validation.Runtime.Sheets;

/// <summary>
/// Resolves a <see cref="SheetsSpreadsheetDependency"/> to its
/// <see cref="ServiceProfile"/> — the read/write capacities the Sheets
/// adapter declared for the spreadsheet (ADR-0019). Registered by
/// <c>AddGoogleSheets()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c>; it recognises only Sheets
/// spreadsheet dependencies and stays silent on everything else.
/// </summary>
/// <remarks>
/// The capacities ride on the dependency, so this contributor is a pure
/// translation: a spreadsheet resolves to write capacity 1 (concurrent
/// writers serialize, avoiding races and quota spikes) and read capacity
/// ∞ (readers parallelize). <see cref="ServiceProfile.AffectsOutputs"/>
/// is irrelevant — a spreadsheet dependency only reaches the scheduler
/// through an item, never as a step's own cache-affecting dependency.
/// </remarks>
internal sealed class SheetsSpreadsheetProfileContributor : IServiceProfileContributor
{
  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.External { Cause: SheetsSpreadsheetDependency sheet }
      ? new ServiceProfile { Capacity = sheet.WriteCapacity, ReadCapacity = sheet.ReadCapacity }
      : null;
}
