"""
@step decorator for declaring Python step contracts.

The @step decorator attaches metadata to Python functions that Flowthru's
pre-flight validator reads to ensure schema compatibility between Python
functions and C# pipeline registrations.
"""


def step(inputs=None, outputs=None, services=None, cacheable=False):
    """
    Declares the input/output schemas and service dependencies for a Python step.

    This decorator attaches metadata to the function that Flowthru reads at
    registration time:

    * ``__flowthru_inputs__`` — schema names for inputs (matched against
      C# generic type parameters during pre-flight).
    * ``__flowthru_outputs__`` — schema names for outputs (same).
    * ``__flowthru_services__`` — fully-qualified class paths of services
      this step depends on (e.g., ``"Services.PyannoteDiarizer"``). The
      Flowthru preflight loop runs each declared service's registered
      sidecar inspector before any step executes.
    * ``__flowthru_cacheable__`` — boolean indicating that the function is
      pure with respect to its declared inputs. When ``True``, Flowthru
      auto-derives a CodeVersion from the .py file content, the project's
      lockfile, and the interpreter version, and the step participates in
      the framework's cache plan like a C# step does. Default is ``False``
      — Python steps are uncacheable unless the author opts in.

    Args:
        inputs: Single schema type or list of schema types for inputs.
                For single-input steps: inputs=SchemaType or inputs=[SchemaType]
                For multi-input steps: inputs=[Schema1, Schema2, ...]
        outputs: Single schema type or list of schema types for outputs.
                 For single-output steps: outputs=SchemaType or outputs=[SchemaType]
                 For multi-output steps: outputs=[Schema1, Schema2, ...]
        services: Optional list of service classes the step depends on.
                  Each entry should be a class reference (or fully-qualified
                  string). The decorator resolves each to a stable
                  ``"module.ClassName"`` path at decoration time using the
                  class's ``__module__`` and ``__qualname__``.
        cacheable: When True, the function asserts that its output is
                   deterministic in its inputs (no I/O, no clock reads,
                   no randomness without a seed). Flowthru folds the .py
                   file content, the project's lockfile, and the
                   interpreter version into the step's CodeVersion so the
                   cache plan can short-circuit unchanged runs. The
                   author is responsible for the assertion — Flowthru has
                   no way to inspect the function body to verify it.
                   Default ``False``.

    Returns:
        Decorated function with the metadata attributes above.

    Example:
        With a service dependency:

            from flowthru import step
            from flowthru_schemas import NormalizedAudio, DiarizationSegmentSchema
            from Services import PyannoteDiarizer

            @step(
                inputs=[NormalizedAudio],
                outputs=[DiarizationSegmentSchema],
                services=[PyannoteDiarizer],
            )
            def diarize(clips, diarizer):
                ...

    Notes:
        - The decorator is required for all Python steps in Flowthru pipelines.
        - The decorator has no runtime behavior — it only attaches metadata for
          pre-flight validation.
        - Services in ``services=[...]`` must have a registered sidecar
          inspector on the C# side (see ``python.RegisterService(...)``).
    """
    # Normalize inputs to list
    if inputs is None:
        input_list = []
    elif isinstance(inputs, list):
        input_list = inputs
    else:
        input_list = [inputs]

    # Normalize outputs to list
    if outputs is None:
        output_list = []
    elif isinstance(outputs, list):
        output_list = outputs
    else:
        output_list = [outputs]

    # Normalize services to list of fully-qualified class paths.
    # Class refs are resolved via __module__ + __qualname__ at decoration time
    # so the C# side gets a stable string identity without needing to import
    # the class itself.
    if services is None:
        service_paths = []
    else:
        service_iter = services if isinstance(services, list) else [services]
        service_paths = [_extract_service_path(s) for s in service_iter]

    def decorator(func):
        """Inner decorator that attaches metadata to the function."""
        # Extract schema names from type objects
        # For Phase 4, we accept type references or strings
        func.__flowthru_inputs__ = [
            _extract_schema_name(schema) for schema in input_list
        ]
        func.__flowthru_outputs__ = [
            _extract_schema_name(schema) for schema in output_list
        ]
        func.__flowthru_services__ = list(service_paths)
        func.__flowthru_cacheable__ = bool(cacheable)

        return func

    return decorator


def _extract_service_path(service):
    """
    Resolve a service to a fully-qualified ``"module.ClassName"`` path.

    Accepts class references (preferred) and strings. Strings pass through
    verbatim — useful for forward-references when the class itself isn't
    yet importable at decoration time.
    """
    if isinstance(service, str):
        return service
    if isinstance(service, type):
        module = getattr(service, "__module__", "")
        qualname = getattr(service, "__qualname__", service.__name__)
        return f"{module}.{qualname}" if module else qualname
    raise TypeError(
        f"@step(services=[...]) entries must be class references or strings; "
        f"got {service!r} ({type(service).__name__})."
    )


def _extract_schema_name(schema):
    """
    Extract the schema name from a type object or string.

    Args:
        schema: Schema type (class), string name, or schema object

    Returns:
        Schema name as string
    """
    if isinstance(schema, str):
        return schema
    elif isinstance(schema, type):
        # Type reference: use __name__
        return schema.__name__
    elif hasattr(schema, "__name__"):
        # Class or function object
        return schema.__name__
    else:
        # Fallback: convert to string
        return str(schema)
