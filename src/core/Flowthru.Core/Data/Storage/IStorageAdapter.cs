namespace Flowthru.Data.Storage;

/// <summary>
/// The universal storage contract — abstracts <c>Load</c>, <c>Save</c>,
/// <c>Exists</c>, and three levels of inspection regardless of underlying
/// strategy. Concrete implementations are constructed via smart
/// constructors and never named directly by Flow/Catalog Developers.
/// </summary>
/// <typeparam name="T">
/// The container type the adapter loads and saves
/// (e.g., <c>IEnumerable&lt;TRow&gt;</c>, a single value, <c>byte[]</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Implementation strategies.</strong> Two patterns satisfy this
/// contract:
/// </para>
/// <list type="bullet">
/// <item>
/// <strong>Composed</strong> — when medium × format × container are
/// orthogonal. The adapter combines an <see cref="IStorageMedium"/>, an
/// <see cref="IFormatSerializer{TRow}"/>, and an
/// <see cref="IContainerAdapter{TContainer, TRow}"/>. Used by file-system
/// formats (JSON, CSV, Parquet).
/// </item>
/// <item>
/// <strong>Direct</strong> — when the layers are not separable (databases,
/// HTTP endpoints with bespoke adapters). The extension implements
/// <see cref="IStorageAdapter{T}"/> directly without going through the
/// composed shape.
/// </item>
/// </list>
/// <para>
/// All operations return <see cref="FlowIO{A}"/> effects: lazy, async,
/// cancellable, and failure-as-value. Errors flow through
/// <see cref="EffResult{A}.Failure"/>; nothing throws.
/// </para>
/// </remarks>
public interface IStorageAdapter<T>
{
  /// <summary>
  /// Capability matrix for this adapter — composed from the underlying
  /// medium, format, and container traits when applicable, or declared
  /// directly when the adapter doesn't decompose along those axes.
  /// </summary>
  StorageTraits Traits { get; }

  /// <summary>Loads data from storage.</summary>
  FlowIO<T> Load();

  /// <summary>Saves data to storage.</summary>
  FlowIO<FlowUnit> Save(T data);

  /// <summary>True if data is currently present at this location.</summary>
  FlowIO<bool> Exists();

  /// <summary>
  /// Shallow inspection — existence, format, headers, and a sample of
  /// rows. Default for raw inputs; minimal overhead. The adapter chooses
  /// how to interpret the sample size.
  /// </summary>
  FlowIO<ValidationResult> InspectShallow(int sampleSize);

  /// <summary>
  /// Deep inspection — every row deserializes successfully. Potentially
  /// significant overhead; opt-in by the catalog author for critical data.
  /// </summary>
  FlowIO<ValidationResult> InspectDeep();

  /// <summary>
  /// Reachability check on a write target — can this output land here?
  /// Distinct from the read-side <see cref="InspectShallow"/>; runs during
  /// pre-flight on items declared as flow outputs.
  /// </summary>
  FlowIO<ValidationResult> InspectTarget();
}
