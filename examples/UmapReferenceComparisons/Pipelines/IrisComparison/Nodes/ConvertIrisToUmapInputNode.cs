using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Converts Iris-specific input schema to universal UmapInput format.
/// </summary>
public static class ConvertIrisToUmapInputNode
{
  public static Func<IEnumerable<IrisInputRow>, Task<IEnumerable<UmapInput>>> Create()
  {
    return async (input) =>
    {
      var result = input.Select(row => new UmapInput
      {
        Id = row.Id,
        Label = row.Label.ToString(),
        Features = new float[] { row.SepalLength, row.SepalWidth, row.PetalLength, row.PetalWidth },
      });

      return await Task.FromResult(result);
    };
  }
}
