using Flowthru.Step;
using Flowthru.Step.Testing;
using IrisFUnit.Data._01_Raw.Schemas;
using IrisFUnit.Data._04_Feature.Schemas;
using IrisFUnit.Data._05_ModelInput.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IrisFUnit.Flows.DataEngineering.Steps;

/// <summary>
/// Splits the classical Iris dataset into training and test sets
/// with one-hot encoding. Each split is separated into features
/// (X) and labels (Y).
/// </summary>
[FlowthruStep]
public static class SplitAndEncodeStep
{
  /// <summary>Configuration options for <see cref="SplitAndEncodeStep"/>.</summary>
  public record Options
  {
    /// <summary>Proportion of data to use for testing (e.g., 0.2 for 20%).</summary>
    public double TestDataRatio { get; init; } = 0.2;
  }

  /// <summary>
  /// Canonical Func-returning Create — the transform receives the
  /// raw iris rows and the configuration-bound <see cref="Options"/>
  /// as a tuple input. Options come from the catalog like any other
  /// fingerprintable input (Phase 5/8 of the smart-caching RFC); a
  /// change to <c>Flowthru:Flows:DataEngineering:SplitOptions</c> in
  /// <c>appsettings.json</c> invalidates this step's cached output
  /// automatically.
  /// </summary>
  public static Func<
    (IEnumerable<IrisRawSchema>, Options),
    (
      IEnumerable<IrisFeatureSchema> Features,
      IEnumerable<FeatureVectorSchema> TrainX,
      IEnumerable<TargetLabelSchema> TrainY,
      IEnumerable<FeatureVectorSchema> TestX,
      IEnumerable<TargetLabelSchema> TestY
    )
  > Create(ILogger logger) =>
    input =>
    {
      var (rawData, options) = input;
      var testDataRatio = options.TestDataRatio;

      // One-hot encode species labels.
      var encoded = rawData
        .Select(row => new IrisFeatureSchema
        {
          SepalLength = row.SepalLength,
          SepalWidth = row.SepalWidth,
          PetalLength = row.PetalLength,
          PetalWidth = row.PetalWidth,
          Setosa = row.Species == "setosa" ? 1.0 : 0.0,
          Versicolor = row.Species == "versicolor" ? 1.0 : 0.0,
          Virginica = row.Species == "virginica" ? 1.0 : 0.0,
        })
        .ToList();

      // Shuffle for a deterministic random train/test split.
      var random = new Random(42);
      var shuffled = encoded.OrderBy(_ => random.Next()).ToList();

      var totalCount = shuffled.Count;
      var testCount = (int)(totalCount * testDataRatio);
      var testData = shuffled.Take(testCount).ToList();
      var trainData = shuffled.Skip(testCount).ToList();

      logger.LogInformation(
        "Encoded {Total} iris rows; split {Train} train / {Test} test ({Ratio:P0})",
        totalCount, trainData.Count, testData.Count, testDataRatio
      );

      var trainX = trainData.Select(row => new FeatureVectorSchema
      {
        SepalLength = row.SepalLength,
        SepalWidth = row.SepalWidth,
        PetalLength = row.PetalLength,
        PetalWidth = row.PetalWidth,
      });
      var trainY = trainData.Select(row => new TargetLabelSchema
      {
        Setosa = row.Setosa,
        Versicolor = row.Versicolor,
        Virginica = row.Virginica,
      });
      var testX = testData.Select(row => new FeatureVectorSchema
      {
        SepalLength = row.SepalLength,
        SepalWidth = row.SepalWidth,
        PetalLength = row.PetalLength,
        PetalWidth = row.PetalWidth,
      });
      var testY = testData.Select(row => new TargetLabelSchema
      {
        Setosa = row.Setosa,
        Versicolor = row.Versicolor,
        Virginica = row.Virginica,
      });

      return (encoded, trainX, trainY, testX, testY);
    };

#if FUNIT_ENABLED
  public class Tests : FUnitContext
  {
    [FUnitStepTest(typeof(SplitAndEncodeStep))]
    public void With20PercentRatio_ProducesCorrectSplitSizes()
    {
      var rawData = Samples.Generate(
        10,
        i => new IrisRawSchema
        {
          SepalLength = 5.0 + i * 0.1,
          SepalWidth = 3.0,
          PetalLength = 1.5,
          PetalWidth = 0.2,
          Species =
            i % 3 == 0 ? "setosa"
            : i % 3 == 1 ? "versicolor"
            : "virginica",
        }
      );

      var (features, trainX, trainY, testX, testY) = Invoke(
        Create(NullLogger.Instance),
        (rawData, new Options { TestDataRatio = 0.2 })
      );

      Assert.That(features.Count(), Is.EqualTo(10));
      Assert.That(trainX.Count() + testX.Count(), Is.EqualTo(10));
      Assert.That(trainX.Count(), Is.EqualTo(8));
      Assert.That(testX.Count(), Is.EqualTo(2));
      Assert.That(trainY.Count(), Is.EqualTo(trainX.Count()));
      Assert.That(testY.Count(), Is.EqualTo(testX.Count()));
    }

    [FUnitStepTest(typeof(SplitAndEncodeStep))]
    public void SetosaRow_EncodesOneHotCorrectly()
    {
      var rawData = Samples.Of(
        new IrisRawSchema
        {
          SepalLength = 5.1,
          SepalWidth = 3.5,
          PetalLength = 1.4,
          PetalWidth = 0.2,
          Species = "setosa",
        }
      );

      var (features, _, _, _, _) = Invoke(Create(NullLogger.Instance), (rawData, new Options { TestDataRatio = 0.0 }));
      var feature = features.Single();

      Assert.That(feature.Setosa, Is.EqualTo(1.0));
      Assert.That(feature.Versicolor, Is.EqualTo(0.0));
      Assert.That(feature.Virginica, Is.EqualTo(0.0));
    }
  }
#endif
}
