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


# region docs:step-python-def
@step(inputs=["CompanySchema"], outputs=["PreprocessedCompanySchema"], cacheable=True)
def preprocess_companies(companies: pd.DataFrame) -> pd.DataFrame:
    # endregion
    """Preprocesses the data for companies.

    Args:
        companies: Raw data.
    Returns:
        Preprocessed data, with `company_rating` converted to a float and
        `iata_approved` converted to boolean.
    """
    n_in = len(companies)
    companies["id"] = companies["id"].astype('int32')
    companies["iata_approved"] = _is_true(companies["iata_approved"])
    companies["company_rating"] = _parse_percentage(companies["company_rating"])
    companies["total_fleet_count"] = companies["total_fleet_count"].replace("", np.nan).astype(float)

    # Filter out rows with missing company_rating (matches C# behavior)
    companies = companies.dropna(subset=["company_rating"])

    dropped = n_in - len(companies)
    if dropped > 0:
        logger.warning(
            "Dropped %d/%d company rows with invalid rating percentages",
            dropped, n_in,
        )
    else:
        logger.info("Preprocessed %d company rows", len(companies))

    return companies
