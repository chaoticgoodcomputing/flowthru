using Flowthru.Data.Catalog;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// Non-generic view of a <see cref="DuckDbTransformStep{TOut}"/> — the
/// data the hermetic SQL schema check needs, without knowing
/// <c>TOut</c>. The pre-flight hook pattern-matches flow steps against
/// this interface instead of reflecting over the open generic (the
/// Python extension's approach, needed there only because its hook and
/// step live behind a public-surface boundary; here both sides share
/// the assembly).
/// </summary>
internal interface IDuckDbTransformDescriptor
{
  /// <summary>The step's label, for diagnostics.</summary>
  string Label { get; }

  /// <summary>The transform body — the SQL to bind.</summary>
  string Sql { get; }

  /// <summary>The input relation bindings, in wire-up order.</summary>
  IReadOnlyList<DuckDbInputRelation> InputRelations { get; }

  /// <summary>The output item the result is written to, for diagnostics.</summary>
  IItem OutputItem { get; }

  /// <summary>The declared output record type's name, for diagnostics.</summary>
  string OutputSchemaName { get; }

  /// <summary>
  /// The output item's declared schema, projected into the columns the
  /// SQL's described result is verified against.
  /// </summary>
  IReadOnlyList<DuckDbExpectedColumn> ExpectedOutputColumns { get; }
}
