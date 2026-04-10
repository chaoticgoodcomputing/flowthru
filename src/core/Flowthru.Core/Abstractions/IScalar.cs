namespace Flowthru.Core.Abstractions;

/// <summary>
/// Marker interface for types that serialize to a single primitive value —
/// i.e., they produce <c>"key": value</c> in JSON, not <c>"key": {...}</c> or <c>"key": [...]</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Enables user-defined NewTypes and value-object wrappers to participate
/// in flat schema classification. Without this interface, the source generator cannot distinguish
/// a <c>CustomerId</c> wrapping a <c>string</c> from a nested object requiring <c>{...}</c>
/// serialization.
/// </para>
/// <para>
/// <strong>The JSON Test:</strong>
/// </para>
/// <para>
/// The definitive question when implementing this interface is: does your type serialize to a
/// single JSON value? A type is a flat scalar if and only if it produces one of:
/// </para>
/// <list type="bullet">
/// <item><c>"key": 42</c> — numeric</item>
/// <item><c>"key": "abc"</c> — string</item>
/// <item><c>"key": true</c> — boolean</item>
/// <item><c>"key": null</c> — null</item>
/// </list>
/// <para>
/// If your type requires <c>"key": { "inner": ... }</c> or <c>"key": [...]</c>, it is NOT a
/// scalar and implementing this interface would misrepresent its structure, causing silent data
/// loss or serialization failures in flat formats like CSV.
/// </para>
/// <para>
/// <strong>Typical Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>NewType / value-object wrappers: <c>record CustomerId(string Value) : IScalar</c></item>
/// <item>Strong-typed identifiers backed by a primitive: <c>record OrderRef(Guid Id) : IScalar</c></item>
/// <item>Domain primitives that round-trip through a single string or numeric field</item>
/// </list>
/// <para>
/// <strong>What this is NOT for:</strong>
/// </para>
/// <list type="bullet">
/// <item>Types with multiple public properties — those are nested objects</item>
/// <item>Collections or dictionaries</item>
/// <item>BCL types like <c>Guid</c>, <c>DateTime</c>, <c>TimeSpan</c> — those are recognized
/// automatically by the source generator as known flat scalars</item>
/// </list>
/// <para>
/// <strong>Relationship with <see cref="IFlatSchema"/>:</strong>
/// </para>
/// <para>
/// <see cref="IFlatSchema"/> marks a <em>row type</em> — a schema whose properties are all
/// scalars. <see cref="IScalar"/> marks a <em>property type</em> — a single value that can
/// appear as a column in a flat row. Nesting a flat row inside another row is still nesting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Single-value wrapper — safe to implement IScalar
/// public readonly record struct CustomerId(string Value) : IScalar;
///
/// // ✅ Schema using the NewType is classified as flat
/// [FlowthruSchema]
/// public partial record OrderSchema
/// {
///     public required CustomerId Id { get; init; }
///     public required string Name { get; init; }
/// }
///
/// // ❌ Multi-property struct — NOT a scalar, do not implement IScalar
/// public readonly record struct Address(string Street, string City) /* : IScalar — wrong */;
/// </code>
/// </example>
public interface IScalar { }
