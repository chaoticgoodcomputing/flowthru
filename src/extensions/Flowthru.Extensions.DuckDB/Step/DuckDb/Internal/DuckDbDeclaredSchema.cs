using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// Outcome of projecting a declared record schema into DuckDB-checkable
/// columns: either the column list, or a human-readable problem naming
/// the property that can't be modelled. A value type over exceptions so
/// wire-up sites can choose to throw (the step's output schema — a
/// wiring bug at the author's call site) while pre-flight sites surface
/// the same problem as an aggregated error value (an input schema — one
/// finding among every other pre-flight finding).
/// </summary>
internal sealed record DuckDbSchemaProjection
{
  private DuckDbSchemaProjection(IReadOnlyList<DuckDbExpectedColumn>? columns, string? problem)
  {
    Columns = columns;
    Problem = problem;
  }

  /// <summary>The projected columns; <c>null</c> when <see cref="Problem"/> is set.</summary>
  public IReadOnlyList<DuckDbExpectedColumn>? Columns { get; }

  /// <summary>Why the schema can't be modelled; <c>null</c> on success.</summary>
  public string? Problem { get; }

  public static DuckDbSchemaProjection Ok(IReadOnlyList<DuckDbExpectedColumn> columns) =>
    new(columns, null);

  public static DuckDbSchemaProjection Failed(string problem) => new(null, problem);
}

/// <summary>
/// Projects a declared record schema (a <c>[FlowthruSchema]</c> row
/// type) into the column set DuckDB schema checks operate on — property
/// names (honouring <c>[SerializedLabel]</c>) and round-trip CLR types,
/// with enums unwrapped to their underlying integer type (matching how
/// Parquet stores them). The single source of truth for both sides of a
/// transform: the step projects its output schema here (throwing at
/// wire-up on failure), and the hermetic pre-flight check projects each
/// input schema here (surfacing failures as pre-flight errors).
/// </summary>
internal static class DuckDbDeclaredSchema
{
  /// <summary>
  /// Project <typeparamref name="TRow"/>'s declared schema. Returns a
  /// failed projection (never throws) when a property is a kind the
  /// DuckDB checks don't cover or a CLR type the
  /// <see cref="DuckDbTypeMap"/> doesn't know.
  /// </summary>
  public static DuckDbSchemaProjection Project<TRow>()
    where TRow : notnull
  {
    var plan = PropertyMappingPlanner.Build<TRow>();
    var columns = new List<DuckDbExpectedColumn>(plan.Bindings.Count);

    foreach (var binding in plan.Bindings)
    {
      Type clrType;
      switch (binding.Kind)
      {
        case PropertyKind.Primitive:
          clrType = binding.EffectiveType;
          break;
        case PropertyKind.Enum:
          clrType = Enum.GetUnderlyingType(binding.EffectiveType);
          break;
        default:
          return DuckDbSchemaProjection.Failed(
            $"Schema property '{typeof(TRow).Name}.{binding.Property.Name}' is "
            + $"classified as {binding.Kind}, which the DuckDB transform's schema "
            + "checks don't support yet — they cover primitive and enum columns. "
            + "Widen the property to a primitive, or transform into an intermediate "
            + "schema and map it in an ordinary step."
          );
      }

      if (!DuckDbTypeMap.IsSupported(clrType))
      {
        return DuckDbSchemaProjection.Failed(
          $"Schema property '{typeof(TRow).Name}.{binding.Property.Name}' has type "
          + $"{clrType.Name}, which the DuckDB transform's schema checks don't "
          + "know how to model. Use a supported primitive type, or map the data in "
          + "an ordinary step."
        );
      }

      columns.Add(new DuckDbExpectedColumn(binding.FieldName, clrType, binding.IsNullable));
    }

    return DuckDbSchemaProjection.Ok(columns);
  }
}
