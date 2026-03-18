"""Data splitting node for train/test split."""
import logging
import pandas as pd
from sklearn.model_selection import train_test_split
from flowthru import node

logger = logging.getLogger(__name__)

# Hardcoded parameters (per user clarification)
TEST_SIZE = 0.2
RANDOM_STATE = 3
FEATURES = [
    "engines",
    "passenger_capacity",
    "crew",
    "d_check_complete",
    "moon_clearance_complete",
    "iata_approved",
    "company_rating",
    "review_scores_rating",
]


@node(
    inputs=["ModelInputTableSchema"],
    outputs=["XValues", "XValues", "YValues", "YValues"],
)
def split_data(data: pd.DataFrame) -> tuple:
    """Splits data into features and targets training and test sets.

    Args:
        data: Data containing features and target.
    Returns:
        Tuple of (X_train, X_test, y_train, y_test) as DataFrames.
    """
    logger.info(f"[split_data] Starting with data shape {data.shape}, columns: {list(data.columns)}")
    
    logger.info(f"[split_data] Extracting features: {FEATURES}")
    X = data[FEATURES]
    y = data[["price"]]  # Single-column DataFrame
    logger.info(f"[split_data] X shape: {X.shape}, y shape: {y.shape}")

    logger.info(f"[split_data] Splitting with test_size={TEST_SIZE}, random_state={RANDOM_STATE}")
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=TEST_SIZE, random_state=RANDOM_STATE
    )
    logger.info(f"[split_data] Completed: X_train {X_train.shape}, X_test {X_test.shape}, y_train {y_train.shape}, y_test {y_test.shape}")

    return X_train, X_test, y_train, y_test
