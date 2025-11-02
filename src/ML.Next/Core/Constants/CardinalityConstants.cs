namespace ML.Next.Core.Constants;

/// <summary>
/// Type-level cardinality constants for key (categorical) columns.
/// These enable compile-time cardinality tracking using phantom types.
/// </summary>
/// <remarks>
/// <para>
/// Key types in ML.NET represent categorical variables with a fixed number of classes.
/// The cardinality indicates how many distinct categories exist.
/// </para>
/// <para>
/// Usage example:
/// <code>
/// ColumnSpec&lt;uint, Card3&gt; speciesLabel; // 3-class categorical (Iris: Setosa, Versicolor, Virginica)
/// </code>
/// </para>
/// <para>
/// The cardinality value is accessible at both compile-time (via type parameter)
/// and runtime (via <c>new Card3()</c> constructor).
/// </para>
/// </remarks>
public static class CardinalityConstants
{
  /// <summary>2 classes (binary classification)</summary>
  public sealed class Card2 : Constant<long>
  {
    public Card2()
      : base(2) { }
  }

  /// <summary>3 classes (e.g., Iris species)</summary>
  public sealed class Card3 : Constant<long>
  {
    public Card3()
      : base(3) { }
  }

  /// <summary>4 classes</summary>
  public sealed class Card4 : Constant<long>
  {
    public Card4()
      : base(4) { }
  }

  /// <summary>5 classes</summary>
  public sealed class Card5 : Constant<long>
  {
    public Card5()
      : base(5) { }
  }

  /// <summary>10 classes (e.g., MNIST digits)</summary>
  public sealed class Card10 : Constant<long>
  {
    public Card10()
      : base(10) { }
  }

  /// <summary>26 classes (e.g., alphabet)</summary>
  public sealed class Card26 : Constant<long>
  {
    public Card26()
      : base(26) { }
  }

  /// <summary>100 classes</summary>
  public sealed class Card100 : Constant<long>
  {
    public Card100()
      : base(100) { }
  }

  /// <summary>1000 classes</summary>
  public sealed class Card1000 : Constant<long>
  {
    public Card1000()
      : base(1000) { }
  }
}
