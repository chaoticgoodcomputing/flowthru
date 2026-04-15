using System.Collections.Generic;
using System.Threading.Tasks;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Flows.FashionComparison.Steps;

/// <summary>
/// Converts FashionMnistInputRow to UmapInput (float[784] features, string label).
/// </summary>
public static class ConvertFashionMnistToUmapInputStep
{
  public static Func<IEnumerable<MnistInputRow>, Task<IEnumerable<UmapInput>>> Create()
  {
    return async (inputRows) =>
    {
      var result = new List<UmapInput>();
      foreach (var row in inputRows)
      {
        var features = new float[784];
        for (int i = 0; i < 784; i++)
        {
          var prop = row.GetType().GetProperty($"Pixel{i}");
          var valueObj = prop?.GetValue(row);
          long pixelValue = valueObj is long l ? l : 0L;
          features[i] = pixelValue;
        }
        result.Add(
          new UmapInput
          {
            Id = row.Id,
            Label = row.Label.ToString(),
            Features = features,
          }
        );
      }
      return await Task.FromResult(result);
    };
  }
}
