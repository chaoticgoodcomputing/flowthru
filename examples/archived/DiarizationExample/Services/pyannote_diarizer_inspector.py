"""Sidecar inspector for :class:`PyannoteDiarizer`.

Mirrors the .NET ``IFlowthruInspector<TService>`` pattern: the validation
logic lives in a separate module from the service, and the Flowthru
integrator wires the two together via ``python.RegisterService(...)
.WithInspector(...)`` in Program.cs. The service file remains
Flowthru-free; this inspector file is where the Flowthru-typed
``ValidationResult`` return value lives.

The framework imports this module, constructs a :class:`PyannoteDiarizer`
instance from the registered service path, and calls :func:`inspect`
with that instance. Returning :meth:`ValidationResult.success`
short-circuits the rest of preflight; returning a failure halts the run
with the contained diagnostic.
"""

from __future__ import annotations

import httpx

from flowthru import ValidationResult, ValidationErrorType

from .pyannote_diarizer import PyannoteDiarizer


def inspect(svc: PyannoteDiarizer) -> ValidationResult:
    """Verify pyannote can authenticate against HuggingFace and access the model.

    Distinguishes the three pyannote-specific failure modes by HTTP status
    code so the diagnostic points the user at the *specific* fix:
      - missing token       (Configuration)
      - 401 invalid token   (Forbidden)
      - 403 terms not yet accepted (Forbidden — different message)
    """
    if not svc.config.hugging_face_token:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Configuration,
            message=(
                "Diarization:HuggingFaceToken is not configured. pyannote "
                "models are gated and require a HuggingFace token. Get one "
                "at https://hf.co/settings/tokens and set it in "
                "appsettings.Local.json or as the env var "
                "Diarization__HuggingFaceToken."
            ),
        )

    url = f"https://huggingface.co/api/models/{svc.config.pyannote_model}"
    headers = {"Authorization": f"Bearer {svc.config.hugging_face_token}"}
    try:
        response = httpx.get(url, headers=headers, timeout=10.0)
    except httpx.HTTPError as exc:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.NotFound,
            message=(
                f"Could not reach huggingface.co to verify access to "
                f"{svc.config.pyannote_model!r}: {exc}"
            ),
        )

    if response.status_code == 401:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Forbidden,
            message=(
                "HuggingFace rejected the token (401). Verify "
                "Diarization:HuggingFaceToken is current and has 'read' scope."
            ),
        )
    if response.status_code == 403:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Forbidden,
            message=(
                f"Token is valid but you have not accepted the model terms "
                f"for {svc.config.pyannote_model!r}. Visit "
                f"https://hf.co/{svc.config.pyannote_model} and click "
                f"'Accept' once while signed in (one-time per HF account)."
            ),
        )
    if response.status_code != 200:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Unknown,
            message=(
                f"Unexpected response {response.status_code} from HuggingFace "
                f"when probing {svc.config.pyannote_model!r}."
            ),
        )

    return ValidationResult.success()
