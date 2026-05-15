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
    logger.info(f"[preprocess_shuttles] Starting with shape {shuttles.shape}, columns: {list(shuttles.columns)}")
    
    # Convert id fields to int32 (matching C# Int32), handling empty strings
    logger.info("[preprocess_shuttles] Converting id fields to int32")
    shuttles["id"] = shuttles["id"].replace("", np.nan).astype('Int32')
    shuttles["company_id"] = shuttles["company_id"].replace("", np.nan).astype('Int32')
    
    # Keep string fields as-is
    # shuttle_type and engine_type are already strings, no conversion needed
    
    # Convert numeric fields to int32, handling empty strings
    logger.info("[preprocess_shuttles] Converting numeric fields to int32")
    shuttles["engines"] = shuttles["engines"].replace("", np.nan).astype('Int32')
    shuttles["passenger_capacity"] = shuttles["passenger_capacity"].replace("", np.nan).astype('Int32')
    shuttles["crew"] = shuttles["crew"].replace("", np.nan).astype('Int32')
    
    # Convert boolean fields
    logger.info("[preprocess_shuttles] Converting boolean fields")
    shuttles["d_check_complete"] = _is_true(shuttles["d_check_complete"])
    shuttles["moon_clearance_complete"] = _is_true(shuttles["moon_clearance_complete"])
    
    # Parse price strings (may contain empty strings)
    logger.info("[preprocess_shuttles] Parsing price strings")
    shuttles["price"] = shuttles["price"].replace("", np.nan).str.replace("$", "").str.replace(",", "").astype(float)
    
    # Filter out rows with missing required fields (matching C# behavior)
    logger.info("[preprocess_shuttles] Filtering out rows with missing required fields")
    shuttles = shuttles.dropna(subset=["id", "company_id", "engines", "passenger_capacity", "crew", "price"]).copy()
    
    logger.info(f"[preprocess_shuttles] Columns after filtering: {list(shuttles.columns)}")
    
    # Convert nullable Int32 to int32 after filtering
    shuttles["id"] = shuttles["id"].astype('int32')
    shuttles["company_id"] = shuttles["company_id"].astype('int32')
    shuttles["engines"] = shuttles["engines"].astype('int32')
    shuttles["passenger_capacity"] = shuttles["passenger_capacity"].astype('int32')
    shuttles["crew"] = shuttles["crew"].astype('int32')
    
    logger.info(f"[preprocess_shuttles] Completed, output shape {shuttles.shape}")
    return shuttles
