using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Sheets;

namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// Storage adapter for one Google Sheets table, exposed as
/// <see cref="IEnumerable{TRow}"/>. Implements <see cref="IStorageAdapter{T}"/>
/// directly (the EFCore grain, not the Medium → Format → Container composition):
/// Sheets is a typed tabular <em>store</em> — a table carries a column schema —
/// so it validates <typeparamref name="TRow"/> against the live table the way
/// EFCore validates against its model, with no serialization marker.
/// </summary>
/// <typeparam name="TRow">
/// Row schema. Sheets rows are tabular, so the schema must be flat
/// (<see cref="IFlatSchema"/>). No cell/serialization marker: the store, not a
/// format, owns interpretation.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Addressing.</strong> A catalog item is <c>(spreadsheetId, tableName)</c>
/// — a stable native-table name, resolved through the narrow
/// <see cref="ISheetsGateway"/> seam. Auth and client lifecycle live behind the
/// gateway, never in the adapter.
/// </para>
/// <para>
/// <strong>Read is schema-driven.</strong> <see cref="Load"/> matches the table's
/// columns to <typeparamref name="TRow"/>'s properties <em>by field name</em>
/// (case-insensitive, the same rule CSV/Excel use), then coerces each
/// <see cref="FieldValue"/> to the property's declared CLR type. A serial
/// <see cref="FieldKind.Number"/> becomes a <see cref="DateTime"/> when the
/// property is temporal — the schema decides, not the field's runtime kind.
/// </para>
/// <para>
/// <strong>Write is replace.</strong> <see cref="Save"/> creates the table from
/// <typeparamref name="TRow"/> if absent, then atomically replaces its data rows
/// — when Flowthru owns the table it holds the full dataset, so replace is the
/// upsert. A <c>saveFunc</c> constructor override supplies append/upsert recipes;
/// compose them on top of <see cref="DefaultSave"/>. The full pre-flight error
/// taxonomy, the smart-constructor factory, and the example are tracked
/// separately.
/// </para>
/// </remarks>
public sealed class GoogleSheetsStorageAdapter<TRow>
  : IStorageAdapter<IEnumerable<TRow>>, IHasServiceDependencies
  where TRow : notnull, IFlatSchema
{
  /// <summary>
  /// Concurrent writers permitted against one spreadsheet. A Save is a
  /// single atomic <c>batchUpdate</c> per spreadsheet, and two of them
  /// race on overlapping ranges while doubling the per-user quota draw —
  /// so writes to one spreadsheet serialize (ADR-0019).
  /// </summary>
  private const int SpreadsheetWriteCapacity = 1;

  private readonly string _spreadsheetId;
  private readonly string _tableName;
  private readonly ISheetsGateway _gateway;
  private readonly Func<ISheetsGateway, string, string, IReadOnlyList<TRow>, CancellationToken, Task>? _saveFunc;
  private readonly IReadOnlyList<ServiceDependency> _serviceDependencies;

  /// <summary>
  /// Bind the adapter to the table named <paramref name="tableName"/> in
  /// <paramref name="spreadsheetId"/>, reached through <paramref name="gateway"/>.
  /// </summary>
  /// <param name="spreadsheetId">The spreadsheet the table lives in.</param>
  /// <param name="tableName">The native table name — the catalog-item identity.</param>
  /// <param name="gateway">The seam the read/write/create operations go through.</param>
  /// <param name="saveFunc">
  /// Optional override for the write strategy. When supplied, <see cref="Save"/>
  /// invokes it instead of the default create-if-absent + atomic-replace; the
  /// gateway, spreadsheet id, table name, materialised rows, and cancellation
  /// token flow through. Compose append/upsert recipes on top of
  /// <see cref="DefaultSave"/>. When <see langword="null"/> the default replace
  /// is used.
  /// </param>
  public GoogleSheetsStorageAdapter(
    string spreadsheetId,
    string tableName,
    ISheetsGateway gateway,
    Func<ISheetsGateway, string, string, IReadOnlyList<TRow>, CancellationToken, Task>? saveFunc = null)
  {
    _spreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
    _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    _saveFunc = saveFunc;
    _serviceDependencies = new ServiceDependency[]
    {
      new ServiceDependency.External(new SheetsSpreadsheetDependency(
        _spreadsheetId, WriteCapacity: SpreadsheetWriteCapacity, ReadCapacity: int.MaxValue)),
    };
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new()
  {
    // A Save replaces the table's rows in one atomic batchUpdate (the seam
    // contract), so the transactional claim is honest. No read cursor.
    IsTransactional = true,
    CanStream = false,
    // The spreadsheet is the conflict resource: concurrent writers
    // serialize (ADR-0019), readers parallelize.
    WriteCapacity = SpreadsheetWriteCapacity,
    ReadCapacity = int.MaxValue,
  };

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _serviceDependencies;

  /// <inheritdoc/>
  public FlowIO<IEnumerable<TRow>> Load() =>
    FlowIO.LiftAsync<IEnumerable<TRow>>(async ct =>
    {
      var table = await ResolveOrThrow(ct).ConfigureAwait(false);
      var data = await _gateway.ReadRows(_spreadsheetId, table, ct).ConfigureAwait(false);
      return DecodeRows(data);
    }, source: Source("Load"));

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<TRow> data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        Source("Save"),
        new InvalidOperationException(
          "Cannot write to a read-only Google Sheets adapter. Verify "
          + "StorageTraits.CanWrite before calling Save() — typically the catalog "
          + "item was Constrain()'d.")));
    }

    return FlowIO.LiftAsync(async ct =>
    {
      var rows = data as IReadOnlyList<TRow> ?? data.ToList();
      await (_saveFunc is null
        ? DefaultSave(_gateway, _spreadsheetId, _tableName, rows, ct)
        : _saveFunc(_gateway, _spreadsheetId, _tableName, rows, ct)).ConfigureAwait(false);
      return FlowUnit.Default;
    }, source: Source("Save"));
  }

  /// <summary>
  /// Default write: create the table from <typeparamref name="TRow"/> if it does
  /// not exist, then atomically replace its data rows. Reference this when
  /// composing a custom <c>saveFunc</c> that wraps the default (e.g. a custom
  /// pre-step followed by the default replace). The encode + create-if-absent +
  /// replace are all expressed against the neutral <see cref="ISheetsGateway"/>
  /// seam, so this composes against any gateway, including the in-memory one.
  /// </summary>
  public static async Task DefaultSave(
    ISheetsGateway gateway,
    string spreadsheetId,
    string tableName,
    IReadOnlyList<TRow> rows,
    CancellationToken ct)
  {
    // Resolve once. Absent → create from TRow, so Flowthru owns the table the
    // way EFCore owns its schema. The created table is then the replace target.
    var table = await gateway.ResolveTable(spreadsheetId, tableName, ct).ConfigureAwait(false);
    if (table is null)
    {
      var schema = SheetsSchemaBuilder.BuildFromRow<TRow>();
      table = await gateway.AddTable(spreadsheetId, tableName, schema, ct).ConfigureAwait(false);
    }

    // Encode against the resolved schema's column order, so a row's fields align
    // to the table's columns left-to-right.
    var body = EncodeRows(table.Schema, rows);
    await gateway.ReplaceRows(spreadsheetId, table, body, ct).ConfigureAwait(false);
  }

  // Encode rows into the resolved schema's column order. The plan is built once
  // per Save; per column position we cache the binding that feeds it (or null to
  // emit an empty field for a column TRow has no property for).
  private static TableData EncodeRows(TableSchema schema, IReadOnlyList<TRow> rows)
  {
    var plan = PropertyMappingPlanner.Build<TRow>();
    var columns = schema.Columns;

    var bindingByColumn = new PropertyBinding?[columns.Count];
    for (int i = 0; i < columns.Count; i++)
    {
      bindingByColumn[i] = plan.TryGetByFieldName(columns[i].Name, out var binding)
        ? binding
        : null;
    }

    var encoded = new List<IReadOnlyList<FieldValue>>(rows.Count);
    foreach (var row in rows)
    {
      var fields = new FieldValue[columns.Count];
      for (int i = 0; i < columns.Count; i++)
      {
        var binding = bindingByColumn[i];
        fields[i] = binding is null
          ? FieldValue.Empty
          : FieldValueEncoder.Encode(row, binding);
      }
      encoded.Add(fields);
    }

    return new TableData(schema, encoded);
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      var table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      return table is not null;
    }, source: Source("Exists"));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      // Read-side pre-flight: spreadsheet reachable → table present → required
      // columns present → column types fit. Each gate is the actionable root
      // cause for the next, so a failure short-circuits rather than piling on
      // downstream noise.
      ResolvedTable? table;
      try
      {
        table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      }
      catch (SheetsSpreadsheetAccessException ex)
      {
        return SpreadsheetUnreachable(ex);
      }

      if (table is null)
      {
        return TableNotFound();
      }

      var shape = ValidateShape(table);
      if (shape.HasErrors) return shape;

      // Optional sample-decode: a column-type fit can still hide a value that
      // won't coerce (a malformed serial date, say). Probe sampleSize rows.
      if (sampleSize > 0)
      {
        try
        {
          var data = await _gateway.ReadRows(_spreadsheetId, table, ct).ConfigureAwait(false);
          _ = DecodeRows(Take(data, sampleSize));
        }
        catch (Exception ex)
        {
          return DeserializationFailure("sample", ex);
        }
      }

      return ValidationResult.Success();
    }, source: Source("InspectShallow"));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      ResolvedTable? table;
      try
      {
        table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      }
      catch (SheetsSpreadsheetAccessException ex)
      {
        return SpreadsheetUnreachable(ex);
      }

      if (table is null)
      {
        return TableNotFound();
      }

      // Shape gates the full-decode probe: a column-type mismatch is the root
      // cause, and a decode failure underneath one is noise.
      var shape = ValidateShape(table);
      if (shape.HasErrors) return shape;

      // Full-decode probe: every row must coerce into TRow.
      try
      {
        var data = await _gateway.ReadRows(_spreadsheetId, table, ct).ConfigureAwait(false);
        _ = DecodeRows(data);
      }
      catch (Exception ex)
      {
        return DeserializationFailure("all", ex);
      }

      return ValidationResult.Success();
    }, source: Source("InspectDeep"));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      // Write-side pre-flight. The spreadsheet must be reachable — Flowthru
      // creates tables, not spreadsheets. If the table is absent, that PASSES:
      // create-if-absent (DefaultSave) will create it from TRow via the same
      // SheetsSchemaBuilder mapping, so a Flowthru-created table always fits. If
      // the table already exists (externally created/edited), its column types
      // must be compatible with TRow.
      ResolvedTable? table;
      try
      {
        table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      }
      catch (SheetsSpreadsheetAccessException ex)
      {
        return SpreadsheetUnreachable(ex);
      }

      if (table is null)
      {
        // Absent table is fine on the write side — Save will create it.
        return ValidationResult.Success();
      }

      return ValidateShape(table);
    }, source: Source("InspectTarget"));

  // ── Internals ─────────────────────────────────────────────────────────────

  // FTGS pre-flight provenance codes. Embedded in the failure message/details
  // (the ValidationError taxonomy stays the EFCore-consistent ValidationErrorType
  // set; these tag the specific Sheets failure for grep-ability in build output).
  private const string FtgsSpreadsheetNotFound = "FTGS1601";
  private const string FtgsSpreadsheetAccessDenied = "FTGS1602";
  private const string FtgsTableNotFound = "FTGS1603";
  private const string FtgsMissingColumn = "FTGS1604";
  private const string FtgsColumnTypeMismatch = "FTGS1605";
  private const string FtgsDeserialization = "FTGS1606";

  // Shape validation, shared by every Inspect path: required columns present
  // (data ⊇ schema, by name) then per-column type fit against TRow's expected
  // ColumnType — the SAME SheetsSchemaBuilder mapping create-if-absent uses, so
  // a Flowthru-created table always passes; a type mismatch only fires for an
  // externally-created or hand-edited table, the intended fail-fast catch.
  private static ValidationResult ValidateShape(ResolvedTable table)
  {
    var plan = PropertyMappingPlanner.Build<TRow>();

    // Live column lookup by name (case-insensitive, the load/save rule).
    var liveByName = new Dictionary<string, TableColumn>(StringComparer.OrdinalIgnoreCase);
    foreach (var column in table.Schema.Columns)
    {
      liveByName[column.Name] = column;
    }

    // 1) Required-column presence. Extra live columns are tolerated.
    var missing = plan.RequiredFieldNames
      .Where(name => !liveByName.ContainsKey(name))
      .ToList();
    if (missing.Count > 0)
    {
      return ValidationResult.Failure(
        catalogKey: table.Name,
        errorType: ValidationErrorType.SchemaMismatch,
        message: $"[{FtgsMissingColumn}] Table '{table.Name}' is missing required "
          + $"column(s) for schema '{typeof(TRow).Name}'.",
        details: $"{FtgsMissingColumn}: absent column(s) "
          + $"{string.Join(", ", missing.Select(m => $"'{m}'"))}.");
    }

    // 2) Column-type fit. For every binding whose column is present, the live
    //    neutral type must equal the type TRow's column would be created with.
    foreach (var binding in plan.Bindings)
    {
      if (!liveByName.TryGetValue(binding.FieldName, out var liveColumn))
      {
        // Absent + nullable: optional, already cleared by the presence gate.
        continue;
      }

      var expected = SheetsSchemaBuilder.ColumnTypeFor(binding, typeof(TRow));
      if (liveColumn.Type != expected)
      {
        return ValidationResult.Failure(
          catalogKey: table.Name,
          errorType: ValidationErrorType.SchemaMismatch,
          message: $"[{FtgsColumnTypeMismatch}] Column '{binding.FieldName}' in table "
            + $"'{table.Name}' has type {liveColumn.Type}, but schema "
            + $"'{typeof(TRow).Name}' expects {expected}.",
          details: $"{FtgsColumnTypeMismatch}: column '{binding.FieldName}' expected "
            + $"{expected}, found {liveColumn.Type}. A Flowthru-created table always "
            + "matches; a mismatch means the table was created or edited outside "
            + "Flowthru.");
      }
    }

    return ValidationResult.Success();
  }

  private ValidationResult SpreadsheetUnreachable(SheetsSpreadsheetAccessException ex)
  {
    var (errorType, code) = ex.Failure switch
    {
      SheetsSpreadsheetAccessFailure.AccessDenied =>
        (ValidationErrorType.WriteAccessDenied, FtgsSpreadsheetAccessDenied),
      _ => (ValidationErrorType.NotFound, FtgsSpreadsheetNotFound),
    };

    return ValidationResult.Failure(
      catalogKey: _tableName,
      errorType: errorType,
      message: $"[{code}] {ex.Message}",
      details: $"{code}: spreadsheet '{_spreadsheetId}' is unreachable "
        + $"({ex.Failure}). The spreadsheet must exist and be accessible to the "
        + "configured credentials; Flowthru creates tables, not spreadsheets.");
  }

  private ValidationResult DeserializationFailure(string scope, Exception ex) =>
    ValidationResult.Failure(
      catalogKey: _tableName,
      errorType: ValidationErrorType.DeserializationError,
      message: $"[{FtgsDeserialization}] Failed to decode {scope} rows from table "
        + $"'{_tableName}' into '{typeof(TRow).Name}'.",
      details: $"{FtgsDeserialization}: {ex.Message}");

  // Shallow copy of the first n rows under the same schema, for the sample probe.
  private static TableData Take(TableData data, int n) =>
    new(data.Schema, data.Rows.Take(n).ToList());

  private async Task<ResolvedTable> ResolveOrThrow(CancellationToken ct)
  {
    var table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
    return table ?? throw new InvalidOperationException(
      $"Table '{_tableName}' not found in spreadsheet '{_spreadsheetId}'.");
  }

  // Decode every row by matching the table's columns to TRow properties by name,
  // ordinal-ignore-case. Column order is irrelevant — position never carries
  // identity here, only the column's name does.
  private static List<TRow> DecodeRows(TableData data)
  {
    var plan = PropertyMappingPlanner.Build<TRow>();
    var columns = data.Schema.Columns;

    // Precompute, per column position, the binding it feeds (or null to skip an
    // unmapped column). Built once per Load, not per row.
    var bindingByColumn = new PropertyBinding?[columns.Count];
    for (int i = 0; i < columns.Count; i++)
    {
      bindingByColumn[i] = plan.TryGetByFieldName(columns[i].Name, out var binding)
        ? binding
        : null;
    }

    var rows = new List<TRow>(data.Rows.Count);
    foreach (var fields in data.Rows)
    {
      var row = SchemaActivator.CreateInstance<TRow>();
      var width = Math.Min(fields.Count, bindingByColumn.Length);
      for (int i = 0; i < width; i++)
      {
        var binding = bindingByColumn[i];
        if (binding is null)
        {
          continue;
        }

        var decoded = FieldValueDecoder.Decode(fields[i], binding);
        if (decoded is null)
        {
          // Empty/null field: leave the property at its default.
          continue;
        }

        binding.Property.SetValue(row, decoded);
      }
      rows.Add(row);
    }

    return rows;
  }

  private ValidationResult TableNotFound() =>
    ValidationResult.Failure(
      catalogKey: _tableName,
      errorType: ValidationErrorType.NotFound,
      message: $"[{FtgsTableNotFound}] Table '{_tableName}' not found in spreadsheet "
        + $"'{_spreadsheetId}'.",
      details: $"{FtgsTableNotFound}: the spreadsheet is reachable but has no table "
        + $"named '{_tableName}'. Resolve the table name, or create the table first.");

  private string Source(string operation) =>
    $"GoogleSheetsStorageAdapter.{operation}[{typeof(TRow).Name}]";
}
