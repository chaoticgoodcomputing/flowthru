using System.Reflection;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;
using Python.Runtime;
using PythonEngineRuntime = Python.Runtime.Runtime;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Handles bidirectional conversion between C# scalar values and Python objects.
/// </summary>
/// <remarks>
/// <para>
/// Supports:
/// <list type="bullet">
/// <item>Primitives: int, long, double, float, string, bool</item>
/// <item>Nullable primitives: int?, double?, etc.</item>
/// <item>Simple records and classes via property iteration</item>
/// </list>
/// </para>
/// <para>
/// <strong>Supported since Phase 5:</strong>
/// <list type="bullet">
/// <item>Arrays: T[] (converted to/from Python lists)</item>
/// <item>Nested records (via recursive marshalling)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Not supported:</strong>
/// <list type="bullet">
/// <item>Generic collections: IEnumerable&lt;T&gt;, List&lt;T&gt; (use arrays or tabular I/O)</item>
/// <item>Arrow/DataFrame interchange (use tabular I/O path)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Thread-safety:</strong> All methods are thread-safe.
/// Caller is responsible for GIL acquisition.
/// </para>
/// </remarks>
public static class ScalarMarshaller
{
  /// <summary>
  /// Converts a C# value to a Python object.
  /// </summary>
  /// <param name="value">C# value to convert. May be null.</param>
  /// <returns>Python object representation of the value.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the value type is not supported for marshalling.
  /// </exception>
  /// <remarks>
  /// Must be called within a GIL-acquired context.
  /// </remarks>
  public static PyObject ToPython(object? value)
  {
    // Handle null
    if (value == null)
    {
      using (Py.GIL())
      {
        return PythonEngineRuntime.None;
      }
    }

    var type = value.GetType();

    // Handle byte[] to Python bytes (before general array handling)
    if (type == typeof(byte[]))
    {
      // Convert byte array directly to Python bytes object
      return ((byte[])value).ToPython();
    }

    // Handle primitives via Python.NET's built-in conversion
    if (IsPrimitiveType(type))
    {
      return value.ToPython();
    }

    // Handle arrays via element-wise conversion
    if (type.IsArray)
    {
      return ArrayToPython(value);
    }

    // Handle records/classes via property iteration
    if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
    {
      return RecordToPython(value);
    }

    throw new InvalidOperationException(
      $"Type '{type.FullName}' is not supported for scalar marshalling. "
        + "Supported types: primitives (int, double, string, bool) and simple records."
    );
  }

  /// <summary>
  /// Converts a Python object to a C# value of type T.
  /// </summary>
  /// <typeparam name="T">Target C# type.</typeparam>
  /// <param name="pyObject">Python object to convert.</param>
  /// <returns>C# value of type T.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when conversion fails or the type is not supported.
  /// </exception>
  /// <remarks>
  /// Must be called within a GIL-acquired context.
  /// </remarks>
  public static T FromPython<T>(PyObject pyObject)
  {
    if (pyObject == null)
    {
      throw new ArgumentNullException(nameof(pyObject));
    }

    var targetType = typeof(T);

    try
    {
      // Handle None → null for nullable types
      if (pyObject.IsNone())
      {
        if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
        {
          throw new InvalidOperationException(
            $"Cannot convert Python None to non-nullable value type '{targetType.Name}'."
          );
        }
        return default!;
      }

      // Handle byte[] from Python bytes object (before general array handling)
      if (targetType == typeof(byte[]))
      {
        return (T)(object)pyObject.As<byte[]>();
      }

      // Handle primitives via Python.NET's built-in conversion
      if (IsPrimitiveType(targetType) || Nullable.GetUnderlyingType(targetType) != null)
      {
        return pyObject.As<T>();
      }

      // Handle arrays via element-wise conversion
      if (targetType.IsArray)
      {
        return ArrayFromPython<T>(pyObject);
      }

      // Handle records/classes via property population
      if (targetType.IsClass || (targetType.IsValueType && !targetType.IsPrimitive))
      {
        return RecordFromPython<T>(pyObject);
      }

      throw new InvalidOperationException(
        $"Type '{targetType.FullName}' is not supported for scalar marshalling."
      );
    }
    catch (PythonException ex)
    {
      throw new InvalidOperationException(
        $"Failed to convert Python object to C# type '{targetType.Name}': {ex.Message}",
        ex
      );
    }
  }

  /// <summary>
  /// Checks if a type is a supported primitive or string.
  /// </summary>
  private static bool IsPrimitiveType(Type type)
  {
    return type == typeof(int)
      || type == typeof(long)
      || type == typeof(double)
      || type == typeof(float)
      || type == typeof(bool)
      || type == typeof(string);
  }

  /// <summary>
  /// Converts a C# array to a Python list.
  /// </summary>
  private static PyObject ArrayToPython(object value)
  {
    using (Py.GIL())
    {
      var array = (Array)value;
      var pyList = new PyList();

      foreach (var element in array)
      {
        var pyElement = element == null ? PythonEngineRuntime.None : ToPython(element);
        pyList.Append(pyElement);
      }

      return pyList;
    }
  }

  /// <summary>
  /// Converts a C# record/class to a Python dictionary.
  /// </summary>
  /// <remarks>
  /// Respects [SerializedLabel] attributes on properties, using external field names
  /// instead of C# property names for dictionary keys.
  /// </remarks>
  private static PyObject RecordToPython(object value)
  {
    using (Py.GIL())
    {
      var dict = new PyDict();

      var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var prop in properties)
      {
        var propValue = prop.GetValue(value);

        // Convert property value to Python, handling nested GIL acquisition
        PyObject pyValue;
        if (propValue == null)
        {
          pyValue = PythonEngineRuntime.None;
        }
        else if (IsPrimitiveType(propValue.GetType()))
        {
          pyValue = propValue.ToPython();
        }
        else
        {
          // Recursively convert non-primitive values (arrays, nested records)
          // (GIL already acquired here, so conversion will reacquire if needed)
          pyValue = ToPython(propValue);
        }

        // Use external field name (respects [SerializedLabel]). RecordToPython is
        // non-generic so it can't use the typed planner directly; inline the SerializedLabel
        // resolution to keep this method's call shape unchanged.
        var label = prop.GetCustomAttribute<SerializedLabelAttribute>();
        var fieldName = label?.Label ?? prop.Name;
        dict.SetItem(fieldName.ToPython(), pyValue);
      }

      return dict;
    }
  }

  /// <summary>
  /// Converts a Python list to a C# array.
  /// </summary>
  private static T ArrayFromPython<T>(PyObject pyObject)
  {
    var targetType = typeof(T);
    var elementType = targetType.GetElementType()!;

    // Convert PyObject to PyList
    using var pyList = new PyList(pyObject);
    var length = pyList.Length();

    // Create array of appropriate type
    var array = Array.CreateInstance(elementType, length);

    // Convert elements
    var convertMethod = typeof(ScalarMarshaller)
      .GetMethod(nameof(FromPython), BindingFlags.Public | BindingFlags.Static)!
      .MakeGenericMethod(elementType);

    for (int i = 0; i < length; i++)
    {
      var pyElement = pyList.GetItem(i);
      var csharpElement = convertMethod.Invoke(null, new object[] { pyElement });
      array.SetValue(csharpElement, i);
    }

    return (T)(object)array;
  }

  /// <summary>
  /// Converts a Python dictionary to a C# record/class.
  /// </summary>
  /// <remarks>
  /// Respects [SerializedLabel] attributes on properties, mapping external field names
  /// from the Python dictionary to C# property names. Uses case-insensitive comparison.
  /// </remarks>
  private static T RecordFromPython<T>(PyObject pyObject)
  {
    var targetType = typeof(T);

    // Use SchemaActivator for instantiation (supports required members and positional records)
    // We know T is a concrete class or value type from the caller's type check,
    // but the compiler can't verify this statically
#pragma warning disable CS8714 // Nullability mismatch
    var instance = SchemaActivator.CreateInstance<T>();
#pragma warning restore CS8714

    // Build the planner once per record-deserialization call to resolve field-name → property
    // bindings (case-insensitive, respects SerializedLabel). The planner subsumes the
    // PropertyMappingHelper.BuildPropertyMap shape via plan.ByFieldName.
    var plan = PropertyMappingPlanner.Build<T>();
    var propertyMap = plan.ByFieldName;

    // Convert PyObject to PyDict for proper dictionary access
    using var dict = new PyDict(pyObject);

    // Iterate over Python dictionary keys
    foreach (PyObject pyKey in dict.Keys())
    {
      var fieldName = pyKey.As<string>();

      // Look up corresponding property binding (case-insensitive)
      if (!propertyMap.TryGetValue(fieldName, out var binding))
      {
        continue;
      }

      var prop = binding.Property;
      if (!prop.CanWrite)
      {
        continue;
      }

      var pyValue = dict.GetItem(pyKey);

      try
      {
        // Convert Python value to C# property type
        var convertMethod = typeof(ScalarMarshaller)
          .GetMethod(nameof(FromPython), BindingFlags.Public | BindingFlags.Static)!
          .MakeGenericMethod(prop.PropertyType);

        var csharpValue = convertMethod.Invoke(null, new object[] { pyValue });
        prop.SetValue(instance, csharpValue);
      }
      catch (TargetInvocationException ex) when (ex.InnerException != null)
      {
        throw new InvalidOperationException(
          $"Failed to convert property '{prop.Name}' of type '{prop.PropertyType.Name}': "
            + ex.InnerException.Message,
          ex.InnerException
        );
      }
    }

    return (T)instance;
  }
}
