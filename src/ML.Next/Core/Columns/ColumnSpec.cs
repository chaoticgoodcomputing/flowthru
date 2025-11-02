using ML.Next.Core.Constants;
using LanguageExt.Traits.Domain;

namespace ML.Next.Core.Columns;

/// <summary>
/// Strongly-typed column specification that carries type information and optional
/// dimension/cardinality constants at compile-time.
/// </summary>
/// <typeparam name="TType">The type of values in the column</typeparam>
/// <remarks>
/// This is the Phase 1 version that tracks only the element type.
/// Use ColumnSpec&lt;TType, TConst&gt; for Phase 2 with dimension/cardinality tracking.
/// </remarks>
public readonly record struct ColumnSpec<TType> : Identifier<ColumnSpec<TType>> {
    /// <summary>
    /// The string name of the column.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Creates a new column specification from a string name.
    /// </summary>
    /// <param name="name">The column name</param>
    /// <returns>A strongly-typed column reference</returns>
    public static ColumnSpec<TType> From(string name) =>
        new() { Value = name };

    /// <summary>
    /// Implicit conversion from string for convenience.
    /// </summary>
    public static implicit operator ColumnSpec<TType>(string name) =>
        From(name);

    /// <summary>
    /// Implicit conversion to string for ML.NET interop.
    /// </summary>
    public static implicit operator string(ColumnSpec<TType> columnSpec) =>
        columnSpec.Value;

    /// <summary>
    /// String representation of the column specification.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Changes the type parameter while keeping the same name.
    /// Use with caution - this is for type-level schema transformations.
    /// </summary>
    public ColumnSpec<TNewType> As<TNewType>() =>
        ColumnSpec<TNewType>.From(Value);
}

/// <summary>
/// Strongly-typed column specification with compile-time dimension or cardinality tracking.
/// </summary>
/// <typeparam name="TType">The type of values in the column</typeparam>
/// <typeparam name="TConst">A Constant&lt;long&gt; type representing dimension (for vectors) or cardinality (for keys)</typeparam>
/// <remarks>
/// <para>
/// This is the Phase 2 version that provides full compile-time type safety including:
/// - Element types (float, uint, string, etc.)
/// - Vector dimensions (Dim4, Dim8, etc.)
/// - Key cardinalities (Card3, Card5, etc.)
/// </para>
/// <para>
/// Usage examples:
/// <code>
/// ColumnSpec&lt;float, Dim1&gt; scalarFeature;           // 1D scalar
/// ColumnSpec&lt;float[], Dim4&gt; vectorFeatures;        // 4D vector
/// ColumnSpec&lt;uint, Card3&gt; categoryLabel;           // 3-class categorical
/// </code>
/// </para>
/// </remarks>
public readonly record struct ColumnSpec<TType, TConst>
    where TConst : Constant<long>, new() {
    private static readonly long _constantValue = new TConst().Value;

    /// <summary>
    /// The string name of the column.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The dimension (for vectors) or cardinality (for keys) value.
    /// </summary>
    public long Dimension => _constantValue;

    /// <summary>
    /// Creates a new column specification from a string name.
    /// The dimension/cardinality is determined by the TConst type parameter.
    /// </summary>
    /// <param name="name">The column name</param>
    /// <returns>A strongly-typed column reference with dimension/cardinality</returns>
    public static ColumnSpec<TType, TConst> From(string name) =>
        new() { Name = name };

    /// <summary>
    /// Implicit conversion from string for convenience.
    /// </summary>
    public static implicit operator ColumnSpec<TType, TConst>(string name) =>
        From(name);

    /// <summary>
    /// Implicit conversion to string for ML.NET interop.
    /// </summary>
    public static implicit operator string(ColumnSpec<TType, TConst> columnSpec) =>
        columnSpec.Name;

    /// <summary>
    /// Conversion to simple ColumnSpec (loses dimension/cardinality info).
    /// </summary>
    public static implicit operator ColumnSpec<TType>(ColumnSpec<TType, TConst> columnSpec) =>
        ColumnSpec<TType>.From(columnSpec.Name);

    /// <summary>
    /// String representation including dimension/cardinality.
    /// </summary>
    public override string ToString() => $"{Name} ({typeof(TType).Name}[{_constantValue}])";

    /// <summary>
    /// Changes the type parameter while keeping the same name and dimension.
    /// Use with caution - this is for type-level schema transformations.
    /// </summary>
    public ColumnSpec<TNewType, TConst> As<TNewType>() =>
        ColumnSpec<TNewType, TConst>.From(Name);

    /// <summary>
    /// Changes the dimension/cardinality while keeping the same name and type.
    /// Use with caution - this is for type-level schema transformations.
    /// </summary>
    public ColumnSpec<TType, TNewConst> WithDimension<TNewConst>()
        where TNewConst : Constant<long>, new() =>
        ColumnSpec<TType, TNewConst>.From(Name);
}
