using Flowthru.Core.Abstractions;

namespace RetailDataMultipipeline.Data._01_Raw.Schemas;

/// <summary>
/// Maps a country name to its ISO-4217 currency code.
/// Maintained independently of the OFX feed — new countries can be added here
/// without touching the rate table.
/// </summary>
[FlowthruSchema]
public partial record CountryCurrencySchema
{
    public required string Country { get; init; }
    public required string Currency { get; init; }
}
