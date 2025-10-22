namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// Feature row used for ML.NET training and prediction.
/// Represents a single data point with features for regression.
/// </summary>
public class FeatureRow
{
  public float Engines { get; set; }
  public float PassengerCapacity { get; set; }
  public float Crew { get; set; }
  public bool DCheckComplete { get; set; }
  public bool MoonClearanceComplete { get; set; }
  public bool IataApproved { get; set; }
  public float CompanyRating { get; set; }
  public float ReviewScoresRating { get; set; }

  /// <summary>
  /// Target variable (price) for training
  /// </summary>
  public float Price { get; set; }
}
