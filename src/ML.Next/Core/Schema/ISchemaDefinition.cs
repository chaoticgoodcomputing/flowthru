namespace ML.Next.Core.Schema;

/// <summary>
/// Marker interface for compile-time schema definitions.
/// Schemas are phantom types that exist only at compile-time to track
/// the structure and types of columns through transformation pipelines.
/// </summary>
/// <remarks>
/// Schema definitions should be implemented as interfaces with properties
/// representing columns. The actual column definitions use ColumnName&lt;TType&gt;
/// to provide type-level information about each column.
/// </remarks>
public interface ISchemaDefinition { }
