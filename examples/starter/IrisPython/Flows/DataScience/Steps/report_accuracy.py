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
    logger.info(f"[report_accuracy] Received predictions shape: {predictions.shape if hasattr(predictions, 'shape') else 'N/A'}")
    logger.info(f"[report_accuracy] Received test_y shape: {test_y.shape if hasattr(test_y, 'shape') else 'N/A'}")
    logger.info(f"[report_accuracy] Predictions type: {type(predictions)}")
    logger.info(f"[report_accuracy] test_y type: {type(test_y)}")
    
    if predictions.empty or test_y.empty:
        logger.warning("[report_accuracy] Received empty DataFrame(s)!")
        return {
            "accuracy": 0.0,
            "correct_predictions": 0,
            "total_samples": 0
        }
    
    # Get true class index from one-hot encoded labels
    target = np.argmax(test_y.to_numpy(), axis=1)
    
    # Get predictions as numpy array
    pred_values = predictions["prediction"].to_numpy()
    
    # Calculate accuracy
    correct = np.sum(pred_values == target)
    total = target.shape[0]
    accuracy = correct / total
    
    # Log the accuracy
    logger.info(f"Model accuracy on test set: {accuracy*100:.2f}%")
    logger.info(f"Correct predictions: {correct}/{total}")
    
    # Return metrics as dictionary (will be saved as JSON)
    return {
        "accuracy": float(accuracy),
        "correct_predictions": int(correct),
        "total_samples": int(total)
    }
