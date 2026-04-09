namespace Flowthru.FUnit.Samples;

/// <summary>
/// Helpers for constructing typed sample data in step and effect tests.
/// </summary>
public class SampleBuilder
{
  /// <summary>
  /// Wraps explicit instances into an <see cref="IEnumerable{T}"/>.
  /// </summary>
  public IEnumerable<T> Of<T>(params T[] items) => items;

  /// <summary>
  /// Generates <paramref name="count"/> rows using a factory function
  /// that receives the zero-based row index.
  /// </summary>
  public IEnumerable<T> Generate<T>(int count, Func<int, T> factory)
  {
    for (int i = 0; i < count; i++)
    {
      yield return factory(i);
    }
  }

  /// <summary>
  /// Loads CSV rows from an embedded resource in the calling assembly.
  /// The resource must be a valid CSV file whose columns match
  /// the public properties of <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">Target row type. Must have a parameterless constructor.</typeparam>
  /// <param name="resourcePath">
  /// The fully-qualified embedded resource name
  /// (e.g. <c>"MyTests.Data.sample.csv"</c>).
  /// </param>
  public IEnumerable<T> FromCsv<T>(string resourcePath)
    where T : new()
  {
    var callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
    using var stream =
      callingAssembly.GetManifestResourceStream(resourcePath)
      ?? throw new InvalidOperationException(
        $"Embedded resource '{resourcePath}' not found in assembly '{callingAssembly.GetName().Name}'. "
          + "Ensure the file is included with <EmbeddedResource> in the project file."
      );

    using var reader = new System.IO.StreamReader(stream);

    var header =
      reader.ReadLine()
      ?? throw new InvalidOperationException($"CSV resource '{resourcePath}' is empty.");

    var columns = header.Split(',');
    var props = typeof(T)
      .GetProperties(
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
      )
      .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    string? line;
    while ((line = reader.ReadLine()) != null)
    {
      if (string.IsNullOrWhiteSpace(line))
        continue;

      var values = line.Split(',');
      var instance = new T();

      for (int i = 0; i < Math.Min(columns.Length, values.Length); i++)
      {
        if (!props.TryGetValue(columns[i].Trim(), out var prop) || !prop.CanWrite)
          continue;

        var value = Convert.ChangeType(values[i].Trim(), prop.PropertyType);
        prop.SetValue(instance, value);
      }

      yield return instance;
    }
  }
}
