using Flowthru.Flow;
using Flowthru.Step.Python;
using MnistDistributed.Data;

namespace MnistDistributed.Flows.Train;

/// <summary>
/// One-step flow that trains a small CNN on synthetic 28×28 grayscale
/// data via PyTorch DDP. The intent of this example is to exercise
/// <see cref="TorchrunLauncher"/> end-to-end and surface the slice-5
/// protocol-coordination gaps. See README for the catalogue of issues
/// the example reproduces.
/// </summary>
public static class TrainFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Train", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "TrainCnnDistributed",
        module: "Flows.Train.Steps.train_ddp",
        function: "train_ddp",
        input: catalog.TrainingConfig,
        output: catalog.ModelWeights,
        executor: executor
      );

      // Second distributed step sharing the same executor. Verifies
      // that the rank-aware worker stays alive across multiple
      // invokes (the slice-5 multi-shot path) — the DAG runs this
      // after TrainCnnDistributed because of the ModelWeights
      // dependency, both invokes flowing through the same N-rank
      // worker pool.
      pipeline.AddPythonStep(
        label: "VerifyModel",
        module: "Flows.Train.Steps.verify_model",
        function: "verify_model",
        input: catalog.ModelWeights,
        output: catalog.VerificationOutput,
        executor: executor
      );
    });
  }
}
