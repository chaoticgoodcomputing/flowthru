"""Confusion matrix visualization node."""
import io
import logging
import matplotlib
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import seaborn as sn
from flowthru import node

logger = logging.getLogger(__name__)


@node(inputs=["ModelPredictions"], outputs="ConfusionMatrix")
def create_confusion_matrix(predictions: pd.DataFrame) -> bytes:
    """Create a confusion matrix from regression predictions binned into categories.
    
    Args:
        predictions: DataFrame with 'Actual' and 'Predicted' columns.
        
    Returns:
        PNG image as bytes.
    """
    logger.info(f"[create_confusion_matrix] Starting with {len(predictions)} prediction records")
    
    # Use non-interactive backend for server/headless environments
    matplotlib.use('Agg')
    
    # Define price bins (Low/Medium/High based on quartiles)
    logger.info("[create_confusion_matrix] Computing price quantiles for binning")
    all_prices = pd.concat([predictions['Actual'], predictions['Predicted']])
    q25, q75 = all_prices.quantile([0.25, 0.75])
    
    logger.info(f"[create_confusion_matrix] Bin thresholds: Low<{q25:.2f}, {q25:.2f}<=Medium<{q75:.2f}, High>={q75:.2f}")
    
    def bin_prices(prices):
        """Bin continuous prices into Low/Medium/High categories."""
        return pd.cut(
            prices,
            bins=[-np.inf, q25, q75, np.inf],
            labels=['Low', 'Medium', 'High']
        )
    
    # Bin actual and predicted values
    actuals_binned = bin_prices(predictions['Actual'])
    predicted_binned = bin_prices(predictions['Predicted'])
    
    logger.info(f"[create_confusion_matrix] Binned {len(actuals_binned)} predictions")
    logger.info(f"[create_confusion_matrix] Actual distribution: {actuals_binned.value_counts().to_dict()}")
    logger.info(f"[create_confusion_matrix] Predicted distribution: {predicted_binned.value_counts().to_dict()}")
    
    # Create confusion matrix
    logger.info("[create_confusion_matrix] Creating confusion matrix crosstab")
    confusion_matrix = pd.crosstab(
        actuals_binned,
        predicted_binned,
        rownames=['Actual'],
        colnames=['Predicted'],
        dropna=False
    )
    
    # Create heatmap
    logger.info("[create_confusion_matrix] Generating heatmap")
    fig, ax = plt.subplots(figsize=(10, 8))
    sn.heatmap(confusion_matrix, annot=True, fmt='d', cmap='Blues', ax=ax, cbar_kws={'label': 'Count'})
    ax.set_title('Confusion Matrix: Price Predictions (Binned)', fontsize=14, fontweight='bold')
    ax.set_xlabel('Predicted Price Category', fontsize=12)
    ax.set_ylabel('Actual Price Category', fontsize=12)
    plt.tight_layout()
    
    # Convert figure to PNG bytes
    logger.info("[create_confusion_matrix] Converting figure to PNG bytes")
    buf = io.BytesIO()
    fig.savefig(buf, format='png', dpi=150, bbox_inches='tight')
    buf.seek(0)
    plt.close(fig)
    
    # Return raw PNG bytes
    png_bytes = buf.read()
    logger.info(f"[create_confusion_matrix] Created PNG image ({len(png_bytes)} bytes)")
    
    return png_bytes
