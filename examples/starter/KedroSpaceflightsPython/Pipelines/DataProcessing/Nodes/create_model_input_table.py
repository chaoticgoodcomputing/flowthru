"""Data joining node to create model input table."""
import logging
import pandas as pd
from flowthru import node

logger = logging.getLogger(__name__)


@node(
    inputs=["ShuttlePreprocessedSchema", "CompanyPreprocessedSchema", "ReviewSchema"],
    outputs=["ModelInputTableSchema"],
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
    logger.info(f"[create_model_input_table] Starting with shuttles {shuttles.shape}, companies {companies.shape}, reviews {reviews.shape}")
    
    logger.info("[create_model_input_table] Converting review columns to numeric")
    reviews["shuttle_id"] = pd.to_numeric(reviews["shuttle_id"], errors="coerce").astype(int)
    reviews["review_scores_rating"] = pd.to_numeric(reviews["review_scores_rating"], errors="coerce")
    
    logger.info("[create_model_input_table] Merging shuttles with reviews")
    rated_shuttles = shuttles.merge(reviews, left_on="id", right_on="shuttle_id", suffixes=("", "_review"))
    logger.info(f"[create_model_input_table] After first merge: {rated_shuttles.shape}")
    
    logger.info("[create_model_input_table] Merging with companies")
    model_input_table = rated_shuttles.merge(
        companies, left_on="company_id", right_on="id", suffixes=("", "_company")
    )
    logger.info(f"[create_model_input_table] After second merge: {model_input_table.shape}")
    
    logger.info("[create_model_input_table] Dropping NaN values")
    model_input_table = model_input_table.dropna()
    
    logger.info(f"[create_model_input_table] Available columns after dropna: {list(model_input_table.columns)}")
    logger.info(f"[create_model_input_table] Dtypes after dropna: {dict(model_input_table.dtypes)}")
    
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
    
    logger.info(f"[create_model_input_table] Completed, output shape {result.shape}")
    logger.info(f"[create_model_input_table] Final dtypes: {dict(result.dtypes)}")
    return result
