"""
Test module for Phase 2 scalar Python step integration.

This module simulates a simple model training function that takes
a configuration object and returns a result object.
"""

from flowthru import step


@step(inputs=["ModelConfigSchema"], outputs=["ModelResultSchema"])
def train_model(config):
    """
    Simulates model training by computing accuracy based on config parameters.

    Args:
        config: Dictionary with 'LearningRate', 'Iterations', and 'ModelName' keys.

    Returns:
        Dictionary with 'Accuracy', 'Loss', 'Success', and 'Message' keys.
    """
    # Simple calculation based on config
    learning_rate = config["LearningRate"]
    iterations = config["Iterations"]
    model_name = config.get("ModelName") or "default"

    # Simulate training: higher learning rate * more iterations = higher accuracy
    # (capped at 1.0)
    accuracy = min(learning_rate * iterations / 100.0, 1.0)
    loss = 1.0 - accuracy

    return {
        "Accuracy": accuracy,
        "Loss": loss,
        "Success": accuracy > 0.5,
        "Message": f"Training completed for {model_name}",
    }


@step(inputs=["ModelConfigSchema"], outputs=["ModelConfigSchema"])
def identity(value):
    """
    Identity function for testing simple pass-through.

    Args:
        value: Any value.

    Returns:
        The same value unchanged.
    """
    return value


@step(inputs=["ModelConfigSchema"], outputs=["ModelConfigSchema"])
def double_iterations(config):
    """
    Doubles the iteration count in a config.

    Args:
        config: Dictionary with at least an 'Iterations' key.

    Returns:
        Modified config with Iterations doubled.
    """
    result = dict(config)
    result["Iterations"] = config["Iterations"] * 2
    return result


@step(inputs=["int", "int"], outputs=["MetricsReportSchema"])
def calculate_metrics(correct, total):
    """
    Calculates metrics and returns a dictionary with snake_case keys.

    This simulates a Python step returning a dictionary to a single-object
    catalog entry with SerializedLabel attributes.

    Args:
        correct: Number of correct predictions (int).
        total: Total number of samples (int).

    Returns:
        Dictionary with 'accuracy', 'correct_predictions', and 'total_samples' keys
        (matching SerializedLabel attributes, not C# property names).
    """
    accuracy = correct / total if total > 0 else 0.0

    # Return dict with snake_case keys (as Python would naturally do)
    return {
        "accuracy": accuracy,
        "correct_predictions": correct,
        "total_samples": total,
    }
