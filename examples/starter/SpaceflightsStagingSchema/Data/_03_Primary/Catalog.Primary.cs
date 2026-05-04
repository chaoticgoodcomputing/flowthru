using Flowthru.Core.Data;
using SpaceflightsStagingSchema.Data._03_Primary.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Model input table — a deferred query view over
  /// <see cref="Companies"/>, <see cref="Shuttles"/>, and <see cref="Reviews"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This item is intentionally an in-memory holder, not a persisted EFCore
  /// table. Its value is a <c>DbQuery&lt;ModelInputTableSchema&gt;</c> produced
  /// by <c>BuildModelInputTableStep</c> via
  /// <c>DbQuery&lt;T&gt;.Project&lt;ModelInputTableSchema&gt;(ctx => …)</c>;
  /// downstream <c>SplitDataStep</c> iterates the resulting <see cref="IEnumerable{T}"/>,
  /// triggering the SQL join lazily.
  /// </para>
  /// <para>
  /// <strong>Why not persist?</strong> The model input table is fully derivable
  /// from the three FK-constrained source tables. Storing it would create a
  /// staleness window, double the storage footprint, and shift the join's
  /// authority away from the database. The view is the right abstraction.
  /// </para>
  /// <para>
  /// <strong>Why is this in the catalog at all?</strong> It is the connection
  /// point between <c>BuildModelInputTableStep</c> (which writes the deferred
  /// query) and <c>SplitDataStep</c> (which reads it). The catalog item acts as
  /// a typed wire between them, not as a storage location.
  /// </para>
  /// </remarks>
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(
      () =>
        ItemFactory.Single.Memory<IEnumerable<ModelInputTableSchema>>(
          label: "ProductionModelInputTableView"
        )
    );
}
