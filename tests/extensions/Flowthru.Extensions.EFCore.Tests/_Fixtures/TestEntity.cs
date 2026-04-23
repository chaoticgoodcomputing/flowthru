namespace Flowthru.Extensions.EFCore.Tests;

public class TestEntity
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
}

/// <summary>Second entity for cross-table fused-path tests.</summary>
public class SourceEntity
{
  public int Id { get; set; }
  public string SourceName { get; set; } = string.Empty;
}
