namespace KedroSpaceflightsGQL.Infra.GqlServer.Types;

// ── Domain records ─────────────────────────────────────────────────────────

/// <summary>Raw string fields matching the CSV source schema.</summary>
public record CompanyRecord(
  string Id,
  string CompanyRating,
  string IataApproved,
  string CompanyLocation
);

/// <summary>Raw string fields matching the Excel source schema.</summary>
public record ShuttleRecord(
  string Id,
  string ShuttleType,
  string CompanyId,
  string Engines,
  string PassengerCapacity,
  string Crew,
  string Price,
  string DCheckComplete,
  string MoonClearanceComplete
);

/// <summary>Raw string fields matching the CSV source schema.</summary>
public record ReviewRecord(string ShuttleId, string ReviewScoresRating);

// ── GQL input types ────────────────────────────────────────────────────────

/// <summary>Input for the <c>addCompany</c> mutation.</summary>
public record AddCompanyInput(
  string Id,
  string CompanyRating,
  string IataApproved,
  string CompanyLocation
);

/// <summary>Input for the <c>addShuttle</c> mutation.</summary>
public record AddShuttleInput(
  string Id,
  string ShuttleType,
  string CompanyId,
  string Engines,
  string PassengerCapacity,
  string Crew,
  string Price,
  string DCheckComplete,
  string MoonClearanceComplete
);

/// <summary>Input for the <c>addReview</c> mutation.</summary>
public record AddReviewInput(string ShuttleId, string ReviewScoresRating);
