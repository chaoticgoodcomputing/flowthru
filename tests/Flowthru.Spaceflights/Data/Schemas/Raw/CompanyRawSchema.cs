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
    public required string Id { get; init; }

    /// <summary>
    /// Company rating as percentage string (e.g., "100%", "38%", or empty)
    /// </summary>
    public string? CompanyRating { get; init; }

    /// <summary>
    /// Company location/country
    /// </summary>
    public string? CompanyLocation { get; init; }

    /// <summary>
    /// Total fleet count as string
    /// </summary>
    public string? TotalFleetCount { get; init; }

    /// <summary>
    /// IATA approval status as "t" (true) or "f" (false)
    /// </summary>
    public required string IataApproved { get; init; }
}
