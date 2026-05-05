"""Validation result types for Python sidecar inspectors.

Mirrors the C# ``Flowthru.Core.Data.Validation.ValidationResult`` shape so
inspector results can be marshalled across the language boundary without
translation. Sidecar inspectors return a :class:`ValidationResult` from
their ``inspect(svc)`` function; the Flowthru Python extension's worker
serializes it to JSON, the C# preflight loop deserializes and merges into
the host run's aggregated validation result.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum


class ValidationErrorType(str, Enum):
    """Mirrors C# ``Flowthru.Core.Data.Validation.ValidationErrorType``.

    String-valued so the wire-format JSON ``error_type`` field is the
    enum value verbatim — no separate name/value mapping needed.
    """

    Configuration = "Configuration"
    Forbidden = "Forbidden"
    NotFound = "NotFound"
    IO = "IO"
    Misconfigured = "Misconfigured"
    Unauthorized = "Unauthorized"
    Unknown = "Unknown"
    InspectionFailure = "InspectionFailure"


@dataclass(frozen=True)
class ValidationResult:
    """Outcome of a sidecar inspector probe.

    Constructed via :meth:`success` or :meth:`failure` factories rather
    than the raw constructor; the dataclass is frozen so a stale instance
    cannot be silently mutated downstream.
    """

    success: bool
    source: str = ""
    error_type: str = ""
    message: str = ""

    @classmethod
    def success(cls) -> "ValidationResult":
        """Probe passed — the service is reachable and ready."""
        return cls(success=True)

    @classmethod
    def failure(
        cls,
        source: str,
        error_type: ValidationErrorType | str,
        message: str,
    ) -> "ValidationResult":
        """Probe failed — preflight halts the run with this diagnostic.

        Args:
            source: Service identifier the failure is attributed to (e.g.,
                ``"PyannoteDiarizer"``). Surfaces in the C# diagnostic
                output to point the user at the failing component.
            error_type: One of the :class:`ValidationErrorType` members,
                or a string with the same value. Strings are accepted to
                avoid forcing inspector authors to import the enum.
            message: Human-readable diagnostic with enough context for the
                user to fix the issue. Multi-line is fine.
        """
        et = error_type.value if isinstance(error_type, ValidationErrorType) else error_type
        return cls(success=False, source=source, error_type=et, message=message)

    def to_dict(self) -> dict:
        """Wire-format dict for the worker → C# response payload."""
        return {
            "success": self.success,
            "source": self.source,
            "error_type": self.error_type,
            "message": self.message,
        }
