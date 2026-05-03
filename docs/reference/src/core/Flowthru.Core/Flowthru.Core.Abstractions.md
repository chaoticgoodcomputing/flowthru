# <a id="Flowthru_Core_Abstractions"></a> Namespace Flowthru.Core.Abstractions

### Classes

 [FlowthruColumnAttribute](Flowthru.Core.Abstractions.FlowthruColumnAttribute.md)

Marks a schema property for automatic NewType generation via source generator.
The source generator will emit a <code>readonly record struct</code> NewType implementing
<xref href="Flowthru.Core.Abstractions.IScalar" data-throw-if-not-resolved="false"></xref> using the provided backing type, placed in a <code>Types</code> namespace
sibling to the schema.

 [FlowthruSchemaAttribute](Flowthru.Core.Abstractions.FlowthruSchemaAttribute.md)

Marks a schema type for automatic interface generation. The source generator
will analyze the type's properties and emit the appropriate marker interfaces:
<ul><li><xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Core.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> based on property types</li><li><xref href="Flowthru.Core.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> for flat schemas (CSV/TSV compatible)</li><li><xref href="Flowthru.Core.Abstractions.IBinarySerializable" data-throw-if-not-resolved="false"></xref> for flat schemas (Parquet compatible)</li><li><xref href="Flowthru.Core.Abstractions.IStructuredSerializable" data-throw-if-not-resolved="false"></xref> for all schemas (JSON/XML compatible)</li></ul>

 [SerializedEnumAttribute](Flowthru.Core.Abstractions.SerializedEnumAttribute.md)

Specifies the serialized string value for an enum member when written to or read from storage.

 [SerializedLabelAttribute](Flowthru.Core.Abstractions.SerializedLabelAttribute.md)

Specifies the external field name for a property when serialized to/from storage.

### Interfaces

 [IBinarySerializable](Flowthru.Core.Abstractions.IBinarySerializable.md)

Marker interface for schema types that can be serialized to columnar binary formats (Parquet, Avro).

 [IFlatSchema](Flowthru.Core.Abstractions.IFlatSchema.md)

Marker interface for schema types with flat (non-nested) structure.

 [IFlatSerializable](Flowthru.Core.Abstractions.IFlatSerializable.md)

Marker interface for schema types that contain only flat, primitive data.

 [INestedSchema](Flowthru.Core.Abstractions.INestedSchema.md)

Marker interface for schema types with nested structure (collections or nested objects).

 [INestedSerializable](Flowthru.Core.Abstractions.INestedSerializable.md)

Marker interface for schema types that contain nested structures or collections.

 [IScalar](Flowthru.Core.Abstractions.IScalar.md)

Marker interface for types that serialize to a single primitive value —
i.e., they produce <code>"key": value</code> in JSON, not <code>"key": {...}</code> or <code>"key": [...]</code>.

 [IStructuredSerializable](Flowthru.Core.Abstractions.IStructuredSerializable.md)

Marker interface for schema types that can be serialized to structured formats (JSON, XML).

 [ITextSerializable](Flowthru.Core.Abstractions.ITextSerializable.md)

Marker interface for schema types that can be serialized to text-based formats (CSV, TSV).

