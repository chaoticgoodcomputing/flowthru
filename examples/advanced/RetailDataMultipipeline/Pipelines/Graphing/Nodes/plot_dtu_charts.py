"""Daily DTU chart generation steps.

Each step reads the consolidated all-countries daily DTU dataset and produces a
PNG line chart (one trace per country) for a single metric.
"""

import io
import logging

import pandas as pd
import plotly.express as px
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["WeeklyDtuSchema"], outputs="DollarsChart")
def plot_dollars_chart(daily_dtu: pd.DataFrame) -> bytes:
    """Line chart of weekly GBP revenue per country.

    Args:
        daily_dtu: Consolidated weekly DTU dataset with columns Country, WeekStartDate, TotalGbp, etc.

    Returns:
        PNG image as bytes.
    """
    logger.info(f"[plot_dollars_chart] {len(daily_dtu)} rows across {daily_dtu['Country'].nunique()} countries")

    df = daily_dtu.copy()
    df["WeekStartDate"] = pd.to_datetime(df["WeekStartDate"])
    df["TotalGbp"] = df["TotalGbp"].astype(float)
    df = df.sort_values("WeekStartDate")

    fig = px.line(
        df,
        x="WeekStartDate",
        y="TotalGbp",
        color="Country",
        title="Weekly Revenue by Country (GBP)",
        labels={"WeekStartDate": "Week", "TotalGbp": "Total Revenue (GBP)", "Country": "Country"},
    )
    fig.update_layout(legend_title_text="Country")

    return pio.to_image(fig, format="png")


@step(inputs=["WeeklyDtuSchema"], outputs="TransactionsChart")
def plot_transactions_chart(daily_dtu: pd.DataFrame) -> bytes:
    """Line chart of weekly transaction count per country.

    Args:
        daily_dtu: Consolidated weekly DTU dataset.

    Returns:
        PNG image as bytes.
    """
    logger.info(f"[plot_transactions_chart] {len(daily_dtu)} rows across {daily_dtu['Country'].nunique()} countries")

    df = daily_dtu.copy()
    df["WeekStartDate"] = pd.to_datetime(df["WeekStartDate"])
    df = df.sort_values("WeekStartDate")

    fig = px.line(
        df,
        x="WeekStartDate",
        y="TransactionCount",
        color="Country",
        title="Weekly Transactions by Country",
        labels={"WeekStartDate": "Week", "TransactionCount": "Transaction Count", "Country": "Country"},
    )
    fig.update_layout(legend_title_text="Country")

    return pio.to_image(fig, format="png")


@step(inputs=["WeeklyDtuSchema"], outputs="UsersChart")
def plot_users_chart(daily_dtu: pd.DataFrame) -> bytes:
    """Line chart of weekly unique customer count per country.

    Args:
        daily_dtu: Consolidated weekly DTU dataset.

    Returns:
        PNG image as bytes.
    """
    logger.info(f"[plot_users_chart] {len(daily_dtu)} rows across {daily_dtu['Country'].nunique()} countries")

    df = daily_dtu.copy()
    df["WeekStartDate"] = pd.to_datetime(df["WeekStartDate"])
    df = df.sort_values("WeekStartDate")

    fig = px.line(
        df,
        x="WeekStartDate",
        y="UniqueCustomers",
        color="Country",
        title="Weekly Unique Customers by Country",
        labels={"WeekStartDate": "Week", "UniqueCustomers": "Unique Customers", "Country": "Country"},
    )
    fig.update_layout(legend_title_text="Country")

    return pio.to_image(fig, format="png")
