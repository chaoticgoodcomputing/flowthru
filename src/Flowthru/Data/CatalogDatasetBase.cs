using Flowthru.Data.Validation;

namespace Flowthru.Data;

/// <summary>
/// Abstract base class for catalog dataset implementations (collections).
/// Provides default implementations of untyped operations using the strongly-typed methods.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Template Method Pattern:</strong> This class defines the skeleton of catalog
/// dataset operations. Derived classes implement the abstract Load() and Save() methods,
/// while this base class provides the untyped variants by delegating to the typed versions.
/// </para>
/// <para>
/// Implementations must provide:
/// - Load(): Retrieve dataset from storage (returns IEnumerable&lt;T&gt;)
/// - Save(IEnumerable&lt;T&gt; data): Persist dataset to storage
/// - Exists(): Check if dataset is present
/// </para>
/// <para>
/// <strong>Inspection Capabilities:</strong> This base class implements both shallow and deep
/// inspection interfaces by default. Derived classes can override these methods to provide
/// more efficient or format-specific validation logic.
/// </para>
/// </remarks>
public abstract class CatalogDatasetBase<T> : ICatalogDataset<T>, IShallowInspectable<T>, IDeepInspectable<T> {
  /// <summary>
  /// Creates a new catalog dataset with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog dataset</param>
  protected CatalogDatasetBase(string key) {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(IEnumerable<T>);

  /// <inheritdoc/>
  public abstract Task<IEnumerable<T>> Load();

  /// <inheritdoc/>
  public abstract Task Save(IEnumerable<T> data);

  /// <inheritdoc/>
  public abstract Task<bool> Exists();

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation loads the data and counts the items.
  /// Derived classes should override this for better performance when possible
  /// (e.g., reading record count from file metadata without loading all data).
  /// </remarks>
  public virtual async Task<int> GetCountAsync() {
    if (!await Exists()) {
      return 0;
    }

    var data = await Load();
    return data.Count();
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation delegates to strongly-typed Load() and boxes the result.
  /// </remarks>
  public virtual async Task<object> LoadUntyped() {
    var data = await Load();
    return data!;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Default implementation casts the object to IEnumerable&lt;T&gt; and delegates to strongly-typed Save().
  /// </remarks>
  /// <exception cref="InvalidCastException">
  /// Thrown if <paramref name="data"/> cannot be cast to type IEnumerable&lt;T&gt;
  /// </exception>
  public virtual async Task SaveUntyped(object data) {
    if (data is not IEnumerable<T> typedData) {
      throw new InvalidCastException(
          $"Cannot save data of type {data?.GetType().Name ?? "null"} " +
          $"to catalog dataset expecting type IEnumerable<{typeof(T).Name}>");
    }

    await Save(typedData);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>Default Implementation:</strong> Checks existence and attempts to load the first
  /// <paramref name="sampleSize"/> rows to validate deserialization.
  /// </para>
  /// <para>
  /// Derived classes should override this method to provide more efficient or format-specific
  /// validation (e.g., checking CSV headers without loading data, validating Parquet metadata).
  /// </para>
  /// </remarks>
  public virtual async Task<ValidationResult> InspectShallow(int sampleSize = 100) {
    var result = new ValidationResult();

    try {
      // 1. Check existence
      if (!await Exists()) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.NotFound,
          $"Data source does not exist",
          $"Catalog entry '{Key}' of type {GetType().Name}"));
        return result;
      }

      // 2. Attempt to load sample rows
      var data = await Load();
      var sample = data.Take(sampleSize).ToList();

      // 3. Check if empty when data was expected
      if (sample.Count == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"Dataset is empty",
          $"Expected at least one row in '{Key}'"));
        return result;
      }

      // Success - data exists and sample rows loaded successfully
      return ValidationResult.Success();
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>Default Implementation:</strong> Loads the entire dataset to validate all rows
  /// can be deserialized successfully.
  /// </para>
  /// <para>
  /// <strong>Performance Warning:</strong> This implementation loads all data into memory.
  /// Derived classes should override with streaming validation when possible.
  /// </para>
  /// </remarks>
  public virtual async Task<ValidationResult> InspectDeep() {
    var result = new ValidationResult();

    try {
      // 1. Perform shallow inspection first
      var shallowResult = await InspectShallow(sampleSize: 100);
      if (shallowResult.HasErrors) {
        return shallowResult;
      }

      // 2. Load and count ALL rows
      var data = await Load();
      var count = data.Count();

      // Success - all rows loaded successfully
      return ValidationResult.Success();
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }
}
