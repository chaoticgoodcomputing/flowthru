"""Data splitting step for train/test split.

Phase 9: parameters arrive as a singleton ``SplitDataOptions`` input
marshalled from C# via the JSON scalar path. ``options`` is a dict
with keys matching the C# record's property names (PascalCase).
"""
import logging
import pandas as pd
from sklearn.model_selection import train_test_split
from flowthru import step

logger = logging.getLogger(__name__)


@step(
    inputs=["ModelInputTableSchema", "SplitDataOptions"],
    outputs=["XValues", "XValues", "YValues", "YValues"],
)
def split_data(data: pd.DataFrame, options: dict) -> tuple:
    """Splits data into features and targets training and test sets.

    Args:
        data: Data containing features and target.
        options: SplitDataOptions singleton — TestSize, RandomState, Features.
    Returns:
        Tuple of (X_train, X_test, y_train, y_test) as DataFrames.
    """
    test_size = options["TestSize"]
    random_state = options["RandomState"]
    features = options["Features"]

    logger.info(f"[split_data] Starting with data shape {data.shape}, columns: {list(data.columns)}")
    logger.info(f"[split_data] Extracting features: {features}")
    X = data[features]
    y = data[["price"]]
    logger.info(f"[split_data] X shape: {X.shape}, y shape: {y.shape}")

    logger.info(f"[split_data] Splitting with test_size={test_size}, random_state={random_state}")
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=test_size, random_state=random_state
    )
    logger.info(f"[split_data] Completed: X_train {X_train.shape}, X_test {X_test.shape}, y_train {y_train.shape}, y_test {y_test.shape}")

    return X_train, X_test, y_train, y_test
