"""
Flowthru Arrow IPC bridge for DataFrame marshalling.

This module handles the boundary between C# Arrow RecordBatch (as IPC buffers)
and pandas DataFrames for Python steps.

**Purpose:**
Provides bidirectional conversion between:
- C# Arrow IPC buffers (from ArrowMarshaller.ToIpcBuffer)
- pandas DataFrames (for Python step functions)

**Type Safety:**
The C# side controls the schema (column names, types, nullability).
This module performs serialization only - schema validation happens at pre-flight.

**Performance:**
Uses Arrow IPC stream format for zero-copy transfer between C# and Python.
DataFrame conversion leverages pyarrow's optimized pandas integration.
"""

import io


def df_from_ipc(ipc_bytes: bytes):
    """
    Convert Arrow IPC buffer to pandas DataFrame.

    Args:
        ipc_bytes: Arrow IPC stream buffer from C# (ArrowMarshaller.ToIpcBuffer)

    Returns:
        pandas DataFrame with columns matching Arrow schema

    Raises:
        pyarrow.ArrowInvalid: If buffer is corrupted or invalid
        ImportError: If pandas or pyarrow not installed
        ValueError: If buffer is None or empty

    Example:
        >>> # Called by ArrowMarshaller internally
        >>> ipc_buffer = marshaller.ToIpcBuffer(record_batch)
        >>> df = df_from_ipc(ipc_buffer)
        >>> print(df.head())
    """
    if ipc_bytes is None:
        raise ValueError("IPC buffer cannot be None")

    if len(ipc_bytes) == 0:
        raise ValueError("IPC buffer cannot be empty")

    try:
        import pyarrow as pa
    except ImportError as e:
        raise ImportError(
            "pyarrow is required for Arrow marshalling. "
            "Install it with: pip install pyarrow"
        ) from e

    try:
        import pandas as pd
    except ImportError as e:
        raise ImportError(
            "pandas is required for DataFrame operations. "
            "Install it with: pip install pandas"
        ) from e

    # Deserialize Arrow IPC stream to Table
    buffer = io.BytesIO(ipc_bytes)
    reader = pa.ipc.open_stream(buffer)
    table = reader.read_all()

    # Convert to pandas DataFrame
    # Note: Uses Arrow-backed dtypes when possible (pandas 2.0+)
    df = table.to_pandas()

    return df


def df_to_ipc(df, dtype_spec=None) -> bytes:
    """
    Convert pandas DataFrame to Arrow IPC buffer with optional dtype normalization.

    Args:
        df: pandas DataFrame to serialize
        dtype_spec: Optional dict mapping column names to target Arrow types.
                   If provided, performs safe type coercion before serialization.
                   Example: {'id': 'int32', 'score': 'float32'}

    Returns:
        Arrow IPC stream buffer for C# consumption (ArrowMarshaller.FromIpcBuffer)

    Raises:
        pyarrow.ArrowTypeError: If DataFrame contains unsupported types
        TypeError: If dtype coercion is incompatible
        OverflowError: If values exceed target dtype range
        ValueError: If DataFrame is None or invalid
        ImportError: If pandas or pyarrow not installed

    Example:
        >>> # Called by ArrowMarshaller internally
        >>> result_df = my_step_function(input_df)
        >>> dtype_spec = {'id': 'int32', 'score': 'float64'}
        >>> ipc_buffer = df_to_ipc(result_df, dtype_spec)
        >>> # C# receives buffer via Python.NET
    """
    if df is None:
        raise ValueError("DataFrame cannot be None")

    try:
        import pyarrow as pa
    except ImportError as e:
        raise ImportError(
            "pyarrow is required for Arrow marshalling. "
            "Install it with: pip install pyarrow"
        ) from e

    try:
        import pandas as pd
        import numpy as np
    except ImportError as e:
        raise ImportError(
            "pandas is required for DataFrame operations. "
            "Install it with: pip install pandas"
        ) from e

    # Automatic dtype normalization if spec provided
    if dtype_spec is not None:
        df = _normalize_dtypes(df, dtype_spec)

    # Convert DataFrame to Arrow Table
    # pyarrow infers schema from pandas dtypes
    try:
        table = pa.Table.from_pandas(df)
    except pa.ArrowTypeError as e:
        raise pa.ArrowTypeError(
            f"DataFrame contains unsupported types for Arrow conversion: {e}"
        ) from e

    # Serialize to IPC stream format
    sink = pa.BufferOutputStream()
    writer = pa.ipc.new_stream(sink, table.schema)

    try:
        # Write table as batches. For empty tables (0 rows), create and write
        # one empty batch so C# can read the schema and instantiate an empty IEnumerable
        if table.num_rows == 0:
            # Create empty RecordBatch with proper schema
            arrays = [pa.array([], type=field.type) for field in table.schema]
            empty_batch = pa.RecordBatch.from_arrays(arrays, schema=table.schema)
            writer.write_batch(empty_batch)
        else:
            writer.write_table(table)
    finally:
        writer.close()

    # Get bytes from buffer
    buf = sink.getvalue()
    return buf.to_pybytes()


def _normalize_dtypes(df, dtype_spec):
    """
    Normalize DataFrame dtypes to match C# schema expectations.

    Performs safe type coercion with overflow/underflow detection.
    Handles pandas defaults (int64/float64) → C# types (int32/float32).

    Args:
        df: pandas DataFrame to normalize
        dtype_spec: Dict mapping column names to pandas dtype strings

    Returns:
        DataFrame with normalized dtypes (modifies a copy, not in-place)

    Raises:
        TypeError: If column cannot be converted to target dtype
        OverflowError: If values exceed target dtype range
    """
    import pandas as pd
    import numpy as np

    df = df.copy()  # Don't mutate original
    
    for col, target_dtype in dtype_spec.items():
        if col not in df.columns:
            continue  # Skip columns not in DataFrame
        
        current_dtype = df[col].dtype
        
        # Skip if already correct dtype
        if str(current_dtype) == target_dtype:
            continue
        
        # Handle numeric narrowing conversions with range checks
        if target_dtype == 'int32':
            _coerce_to_int32(df, col, current_dtype)
        elif target_dtype == 'int16':
            _coerce_to_int16(df, col, current_dtype)
        elif target_dtype == 'float32':
            _coerce_to_float32(df, col, current_dtype)
        elif 'datetime64' in str(current_dtype):
            # Handle datetime timezone conversions
            is_current_tz_aware = hasattr(current_dtype, 'tz') and current_dtype.tz is not None
            is_target_tz_aware = 'datetime64[ns, ' in target_dtype
            
            if is_current_tz_aware and not is_target_tz_aware:
                # timezone-aware to timezone-naive: remove timezone
                df[col] = df[col].dt.tz_localize(None)
            elif is_current_tz_aware and is_target_tz_aware:
                # timezone-aware to timezone-aware: convert timezone
                target_tz = target_dtype.split(', ')[1].rstrip(']')
                df[col] = df[col].dt.tz_convert(target_tz)
            elif not is_current_tz_aware and is_target_tz_aware:
                # timezone-naive to timezone-aware: localize to timezone
                target_tz = target_dtype.split(', ')[1].rstrip(']')
                df[col] = df[col].dt.tz_localize(target_tz)
            else:
                # Both timezone-naive or unit conversion (us to ns, etc.)
                try:
                    df[col] = df[col].astype(target_dtype)
                except (ValueError, TypeError) as e:
                    raise TypeError(
                        f"Cannot convert column '{col}' from {current_dtype} to {target_dtype}: {e}"
                    ) from e
        else:
            # Standard pandas dtype conversion for everything else
            try:
                df[col] = df[col].astype(target_dtype)
            except (ValueError, TypeError) as e:
                raise TypeError(
                    f"Cannot convert column '{col}' from {current_dtype} to {target_dtype}: {e}"
                ) from e
    
    return df


def _coerce_to_int32(df, col, current_dtype):
    """Coerce column to int32 with overflow detection."""
    import numpy as np
    import pandas as pd
    
    # Check for NaN values - use nullable Int32 type if present
    has_nulls = df[col].isna().any()
    
    if current_dtype in ['int64', 'Int64', 'float64', 'float32']:
        # Check range before conversion (only on non-null values)
        col_min = df[col].min()
        col_max = df[col].max()
        
        # Skip range check if all values are NaN
        if not (pd.isna(col_min) and pd.isna(col_max)):
            int32_min = np.iinfo(np.int32).min
            int32_max = np.iinfo(np.int32).max
            
            if col_min < int32_min or col_max > int32_max:
                raise OverflowError(
                    f"Column '{col}' contains values outside int32 range "
                    f"(min={col_min}, max={col_max}). "
                    f"int32 range: [{int32_min}, {int32_max}]. "
                    f"Change C# schema to 'long' or filter data."
                )
        
        # For float types, also check for fractional values (on non-null values)
        if current_dtype in ['float64', 'float32']:
            non_null_values = df[col].dropna()
            if len(non_null_values) > 0 and not (non_null_values == non_null_values.astype('int64')).all():
                raise TypeError(
                    f"Column '{col}' contains fractional values, cannot convert to int32. "
                    f"Use 'float' or 'double' in C# schema, or round values in Python step."
                )
        
        # Use nullable Int32 if there are nulls, otherwise regular int32
        if has_nulls:
            df[col] = df[col].astype('Int32')  # Nullable integer type
        else:
            df[col] = df[col].astype('int32')
    else:
        raise TypeError(
            f"Cannot coerce column '{col}' with dtype {current_dtype} to int32. "
            f"Convert to numeric type in Python step first."
        )


def _coerce_to_int16(df, col, current_dtype):
    """Coerce column to int16 with overflow detection."""
    import numpy as np
    import pandas as pd
    
    has_nulls = df[col].isna().any()
    
    if current_dtype in ['int64', 'int32', 'Int64', 'Int32', 'float64', 'float32']:
        col_min = df[col].min()
        col_max = df[col].max()
        
        # Skip range check if all values are NaN
        if not (pd.isna(col_min) and pd.isna(col_max)):
            int16_min = np.iinfo(np.int16).min
            int16_max = np.iinfo(np.int16).max
            
            if col_min < int16_min or col_max > int16_max:
                raise OverflowError(
                    f"Column '{col}' contains values outside int16 range "
                    f"(min={col_min}, max={col_max}). "
                    f"int16 range: [{int16_min}, {int16_max}]."
                )
        
        if current_dtype in ['float64', 'float32']:
            non_null_values = df[col].dropna()
            if len(non_null_values) > 0 and not (non_null_values == non_null_values.astype('int64')).all():
                raise TypeError(
                    f"Column '{col}' contains fractional values, cannot convert to int16."
                )
        
        if has_nulls:
            df[col] = df[col].astype('Int16')  # Nullable integer type
        else:
            df[col] = df[col].astype('int16')
    else:
        raise TypeError(f"Cannot coerce column '{col}' with dtype {current_dtype} to int16.")


def _coerce_to_float32(df, col, current_dtype):
    """Coerce column to float32 with range detection."""
    import numpy as np
    
    if current_dtype in ['float64', 'int64', 'int32', 'int16']:
        # Check if values fit in float32 range
        col_min = df[col].min()
        col_max = df[col].max()
        
        float32_min = np.finfo(np.float32).min
        float32_max = np.finfo(np.float32).max
        
        if col_min < float32_min or col_max > float32_max:
            raise OverflowError(
                f"Column '{col}' contains values outside float32 range "
                f"(min={col_min}, max={col_max}). "
                f"Change C# schema to 'double'."
            )
        
        df[col] = df[col].astype('float32')
    else:
        raise TypeError(f"Cannot coerce column '{col}' with dtype {current_dtype} to float32.")
