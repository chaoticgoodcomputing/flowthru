"""Model evaluation step."""
import logging
import numpy as np
import pandas as pd
from sklearn.metrics import max_error, mean_absolute_error, r2_score
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["LinearRegressionModel", "XValues", "YValues"], outputs="ModelMetrics")
def evaluate_model(regressor_params: dict, X_test: pd.DataFrame, y_test: pd.DataFrame) -> dict:
    """Calculates and logs the coefficient of determination.

    Args:
        regressor_params: Dictionary with model parameters (Coefficients, Intercept, FeatureNames).
        X_test: Testing data of independent features.
        y_test: Testing data for price (single-column DataFrame).
        
    Returns:
        Dictionary with metrics (r2_score, mae, max_error).
    """
    # Convert single-column DataFrame to Series
    y_test_series = y_test.squeeze()

    # Reconstruct predictions from model parameters: y = X @ coef + intercept
    coefficients = np.array(regressor_params['Coefficients'])
    intercept = regressor_params['Intercept']

    # Ensure X_test columns match the feature order
    feature_names = regressor_params['FeatureNames']
    X_test_ordered = X_test[feature_names]

    y_pred = X_test_ordered.values @ coefficients + intercept

    score = r2_score(y_test_series, y_pred)
    mae = mean_absolute_error(y_test_series, y_pred)
    me = max_error(y_test_series, y_pred)

    logger.info(
        "Evaluated on %d test rows: R²=%.3f, MAE=%.2f, MaxError=%.2f",
        len(y_test_series), score, mae, me,
    )

    return {
        "R2Score": float(score),
        "MeanAbsoluteError": float(mae),
        "MaxError": float(me),
    }
