using Flowthru.Core.Steps;
using Flowthru.FUnit;
using KedroIrisFUnit.Data._01_Raw.Schemas;
using KedroIrisFUnit.Data._04_Feature.Schemas;
using KedroIrisFUnit.Data._05_ModelInput.Schemas;

namespace KedroIrisFUnit.Flows.DataEngineering.Steps;

/// <summary>
/// Splits the classical Iris dataset into training and test sets with one-hot encoding.
/// Each split is separated into features (X) and labels (Y).
/// </summary>
[FlowthruStep]
public static class SplitAndEncodeStep
{
    /// <summary>
    /// Creates a data splitting function that encodes species labels and partitions the dataset.
    /// </summary>
    /// <param name="testDataRatio">Proportion of data to use for testing (e.g., 0.2 for 20%).</param>
    /// <returns>
    /// A function that transforms raw iris data into feature-encoded data and four separate
    /// training/test splits (train_x, train_y, test_x, test_y).
    /// </returns>
    public static Func<
      IEnumerable<IrisRawSchema>,
      (
        IEnumerable<IrisFeatureSchema> Features,
        IEnumerable<FeatureVectorSchema> TrainX,
        IEnumerable<TargetLabelSchema> TrainY,
        IEnumerable<FeatureVectorSchema> TestX,
        IEnumerable<TargetLabelSchema> TestY
      )
    > Create(double testDataRatio)
    {
        return (rawData) =>
        {
            // One-hot encode species labels
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

            // Shuffle data for random train/test split
            var random = new Random(42); // Fixed seed for reproducibility
            var shuffled = encoded.OrderBy(_ => random.Next()).ToList();

            // Split into train and test sets
            var totalCount = shuffled.Count;
            var testCount = (int)(totalCount * testDataRatio);
            var testData = shuffled.Take(testCount).ToList();
            var trainData = shuffled.Skip(testCount).ToList();

            // Separate features (X) from labels (Y)
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
    }

#if FUNIT_ENABLED
    /// <summary>
    /// FUnit tests for <see cref="SplitAndEncodeStep"/>.
    /// </summary>
    public class Tests : FunitContext
    {
        /// <summary>
        /// With a 20% test ratio on 10 rows, the step should place 2 rows in the test
        /// split and 8 in training. Feature count must equal the full input count, and
        /// each X split must have a paired Y split of the same length.
        /// </summary>
        [StepTest(typeof(SplitAndEncodeStep))]
        public void With20PercentRatio_ProducesCorrectSplitSizes()
        {
            // Arrange — 10 rows cycling evenly across all three species
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

            // Apply
            var (features, trainX, trainY, testX, testY) = Invoke(Create(testDataRatio: 0.2), rawData);

            // Assert
            Assert.That(features.Count(), Is.EqualTo(10));
            Assert.That(trainX.Count() + testX.Count(), Is.EqualTo(10));
            Assert.That(trainX.Count(), Is.EqualTo(8));
            Assert.That(testX.Count(), Is.EqualTo(2));
            Assert.That(trainY.Count(), Is.EqualTo(trainX.Count()));
            Assert.That(testY.Count(), Is.EqualTo(testX.Count()));
        }

        /// <summary>
        /// A setosa row should produce a one-hot encoding of [1, 0, 0] — only the
        /// <c>Setosa</c> field set to 1.0, all others to 0.0.
        /// </summary>
        [StepTest(typeof(SplitAndEncodeStep))]
        public void SetosaRow_EncodesOneHotCorrectly()
        {
            // Arrange — testDataRatio: 0.0 keeps all rows in train; features reflects full encoding
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

            // Apply
            var (features, _, _, _, _) = Invoke(Create(testDataRatio: 0.0), rawData);
            var feature = features.Single();

            // Assert
            Assert.That(feature.Setosa, Is.EqualTo(1.0));
            Assert.That(feature.Versicolor, Is.EqualTo(0.0));
            Assert.That(feature.Virginica, Is.EqualTo(0.0));
        }
    }
#endif
}
