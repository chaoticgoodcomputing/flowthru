using EFCore.BulkExtensions;
using Flowthru.Extensions.EFCore.Bulk.Internal;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

[TestFixture]
public class BulkConfigMapperTests
{
  [Test]
  public void ToBulkConfig_With_Null_Options_Returns_Defaults()
  {
    var config = BulkConfigMapper.ToBulkConfig(null);

    Assert.Multiple(() =>
    {
      Assert.That(config.BatchSize, Is.EqualTo(2000));
      Assert.That(config.PreserveInsertOrder, Is.True);
      Assert.That(config.SetOutputIdentity, Is.False);
      Assert.That(config.UseUnlogged, Is.False);
      Assert.That(config.PropertiesToInclude, Is.Null);
      Assert.That(config.PropertiesToExclude, Is.Null);
    });
  }

  [Test]
  public void ToBulkConfig_Maps_All_Options()
  {
    var options = new BulkSaveOptions
    {
      BatchSize = 10000,
      TimeoutSeconds = 300,
      PreserveInsertOrder = false,
      SetOutputIdentity = true,
      UseUnlogged = true,
      PropertiesToInclude = ["Name"],
      PropertiesToExclude = null,
    };

    var config = BulkConfigMapper.ToBulkConfig(options);

    Assert.Multiple(() =>
    {
      Assert.That(config.BatchSize, Is.EqualTo(10000));
      Assert.That(config.BulkCopyTimeout, Is.EqualTo(300));
      Assert.That(config.PreserveInsertOrder, Is.False);
      Assert.That(config.SetOutputIdentity, Is.True);
      Assert.That(config.UseUnlogged, Is.True);
      Assert.That(config.PropertiesToInclude, Is.EqualTo(new List<string> { "Name" }));
      Assert.That(config.PropertiesToExclude, Is.Null);
    });
  }

  [Test]
  public void ToBulkConfig_Excludes_Properties()
  {
    var options = new BulkSaveOptions { PropertiesToExclude = ["Id", "Name"] };

    var config = BulkConfigMapper.ToBulkConfig(options);

    Assert.That(config.PropertiesToExclude, Is.EqualTo(new List<string> { "Id", "Name" }));
    Assert.That(config.PropertiesToInclude, Is.Null);
  }

  [Test]
  public void ToBulkConfig_Without_Timeout_Does_Not_Set_BulkCopyTimeout()
  {
    var options = new BulkSaveOptions { TimeoutSeconds = null };

    var config = BulkConfigMapper.ToBulkConfig(options);

    // BulkConfig default is null, which means provider default (30s)
    Assert.That(config.BulkCopyTimeout, Is.Null);
  }
}
