# <a id="Flowthru_Abstractions"></a> Namespace Flowthru.Abstractions

### Classes

 [FlowthruSchemaAttribute](Flowthru.Abstractions.FlowthruSchemaAttribute.md)

Marks a schema type for automatic interface generation. The source generator
will analyze the type's properties and emit the appropriate marker interfaces:
<ul><li><xref href="Flowthru.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> based on property types</li><li><xref href="Flowthru.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> for flat schemas (CSV/TSV compatible)</li><li><xref href="Flowthru.Abstractions.IBinarySerializable" data-throw-if-not-resolved="false"></xref> for flat schemas (Parquet compatible)</li><li><xref href="Flowthru.Abstractions.IStructuredSerializable" data-throw-if-not-resolved="false"></xref> for all schemas (JSON/XML compatible)</li></ul>

 [SerializedEnumAttribute](Flowthru.Abstractions.SerializedEnumAttribute.md)

Specifies the serialized string value for an enum member when written to or read from storage.

 [SerializedLabelAttribute](Flowthru.Abstractions.SerializedLabelAttribute.md)

Specifies the external field name for a property when serialized to/from storage.

### Interfaces

 [IBinarySerializable](Flowthru.Abstractions.IBinarySerializable.md)

Marker interface for schema types that can be serialized to columnar binary formats (Parquet, Avro).

 [IFlatSchema](Flowthru.Abstractions.IFlatSchema.md)

Marker interface for schema types with flat (non-nested) structure.

 [IFlatSerializable](Flowthru.Abstractions.IFlatSerializable.md)

Marker interface for schema types that contain only flat, primitive data.

 [INestedSchema](Flowthru.Abstractions.INestedSchema.md)

Marker interface for schema types with nested structure (collections or nested objects).

 [INestedSerializable](Flowthru.Abstractions.INestedSerializable.md)

Marker interface for schema types that contain nested structures or collections.

 [IScalar](Flowthru.Abstractions.IScalar.md)

Marker interface for types that serialize to a single primitive value —
i.e., they produce <code>"key": value</code> in JSON, not <code>"key": {...}</code> or <code>"key": [...]</code>.

 [IStructuredSerializable](Flowthru.Abstractions.IStructuredSerializable.md)

Marker interface for schema types that can be serialized to structured formats (JSON, XML).

 [ITextSerializable](Flowthru.Abstractions.ITextSerializable.md)

Marker interface for schema types that can be serialized to text-based formats (CSV, TSV).

