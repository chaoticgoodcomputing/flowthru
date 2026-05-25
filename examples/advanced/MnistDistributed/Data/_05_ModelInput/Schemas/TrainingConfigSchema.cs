using Flowthru.Data.Schema;

namespace MnistDistributed.Data._05_ModelInput.Schemas;

/// <summary>
/// Hyperparameters for the distributed MNIST-shaped CNN training step.
/// Passed as a JSON singleton via the Flowthru protocol — Python
/// receives the record as a dict with PascalCase keys matching the
/// property names (no <c>[SerializedLabel]</c> rewriting on the
/// singleton path; that convention belongs to the tabular / Arrow path).
/// </summary>
[FlowthruSchema]
public partial record TrainingConfigSchema
{
  /// <summary>
  /// Synthetic samples to generate per epoch — small enough that CPU
  /// DDP training completes in a few seconds while still exercising
  /// real gradient synchronization.
  /// </summary>
  public int NumSamples { get; init; }

  /// <summary>Mini-batch size.</summary>
  public int BatchSize { get; init; }

  /// <summary>Number of training epochs.</summary>
  public int Epochs { get; init; }

  /// <summary>SGD learning rate.</summary>
  public double LearningRate { get; init; }
}
