using Microsoft.ML;
using LanguageExt;
using LanguageExt.Common;
using Flowthru.ML.Next.Core.Schema;
using Flowthru.ML.Next.Core.Columns;
using static LanguageExt.Prelude;

namespace Flowthru.ML.Next.Validation;

/// <summary>
/// Runtime column requirements for validation.
/// </summary>
/// <param name="Name">Column name</param>
/// <param name="ExpectedType">Expected CLR type</param>
/// <param name="IsRequired">Whether column must exist</param>
public readonly record struct ColumnRequirement(string Name, Type ExpectedType, bool IsRequired);

/// <summary>
/// Schema validation that accumulates all errors using LanguageExt's Validation monad.
/// </summary>
public static class SchemaValidator
{
  /// <summary>
  /// Validate that a DataView contains all required columns with correct types.
  /// </summary>
  /// <param name="dataView">DataView to validate</param>
  /// <param name="requiredColumns">Expected column requirements</param>
  /// <returns>Success or accumulated errors</returns>
  public static Validation<Error, Unit> ValidateSchema(
      IDataView dataView,
      params ColumnRequirement[] requiredColumns)
  {
    var schema = dataView.Schema;
    var errors = new List<Error>();

    foreach (var required in requiredColumns)
    {
      // Check if column exists
      var column = schema.GetColumnOrNull(required.Name);

      if (column == null)
      {
        if (required.IsRequired)
        {
          errors.Add(Error.New($"Required column '{required.Name}' not found in schema"));
        }
        continue;
      }

      // Validate column type
      var expectedTypeName = required.ExpectedType.Name;
      var actualTypeName = column.Value.Type.RawType.Name;

      if (actualTypeName != expectedTypeName)
      {
        errors.Add(Error.New(
            $"Column '{required.Name}' has type '{actualTypeName}' but expected '{expectedTypeName}'"));
      }
    }

    return errors.Count == 0
        ? Success<Error, Unit>(unit)
        : Fail<Error, Unit>(LanguageExt.Seq.create(errors.ToArray()));
  }

  /// <summary>
  /// Validate that two schemas are compatible for transformation composition.
  /// </summary>
  /// <param name="outputSchema">Output schema from first transformer</param>
  /// <param name="inputSchema">Input schema expected by second transformer</param>
  /// <returns>Success or accumulated errors</returns>
  public static Validation<Error, Unit> ValidateSchemaCompatibility(
      Microsoft.ML.DataViewSchema outputSchema,
      Microsoft.ML.DataViewSchema inputSchema)
  {
    var errors = new List<Error>();

    // Check that all input columns exist in output
    foreach (var inputColumn in inputSchema)
    {
      var outputColumn = outputSchema.GetColumnOrNull(inputColumn.Name);

      if (outputColumn == null)
      {
        errors.Add(Error.New($"Output schema missing required input column '{inputColumn.Name}'"));
        continue;
      }

      if (inputColumn.Type.RawType != outputColumn.Value.Type.RawType)
      {
        errors.Add(Error.New(
            $"Column '{inputColumn.Name}' type mismatch: output has {outputColumn.Value.Type.RawType.Name}, input expects {inputColumn.Type.RawType.Name}"));
      }
    }

    return errors.Count == 0
        ? Success<Error, Unit>(unit)
        : Fail<Error, Unit>(LanguageExt.Seq.create(errors.ToArray()));
  }
}
