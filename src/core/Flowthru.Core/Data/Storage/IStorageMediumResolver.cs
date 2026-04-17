namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Resolves the appropriate <see cref="IStorageMedium"/> for a given file path or URI string.
/// </summary>
/// <remarks>
/// <para>
/// Falls back to <see cref="Medium.FileStorageMedium"/> for bare paths and <c>file://</c>
/// URIs. For all other URI schemes, registered <see cref="IStorageMediumProvider"/>
/// implementations are consulted in registration order.
/// </para>
/// <para>
/// <strong>DI-based usage (recommended):</strong>
/// </para>
/// <code>
/// services.AddFlowthru(flowthru =>
/// {
///     flowthru.UseHttp();          // registers HttpStorageMediumProvider
///     flowthru.RegisterCatalog(sp => new MyCatalog(
///         dataPath,
///         sp.GetRequiredService&lt;IStorageMediumResolver&gt;()
///     ));
/// });
/// </code>
/// <para>
/// <strong>Direct-construction usage (standalone, tests):</strong>
/// </para>
/// <code>
/// var resolver = new StorageMediumResolver()
///     .Register(new HttpStorageMediumProvider());
///
/// var catalog = new MyCatalog(dataPath, resolver);
/// </code>
/// </remarks>
public interface IStorageMediumResolver
{
  /// <summary>
  /// Returns the appropriate <see cref="IStorageMedium"/> for the given path or URI string.
  /// </summary>
  /// <param name="pathOrUri">
  /// A local file path (absolute or relative), a <c>file://</c> URI, or any other URI
  /// whose scheme is handled by a registered <see cref="IStorageMediumProvider"/>.
  /// </param>
  IStorageMedium Resolve(string pathOrUri);
}
