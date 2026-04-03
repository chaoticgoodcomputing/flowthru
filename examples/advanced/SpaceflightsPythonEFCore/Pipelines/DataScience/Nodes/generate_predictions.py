"""Model prediction generation step."""
import logging
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["LinearRegressionModel", "XValues", "YValues"], outputs="ModelPredictions")
def generate_predictions(regressor_params: dict, X_test: pd.DataFrame, y_test: pd.DataFrame) -> pd.DataFrame:
    """Generate predictions from the trained model for visualization.
    
    Args:
        regressor_params: Dictionary with model parameters (Coefficients, Intercept, FeatureNames).
        X_test: Testing data of independent features.
        y_test: Testing data for price (single-column DataFrame).
        
    Returns:
        DataFrame with Actual and Predicted columns.
    """
    logger.info(f"[generate_predictions] Starting with X_test {X_test.shape}, y_test {y_test.shape}")
    logger.info(f"[generate_predictions] Model has {len(regressor_params['Coefficients'])} coefficients")
    
    # Convert single-column DataFrame to Series
    y_test_series = y_test.squeeze()
    
    # Reconstruct predictions from model parameters: y = X @ coef + intercept
    logger.info("[generate_predictions] Making predictions using model parameters")
    coefficients = np.array(regressor_params['Coefficients'])
    intercept = regressor_params['Intercept']
    
    # Ensure X_test columns match the feature order
    feature_names = regressor_params['FeatureNames']
    X_test_ordered = X_test[feature_names]
    
    y_pred = X_test_ordered.values @ coefficients + intercept
    
    logger.info(f"[generate_predictions] Generated {len(y_pred)} predictions")
    
    # Create DataFrame with actual and predicted values
    result = pd.DataFrame({
        'Actual': y_test_series.values,
        'Predicted': y_pred
    })
    
    logger.info(f"[generate_predictions] Returning {len(result)} prediction records")
    return result
