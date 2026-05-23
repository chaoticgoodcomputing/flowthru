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
    logger.info(
        "Training linear regression on %d samples × %d features",
        len(X_train), X_train.shape[1],
    )

    regressor = LinearRegression()
    y_train_series = y_train.squeeze()
    regressor.fit(X_train, y_train_series)

    result = {
        "Coefficients": regressor.coef_.tolist(),
        "Intercept": float(regressor.intercept_),
        "FeatureNames": list(X_train.columns),
    }
    logger.info(
        "Training complete (intercept=%.2f, %d coefficients)",
        result["Intercept"], len(result["Coefficients"]),
    )
    return result
