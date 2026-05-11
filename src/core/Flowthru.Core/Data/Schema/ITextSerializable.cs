namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface declaring a schema can be serialized to text-based
/// formats (CSV, TSV, plain text). Source-gen-emitted for flat schemas.
/// </summary>
/// <remarks>
/// Format serializers that produce text output (CsvFormatSerializer, etc.)
/// constrain their generic parameter <c>where TRow : ITextSerializable</c>
/// to require this marker. Schemas with nested structure cannot be text-
/// serialized and do not receive this marker.
/// </remarks>
public interface ITextSerializable
{
}
