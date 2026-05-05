"""
Flowthru Python extension package.

Provides decorators and utilities for writing Python steps in Flowthru pipelines.

Public API:
    step                        — @step decorator for declaring step contracts
    config                      — IConfiguration view; nested access + typed getters
    ValidationResult            — sidecar inspector return type
    ValidationErrorType         — taxonomy of inspector failure modes
    ConfigCoercionError         — raised by config typed accessors on parse failure
"""

from . import config
from .step import step
from .validation import ValidationErrorType, ValidationResult
from .config import ConfigCoercionError

__all__ = [
    "step",
    "config",
    "ValidationResult",
    "ValidationErrorType",
    "ConfigCoercionError",
]
__version__ = "0.2.0"
