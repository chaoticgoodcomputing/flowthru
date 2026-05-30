using Google.Apis.Sheets.v4.Data;

namespace Flowthru.Data.Storage.Sheets.Internal;

/// <summary>
/// Pure, stateless translation between Flowthru's neutral tabular vocabulary
/// and the Google Sheets SDK types. This is the only place in the extension
/// where the neutral schema/rows meet <c>Google.Apis.Sheets.v4.Data</c> —
/// factored out so the mapping (column-type strings, serial-date conversion,
/// the column-index-0 coalesce) is unit-testable without a live
/// <c>SheetsService</c>.
/// </summary>
internal static class SheetsTranslator
{
  /// <summary>
  /// The serial-date epoch Google Sheets uses: a cell value of <c>0</c> is
  /// 1899-12-30, and the integer part counts whole days, the fractional part
  /// the time of day. (Sheets reproduces the historical Lotus 1-2-3 epoch.)
  /// </summary>
  internal static readonly DateTime SerialEpoch = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

  // ── Field masks ────────────────────────────────────────────────────────

  /// <summary>
  /// Fields mask for the write request: only the value and the number format
  /// are authored, so user-applied formatting elsewhere on a cell would be
  /// untouched were the range to overlap (it does not — only the data region
  /// is replaced).
  /// </summary>
  internal const string WriteFieldsMask = "userEnteredValue,userEnteredFormat.numberFormat";

  /// <summary>
  /// Fields mask for the clear request: <c>*</c> wipes every authored field of
  /// the prior data region so no stale value, format, or note survives into
  /// the replaced rows.
  /// </summary>
  internal const string ClearFieldsMask = "*";

  // ── Column-type ↔ Google string (the verified tokens) ───────────────────

  /// <summary>
  /// Map a neutral <see cref="ColumnType"/> to the Google Sheets column-type
  /// string. <c>TEXT</c> / <c>DOUBLE</c> / <c>DATE_TIME</c> are verified to
  /// round-trip verbatim (spike #93); the rest map conservatively to Sheets'
  /// nearest native column type.
  /// </summary>
  internal static string ToColumnTypeString(ColumnType type) => type switch
  {
    ColumnType.Text => "TEXT",
    ColumnType.Number => "DOUBLE",
    ColumnType.Bool => "CHECKBOX",
    ColumnType.DateTime => "DATE_TIME",
    ColumnType.Date => "DATE",
    ColumnType.Time => "TIME",
    _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown column type."),
  };

  /// <summary>
  /// Map a Google Sheets column-type string back to a neutral
  /// <see cref="ColumnType"/>. An unrecognised or missing type falls back to
  /// <see cref="ColumnType.Text"/> — the values API surfaces it as raw text,
  /// and the schema-driven adapter does the coercion.
  /// </summary>
  internal static ColumnType FromColumnTypeString(string? columnType) => columnType switch
  {
    "DOUBLE" => ColumnType.Number,
    "CHECKBOX" => ColumnType.Bool,
    "DATE_TIME" => ColumnType.DateTime,
    "DATE" => ColumnType.Date,
    "TIME" => ColumnType.Time,
    _ => ColumnType.Text,
  };

  // ── Serial-date conversion (pure, both directions) ──────────────────────

  /// <summary>Convert a CLR <see cref="DateTime"/> to a Sheets serial number.</summary>
  internal static double ToSerial(DateTime value) =>
    (value - SerialEpoch).TotalDays;

  /// <summary>Convert a Sheets serial number back to a CLR <see cref="DateTime"/>.</summary>
  internal static DateTime FromSerial(double serial) =>
    SerialEpoch.AddDays(serial);

  /// <summary>Map a temporal kind to the Sheets <c>numberFormat</c> type token.</summary>
  internal static string NumberFormatType(TemporalKind kind) => kind switch
  {
    TemporalKind.Date => "DATE",
    TemporalKind.DateTime => "DATE_TIME",
    TemporalKind.Time => "TIME",
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown temporal kind."),
  };

  // ── Neutral schema ↔ Google Table ───────────────────────────────────────

  /// <summary>
  /// Translate a neutral <see cref="TableSchema"/> into the Google
  /// <see cref="Table"/> body for an <c>addTable</c> request. The table is
  /// anchored at the tab origin and sized one header row plus
  /// <paramref name="initialDataRows"/> data rows wide enough for the schema's
  /// columns. Each column carries its index, name, and the mapped column-type
  /// string.
  /// </summary>
  /// <param name="name">The table name (its catalog-item identity).</param>
  /// <param name="schema">The neutral schema to realise as native columns.</param>
  /// <param name="sheetId">The tab the table is created on.</param>
  /// <param name="initialDataRows">
  /// Data-row count to size the initial range to (the header occupies one
  /// further row). Sheets grows the table on subsequent writes; this is just
  /// the floor.
  /// </param>
  internal static Table ToTable(string name, TableSchema schema, int sheetId, int initialDataRows = 1)
  {
    var columns = new List<TableColumnProperties>(schema.ColumnCount);
    for (var i = 0; i < schema.ColumnCount; i++)
    {
      var column = schema.Columns[i];
      columns.Add(new TableColumnProperties
      {
        ColumnIndex = i,
        ColumnName = column.Name,
        ColumnType = ToColumnTypeString(column.Type),
      });
    }

    return new Table
    {
      Name = name,
      Range = new GridRange
      {
        SheetId = sheetId,
        StartRowIndex = 0,
        EndRowIndex = 1 + Math.Max(initialDataRows, 1),
        StartColumnIndex = 0,
        EndColumnIndex = schema.ColumnCount,
      },
      ColumnProperties = columns,
    };
  }

  /// <summary>
  /// Translate a Google <see cref="Table"/> (from <c>spreadsheets.get</c>) into
  /// a neutral <see cref="ResolvedTable"/>. Columns are ordered by
  /// <c>ColumnIndex</c>, coalescing a null index to <c>0</c> — the API omits
  /// <c>columnIndex</c> for column 0 (proto3 zero-omission, verified in spike
  /// #93). Returns <see langword="null"/> if the table carries no range
  /// (nothing addressable).
  /// </summary>
  internal static ResolvedTable? ToResolvedTable(Table? table)
  {
    if (table?.Range is null) return null;

    var ordered = (table.ColumnProperties ?? new List<TableColumnProperties>())
      .OrderBy(c => c.ColumnIndex ?? 0)
      .Select(c => new TableColumn(
        Name: c.ColumnName ?? string.Empty,
        Type: FromColumnTypeString(c.ColumnType)))
      .ToList();

    var range = table.Range;
    return new ResolvedTable(
      Name: table.Name ?? string.Empty,
      Schema: new TableSchema(ordered),
      Range: new TableRange(
        SheetId: range.SheetId ?? 0,
        StartRowIndex: range.StartRowIndex ?? 0,
        EndRowIndex: range.EndRowIndex ?? 0,
        StartColumnIndex: range.StartColumnIndex ?? 0,
        EndColumnIndex: range.EndColumnIndex ?? 0));
  }

  // ── Neutral FieldValue → Google CellData ─────────────────────────────────

  /// <summary>
  /// Translate one neutral <see cref="FieldValue"/> to a Google
  /// <see cref="CellData"/>. <see cref="FieldKind.Empty"/> becomes a
  /// <see cref="CellData"/> with no value (which, under
  /// <see cref="WriteFieldsMask"/>, blanks the cell).
  /// </summary>
  internal static CellData ToCellData(FieldValue field) => field.Kind switch
  {
    FieldKind.Number => new CellData
    {
      UserEnteredValue = new ExtendedValue { NumberValue = field.NumberValue },
    },
    FieldKind.Bool => new CellData
    {
      UserEnteredValue = new ExtendedValue { BoolValue = field.BoolValue },
    },
    FieldKind.Text => new CellData
    {
      UserEnteredValue = new ExtendedValue { StringValue = field.TextValue },
    },
    FieldKind.Temporal => new CellData
    {
      UserEnteredValue = new ExtendedValue { NumberValue = ToSerial(field.TemporalValue) },
      UserEnteredFormat = new CellFormat
      {
        NumberFormat = new NumberFormat { Type = NumberFormatType(field.TemporalKind) },
      },
    },
    // Empty: a CellData with a null UserEnteredValue clears the cell under the
    // write mask.
    _ => new CellData(),
  };

  /// <summary>Translate a neutral row into Google <see cref="RowData"/>, padded to <paramref name="width"/>.</summary>
  internal static RowData ToRowData(IReadOnlyList<FieldValue> row, int width)
  {
    var values = new List<CellData>(width);
    for (var c = 0; c < width; c++)
    {
      values.Add(ToCellData(c < row.Count ? row[c] : FieldValue.Empty));
    }
    return new RowData { Values = values };
  }

  // ── Google read result → neutral FieldValue ──────────────────────────────

  /// <summary>
  /// Translate one raw value from <c>spreadsheets.values.get</c> (requested
  /// with <c>UNFORMATTED_VALUE</c> + <c>SERIAL_NUMBER</c>) to a neutral
  /// <see cref="FieldValue"/>. The values API returns <see cref="double"/>,
  /// <see cref="bool"/>, or <see cref="string"/>; a serial date arrives as a
  /// <see cref="double"/> and is surfaced as <see cref="FieldKind.Number"/> —
  /// coercion to a temporal value is the schema-driven adapter's job, not the
  /// gateway's.
  /// </summary>
  internal static FieldValue FromRawValue(object? raw) => raw switch
  {
    null => FieldValue.Empty,
    bool b => FieldValue.Bool(b),
    double d => FieldValue.Number(d),
    // Json.NET (the Sheets client's serializer) surfaces unformatted numbers
    // as long/int when they have no fractional part.
    long l => FieldValue.Number(l),
    int i => FieldValue.Number(i),
    decimal m => FieldValue.Number((double)m),
    string s when s.Length == 0 => FieldValue.Empty,
    string s => FieldValue.Text(s),
    _ => FieldValue.Text(raw.ToString() ?? string.Empty),
  };

  /// <summary>
  /// Translate a <see cref="ValueRange"/> (from <c>values.get</c> over a
  /// table's data range) into neutral rows under <paramref name="schema"/>. A
  /// null or absent <c>Values</c> collection yields an empty body.
  /// </summary>
  internal static TableData FromValueRange(TableSchema schema, ValueRange? range)
  {
    var rawRows = range?.Values;
    if (rawRows is null || rawRows.Count == 0) return TableData.Empty(schema);

    var rows = new List<IReadOnlyList<FieldValue>>(rawRows.Count);
    foreach (var rawRow in rawRows)
    {
      if (rawRow is null)
      {
        rows.Add(Array.Empty<FieldValue>());
        continue;
      }
      var fields = new List<FieldValue>(rawRow.Count);
      foreach (var raw in rawRow)
      {
        fields.Add(FromRawValue(raw));
      }
      rows.Add(fields);
    }
    return new TableData(schema, rows);
  }

  // ── Atomic data-row clear + write batch ──────────────────────────────────

  /// <summary>
  /// Build the single atomic <see cref="BatchUpdateSpreadsheetRequest"/> that
  /// replaces a table's <em>data</em> rows, leaving the header/columns intact:
  /// one request clears the prior data region, the next writes the new typed
  /// rows — both in one <c>Requests</c> list so the API applies them
  /// all-or-nothing and no torn or trailing rows survive. Both requests are
  /// scoped to the table's range (sheet id and column span), leaving every
  /// other tab and the table's own header untouched.
  /// </summary>
  /// <param name="table">The resolved table whose data region is replaced.</param>
  /// <param name="rows">The neutral rows to write, anchored just below the header.</param>
  internal static BatchUpdateSpreadsheetRequest BuildReplaceBatch(ResolvedTable table, TableData rows)
  {
    var range = table.Range;
    // Data rows start one row below the header.
    var dataStartRow = range.StartRowIndex + 1;
    var width = range.EndColumnIndex - range.StartColumnIndex;

    var requests = new List<Request>(2);

    // 1) Clear the prior data region (everything below the header within the
    //    table's column span). UpdateCells with a Range, no Rows, and a "*"
    //    mask blanks every authored field — this removes rows the new data is
    //    shorter than (no trailing rows).
    if (range.EndRowIndex > dataStartRow && width > 0)
    {
      requests.Add(new Request
      {
        UpdateCells = new UpdateCellsRequest
        {
          Range = new GridRange
          {
            SheetId = range.SheetId,
            StartRowIndex = dataStartRow,
            EndRowIndex = range.EndRowIndex,
            StartColumnIndex = range.StartColumnIndex,
            EndColumnIndex = range.EndColumnIndex,
          },
          Fields = ClearFieldsMask,
        },
      });
    }

    // 2) Write the new typed rows anchored just below the header at the table's
    //    start column. Start (not Range) lets the API size the write to the
    //    rows supplied.
    if (rows.RowCount > 0 && width > 0)
    {
      var rowData = new List<RowData>(rows.RowCount);
      foreach (var row in rows.Rows)
      {
        rowData.Add(ToRowData(row, width));
      }

      requests.Add(new Request
      {
        UpdateCells = new UpdateCellsRequest
        {
          Start = new GridCoordinate
          {
            SheetId = range.SheetId,
            RowIndex = dataStartRow,
            ColumnIndex = range.StartColumnIndex,
          },
          Rows = rowData,
          Fields = WriteFieldsMask,
        },
      });
    }

    return new BatchUpdateSpreadsheetRequest { Requests = requests };
  }

  /// <summary>
  /// Build the single <see cref="BatchUpdateSpreadsheetRequest"/> with one
  /// <see cref="AddTableRequest"/> that creates a native table from
  /// <paramref name="schema"/> on <paramref name="sheetId"/>.
  /// </summary>
  internal static BatchUpdateSpreadsheetRequest BuildAddTableBatch(
    string name, TableSchema schema, int sheetId, int initialDataRows = 1) =>
    new()
    {
      Requests = new List<Request>
      {
        new()
        {
          AddTable = new AddTableRequest
          {
            Table = ToTable(name, schema, sheetId, initialDataRows),
          },
        },
      },
    };
}
