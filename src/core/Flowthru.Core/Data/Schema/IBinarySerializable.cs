namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface declaring a schema can be serialized to binary formats
/// (Parquet, Avro, protobuf). Source-gen-emitted automatically based on
/// the schema's property classification.
/// </summary>
/// <remarks>
/// Format serializers that produce binary output (ParquetFormatSerializer,
/// etc.) constrain their generic parameter
/// <c>where TRow : IBinarySerializable</c> to require this marker.
/// </remarks>
public interface IBinarySerializable
{
}
