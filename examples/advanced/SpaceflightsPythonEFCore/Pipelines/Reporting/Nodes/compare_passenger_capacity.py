"""Passenger capacity comparison visualization steps."""
import logging
import pandas as pd
import plotly.express as px
import plotly.graph_objs as go
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["PreprocessedShuttleSchema"], outputs="CapacityPlotExpress")
def compare_passenger_capacity_exp(preprocessed_shuttles: pd.DataFrame) -> str:
    """Create passenger capacity comparison using plotly.express.
    
    Args:
        preprocessed_shuttles: Preprocessed shuttle data.
        
    Returns:
        JSON string containing plotly figure specification.
    """
    logger.info(f"[compare_passenger_capacity_exp] Starting with {preprocessed_shuttles.shape} shuttles")
    
    # Group by shuttle type and calculate mean passenger capacity
    data_frame = (
        preprocessed_shuttles.groupby(["shuttle_type"])
        .mean(numeric_only=True)
        .reset_index()
    )
    
    logger.info(f"[compare_passenger_capacity_exp] Grouped into {len(data_frame)} shuttle types")
    
    # Create bar chart using plotly.express
    fig = px.bar(
        data_frame,
        x="shuttle_type",
        y="passenger_capacity",
        title="Shuttle Passenger Capacity by Type (Plotly Express)",
        labels={
            "shuttle_type": "Shuttle Type",
            "passenger_capacity": "Average Passenger Capacity"
        }
    )
    
    logger.info("[compare_passenger_capacity_exp] Created plotly.express figure")
    
    # Return figure as JSON string using plotly's serializer
    return pio.to_json(fig, pretty=True)


@step(inputs=["PreprocessedShuttleSchema"], outputs="CapacityPlotGraphObj")
def compare_passenger_capacity_go(preprocessed_shuttles: pd.DataFrame) -> str:
    """Create passenger capacity comparison using plotly.graph_objects.
    
    Args:
        preprocessed_shuttles: Preprocessed shuttle data.
        
    Returns:
        JSON string containing plotly figure specification.
    """
    logger.info(f"[compare_passenger_capacity_go] Starting with {preprocessed_shuttles.shape} shuttles")
    
    # Group by shuttle type and calculate mean passenger capacity
    data_frame = (
        preprocessed_shuttles.groupby(["shuttle_type"])
        .mean(numeric_only=True)
        .reset_index()
    )
    
    logger.info(f"[compare_passenger_capacity_go] Grouped into {len(data_frame)} shuttle types")
    
    # Create bar chart using plotly.graph_objects
    fig = go.Figure(
        [
            go.Bar(
                x=data_frame["shuttle_type"],
                y=data_frame["passenger_capacity"],
            )
        ]
    )
    
    fig.update_layout(
        title="Shuttle Passenger Capacity by Type (Graph Objects)",
        xaxis_title="Shuttle Type",
        yaxis_title="Average Passenger Capacity"
    )
    
    logger.info("[compare_passenger_capacity_go] Created plotly.graph_objects figure")
    
    # Return figure as JSON string using plotly's serializer
    return pio.to_json(fig, pretty=True)
