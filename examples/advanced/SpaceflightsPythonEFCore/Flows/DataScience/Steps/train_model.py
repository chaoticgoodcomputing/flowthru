"""Model training step using scikit-learn."""
import logging
import pandas as pd
from sklearn.linear_model import LinearRegression
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["XValues", "YValues"], outputs="LinearRegressionModel")
def train_model(X_train: pd.DataFrame, y_train: pd.DataFrame) -> dict:
    """Trains the linear regression model.

    Args:
        X_train: Training data of independent features.
        y_train: Training data for price (single-column DataFrame).

    Returns:
        Dictionary with model parameters (coefficients, intercept, feature_names).
    """
    logger.info(f"[train_model] Starting with X_train {X_train.shape}, y_train {y_train.shape}")
    
    regressor = LinearRegression()
    
    # Convert single-column DataFrame to Series for sklearn
    y_train_series = y_train.squeeze()
    logger.info(f"[train_model] Fitting LinearRegression with {len(X_train)} samples")
    
    regressor.fit(X_train, y_train_series)
    logger.info(f"[train_model] Model trained successfully, coefficients shape: {regressor.coef_.shape}")
    
    # Extract model parameters to match LinearRegressionModel schema
    result = {
        "Coefficients": regressor.coef_.tolist(),
        "Intercept": float(regressor.intercept_),
        "FeatureNames": list(X_train.columns),
    }
    logger.info(f"[train_model] Returning model with {len(result['Coefficients'])} coefficients, intercept={result['Intercept']}")
    return result
