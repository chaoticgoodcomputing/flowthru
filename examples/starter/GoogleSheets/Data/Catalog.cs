using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.Sheets;

namespace GoogleSheets.Data;

/// <summary>
/// Data catalog for the Google Sheets pipeline. The catalog obtains its
/// <see cref="ISheetsGateway"/> by injection — the same way the EF Core example's
/// catalog takes an injected <c>DbContext</c> factory — so it never sees a
/// credential and never references the Google SDK. The host decides which gateway
/// to hand it: the offline <c>InMemorySheetsGateway</c> here, or a live
/// <c>SheetsService</c>-backed one in production, with no change to the catalog.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly ISheetsGateway _sheets;
  private readonly string _spreadsheetId;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="sheets">
  /// The Sheets gateway, registered in the host via <c>AddGoogleSheets(...)</c>
  /// and resolved out of DI. Carries the auth and client lifecycle; the catalog
  /// only routes table reads/writes through it.
  /// </param>
  /// <param name="spreadsheetId">The id of the spreadsheet the tables live in.</param>
  public Catalog(ISheetsGateway sheets, string spreadsheetId)
  {
    _sheets = sheets ?? throw new ArgumentNullException(nameof(sheets));
    _spreadsheetId = spreadsheetId ?? throw new ArgumentNullException(nameof(spreadsheetId));
  }
}
