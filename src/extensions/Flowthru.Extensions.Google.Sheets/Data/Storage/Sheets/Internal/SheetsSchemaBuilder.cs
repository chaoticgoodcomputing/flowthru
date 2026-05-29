using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Sheets.Internal;

/// <summary>
/// Derives a neutral <see cref="TableSchema"/> from a row schema for
/// create-if-absent. One column per binding, named by the binding's field name,
/// typed by mapping the binding's declared CLR type to a neutral
/// <see cref="ColumnType"/>. Property declaration order is preserved — table
/// column position is significant.
/// </summary>
/// <remarks>
/// This is the write-side complement to the read path's name-based column
/// matching: when Flowthru owns the table, it creates the table from the row
/// schema, so the column types come straight from the schema rather than being
/// read back from a live table. A <c>byte[]</c> property has no faithful table
/// column type, so it is rejected here with an actionable error rather than
/// silently stringified.
/// </remarks>
internal static class SheetsSchemaBuilder
{
  /// <summary>Build a neutral schema from the public properties of <typeparamref name="TRow"/>.</summary>
  public static TableSchema BuildFromRow<TRow>()
  {
    var plan = PropertyMappingPlanner.Build<TRow>();
    var columns = new List<TableColumn>(plan.Bindings.Count);
    foreach (var binding in plan.Bindings)
    {
      columns.Add(new TableColumn(binding.FieldName, ColumnTypeFor(binding, typeof(TRow))));
    }

    return new TableSchema(columns);
  }

  private static ColumnType ColumnTypeFor(PropertyBinding binding, Type rowType)
  {
    // An IScalar surfaces as its backing primitive's column type, so the created
    // column matches what FieldValueEncoder actually writes (a Number-backed
    // NewType writes a Number field, not text).
    var type = binding.Kind == PropertyKind.IScalar
      ? binding.IScalar!.BackingType
      : binding.EffectiveType;

    // byte[] is the one classified-Primitive type with no table column type —
    // reject it loudly. Other blobs are unreachable behind IFlatSchema.
    if (type.IsArray && type.GetElementType() == typeof(byte))
    {
      throw new SchemaMismatchException(
        $"Property '{binding.FieldName}' on '{rowType.Name}' is a byte[] blob, which "
        + "has no Google Sheets column type. Drop the column or serialize it to text "
        + "in a projecting Step before writing.");
    }

    // Temporal shapes map one-to-one onto the dedicated column types.
    if (type == typeof(DateTime)) return ColumnType.DateTime;
    if (type == typeof(DateOnly)) return ColumnType.Date;
    if (type == typeof(TimeOnly)) return ColumnType.Time;

    if (type == typeof(bool)) return ColumnType.Bool;

    if (IsNumeric(type)) return ColumnType.Number;

    // Everything else — string, enum (serialized form), a text-backed IScalar,
    // Guid, TimeSpan, DateTimeOffset, Half/Int128/UInt128 — lands on a text
    // column. The schema-driven decoder coerces it back on read.
    return ColumnType.Text;
  }

  private static bool IsNumeric(Type type) =>
    type == typeof(int)
    || type == typeof(long)
    || type == typeof(short)
    || type == typeof(byte)
    || type == typeof(sbyte)
    || type == typeof(uint)
    || type == typeof(ulong)
    || type == typeof(ushort)
    || type == typeof(double)
    || type == typeof(float)
    || type == typeof(decimal);
}
