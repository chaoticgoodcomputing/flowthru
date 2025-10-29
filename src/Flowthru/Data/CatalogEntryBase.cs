using System.Collections;
using Flowthru.Data.Validation;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data;

/// <summary>
/// Abstract base class for all catalog entry implementations.
/// Provides default implementations using Aff&lt;T&gt; effects.
/// </summary>
/// <typeparam name="T">
/// The data type stored in this catalog entry.
/// Can be a singleton type (e.g., LinearRegressionModel) or
/// a collection type (e.g., Seq&lt;FeatureRow&gt;, IEnumerable&lt;Row&gt;).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Unified Design:</strong> This single base class replaces the previous
/// CatalogObjectBase/CatalogDatasetBase split. Cardinality is determined by inspecting
/// the type parameter T at runtime.
/// </para>
/// <para>
/// <strong>Type Introspection:</strong> The GetCountAsync and validation methods use
/// reflection to determine if T is an IEnumerable type, adapting behavior accordingly.
/// </para>
/// </remarks>
public abstract class CatalogEntryBase<T> : ICatalogEntry<T>, IShallowInspectable<T>, IDeepInspectable<T> {
  private InspectionLevel? _preferredInspectionLevel;

  /// <summary>
  /// Creates a new catalog entry with the specified key.
  /// </summary>
  protected CatalogEntryBase(string key) {
    Key = key ?? throw new ArgumentNullException(nameof(key));
  }

  /// <inheritdoc/>
  public string Key { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(T);

  /// <inheritdoc/>
  public InspectionLevel? PreferredInspectionLevel {
    get => _preferredInspectionLevel;
    protected set => _preferredInspectionLevel = value;
  }

  /// <summary>
  /// Load data from storage.
  /// Subclasses must implement this method.
  /// </summary>
  public abstract IO<T> Load();

  /// <summary>
  /// Save data to storage.
  /// Subclasses must implement this method.
  /// </summary>
  public abstract IO<Unit> Save(T data);

  /// <summary>
  /// Check if data exists.
  /// Subclasses must implement this method.
  /// </summary>
  public abstract IO<bool> Exists();

  /// <inheritdoc/>
  public virtual IO<int> GetCountAsync() =>
    from exists in Exists()
    from count in exists ? CalculateCount() : IO.pure(0)
    select count;

  /// <summary>
  /// Calculates the count based on whether T is a collection or singleton.
  /// </summary>
  private IO<int> CalculateCount() {
    var type = typeof(T);

    // Check if T is IEnumerable (but not string, which also implements IEnumerable)
    if (IsCollectionType(type)) {
      // T is a collection type - count the items
      return Load().Map(data => {
        if (data is IEnumerable enumerable and not string) {
          return enumerable.Cast<object>().Count();
        }
        return 0;
      });
    } else {
      // T is a singleton type
      return IO.pure(1);
    }
  }

  /// <summary>
  /// Determines if the given type is a collection type.
  /// </summary>
  private static bool IsCollectionType(Type type) {
    // String is IEnumerable but we don't consider it a collection
    if (type == typeof(string)) {
      return false;
    }

    // Check if type implements IEnumerable
    return typeof(IEnumerable).IsAssignableFrom(type);
  }

  /// <inheritdoc/>
  public virtual IO<object> LoadUntyped() =>
    Load().Map(data => (object)data!);

  /// <inheritdoc/>
  public virtual IO<Unit> SaveUntyped(object data) {
    // Try direct cast first
    if (data is T typedData) {
      return Save(typedData);
    }

    // If T is Seq<X> and data is IEnumerable, convert it
    var type = typeof(T);
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Seq<>)) {
      if (data is IEnumerable enumerable) {
        // Convert IEnumerable to Seq<X>
        var elementType = type.GetGenericArguments()[0];
        var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!.MakeGenericMethod(elementType);
        var castEnumerable = castMethod.Invoke(null, new object[] { enumerable });

        // Now convert to Seq using toSeq from Prelude
        var seqMethod = typeof(Prelude).GetMethods()
          .First(m => m.Name == "toSeq" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
          .MakeGenericMethod(elementType);
        var seqData = seqMethod.Invoke(null, new[] { castEnumerable });

        if (seqData is T converted) {
          return Save(converted);
        }
      }
    }

    // Type mismatch - fail with descriptive error
    return IO.fail<Unit>(new Exception(
      $"Cannot save data of type '{data?.GetType().Name ?? "null"}' " +
      $"to catalog entry '{Key}' expecting type '{typeof(T).Name}'"));
  }

  /// <inheritdoc/>
  public virtual IO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    from exists in Exists()
    from result in !exists
      ? IO.pure(ValidationResult.Failure(Key, ValidationErrorType.NotFound, "Data source does not exist"))
      : PerformShallowInspection(sampleSize)
    select result;

  private IO<ValidationResult> PerformShallowInspection(int sampleSize) {
    var type = typeof(T);

    if (IsCollectionType(type)) {
      // For collections, sample some items
      return Load().Map(data => {
        if (data is IEnumerable enumerable and not string) {
          var sample = enumerable.Cast<object>().Take(sampleSize).ToList();
          return sample.Count == 0
            ? ValidationResult.Failure(Key, ValidationErrorType.EmptyDataset, "Dataset is empty")
            : ValidationResult.Success();
        }
        return ValidationResult.Failure(Key, ValidationErrorType.InvalidFormat,
          "Expected collection but data is not enumerable");
      });
    } else {
      // For singletons, just verify it loads
      return Load().Map(_ => ValidationResult.Success());
    }
  }

  /// <inheritdoc/>
  public virtual IO<ValidationResult> InspectDeep() =>
    from shallow in InspectShallow(100)
    from result in shallow.HasErrors
      ? IO.pure(shallow)
      : PerformDeepInspection()
    select result;

  private IO<ValidationResult> PerformDeepInspection() {
    var type = typeof(T);

    if (IsCollectionType(type)) {
      // For collections, load all items to verify they deserialize
      return Load().Map(data => {
        if (data is IEnumerable enumerable and not string) {
          // Force enumeration to catch serialization errors
          var count = enumerable.Cast<object>().Count();
          return count == 0
            ? ValidationResult.Failure(Key, ValidationErrorType.EmptyDataset, "Dataset is empty")
            : ValidationResult.Success();
        }
        return ValidationResult.Failure(Key, ValidationErrorType.InvalidFormat,
          "Expected collection but data is not enumerable");
      });
    } else {
      // For singletons, already validated in shallow
      return IO.pure(ValidationResult.Success());
    }
  }

  /// <summary>
  /// Configures the preferred inspection level for this catalog entry.
  /// </summary>
  public CatalogEntryBase<T> WithInspectionLevel(InspectionLevel level) {
    PreferredInspectionLevel = level;
    return this;
  }
}
