using Flowthru.Core.Abstractions;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// Simple flat schema for testing basic Where and Select operations.
/// </summary>
public record PersonSchema
{
  public required string Name { get; init; }
  public required int Age { get; init; }
  public required bool IsActive { get; init; }
}

/// <summary>
/// Schema with <see cref="SerializedLabelAttribute"/> to test column name resolution.
/// </summary>
public record LabeledSchema
{
  [SerializedLabel("full_name")]
  public required string FullName { get; init; }

  [SerializedLabel("employee_id")]
  public required int EmployeeId { get; init; }

  public required string Department { get; init; }
}

/// <summary>
/// Projection target for Select tests.
/// </summary>
public record NameOnlySchema
{
  public required string Name { get; init; }
}

/// <summary>
/// Projection target for Select tests with computed fields.
/// </summary>
public record PersonSummarySchema
{
  public required string Name { get; init; }
  public required int Age { get; init; }
}

/// <summary>
/// Schema for the "right" side of Join tests.
/// </summary>
public record DepartmentSchema
{
  public required string Name { get; init; }
  public required int DeptId { get; init; }
}

/// <summary>
/// Schema for the "left" side of Join tests — has a foreign key.
/// </summary>
public record EmployeeSchema
{
  public required string Name { get; init; }
  public required int DeptId { get; init; }
}

/// <summary>
/// Projection target for Join result.
/// </summary>
public record EmployeeDeptSchema
{
  public required string EmployeeName { get; init; }
  public required string DepartmentName { get; init; }
}

/// <summary>
/// Schema for GroupBy / Aggregate tests.
/// </summary>
public record SalesSchema
{
  public required string Category { get; init; }
  public required double Amount { get; init; }
  public required int Quantity { get; init; }
}

/// <summary>
/// Aggregate result schema for GroupBy / Aggregate tests.
/// </summary>
public record SalesSummarySchema
{
  public required string Category { get; init; }
  public required double TotalAmount { get; init; }
  public required long TotalCount { get; init; }
}

/// <summary>
/// Schema with a nullable reference property for null-check translation tests.
/// </summary>
public record OrderSchema
{
  public required string OrderId { get; init; }
  public required string? Region { get; init; }
  public required double Amount { get; init; }
  public required DateTime OrderDate { get; init; }
}

/// <summary>
/// Schema with numeric columns for Math method translation tests.
/// </summary>
public record MeasurementSchema
{
  public required double Value { get; init; }
  public required double Temperature { get; init; }
}
