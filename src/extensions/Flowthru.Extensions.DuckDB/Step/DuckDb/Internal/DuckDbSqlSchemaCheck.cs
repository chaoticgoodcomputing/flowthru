using DuckDB.NET.Data;
using Flowthru.Data.Storage;
using Flowthru.Validation.PreFlight.DuckDb;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// The hermetic SQL schema check behind every design-time and pre-flight
/// validation surface for DuckDB transforms: build <em>empty</em>
/// in-memory tables from the <em>declared</em> input record schemas
/// (named per the step's relation bindings), <c>DESCRIBE</c> the
/// transform SQL against them (binding without executing), and verify
/// the described result schema against the declared output schema.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Hermetic — nothing outside the process is reached.</strong>
/// The embedded engine opens an in-memory database, the tables hold zero
/// rows built purely from declared metadata, and <c>DESCRIBE</c> only
/// binds the query. No socket, no data file, no external database — the
/// only load is the DuckDB native library that ships inside the
/// application's own deployment, the same class of operation as the CLR
/// loading an assembly (see the carve-out documented on
/// <see cref="Flowthru.Flow.ValidationDepth.Hermetic"/>).
/// </para>
/// <para>
/// The SQL text is trimmed with the same <see cref="DuckDbSql"/>
/// discipline the runtime engine uses, and the result schema is verified
/// by the same <see cref="DuckDbSchemaVerifier"/>/<see cref="DuckDbTypeMap"/>
/// pair — what passes here is exactly what the engine would accept at
/// execution time against real files with the same schemas.
/// </para>
/// </remarks>
internal static class DuckDbSqlSchemaCheck
{
  /// <summary>
  /// Run the check for one transform. Returns every finding as a typed
  /// error value (empty list = pass); never throws for validation
  /// outcomes.
  /// </summary>
  public static async Task<IReadOnlyList<DuckDbPreFlightError>> RunAsync(
    IDuckDbTransformDescriptor transform,
    CancellationToken cancellationToken
  )
  {
    // Project every declared input schema first. If any input can't be
    // modelled, report all such inputs and stop — DESCRIBE against a
    // partial table set would produce spurious "table does not exist"
    // binder errors masking the real cause.
    var inputProblems = new List<DuckDbPreFlightError>();
    var tables = new List<(string RelationName, IReadOnlyList<DuckDbExpectedColumn> Columns)>();
    foreach (var relation in transform.InputRelations)
    {
      if (relation.DeclaredSchema.Columns is { } columns)
      {
        tables.Add((relation.RelationName, columns));
      }
      else
      {
        inputProblems.Add(new DuckDbPreFlightError.InputSchemaUnsupported(
          StepLabel: transform.Label,
          RelationName: relation.RelationName,
          ItemLabel: relation.Item.Label,
          Detail: relation.DeclaredSchema.Problem!
        ));
      }
    }
    if (inputProblems.Count > 0) return inputProblems;

    var sql = DuckDbSql.TrimTerminator(transform.Sql);

    using var connection = new DuckDBConnection("Data Source=:memory:");
    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    // Empty tables from declared metadata — zero rows, so nothing later
    // can accidentally read data. Column DDL types are the canonical
    // engine type each declared CLR column materialises as, from the
    // same type map the verifier checks against.
    foreach (var (relationName, columns) in tables)
    {
      var columnDdl = string.Join(", ", columns.Select(c =>
        $"{DuckDbSql.QuoteIdentifier(c.Name)} {DuckDbTypeMap.CanonicalDdlType(c.ClrType)}"
      ));
      using var create = connection.CreateCommand();
      create.CommandText =
        $"CREATE TABLE {DuckDbSql.QuoteIdentifier(relationName)} ({columnDdl})";
      await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    // DESCRIBE binds the query without executing it — one metadata row
    // per result column. A parser/binder rejection is the SQL
    // disagreeing with the declared input contracts.
    IReadOnlyList<(string Name, string DuckDbType)> resultColumns;
    try
    {
      using var describe = connection.CreateCommand();
      describe.CommandText = $"DESCRIBE {sql}";
      var columns = new List<(string, string)>();
      using var reader = await describe.ExecuteReaderAsync(cancellationToken)
        .ConfigureAwait(false);
      while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
      {
        columns.Add((reader.GetString(0), reader.GetString(1)));
      }
      resultColumns = columns;
    }
    catch (DuckDBException dde)
    {
      return new DuckDbPreFlightError[]
      {
        new DuckDbPreFlightError.SqlPreparationFailed(
          StepLabel: transform.Label,
          RelationBindings: DescribeBindings(transform.InputRelations),
          Detail: dde.Message
        ),
      };
    }

    var mismatch = DuckDbSchemaVerifier.Verify(transform.ExpectedOutputColumns, resultColumns);
    if (mismatch is not null)
    {
      return new DuckDbPreFlightError[]
      {
        new DuckDbPreFlightError.ResultSchemaMismatch(
          StepLabel: transform.Label,
          OutputItemLabel: transform.OutputItem.Label,
          OutputSchemaName: transform.OutputSchemaName,
          Mismatch: mismatch
        ),
      };
    }

    return Array.Empty<DuckDbPreFlightError>();
  }

  /// <summary>
  /// Render a typed pre-flight error into the
  /// <see cref="ValidationResult"/> shape the design-time surfaces
  /// (<c>DuckDbTransformStep.Validate()</c>,
  /// <c>BuiltFlow.ValidateDuckDbTransforms()</c>) return — same message
  /// text, with the FTDDB3xxx diagnostic code carried in
  /// <see cref="ValidationError.Details"/>.
  /// </summary>
  public static ValidationError ToValidationError(string stepLabel, DuckDbPreFlightError error) =>
    new(
      catalogKey: stepLabel,
      errorType: error switch
      {
        DuckDbPreFlightError.InputSchemaUnsupported => ValidationErrorType.InspectionFailure,
        _ => ValidationErrorType.SchemaMismatch,
      },
      message: error.Message,
      details: error.DiagnosticCode
    );

  private static IReadOnlyList<string> DescribeBindings(
    IReadOnlyList<DuckDbInputRelation> relations
  ) =>
    relations
      .Select(r => $"relation '{r.RelationName}' (item '{r.Item.Label}', schema {r.RowSchemaName})")
      .ToList();
}
