"""Model evaluation node."""
import logging
import numpy as np
import pandas as pd
from sklearn.metrics import max_error, mean_absolute_error, r2_score
from flowthru import node

logger = logging.getLogger(__name__)


@node(inputs=["LinearRegressionModel", "XValues", "YValues"], outputs="ModelMetrics")
def evaluate_model(regressor_params: dict, X_test: pd.DataFrame, y_test: pd.DataFrame) -> dict:
    """Calculates and logs the coefficient of determination.

    Args:
        regressor_params: Dictionary with model parameters (Coefficients, Intercept, FeatureNames).
        X_test: Testing data of independent features.
        y_test: Testing data for price (single-column DataFrame).
        
    Returns:
        Dictionary with metrics (r2_score, mae, max_error).
    """
    logger.info(f"[evaluate_model] Starting with X_test {X_test.shape}, y_test {y_test.shape}")
    logger.info(f"[evaluate_model] Model has {len(regressor_params['Coefficients'])} coefficients")
    
    # Convert single-column DataFrame to Series
    y_test_series = y_test.squeeze()
    
    # Reconstruct predictions from model parameters: y = X @ coef + intercept
    logger.info("[evaluate_model] Making predictions using model parameters")
    coefficients = np.array(regressor_params['Coefficients'])
    intercept = regressor_params['Intercept']
    
    # Ensure X_test columns match the feature order
    feature_names = regressor_params['FeatureNames']
    X_test_ordered = X_test[feature_names]
    
    y_pred = X_test_ordered.values @ coefficients + intercept
    
    logger.info("[evaluate_model] Calculating metrics")
    score = r2_score(y_test_series, y_pred)
    mae = mean_absolute_error(y_test_series, y_pred)
    me = max_error(y_test_series, y_pred)
    
    logger.info(f"[evaluate_model] Model has a coefficient R^2 of {score:.3f} on test data")
    logger.info(f"[evaluate_model] MAE: {mae:.2f}, Max Error: {me:.2f}")
    
    result = {
        "R2Score": float(score),
        "MeanAbsoluteError": float(mae),
        "MaxError": float(me)
    }
    logger.info(f"[evaluate_model] Returning metrics: {result}")
    return result
