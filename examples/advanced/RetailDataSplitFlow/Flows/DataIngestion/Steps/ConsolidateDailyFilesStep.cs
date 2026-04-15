using System.Globalization;
using Flowthru.Core.Steps;
using RetailDataMultipipeline.Data._01_Raw.Schemas;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;

namespace RetailDataMultipipeline.Flows.DataIngestion.Steps;

/// <summary>
/// Parses raw all-string transaction records into the fully-typed intermediate
/// schema. The concatenation of all daily files is handled upstream by the
/// <c>CsvDirectory</c> catalog entry; this node's job is type coercion.
/// </summary>
/// <remarks>
/// Parsing rules:
/// <list type="bullet">
/// <item><c>Quantity</c> — <c>int.Parse</c>; negative values represent returns.</item>
/// <item><c>InvoiceDate</c> — parsed with <c>InvariantCulture</c> from
///   <c>"yyyy-MM-dd HH:mm:ss"</c> format.</item>
/// <item><c>UnitPrice</c> — <c>decimal.Parse</c> with <c>InvariantCulture</c>.</item>
/// <item><c>CustomerId</c> — nullable; source encodes as <c>"17850.0"</c> (float string),
///   so parsed as <c>double</c> then cast to <c>int</c>.</item>
/// <item><c>Description</c> — nullable; empty strings are stored as <c>null</c>.</item>
/// </list>
/// </remarks>
[FlowthruStep]
public static class ConsolidateDailyFilesStep
{
    public static Func<
      IEnumerable<RetailTransactionSchema>,
      IEnumerable<RetailTransactionIntermediateSchema>
    > Create()
    {
        return transactions => transactions.Select(Parse);
    }

    private static RetailTransactionIntermediateSchema Parse(RetailTransactionSchema raw)
    {
        return new RetailTransactionIntermediateSchema
        {
            InvoiceNo = raw.InvoiceNo,
            StockCode = raw.StockCode,
            Description = string.IsNullOrWhiteSpace(raw.Description) ? null : raw.Description,
            Quantity = int.Parse(raw.Quantity, CultureInfo.InvariantCulture),
            InvoiceDate = DateTime.Parse(raw.InvoiceDate, CultureInfo.InvariantCulture),
            UnitPrice = decimal.Parse(raw.UnitPrice, CultureInfo.InvariantCulture),
            CustomerId = string.IsNullOrWhiteSpace(raw.CustomerID)
            ? null
            : (int)double.Parse(raw.CustomerID, CultureInfo.InvariantCulture),
            Country = raw.Country,
        };
    }
}
