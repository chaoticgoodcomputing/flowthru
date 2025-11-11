namespace Flowthru.Extensions.ML.UMAP.Core.Markers;

/// <summary>
/// Phantom type marker for data size categories.
/// Used to enforce compile-time constraints on strategy compatibility based on dataset size.
/// </summary>
/// <remarks>
/// This is a marker interface with no members - it exists purely for compile-time type checking.
/// Implementations indicate whether a strategy is suitable for small or large datasets.
/// </remarks>
public interface IDataSizeMarker { }

/// <summary>
/// Marker indicating strategies optimized for small datasets (typically &lt; 4096 samples).
/// Small dataset strategies often use exact algorithms rather than approximations.
/// </summary>
public interface ISmallData : IDataSizeMarker { }

/// <summary>
/// Marker indicating strategies optimized for large datasets (typically ≥ 4096 samples).
/// Large dataset strategies typically use approximation algorithms for efficiency.
/// </summary>
public interface ILargeData : IDataSizeMarker { }

/// <summary>
/// Marker indicating strategies that work for any dataset size.
/// These strategies adapt their behavior based on the actual data size.
/// </summary>
public interface IAnySize : ISmallData, ILargeData { }
