using Flowthru.Core.Data;
using Flowthru.Extensions.Spark.Data;

namespace Flowthru.Extensions.Spark;

/// <summary>
/// Extends <see cref="ItemFactory"/> with a <c>Frame</c> property for
/// <see cref="Flowthru.Misc.DataFrames.TypedFrame{T}"/> catalog items.
/// </summary>
public static partial class ItemFactory
{
  /// <summary>
  /// Factory methods for <see cref="Flowthru.Misc.DataFrames.TypedFrame{T}"/> catalog entries.
  /// </summary>
  /// <remarks>
  /// TypedFrame items are always in-memory. See <see cref="FrameItemFactory"/> for details.
  /// </remarks>
  public static FrameItemFactory Frame { get; } = new FrameItemFactory();
}
