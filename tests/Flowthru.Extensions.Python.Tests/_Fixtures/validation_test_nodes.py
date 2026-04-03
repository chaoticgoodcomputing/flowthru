"""
Test fixtures for Phase 4 validation testing.

This module contains deliberately malformed steps to test validation error detection.
"""

from flowthru import step


# ─── Valid steps for baseline testing ───────────────────────────────────


@step(inputs=["ModelConfigSchema"], outputs=["ModelResultSchema"])
def valid_step(config):
    """A properly decorated step for baseline testing."""
    return {
        "Accuracy": 0.95,
        "Loss": 0.05,
        "Success": True,
        "Message": "Validation test",
    }


# ─── Missing decorator (caught at registration time) ────────────────────


def missing_decorator_step(config):
    """Node without @step decorator - should fail registration-time validation."""
    return config


# ─── Schema mismatch scenarios (caught at pre-flight) ───────────────────


@step(inputs=["WrongInputSchema"], outputs=["ModelResultSchema"])
def wrong_input_schema(config):
    """Decorator declares wrong input schema."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }


@step(inputs=["ModelConfigSchema"], outputs=["WrongOutputSchema"])
def wrong_output_schema(config):
    """Decorator declares wrong output schema."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }


@step(
    inputs=["ModelConfigSchema", "ExtraInputSchema"],
    outputs=["ModelResultSchema"]
)
def too_many_inputs(config):
    """Decorator declares too many input schemas."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }


@step(
    inputs=["ModelConfigSchema"],
    outputs=["ModelResultSchema", "ExtraOutputSchema"],
)
def too_many_outputs(config):
    """Decorator declares too many output schemas."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }


@step(inputs=[], outputs=["ModelResultSchema"])
def zero_inputs(config):
    """Decorator declares no input schemas."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }


@step(inputs=["ModelConfigSchema"], outputs=[])
def zero_outputs(config):
    """Decorator declares no output schemas."""
    return {
        "Accuracy": 0.5,
        "Loss": 0.5,
        "Success": True,
        "Message": "Test",
    }
