namespace Flowthru.Data.Storage;

/// <summary>
/// Capability marker for an <see cref="IStorageAdapter{T}"/> that
/// declares a canonical <see cref="Flowthru.Data.Catalog.IItem.StorageKind"/>
/// identifier for the items it backs. Adapters bound to a runtime
/// service (a GraphQL endpoint, an HTTP API, a database connection)
/// implement this so metadata providers can surface a distinct shape —
/// Core defines the slot, the adapter populates it, the renderer maps
/// it.
/// </summary>
/// <remarks>
/// <para>
/// Sibling pattern to <see cref="IHasEfficientCount"/> and
/// <see cref="ISupportsFingerprint"/>: an adapter opts in by
/// implementing the interface, and <see cref="Flowthru.Data.Catalog.Item{T}"/>
/// surfaces the capability via runtime <c>is</c> tests. File-backed
/// adapters that don't implement this interface produce items whose
/// <see cref="Flowthru.Data.Catalog.IItem.StorageKind"/> stays null —
/// the default file-backed-or-unspecified case.
/// </para>
/// <para>
/// <strong>Value contract.</strong> A canonical lowercase identifier
/// (e.g. <c>"gql"</c>, <c>"http"</c>, <c>"database"</c>,
/// <c>"memory"</c>). Renderers map known service-backed kinds to a
/// distinct shape and fall back to the default for unknown ones, so
/// new kinds drop in without renderer changes.
/// </para>
/// </remarks>
public interface IHasStorageKind
{
  /// <summary>The adapter's canonical storage-kind identifier.</summary>
  string StorageKind { get; }
}
