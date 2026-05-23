"""Data preprocessing steps for shuttle data."""
import logging
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _is_true(x: pd.Series) -> pd.Series:
    """Convert 't'/'f' strings to boolean."""
    return x == "t"


def _parse_money(x: pd.Series) -> pd.Series:
    """Parse money strings to floats (e.g., '$1,234.56' -> 1234.56)."""
    x = x.str.replace("$", "").str.replace(",", "")
    x = x.astype(float)
    return x


@step(inputs=["ShuttleSchema"], outputs=["PreprocessedShuttleSchema"], cacheable=True)
def preprocess_shuttles(shuttles: pd.DataFrame) -> pd.DataFrame:
    """Preprocesses the data for shuttles.

    Args:
        shuttles: Raw data.
    Returns:
        Preprocessed data, with `price` converted to a float and `d_check_complete`,
        `moon_clearance_complete` converted to boolean.
    """
    n_in = len(shuttles)

    # Convert id fields to int32 (matching C# Int32), handling empty strings
    shuttles["id"] = shuttles["id"].replace("", np.nan).astype('Int32')
    shuttles["company_id"] = shuttles["company_id"].replace("", np.nan).astype('Int32')

    # Convert numeric fields to int32, handling empty strings
    shuttles["engines"] = shuttles["engines"].replace("", np.nan).astype('Int32')
    shuttles["passenger_capacity"] = shuttles["passenger_capacity"].replace("", np.nan).astype('Int32')
    shuttles["crew"] = shuttles["crew"].replace("", np.nan).astype('Int32')

    # Convert boolean fields
    shuttles["d_check_complete"] = _is_true(shuttles["d_check_complete"])
    shuttles["moon_clearance_complete"] = _is_true(shuttles["moon_clearance_complete"])

    # Parse price strings (may contain empty strings)
    shuttles["price"] = shuttles["price"].replace("", np.nan).str.replace("$", "").str.replace(",", "").astype(float)

    # Filter out rows with missing required fields (matching C# behavior)
    shuttles = shuttles.dropna(subset=["id", "company_id", "engines", "passenger_capacity", "crew", "price"]).copy()

    # Convert nullable Int32 to int32 after filtering
    for col in ("id", "company_id", "engines", "passenger_capacity", "crew"):
        shuttles[col] = shuttles[col].astype('int32')

    dropped = n_in - len(shuttles)
    if dropped > 0:
        logger.warning(
            "Dropped %d/%d shuttle rows with invalid numeric/currency fields",
            dropped, n_in,
        )
    else:
        logger.info("Preprocessed %d shuttle rows", len(shuttles))

    return shuttles
