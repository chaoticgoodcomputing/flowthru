"""
Test fixtures for Phase 3 tabular (DataFrame) Python node integration.

This module provides test functions that receive and return pandas DataFrames,
demonstrating Arrow marshalling for tabular data.
"""

from flowthru import node


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def passthrough(df):
    """
    Identity function for round-trip testing.

    Args:
        df: Input DataFrame

    Returns:
        The same DataFrame unchanged
    """
    return df


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def transform_simple(df):
    """
    Transform function that adds a computed column and filters rows.

    Args:
        df: Input DataFrame with 'value' column

    Returns:
        DataFrame with additional 'computed' column and filtered rows
    """
    return df.assign(computed=lambda d: d["value"] * 2).query("value > 0")


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def filter_rows(df):
    """
    Filter DataFrame to only rows where value > 50.

    Args:
        df: Input DataFrame with 'value' column

    Returns:
        Filtered DataFrame
    """
    return df[df["value"] > 50].reset_index(drop=True)


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def rename_columns(df):
    """
    Rename columns in DataFrame (for schema validation testing).

    Args:
        df: Input DataFrame with 'name' column

    Returns:
        DataFrame with 'name' renamed to 'label'
    """
    return df.rename(columns={"name": "label"})


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def add_computed_column(df):
    """
    Add a computed column based on existing data.

    Args:
        df: Input DataFrame with 'id' and 'value' columns

    Returns:
        DataFrame with additional 'double_value' column
    """
    return df.assign(double_value=lambda d: d["value"] * 2)


@node(inputs=["SimpleRowSchema"], outputs=["SimpleRowSchema"])
def aggregate_data(df):
    """
    Aggregate DataFrame by computing summary statistics.

    Args:
        df: Input DataFrame with 'value' column

    Returns:
        Single-row DataFrame with mean and count
    """
    import pandas as pd

    return pd.DataFrame(
        [{"mean_value": df["value"].mean(), "row_count": len(df)}]
    )
