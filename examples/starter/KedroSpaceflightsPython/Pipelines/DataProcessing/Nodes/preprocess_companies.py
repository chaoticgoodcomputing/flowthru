"""Data preprocessing steps for companies data."""
import logging
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _is_true(x: pd.Series) -> pd.Series:
    """Convert 't'/'f' strings to boolean."""
    return x == "t"


def _parse_percentage(x: pd.Series) -> pd.Series:
    """Parse percentage strings to floats (e.g., '100%' -> 1.0)."""
    x = x.str.replace("%", "")
    # Convert empty strings to NaN so they can be handled as missing values
    x = x.replace("", np.nan)
    x = x.astype(float) / 100
    return x


@step(inputs=["CompanyRawSchema"], outputs=["CompanyPreprocessedSchema"])
def preprocess_companies(companies: pd.DataFrame) -> pd.DataFrame:
    """Preprocesses the data for companies.

    Args:
        companies: Raw data.
    Returns:
        Preprocessed data, with `company_rating` converted to a float and
        `iata_approved` converted to boolean.
    """
    print(f"[PYTHON] preprocess_companies STARTED with shape {companies.shape}", flush=True)
    print(f"[PYTHON] columns: {list(companies.columns)}", flush=True)
    logger.info(f"[preprocess_companies] Starting with shape {companies.shape}, columns: {list(companies.columns)}")
    
    print("[PYTHON] Converting id to int...", flush=True)
    companies["id"] = companies["id"].astype('int32')
    
    print("[PYTHON] Converting iata_approved...", flush=True)
    logger.info("[preprocess_companies] Converting iata_approved to boolean")
    companies["iata_approved"] = _is_true(companies["iata_approved"])
    
    print("[PYTHON] Parsing company_rating...", flush=True)
    logger.info("[preprocess_companies] Parsing company_rating percentages")
    companies["company_rating"] = _parse_percentage(companies["company_rating"])
    
    print("[PYTHON] Converting total_fleet_count to float...", flush=True)
    companies["total_fleet_count"] = companies["total_fleet_count"].replace("", np.nan).astype(float)
    
    # Filter out rows with missing company_rating (matches C# behavior)
    print("[PYTHON] Filtering out rows with missing company_rating...", flush=True)
    companies = companies.dropna(subset=["company_rating"])
    
    print(f"[PYTHON] preprocess_companies COMPLETED, output shape {companies.shape}", flush=True)
    print(f"[PYTHON] Final columns: {list(companies.columns)}", flush=True)
    logger.info(f"[preprocess_companies] Completed, output shape {companies.shape}")
    return companies
