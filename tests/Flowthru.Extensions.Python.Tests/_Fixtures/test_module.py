"""
Test fixture for Phase 1 Python executor tests.
Contains trivial functions for validating basic invocation.
"""


def add(a, b):
    """Adds two numbers."""
    return a + b


def multiply(x, y):
    """Multiplies two numbers."""
    return x * y


def concat_strings(s1, s2):
    """Concatenates two strings."""
    return s1 + s2


def return_none():
    """Returns None."""
    return None


def raise_exception(x):
    """Raises a ValueError for error testing."""
    raise ValueError("Intentional test exception")
