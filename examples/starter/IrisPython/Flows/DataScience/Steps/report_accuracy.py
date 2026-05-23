"""Step for reporting model accuracy metrics."""
import logging
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["PredictionSchema", "TargetLabelSchema"], outputs="AccuracyReportSchema")
def report_accuracy(predictions: pd.DataFrame, test_y: pd.DataFrame) -> dict:
    """Step for reporting the accuracy of the predictions.

    Args:
        predictions: Predicted class indices (DataFrame with 'prediction' column).
        test_y: True target labels (DataFrame with one-hot encoded species).

    Returns:
        Dictionary with accuracy metrics (to be saved as JSON).
    """
    if predictions.empty or test_y.empty:
        logger.warning("Received empty DataFrame(s); accuracy report defaults to zero")
        return {
            "accuracy": 0.0,
            "correct_predictions": 0,
            "total_samples": 0,
        }

    # Get true class index from one-hot encoded labels
    target = np.argmax(test_y.to_numpy(), axis=1)
    pred_values = predictions["prediction"].to_numpy()

    correct = int(np.sum(pred_values == target))
    total = int(target.shape[0])
    accuracy = correct / total

    logger.info("Test-set accuracy: %.2f%% (%d/%d)", accuracy * 100, correct, total)

    return {
        "accuracy": float(accuracy),
        "correct_predictions": correct,
        "total_samples": total,
    }
