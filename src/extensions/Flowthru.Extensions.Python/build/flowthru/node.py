"""
@node decorator for declaring Python node contracts.

The @node decorator attaches metadata to Python functions that Flowthru's
pre-flight validator reads to ensure schema compatibility between Python
functions and C# pipeline registrations.
"""


def node(inputs=None, outputs=None):
    """
    Declares the input and output schemas for a Python node function.

    This decorator attaches metadata (__flowthru_inputs__, __flowthru_outputs__)
    that Flowthru's pre-flight validator reads via Python.NET. The validator
    compares this metadata against the C# generic type parameters used during
    node registration to catch schema mismatches before pipeline execution.

    Args:
        inputs: Single schema type or list of schema types for inputs.
                For single-input nodes: inputs=SchemaType or inputs=[SchemaType]
                For multi-input nodes: inputs=[Schema1, Schema2, ...]
        outputs: Single schema type or list of schema types for outputs.
                 For single-output nodes: outputs=SchemaType or outputs=[SchemaType]
                 For multi-output nodes: outputs=[Schema1, Schema2, ...]

    Returns:
        Decorated function with __flowthru_inputs__ and __flowthru_outputs__ attributes.

    Example:
        Single input, single output (tabular):

            from flowthru import node
            from flowthru_schemas import IrisRawSchema, IrisFeatureSchema

            @node(inputs=[IrisRawSchema], outputs=[IrisFeatureSchema])
            def transform(df):
                return df.assign(species_encoded=df['species'].map({...}))

        Single input, single output (scalar):

            from flowthru import node
            from flowthru_schemas import ModelConfig, ModelMetrics

            @node(inputs=[ModelConfig], outputs=[ModelMetrics])
            def train_model(config):
                accuracy = config['LearningRate'] * config['Iterations'] / 100.0
                return {'Accuracy': accuracy, 'Loss': 1.0 - accuracy}

        Multi-input, multi-output (Phase 5):

            @node(
                inputs=[TrainDataSchema, TestDataSchema],
                outputs=[FeaturesSchema, LabelsSchema]
            )
            def split_features(train_df, test_df):
                features = ...
                labels = ...
                return features, labels

    Notes:
        - The decorator is required for all Python nodes in Flowthru pipelines.
        - Schema types should be imported from the generated flowthru_schemas package
          (available in Phase 5), or referenced by name as strings.
        - For Phase 4, pass schema type objects or class references directly.
        - The decorator has no runtime behavior — it only attaches metadata for
          pre-flight validation.
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

        return func

    return decorator


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
