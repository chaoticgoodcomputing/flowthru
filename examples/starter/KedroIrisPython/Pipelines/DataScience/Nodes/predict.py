"""Node for generating predictions using a trained model."""
import logging
import pickle
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _sigmoid(z):
    """A helper sigmoid function used for prediction."""
    return 1 / (1 + np.exp(-z))


@step(inputs=["bytes", "FeatureVectorSchema"], outputs=["PredictionSchema"])
def predict(model_bytes: bytes, test_x: pd.DataFrame) -> pd.DataFrame:
    """Node for making predictions given a pre-trained model and a test set.
    
    Args:
        model_bytes: Pickled model weights (bytes).
        test_x: Test feature vectors (DataFrame with sepal/petal measurements).
        
    Returns:
        DataFrame with predicted class indices.
    """
    # Unpickle the model
    model = pickle.loads(model_bytes)
    X = test_x.to_numpy()
    
    logger.info(f"Making predictions on {X.shape[0]} samples")
    logger.info(f"Model shape: {model.shape}")
    
    # Add bias to the features
    bias = np.ones((X.shape[0], 1))
    X = np.concatenate((bias, X), axis=1)
    
    # Predict "probabilities" for each class
    result = _sigmoid(np.dot(X, model))
    
    # Return the index of the class with max probability for all samples
    predictions = np.argmax(result, axis=1)
    
    logger.info(f"Generated {len(predictions)} predictions")
    
    # Return as DataFrame with 'prediction' column
    return pd.DataFrame({"prediction": predictions})
