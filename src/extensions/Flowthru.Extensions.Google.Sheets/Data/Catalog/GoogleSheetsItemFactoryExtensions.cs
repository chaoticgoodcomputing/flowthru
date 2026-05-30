using Flowthru.Data.Schema;
using Flowthru.Data.Storage.Sheets;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute Google Sheets smart constructors into
/// <see cref="ItemFactory.Enumerable"/>. End users see them as
/// <c>ItemFactory.Enumerable.GoogleSheets&lt;TRow&gt;(...)</c> via a single
/// <c>using Flowthru.Data.Catalog;</c> import.
/// </summary>
public static class GoogleSheetsItemFactoryExtensions
{
  /// <summary>
  /// A Google Sheets table holding rows of <typeparamref name="TRow"/>. The
  /// catalog item is addressed by <c>(spreadsheetId, tableName)</c> — a stable
  /// native-table name, not a fragile cell range — and reads and writes one tab
  /// in the spreadsheet, leaving sibling tabs (e.g. human-readable formula tabs)
  /// untouched.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Authentication is supplied through the gateway, never here.</strong>
  /// Register the gateway in your host with <c>builder.AddGoogleSheets(...)</c>,
  /// resolve it into your <c>Catalog</c> via constructor injection (the same way
  /// the EF Core catalog takes an injected context), and pass it to this
  /// constructor. The gateway owns the authenticated client; the catalog item
  /// never sees a credential.
  /// </para>
  /// <para>
  /// <strong>Read</strong> matches the table's columns to
  /// <typeparamref name="TRow"/>'s properties by name (case-insensitive) and
  /// coerces each cell to the property's declared type. <strong>Write</strong>
  /// creates the table from <typeparamref name="TRow"/> if it does not exist,
  /// then atomically replaces its rows — when Flowthru owns the table it holds
  /// the full dataset every run, so replace is the upsert. Supply
  /// <paramref name="saveFunc"/> for an append/upsert recipe instead.
  /// </para>
  /// </remarks>
  /// <typeparam name="TRow">
  /// The row type. Sheets rows are tabular, so the schema must be flat
  /// (<see cref="IFlatSchema"/>): each property maps to one column.
  /// </typeparam>
  /// <param name="_">The factory anchor — discriminates the extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="spreadsheetId">The id of the spreadsheet the table lives in.</param>
  /// <param name="tableName">
  /// The native table name — the catalog-item identity. Flowthru creates the
  /// table under this name on first write if it is absent.
  /// </param>
  /// <param name="gateway">
  /// The Sheets gateway, obtained by injecting <see cref="ISheetsGateway"/> into
  /// your <c>Catalog</c> after registering it with
  /// <c>builder.AddGoogleSheets(...)</c>. The gateway carries the auth and client
  /// lifecycle; swap it for an offline gateway in tests with no code change here.
  /// </param>
  /// <param name="saveFunc">
  /// Optional write-strategy override. When null, the default create-if-absent +
  /// atomic-replace is used. Supply a delegate to express an append/upsert recipe;
  /// the gateway, spreadsheet id, table name, rows, and cancellation token flow
  /// through. Compose on top of
  /// <see cref="GoogleSheetsStorageAdapter{TRow}.DefaultSave"/>.
  /// </param>
  public static IItem<IEnumerable<TRow>> GoogleSheets<TRow>(
    this EnumerableItemFactory _,
    string label,
    string spreadsheetId,
    string tableName,
    ISheetsGateway gateway,
    Func<ISheetsGateway, string, string, IReadOnlyList<TRow>, CancellationToken, Task>? saveFunc = null
  )
    where TRow : notnull, IFlatSchema =>
    new Item<IEnumerable<TRow>>(
      label,
      new GoogleSheetsStorageAdapter<TRow>(spreadsheetId, tableName, gateway, saveFunc)
    );
}
