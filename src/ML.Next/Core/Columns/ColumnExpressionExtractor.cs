using System.Linq.Expressions;

namespace ML.Next.Core.Columns;

/// <summary>
/// Utilities for extracting column names from expression trees.
/// This enables compile-time-checked column references via lambda expressions.
/// </summary>
/// <remarks>
/// <para>
/// Instead of using error-prone string literals:
/// <code>
/// ColumnTransforms.Normalize(context, "SepalLength")  // Typos not caught!
/// </code>
/// </para>
/// <para>
/// Use expression trees for compile-time safety:
/// <code>
/// ColumnTransforms.Normalize(context, schema => schema.SepalLength)  // Compile-time checked!
/// </code>
/// </para>
/// </remarks>
public static class ColumnExpressionExtractor
{
  /// <summary>
  /// Extracts a column name from a property access expression.
  /// </summary>
  /// <typeparam name="TSchema">The schema type containing column properties</typeparam>
  /// <typeparam name="TColumn">The column type (e.g., ColumnSpec&lt;float&gt;)</typeparam>
  /// <param name="selector">Expression selecting a property from the schema</param>
  /// <returns>The name of the selected property</returns>
  /// <exception cref="ArgumentException">If the expression is not a simple property access</exception>
  /// <example>
  /// <code>
  /// interface IMySchema {
  ///   ColumnSpec&lt;float&gt; Temperature { get; }
  /// }
  /// 
  /// Expression&lt;Func&lt;IMySchema, ColumnSpec&lt;float&gt;&gt;&gt; selector = schema => schema.Temperature;
  /// string columnName = ExtractColumnName(selector);  // Returns "Temperature"
  /// </code>
  /// </example>
  public static string ExtractColumnName<TSchema, TColumn>(
      Expression<Func<TSchema, TColumn>> selector)
  {
    // Handle direct property access
    if (selector.Body is MemberExpression memberExpr)
    {
      return memberExpr.Member.Name;
    }

    // Handle conversion expressions (e.g., float -> object)
    if (selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpr
        && unaryExpr.Operand is MemberExpression convertedMemberExpr)
    {
      return convertedMemberExpr.Member.Name;
    }

    throw new ArgumentException(
        $"Expression must be a simple property access (e.g., 'schema => schema.PropertyName'). " +
        $"Got: {selector.Body.NodeType}",
        nameof(selector));
  }

  /// <summary>
  /// Extracts a column name from a ColumnSpec property access expression.
  /// This overload is specifically for ColumnSpec&lt;TType&gt; properties.
  /// </summary>
  /// <typeparam name="TSchema">The schema type containing column properties</typeparam>
  /// <typeparam name="TType">The column element type</typeparam>
  /// <param name="selector">Expression selecting a ColumnSpec property from the schema</param>
  /// <returns>The name stored in the ColumnSpec</returns>
  /// <remarks>
  /// This method first extracts the property name, then accesses the ColumnSpec's Value.
  /// If the ColumnSpec has not been initialized with a name, it falls back to the property name.
  /// </remarks>
  public static string ExtractColumnSpecName<TSchema, TType>(
      Expression<Func<TSchema, ColumnSpec<TType>>> selector)
  {
    // For ColumnSpec, we want the Value property, not the property name
    // But since ColumnSpec might be declared inline, we need to handle both cases

    if (selector.Body is MemberExpression memberExpr)
    {
      var propertyName = memberExpr.Member.Name;

      // Try to evaluate the expression to get the actual ColumnSpec value
      try
      {
        var compiled = selector.Compile();
        var schemaInstance = System.Activator.CreateInstance<TSchema>();
        var columnSpec = compiled(schemaInstance);

        // If the ColumnSpec has a non-empty Value, use that
        if (!string.IsNullOrEmpty(columnSpec.Value))
        {
          return columnSpec.Value;
        }
      }
      catch
      {
        // If we can't evaluate (e.g., interface with no implementation),
        // fall back to property name
      }

      // Fall back to property name if ColumnSpec value is empty
      return propertyName;
    }

    throw new ArgumentException(
        $"Expression must be a simple property access (e.g., 'schema => schema.PropertyName'). " +
        $"Got: {selector.Body.NodeType}",
        nameof(selector));
  }

  /// <summary>
  /// Extracts a column name from a ColumnSpec with dimension/cardinality property access expression.
  /// </summary>
  /// <typeparam name="TSchema">The schema type containing column properties</typeparam>
  /// <typeparam name="TType">The column element type</typeparam>
  /// <typeparam name="TConst">The dimension/cardinality constant type</typeparam>
  /// <param name="selector">Expression selecting a ColumnSpec property from the schema</param>
  /// <returns>The name stored in the ColumnSpec</returns>
  public static string ExtractColumnSpecName<TSchema, TType, TConst>(
      Expression<Func<TSchema, ColumnSpec<TType, TConst>>> selector)
      where TConst : Constants.Constant<long>, new()
  {
    if (selector.Body is MemberExpression memberExpr)
    {
      var propertyName = memberExpr.Member.Name;

      // Try to evaluate the expression to get the actual ColumnSpec value
      try
      {
        var compiled = selector.Compile();
        var schemaInstance = System.Activator.CreateInstance<TSchema>();
        var columnSpec = compiled(schemaInstance);

        // If the ColumnSpec has a non-empty Name, use that
        if (!string.IsNullOrEmpty(columnSpec.Name))
        {
          return columnSpec.Name;
        }
      }
      catch
      {
        // If we can't evaluate (e.g., interface with no implementation),
        // fall back to property name
      }

      // Fall back to property name if ColumnSpec name is empty
      return propertyName;
    }

    throw new ArgumentException(
        $"Expression must be a simple property access (e.g., 'schema => schema.PropertyName'). " +
        $"Got: {selector.Body.NodeType}",
        nameof(selector));
  }
}
