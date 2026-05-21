namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Wraps an inner scope value with the backend-assigned identifier
/// for its external state. Backends compose this around an underlying
/// <see cref="Flowthru.Prelude.FlowResource{TInner}"/> via
/// <c>Acquire.Map</c>, so
/// <see cref="IResourceBackend{TScope}.ExternalStateIdentifier"/> can
/// be implemented as a property read instead of requiring a
/// backend-side scope→id <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TInner">
/// The underlying scope type produced by the resource being wrapped
/// (e.g. <c>DbScope</c>, <c>Stream</c>).
/// </typeparam>
/// <remarks>
/// Tests written against this scope reach the inner value via
/// <see cref="Inner"/>; the
/// <see cref="ExternalStateId"/> field is for the laws' isolation
/// checks and isn't intended for use by the body.
/// </remarks>
public sealed record BackendScope<TInner>(TInner Inner, string ExternalStateId);
