using System.Linq.Expressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using LanguageExt;
using LanguageExt.Common;
using Flowthru.ML.Next.Core.Schema;
using Flowthru.ML.Next.Core.Columns;

namespace Flowthru.ML.Next.Transform;

/// <summary>
/// Type-safe column transformation operations.
/// </summary>
public static class ColumnTransforms {
    /// <summary>
    /// Concatenate multiple columns into a single output column.
    /// This is a stateless transformation (no fitting required).
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema with new concatenated column</typeparam>
    /// <param name="context">MLContext</param>
    /// <param name="outputColumn">Name of the output column</param>
    /// <param name="inputColumns">Names of columns to concatenate</param>
    /// <returns>Estimator that concatenates the specified columns</returns>
    public static Estimator<TSchemaIn, TSchemaOut> Concatenate<TSchemaIn, TSchemaOut>(
        MLContext context,
        string outputColumn,
        params string[] inputColumns)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var estimator = context.Transforms.Concatenate(outputColumn, inputColumns);
        return Estimator<TSchemaIn, TSchemaOut>.From(estimator);
    }

    /// <summary>
    /// Normalizes a column using min-max normalization.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (typically same as input)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnName">Name of the column to normalize</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that normalizes the column</returns>
    public static Estimator<TSchemaIn, TSchemaOut> NormalizeMinMax<TSchemaIn, TSchemaOut>(
        MLContext context,
        string columnName,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var estimator = context.Transforms.NormalizeMinMax(
            outputColumn ?? columnName,
            columnName);
        return Estimator<TSchemaIn, TSchemaOut>.From(estimator);
    }

    /// <summary>
    /// Normalizes a column using mean-variance normalization.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (typically same as input)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnName">Name of the column to normalize</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that normalizes the column</returns>
    public static Estimator<TSchemaIn, TSchemaOut> NormalizeMeanVariance<TSchemaIn, TSchemaOut>(
        MLContext context,
        string columnName,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var estimator = context.Transforms.NormalizeMeanVariance(
            outputColumn ?? columnName,
            columnName);
        return Estimator<TSchemaIn, TSchemaOut>.From(estimator);
    }

    /// <summary>
    /// Maps text values to key types (categorical encoding).
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (must include key-typed column)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnName">Name of the text column to convert</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that converts text to keys</returns>
    public static Estimator<TSchemaIn, TSchemaOut> MapValueToKey<TSchemaIn, TSchemaOut>(
        MLContext context,
        string columnName,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var estimator = context.Transforms.Conversion.MapValueToKey(
            outputColumn ?? columnName,
            columnName);
        return Estimator<TSchemaIn, TSchemaOut>.From(estimator);
    }

    /// <summary>
    /// Maps key values back to their original values.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (must include the original value type)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnName">Name of the key column to convert back</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that converts keys back to values</returns>
    public static Estimator<TSchemaIn, TSchemaOut> MapKeyToValue<TSchemaIn, TSchemaOut>(
        MLContext context,
        string columnName,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var estimator = context.Transforms.Conversion.MapKeyToValue(
            outputColumn ?? columnName,
            columnName);
        return Estimator<TSchemaIn, TSchemaOut>.From(estimator);
    }

    // ============================================================================
    // Expression Tree Overloads (Compile-Time Column Name Safety)
    // ============================================================================

    /// <summary>
    /// Normalizes a column using min-max normalization with compile-time column name checking.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (typically same as input)</typeparam>
    /// <typeparam name="TType">Column element type</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnSelector">Expression selecting the column to normalize</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that normalizes the column</returns>
    /// <example>
    /// <code>
    /// var estimator = ColumnTransforms.NormalizeMinMax&lt;IrisRawSchema, IrisRawSchema, float&gt;(
    ///     context,
    ///     schema => schema.SepalLength  // Compile-time checked!
    /// );
    /// </code>
    /// </example>
    public static Estimator<TSchemaIn, TSchemaOut> NormalizeMinMax<TSchemaIn, TSchemaOut, TType>(
        MLContext context,
        Expression<Func<TSchemaIn, ColumnSpec<TType>>> columnSelector,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnName = ColumnExpressionExtractor.ExtractColumnSpecName(columnSelector);
        return NormalizeMinMax<TSchemaIn, TSchemaOut>(context, columnName, outputColumn);
    }

    /// <summary>
    /// Normalizes a column using mean-variance normalization with compile-time column name checking.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (typically same as input)</typeparam>
    /// <typeparam name="TType">Column element type</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnSelector">Expression selecting the column to normalize</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that normalizes the column</returns>
    public static Estimator<TSchemaIn, TSchemaOut> NormalizeMeanVariance<TSchemaIn, TSchemaOut, TType>(
        MLContext context,
        Expression<Func<TSchemaIn, ColumnSpec<TType>>> columnSelector,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnName = ColumnExpressionExtractor.ExtractColumnSpecName(columnSelector);
        return NormalizeMeanVariance<TSchemaIn, TSchemaOut>(context, columnName, outputColumn);
    }

    /// <summary>
    /// Maps text values to key types with compile-time column name checking.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (must include key-typed column)</typeparam>
    /// <typeparam name="TType">Column element type (typically string)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnSelector">Expression selecting the column to convert</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that converts text to keys</returns>
    public static Estimator<TSchemaIn, TSchemaOut> MapValueToKey<TSchemaIn, TSchemaOut, TType>(
        MLContext context,
        Expression<Func<TSchemaIn, ColumnSpec<TType>>> columnSelector,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnName = ColumnExpressionExtractor.ExtractColumnSpecName(columnSelector);
        return MapValueToKey<TSchemaIn, TSchemaOut>(context, columnName, outputColumn);
    }

    /// <summary>
    /// Maps key values back to their original values with compile-time column name checking.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (must include the original value type)</typeparam>
    /// <typeparam name="TType">Column element type</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnSelector">Expression selecting the key column to convert back</param>
    /// <param name="outputColumn">Optional output column name (defaults to input column)</param>
    /// <returns>An estimator that converts keys back to values</returns>
    public static Estimator<TSchemaIn, TSchemaOut> MapKeyToValue<TSchemaIn, TSchemaOut, TType>(
        MLContext context,
        Expression<Func<TSchemaIn, ColumnSpec<TType>>> columnSelector,
        string? outputColumn = null)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnName = ColumnExpressionExtractor.ExtractColumnSpecName(columnSelector);
        return MapKeyToValue<TSchemaIn, TSchemaOut>(context, columnName, outputColumn);
    }

    /// <summary>
    /// Concatenate multiple columns into a single output column with compile-time column name checking.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema with new concatenated column</typeparam>
    /// <param name="context">MLContext</param>
    /// <param name="outputColumnName">Name of the output column</param>
    /// <param name="columnSelectors">Expression selecting columns to concatenate</param>
    /// <returns>Estimator that concatenates the specified columns</returns>
    /// <example>
    /// <code>
    /// var estimator = ColumnTransforms.Concatenate&lt;IrisRawSchema, IrisFeaturesSchema&gt;(
    ///     context,
    ///     "Features",
    ///     schema => schema.SepalLength,
    ///     schema => schema.SepalWidth,
    ///     schema => schema.PetalLength,
    ///     schema => schema.PetalWidth
    /// );
    /// </code>
    /// </example>
    public static Estimator<TSchemaIn, TSchemaOut> Concatenate<TSchemaIn, TSchemaOut>(
        MLContext context,
        string outputColumnName,
        params Expression<Func<TSchemaIn, object>>[] columnSelectors)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnNames = columnSelectors
            .Select(selector => ColumnExpressionExtractor.ExtractColumnName(selector))
            .ToArray();
        return Concatenate<TSchemaIn, TSchemaOut>(context, outputColumnName, columnNames);
    }

    /// <summary>
    /// Maps text values to key types with compile-time column name checking, using in-place transformation.
    /// </summary>
    /// <typeparam name="TSchemaIn">Input schema</typeparam>
    /// <typeparam name="TSchemaOut">Output schema (must include key-typed column)</typeparam>
    /// <typeparam name="TType">Column element type (typically string or float)</typeparam>
    /// <param name="context">ML.NET context</param>
    /// <param name="columnSelector">Expression selecting the column to convert in-place</param>
    /// <returns>An estimator that converts text to keys in the same column</returns>
    /// <remarks>
    /// This overload performs in-place transformation where the output column has the same name as the input.
    /// </remarks>
    public static Estimator<TSchemaIn, TSchemaOut> MapValueToKeyInPlace<TSchemaIn, TSchemaOut, TType>(
        MLContext context,
        Expression<Func<TSchemaIn, ColumnSpec<TType>>> columnSelector)
        where TSchemaIn : ISchemaDefinition
        where TSchemaOut : ISchemaDefinition {
        var columnName = ColumnExpressionExtractor.ExtractColumnSpecName(columnSelector);
        return MapValueToKey<TSchemaIn, TSchemaOut>(context, columnName, columnName);
    }
}
