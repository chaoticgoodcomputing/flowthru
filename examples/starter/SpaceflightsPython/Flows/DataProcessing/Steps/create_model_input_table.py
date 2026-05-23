"""Data joining step to create model input table."""
import logging
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(
    inputs=["PreprocessedShuttleSchema", "PreprocessedCompanySchema", "ReviewSchema"],
    outputs=["ModelInputTableSchema"],
    cacheable=True,
)
def create_model_input_table(
    shuttles: pd.DataFrame, companies: pd.DataFrame, reviews: pd.DataFrame
) -> pd.DataFrame:
    """Combines all data to create a model input table.

    Args:
        shuttles: Preprocessed data for shuttles.
        companies: Preprocessed data for companies.
        reviews: Raw data for reviews.
    Returns:
        Model input table.
    """
    n_shuttles = len(shuttles)
    n_companies = len(companies)
    n_reviews_in = len(reviews)

    reviews["shuttle_id"] = pd.to_numeric(reviews["shuttle_id"], errors="coerce").astype(int)
    reviews["review_scores_rating"] = pd.to_numeric(reviews["review_scores_rating"], errors="coerce")
    dropped_reviews = n_reviews_in - reviews["review_scores_rating"].notna().sum()
    if dropped_reviews > 0:
        logger.warning(
            "Dropped %d/%d reviews with unparseable rating scores",
            int(dropped_reviews), n_reviews_in,
        )

    rated_shuttles = shuttles.merge(reviews, left_on="id", right_on="shuttle_id", suffixes=("", "_review"))
    model_input_table = rated_shuttles.merge(
        companies, left_on="company_id", right_on="id", suffixes=("", "_company")
    )
    model_input_table = model_input_table.dropna()

    # Convert all integer columns explicitly to int32 (C# Int32 type)
    # After dropna(), we can safely cast without null issues
    for col in ["id", "engines", "passenger_capacity", "crew", "company_id"]:
        model_input_table[col] = model_input_table[col].astype('int32')

    # Convert id fields to strings to match ModelInputTableSchema
    model_input_table["id"] = model_input_table["id"].astype(str)
    model_input_table["company_id"] = model_input_table["company_id"].astype(str)

    # Select and rename columns to match ModelInputTableSchema exactly
    # All fields use snake_case SerializedLabel attributes
    result = pd.DataFrame({
        "shuttle_id": model_input_table["id"],
        "shuttle_type": model_input_table["shuttle_type"],
        "engine_type": model_input_table["engine_type"],
        "company_id": model_input_table["company_id"],
        "engines": model_input_table["engines"],
        "passenger_capacity": model_input_table["passenger_capacity"],
        "crew": model_input_table["crew"],
        "d_check_complete": model_input_table["d_check_complete"],
        "moon_clearance_complete": model_input_table["moon_clearance_complete"],
        "price": model_input_table["price"],
        "iata_approved": model_input_table["iata_approved"],
        "company_rating": model_input_table["company_rating"],
        "company_location": model_input_table["company_location"],
        "total_fleet_count": model_input_table["total_fleet_count"],
        "review_scores_rating": model_input_table["review_scores_rating"],
    })

    logger.info(
        "Joined %d shuttle rows × %d company rows × %d reviews → %d model-input rows",
        n_shuttles, n_companies, n_reviews_in, len(result),
    )
    return result
