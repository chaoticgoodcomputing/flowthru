using Flowthru.Core.Data;

namespace Flowthru.Meta.Diagnostics.Tests.Fixtures;

/// <summary>
/// Minimal CatalogAbstract that exposes a curated list of items via GetAllItems().
/// Tests construct one and pass it through DI as a registered CatalogAbstract.
/// </summary>
internal sealed class FakeCatalog : CatalogAbstract
{
  private readonly IReadOnlyList<IItem> _items;

  public FakeCatalog(params IItem[] items) : base("FakeCatalog")
  {
    _items = items;
  }

  public override IEnumerable<IItem> GetAllItems() => _items;
}
