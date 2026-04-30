using Flowthru.Core.Steps;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Flows.DigitsComparison.Steps;

/// <summary>
/// Converts Digits-specific input schema to universal UmapInput format.
/// </summary>
[FlowthruStep]
public static class ConvertDigitsToUmapInputStep
{
  public static Func<IEnumerable<DigitsInputRow>, Task<IEnumerable<UmapInput>>> Create()
  {
    return async (input) =>
    {
      var result = input.Select(row => new UmapInput
      {
        Id = row.Id,
        Label = row.Label.ToString(),
        Features = new float[]
        {
          (float)row.Pixel0,
          (float)row.Pixel1,
          (float)row.Pixel2,
          (float)row.Pixel3,
          (float)row.Pixel4,
          (float)row.Pixel5,
          (float)row.Pixel6,
          (float)row.Pixel7,
          (float)row.Pixel8,
          (float)row.Pixel9,
          (float)row.Pixel10,
          (float)row.Pixel11,
          (float)row.Pixel12,
          (float)row.Pixel13,
          (float)row.Pixel14,
          (float)row.Pixel15,
          (float)row.Pixel16,
          (float)row.Pixel17,
          (float)row.Pixel18,
          (float)row.Pixel19,
          (float)row.Pixel20,
          (float)row.Pixel21,
          (float)row.Pixel22,
          (float)row.Pixel23,
          (float)row.Pixel24,
          (float)row.Pixel25,
          (float)row.Pixel26,
          (float)row.Pixel27,
          (float)row.Pixel28,
          (float)row.Pixel29,
          (float)row.Pixel30,
          (float)row.Pixel31,
          (float)row.Pixel32,
          (float)row.Pixel33,
          (float)row.Pixel34,
          (float)row.Pixel35,
          (float)row.Pixel36,
          (float)row.Pixel37,
          (float)row.Pixel38,
          (float)row.Pixel39,
          (float)row.Pixel40,
          (float)row.Pixel41,
          (float)row.Pixel42,
          (float)row.Pixel43,
          (float)row.Pixel44,
          (float)row.Pixel45,
          (float)row.Pixel46,
          (float)row.Pixel47,
          (float)row.Pixel48,
          (float)row.Pixel49,
          (float)row.Pixel50,
          (float)row.Pixel51,
          (float)row.Pixel52,
          (float)row.Pixel53,
          (float)row.Pixel54,
          (float)row.Pixel55,
          (float)row.Pixel56,
          (float)row.Pixel57,
          (float)row.Pixel58,
          (float)row.Pixel59,
          (float)row.Pixel60,
          (float)row.Pixel61,
          (float)row.Pixel62,
          (float)row.Pixel63,
        },
      });

      return await Task.FromResult(result);
    };
  }
}
