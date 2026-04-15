using Flowthru.Core.Steps;
using Flowthru.FUnit;
using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create()
  {
    return (input) =>
      input.Select(raw => Parse(raw)).Where(item => item != null).Cast<PreprocessedShuttleSchema>();
  }

  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    bool dCheckComplete = raw.DCheckComplete.Trim().ToLowerInvariant() == "t";
    bool moonClearanceComplete = raw.MoonClearanceComplete.Trim().ToLowerInvariant() == "t";

    if (!int.TryParse(raw.Engines, out var engines))
    {
      return null;
    }

    if (!int.TryParse(raw.PassengerCapacity, out var passengerCapacity))
    {
      return null;
    }

    if (!int.TryParse(raw.Crew, out var crew))
    {
      return null;
    }

    if (!TryParseMoney(raw.Price, out var price))
    {
      return null;
    }

    return new PreprocessedShuttleSchema
    {
      Id = raw.Id,
      ShuttleType = raw.ShuttleType,
      CompanyId = raw.CompanyId,
      Engines = engines,
      PassengerCapacity = passengerCapacity,
      Crew = crew,
      Price = price,
      DCheckComplete = dCheckComplete,
      MoonClearanceComplete = moonClearanceComplete,
    };
  }

  private static bool TryParseMoney(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return decimal.TryParse(cleaned, out result);
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="PreprocessShuttlesStep"/>.</summary>
  public class Tests : FunitContext
  {
    private static readonly ShuttleSchema ValidRaw =
      new()
      {
        Id = "s1",
        ShuttleType = "TypeA",
        CompanyId = "c1",
        Engines = "2",
        PassengerCapacity = "100",
        Crew = "5",
        Price = "$1,200.50",
        DCheckComplete = "t",
        MoonClearanceComplete = "f",
      };

    [StepTest(typeof(PreprocessShuttlesStep))]
    public void ValidRecord_ParsesAllFieldsCorrectly()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw)).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].Engines, Is.EqualTo(2));
      Assert.That(result[0].PassengerCapacity, Is.EqualTo(100));
      Assert.That(result[0].Crew, Is.EqualTo(5));
      Assert.That(result[0].Price, Is.EqualTo(1200.50m));
      Assert.That(result[0].DCheckComplete, Is.True);
      Assert.That(result[0].MoonClearanceComplete, Is.False);
    }

    [StepTest(typeof(PreprocessShuttlesStep))]
    public void NonNumericEngines_RecordIsFiltered()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw with { Engines = "not-a-number" }))
        .ToList();

      Assert.That(result, Is.Empty);
    }

    [StepTest(typeof(PreprocessShuttlesStep))]
    public void InvalidPrice_RecordIsFiltered()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw with { Price = "free" })).ToList();

      Assert.That(result, Is.Empty);
    }

    [StepTest(typeof(PreprocessShuttlesStep))]
    public void PriceWithDollarSignAndComma_ParsesCorrectly()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw with { Price = "$10,000.00" })).ToList();

      Assert.That(result[0].Price, Is.EqualTo(10000.00m));
    }
  }
#endif
}
