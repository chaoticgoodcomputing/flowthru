using System.Runtime.CompilerServices;
using Flowthru.Core.Data.Storage;
using Microsoft.ML;

namespace Flowthru.Extensions.MLNet.Container;

/// <summary>
/// Container adapter for ML.NET IDataView - columnar data representation.
/// </summary>
/// <typeparam name="T">The row schema type</typeparam>
/// <remarks>
/// <para>
/// <strong>NEW CAPABILITY:</strong> This adapter enables native ML.NET integration with Flowthru!
/// </para>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Columnar storage:</strong> Optimized for ML.NET operations</item>
/// <item><strong>Lazy evaluation:</strong> Data loaded on-demand during iteration</item>
/// <item><strong>Type safety:</strong> Strongly-typed row schema</item>
/// <item><strong>ML.NET native:</strong> Direct integration with ML.NET pipelines</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Machine learning pipelines using ML.NET</item>
/// <item>Data transformations (normalization, encoding, etc.)</item>
/// <item>Feature engineering workflows</item>
/// <item>Model training and evaluation</item>
/// </list>
/// <para>
/// <strong>Integration with ML.Next:</strong>
/// </para>
/// <para>
/// This adapter bridges Flowthru catalogs with ML.Next's type-safe wrappers,
/// enabling end-to-end compile-time safety for ML pipelines.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record FeatureRow(
///     float Feature1,
///     float Feature2,
///     int Label
/// ) : IFlatSchema, ITextSerializable;
///
/// var mlContext = new MLContext();
/// var adapter = new DataViewContainerAdapter&lt;FeatureRow&gt;(mlContext);
///
/// // From rows to IDataView
/// var dataView = await adapter.FromRows(rowStream);
///
/// // Use with ML.NET
/// var pipeline = mlContext.Transforms
///     .NormalizeMinMax("Feature1")
///     .Append(mlContext.Transforms.NormalizeMinMax("Feature2"));
///
/// var model = pipeline.Fit(dataView);
/// var transformedData = model.Transform(dataView);
///
/// // Back to rows
/// var transformedRows = adapter.ToRows(transformedData);
/// </code>
/// </example>
public sealed class DataViewContainerAdapter<T> : IContainerAdapter<IDataView, T>
  where T : class, new()
{
  private readonly MLContext _mlContext;

  /// <summary>
  /// Creates a new IDataView container adapter.
  /// </summary>
  /// <param name="mlContext">The ML.NET context for data operations</param>
  /// <exception cref="ArgumentNullException">Thrown if mlContext is null</exception>
  public DataViewContainerAdapter(MLContext mlContext)
  {
    _mlContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
  }

  /// <summary>
  /// Gets the ML.NET context used by this adapter.
  /// </summary>
  public MLContext MLContext => _mlContext;

  /// <inheritdoc/>
  public async Task<IDataView> FromRows(IAsyncEnumerable<T> rows)
  {
    if (rows == null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    // Materialize rows to enumerable first
    // ML.NET's LoadFromEnumerable requires IEnumerable
    var list = new List<T>();
    await foreach (var row in rows)
    {
      list.Add(row);
    }

    // Convert to IDataView using ML.NET
    var dataView = _mlContext.Data.LoadFromEnumerable(list);

    return dataView;
  }

  /// <inheritdoc/>
  public async IAsyncEnumerable<T> ToRows(IDataView container)
  {
    if (container == null)
    {
      throw new ArgumentNullException(nameof(container));
    }

    // Convert IDataView back to strongly-typed rows
    var enumerable = _mlContext.Data.CreateEnumerable<T>(
      container,
      reuseRowObject: false // Create new object for each row for safety
    );

    foreach (var row in enumerable)
    {
      yield return row;
    }
  }
}
