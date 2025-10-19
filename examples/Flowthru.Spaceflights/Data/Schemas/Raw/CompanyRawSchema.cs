using CsvHelper.Configuration.Attributes;

namespace Flowthru.Spaceflights.Data.Schemas.Raw;

/// <summary>
/// Raw company data as read from CSV file.
/// Matches structure of Datasets/01_Raw/companies.csv
/// </summary>
public record CompanyRawSchema
{
    /// <summary>
    /// Company identifier
    /// </summary>
    [Name("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Company rating as percentage string (e.g., "100%", "38%", or empty)
    /// </summary>
    [Name("company_rating")]
    public string? CompanyRating { get; init; }

    /// <summary>
    /// Company location/country
    /// </summary>
    [Name("company_location")]
    public string? CompanyLocation { get; init; }

    /// <summary>
    /// Total fleet count as string
    /// </summary>
    [Name("total_fleet_count")]
    public string? TotalFleetCount { get; init; }

    /// <summary>
    /// IATA approval status as "t" (true) or "f" (false)
    /// </summary>
    [Name("iata_approved")]
    public required string IataApproved { get; init; }
}
