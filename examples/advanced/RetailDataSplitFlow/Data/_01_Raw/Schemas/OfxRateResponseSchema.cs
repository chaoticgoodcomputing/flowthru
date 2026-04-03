using Flowthru.Abstractions;

namespace RetailDataMultipipeline.Data._01_Raw.Schemas;

/// <summary>
/// Stubbed OFX API response for a XXX/GBP/1000 conversion request.
/// Field names are preserved verbatim from the OFX wire format.
/// </summary>
/// <remarks>
/// The <c>convertedAmount</c> field reflects the result of converting 1000 units
/// of <c>Currency</c> to GBP at the current <c>ofxRate</c>. It is carried along
/// as context; only <c>ofxRate</c> is used in per-transaction arithmetic.
/// </remarks>
[FlowthruSchema]
public partial record OfxRateResponseSchema
{
  public required string Currency { get; init; }
  public required decimal ofxRate { get; init; }
  public required decimal inverseOfxRate { get; init; }
  public required decimal convertedAmount { get; init; }
}
