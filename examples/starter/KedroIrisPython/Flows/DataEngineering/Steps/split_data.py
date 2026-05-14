"""Data engineering step for splitting iris data into train/test sets.

Phase 9: ``options`` arrives as a singleton ``SplitDataOptions`` input,
marshalled from C# via the JSON scalar path.
"""
import pandas as pd
from flowthru import step


@step(
    inputs=["IrisRawSchema", "SplitDataOptions"],
    outputs=["FeatureVectorSchema", "TargetLabelSchema", "FeatureVectorSchema", "TargetLabelSchema"]
)
def split_data(data: pd.DataFrame, options: dict) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """Step for splitting the classical Iris data set into training and test
    sets, each split into features and labels.

    Args:
        data: Raw iris DataFrame with measurements and species labels.
        options: SplitDataOptions singleton — TestDataRatio, RandomState.

    Returns:
        Tuple of (train_x, train_y, test_x, test_y) DataFrames.
    """
    test_data_ratio = options["TestDataRatio"]
    random_state = options["RandomState"]

    # Rename species column to 'target' for processing
    data = data.copy()
    data.columns = [
        "sepal_length",
        "sepal_width",
        "petal_length",
        "petal_width",
        "target",
    ]

    # Get sorted class names for consistent one-hot encoding order
    classes = sorted(data["target"].unique())

    # One-hot encoding for the target variable
    data = pd.get_dummies(data, columns=["target"], prefix="Iris", prefix_sep="-")

    # Shuffle all the data
    data = data.sample(frac=1, random_state=random_state).reset_index(drop=True)

    # Split to training and testing data
    n = data.shape[0]
    n_test = int(n * test_data_ratio)
    training_data = data.iloc[n_test:, :].reset_index(drop=True)
    test_data = data.iloc[:n_test, :].reset_index(drop=True)

    # Split the data to features and labels
    train_data_x = training_data.loc[:, "sepal_length":"petal_width"]
    train_data_y = training_data[[f"Iris-{cls}" for cls in classes]]
    test_data_x = test_data.loc[:, "sepal_length":"petal_width"]
    test_data_y = test_data[[f"Iris-{cls}" for cls in classes]]

    return train_data_x, train_data_y, test_data_x, test_data_y
