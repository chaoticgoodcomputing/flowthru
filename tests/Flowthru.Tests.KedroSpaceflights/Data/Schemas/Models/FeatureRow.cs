using Flowthru.Abstractions;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

/// <summary>
/// Feature row used for ML.NET training and prediction.
/// Represents a single data point with features for regression.
/// </summary>
/// <remarks>
/// Note: MoonClearanceComplete has been removed from this schema as it had zero variance
/// in the dataset (all False values), which caused singular matrix errors in Math.NET's
/// QR decomposition. Sklearn handles this via pseudo-inverse, but Math.NET requires
/// explicit feature selection.
/// </remarks>
public class FeatureRow : IFlatSerializable {
  public float Engines { get; set; }
  public float PassengerCapacity { get; set; }
  public float Crew { get; set; }
  public bool DCheckComplete { get; set; }
  public bool IataApproved { get; set; }
  public float CompanyRating { get; set; }
  public float ReviewScoresRating { get; set; }

  /// <summary>
  /// Target variable (price) for training
  /// </summary>
  public float Price { get; set; }

  /// <summary>
  /// Converts this feature row to a double array for model training/prediction.
  /// Centralizes feature extraction logic to ensure consistency across training,
  /// evaluation, and cross-validation.
  /// </summary>
  /// <returns>
  /// Array of features in the order expected by the linear regression model:
  /// [Engines, PassengerCapacity, Crew, DCheckComplete, IataApproved, CompanyRating, ReviewScoresRating]
  /// </returns>
  public double[] ToFeatureArray() => new[] {
    (double)Engines,
    (double)PassengerCapacity,
    (double)Crew,
    DCheckComplete ? 1.0 : 0.0,
    IataApproved ? 1.0 : 0.0,
    (double)CompanyRating,
    (double)ReviewScoresRating
  };

  /// <summary>
  /// Gets the feature names in the same order as ToFeatureArray().
  /// </summary>
  public static string[] FeatureNames => new[] {
    "Engines",
    "PassengerCapacity",
    "Crew",
    "DCheckComplete",
    "IataApproved",
    "CompanyRating",
    "ReviewScoresRating"
  };
}
