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
    cacheable=True,
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

    X = data[features]
    y = data[["price"]]
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=test_size, random_state=random_state
    )

    logger.info(
        "Split %d rows: %d train / %d test (%.0f%% test, seed=%d)",
        len(data), len(X_train), len(X_test), test_size * 100, random_state,
    )

    return X_train, X_test, y_train, y_test
