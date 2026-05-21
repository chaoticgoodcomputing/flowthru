"""Step for training a multi-class logistic regression model."""
import logging
import pickle
import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _sigmoid(z):
    """A helper sigmoid function used by the training."""
    return 1 / (1 + np.exp(-z))


@step(inputs=["FeatureVectorSchema", "TargetLabelSchema"], outputs=["bytes"])
def train_model(train_x: pd.DataFrame, train_y: pd.DataFrame) -> bytes:
    """Step for training a simple multi-class logistic regression model.
    
    Args:
        train_x: Training feature vectors (DataFrame with sepal/petal measurements).
        train_y: Training target labels (DataFrame with one-hot encoded species).
        
    Returns:
        Pickled model weights as bytes.
    """
    # Hard-coded hyperparameters
    num_iter = 10000
    lr = 0.01
    
    X = train_x.to_numpy()
    Y = train_y.to_numpy()
    
    logger.info(f"Training model with {num_iter} iterations, learning rate {lr}")
    logger.info(f"Training data shape: X={X.shape}, Y={Y.shape}")
    
    # Add bias to the features
    bias = np.ones((X.shape[0], 1))
    X = np.concatenate((bias, X), axis=1)
    
    weights = []
    # Train one model for each class in Y
    for k in range(Y.shape[1]):
        # Initialise weights
        theta = np.zeros(X.shape[1])
        y = Y[:, k]
        for i in range(num_iter):
            z = np.dot(X, theta)
            h = _sigmoid(z)
            gradient = np.dot(X.T, (h - y)) / y.size
            theta -= lr * gradient
            
            # Log progress every 2000 iterations
            if (i + 1) % 2000 == 0:
                logger.info(f"Class {k}: iteration {i+1}/{num_iter}")
        
        # Save the weights for each model
        weights.append(theta)
        logger.info(f"Completed training for class {k}")
    
    # Return a joint multi-class model with weights for all classes
    model = np.vstack(weights).transpose()
    logger.info(f"Training complete. Model shape: {model.shape}")
    
    # Pickle the model and return as bytes
    return pickle.dumps(model)
