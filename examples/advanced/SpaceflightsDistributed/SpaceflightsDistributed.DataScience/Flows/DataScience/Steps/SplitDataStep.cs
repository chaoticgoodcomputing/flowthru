using Flowthru.Core.Steps;
using Flowthru.FUnit;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;

namespace SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

[FlowthruStep]
public static class SplitDataStep
{
    public record ModelOptions
    {
        public double TestSize { get; init; } = 0.2;
        public int RandomState { get; init; } = 3;
    }

    public static Func<
      IEnumerable<ModelInputTableSchema>,
      (IEnumerable<TrainingData>, IEnumerable<TestData>)
    > Create(ModelOptions options)
    {
        return (input) =>
        {
            var data = input.ToList();
            var random = new Random(options.RandomState);
            var shuffled = data.OrderBy(_ => random.Next()).ToList();
            var splitIndex = (int)(shuffled.Count * (1 - options.TestSize));

            var trainData = shuffled
          .Take(splitIndex)
          .Select(row => new TrainingData
            {
                Features = new FeatureVector
                {
                    Engines = row.Engines,
                    PassengerCapacity = row.PassengerCapacity,
                    Crew = row.Crew,
                    DCheckComplete = row.DCheckComplete,
                    MoonClearanceComplete = row.MoonClearanceComplete,
                    IataApproved = row.IataApproved,
                    CompanyRating = row.CompanyRating,
                    ReviewScoresRating = row.ReviewScoresRating,
                },
                Label = row.Price,
            });

            var testData = shuffled
          .Skip(splitIndex)
          .Select(row => new TestData
            {
                Features = new FeatureVector
                {
                    Engines = row.Engines,
                    PassengerCapacity = row.PassengerCapacity,
                    Crew = row.Crew,
                    DCheckComplete = row.DCheckComplete,
                    MoonClearanceComplete = row.MoonClearanceComplete,
                    IataApproved = row.IataApproved,
                    CompanyRating = row.CompanyRating,
                    ReviewScoresRating = row.ReviewScoresRating,
                },
                Label = row.Price,
            });

            return (trainData, testData);
        };
    }

#if FUNIT_ENABLED
    /// <summary>FUnit tests for <see cref="SplitDataStep"/>.</summary>
    public class Tests : Flowthru.FUnit.FunitContext
    {
        private static ModelInputTableSchema MakeRow(string id, decimal price = 1000m) =>
          new()
          {
              ShuttleId = id,
              ShuttleType = "Type1",
              CompanyId = "c1",
              Engines = 2,
              PassengerCapacity = 100,
              Crew = 5,
              DCheckComplete = true,
              MoonClearanceComplete = false,
              Price = price,
              IataApproved = true,
              CompanyRating = 0.9m,
              ReviewScoresRating = 4.5m,
          };

        [StepTest(typeof(SplitDataStep))]
        public void Split_ProducesCorrectTrainTestRatio()
        {
            var rows = Enumerable.Range(0, 10).Select(i => MakeRow(i.ToString()));
            var options = new ModelOptions { TestSize = 0.2, RandomState = 42 };

            var (train, test) = Invoke(Create(options), rows);

            Assert.That(train.Count(), Is.EqualTo(8));
            Assert.That(test.Count(), Is.EqualTo(2));
        }

        [StepTest(typeof(SplitDataStep))]
        public void Split_IsReproducibleWithSameRandomState()
        {
            var rows = Enumerable.Range(0, 10).Select(i => MakeRow(i.ToString())).ToList();
            var options = new ModelOptions { TestSize = 0.3, RandomState = 7 };

            var (train1, _) = Invoke(Create(options), rows);
            var (train2, _) = Invoke(Create(options), rows);

            Assert.That(train1.Select(r => r.Label), Is.EqualTo(train2.Select(r => r.Label)));
        }
    }
#endif
}
