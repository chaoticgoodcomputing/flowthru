using RetailData.Data._01_Raw.Schemas;
using RetailData.Data._02_Intermediate.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Cleans raw retail data: parses types, trims descriptions, truncates dates
/// </summary>
public static class CleanDataNode
{
  public static Func<IEnumerable<RawRetailSchema>, Task<IEnumerable<CleanedRetailSchema>>> Create()
  {
    return async (input) =>
    {
      var cleaned = input
        .Select(raw => ParseRecord(raw))
        .Where(item => item != null)
        .Cast<CleanedRetailSchema>();

      return await Task.FromResult(cleaned);
    };
  }

  private static CleanedRetailSchema? ParseRecord(RawRetailSchema raw)
  {
    // Parse quantity
    if (!int.TryParse(raw.Quantity, out var quantity))
      return null;

    // Parse unit price
    if (!decimal.TryParse(raw.UnitPrice, out var unitPrice))
      return null;

    // Skip records with negative or zero prices
    if (unitPrice <= 0)
      return null;

    // Parse and reformat date to ISO format (yyyy-MM-dd) for proper sorting
    var datePart = raw.InvoiceDate.Split(' ')[0];
    if (!DateTime.TryParse(datePart, out var parsedDate))
      return null;
    var isoDate = parsedDate.ToString("yyyy-MM-dd");

    // Clean description - trim whitespace
    var description = raw.Description?.Trim() ?? string.Empty;

    // Skip records with empty descriptions
    if (string.IsNullOrWhiteSpace(description))
      return null;

    // Calculate total amount
    var totalAmount = quantity * unitPrice;

    return new CleanedRetailSchema
    {
      InvoiceNo = raw.InvoiceNo.Trim(),
      StockCode = raw.StockCode.Trim(),
      Description = description,
      Quantity = quantity,
      InvoiceDate = isoDate,
      UnitPrice = unitPrice,
      CustomerID = raw.CustomerID.Trim(),
      Country = raw.Country.Trim(),
      TotalAmount = totalAmount,
    };
  }
}
