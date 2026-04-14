using Flowthru.Core.Data;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark;
using Flowthru.Extensions.Spark.Data;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("FrameItemFactory")]
public class FrameItemFactoryTests
{
    // ===================================================================
    //  ItemFactory.Frame property
    // ===================================================================

    [Test]
    public void ItemFactory_Frame_IsNotNull()
    {
        Assert.That(ItemFactory.Frame, Is.Not.Null);
    }

    [Test]
    public void ItemFactory_Frame_IsFrameItemFactory()
    {
        Assert.That(ItemFactory.Frame, Is.InstanceOf<FrameItemFactory>());
    }

    // ===================================================================
    //  Memory<T> factory
    // ===================================================================

    [Test]
    public void Frame_Memory_ReturnsItemOfTypedFrame()
    {
        var item = ItemFactory.Frame.Memory<PersonSchema>("test");

        Assert.That(item, Is.Not.Null);
        Assert.That(item, Is.InstanceOf<Item<TypedFrame<PersonSchema>>>());
    }

    [Test]
    public void Frame_Memory_PreservesLabel()
    {
        var item = ItemFactory.Frame.Memory<PersonSchema>("my-frame");

        Assert.That(item.Label, Is.EqualTo("my-frame"));
    }

    [Test]
    public void Frame_Memory_StorageTraits_IsNotPersistent()
    {
        var item = ItemFactory.Frame.Memory<PersonSchema>("test");

        Assert.That(item.Traits.IsPersistent, Is.False);
    }

    // ===================================================================
    //  Save → Load round-trip: no serialization, same reference
    // ===================================================================

    [Test]
    public async Task Frame_Memory_SaveThenLoad_ReturnsSameReference()
    {
        var item = ItemFactory.Frame.Memory<PersonSchema>("test");
        var provider = new TestFrameProvider();
        var frame = new TypedFrame<PersonSchema>(provider);

        await item.Save(frame).Run();
        var loaded = await item.Load().Run();

        Assert.That(loaded, Is.SameAs(frame));
    }

    [Test]
    public async Task Frame_Memory_LoadAfterTwoSaves_ReturnsLastSavedReference()
    {
        var item = ItemFactory.Frame.Memory<PersonSchema>("test");
        var provider = new TestFrameProvider();
        var first = new TypedFrame<PersonSchema>(provider);
        var second = new TypedFrame<PersonSchema>(provider);

        await item.Save(first).Run();
        await item.Save(second).Run();
        var loaded = await item.Load().Run();

        Assert.That(loaded, Is.SameAs(second));
    }
}
