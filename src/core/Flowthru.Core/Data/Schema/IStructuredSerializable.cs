namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface declaring a schema can be serialized to structured
/// hierarchical formats (JSON, XML, YAML). Source-gen-emitted for both
/// flat and nested schemas — every well-formed schema is structured-
/// serializable.
/// </summary>
/// <remarks>
/// Format serializers that produce structured output (JsonFormatSerializer,
/// XmlFormatSerializer, etc.) constrain their generic parameter
/// <c>where TRow : IStructuredSerializable</c> to require this marker.
/// Because every schema is structurally compatible with hierarchical
/// formats, this is the broadest of the format-family markers.
/// </remarks>
public interface IStructuredSerializable
{
}
