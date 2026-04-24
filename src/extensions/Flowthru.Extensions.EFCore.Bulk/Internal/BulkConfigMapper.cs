using EFCore.BulkExtensions;

namespace Flowthru.Extensions.EFCore.Bulk.Internal;

/// <summary>
/// Maps <see cref="BulkSaveOptions"/> to <see cref="BulkConfig"/>.
/// </summary>
internal static class BulkConfigMapper
{
  internal static BulkConfig ToBulkConfig(BulkSaveOptions? options)
  {
    options ??= new BulkSaveOptions();

    var config = new BulkConfig
    {
      BatchSize = options.BatchSize,
      PreserveInsertOrder = options.PreserveInsertOrder,
      SetOutputIdentity = options.SetOutputIdentity,
      UseUnlogged = options.UseUnlogged,
    };

    if (options.TimeoutSeconds.HasValue)
      config.BulkCopyTimeout = options.TimeoutSeconds.Value;

    if (options.PropertiesToInclude is { Count: > 0 } include)
      config.PropertiesToInclude = [.. include];

    if (options.PropertiesToExclude is { Count: > 0 } exclude)
      config.PropertiesToExclude = [.. exclude];

    return config;
  }
}
