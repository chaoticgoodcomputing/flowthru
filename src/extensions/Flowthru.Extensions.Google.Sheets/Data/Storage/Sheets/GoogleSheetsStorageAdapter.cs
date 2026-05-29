using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

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
  : IStorageAdapter<IEnumerable<TRow>>
  where TRow : notnull, IFlatSchema
{
  private readonly string _spreadsheetId;
  private readonly string _tableName;
  private readonly ISheetsGateway _gateway;
  private readonly Func<ISheetsGateway, string, string, IReadOnlyList<TRow>, CancellationToken, Task>? _saveFunc;

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
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new()
  {
    // A Save replaces the table's rows in one atomic batchUpdate (the seam
    // contract), so the transactional claim is honest. No read cursor.
    IsTransactional = true,
    CanStream = false,
  };

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
      var table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      if (table is null)
      {
        return TableNotFound();
      }

      // Field-presence check (data ⊇ schema): every required property must have
      // a matching column by name. Extra columns are tolerated. Column-type
      // validation is the fuller pre-flight pass's job, not this one.
      var plan = PropertyMappingPlanner.Build<TRow>();
      var columnNames = new HashSet<string>(
        table.Schema.Columns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

      var missing = plan.RequiredFieldNames
        .Where(name => !columnNames.Contains(name))
        .ToList();

      if (missing.Count > 0)
      {
        return ValidationResult.Failure(
          catalogKey: _tableName,
          errorType: ValidationErrorType.SchemaMismatch,
          message: $"Table '{_tableName}' is missing required column(s) for schema "
            + $"'{typeof(TRow).Name}'.",
          details: $"Absent: {string.Join(", ", missing.Select(m => $"'{m}'"))}.");
      }

      return ValidationResult.Success();
    }, source: Source("InspectShallow"));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      var table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      if (table is null)
      {
        return TableNotFound();
      }

      // Full-decode probe: every row must coerce into TRow. The fuller
      // column-type pre-flight pass deepens this later.
      try
      {
        var data = await _gateway.ReadRows(_spreadsheetId, table, ct).ConfigureAwait(false);
        _ = DecodeRows(data);
      }
      catch (Exception ex)
      {
        return ValidationResult.FromException(_tableName, ex);
      }

      return ValidationResult.Success();
    }, source: Source("InspectDeep"));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      var table = await _gateway.ResolveTable(_spreadsheetId, _tableName, ct).ConfigureAwait(false);
      // Minimal: the table is reachable. Rich write-target validation (create-
      // if-absent, column-type fit) is the fuller pre-flight pass's job.
      return table is null ? TableNotFound() : ValidationResult.Success();
    }, source: Source("InspectTarget"));

  // ── Internals ─────────────────────────────────────────────────────────────

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
      message: $"Table '{_tableName}' not found in spreadsheet '{_spreadsheetId}'.",
      details: "Resolve the table name and spreadsheet id, or create the table first.");

  private string Source(string operation) =>
    $"GoogleSheetsStorageAdapter.{operation}[{typeof(TRow).Name}]";
}
