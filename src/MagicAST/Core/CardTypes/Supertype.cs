namespace MagicAST.Core.CardTypes;

/// <summary>
/// Represents supertypes that can modify card types.
/// Supertypes appear before the card type on the type line.
/// </summary>
public enum Supertype
{
  /// <summary>
  /// Basic supertype (applies to lands).
  /// </summary>
  Basic,

  /// <summary>
  /// Legendary supertype (applies to any permanent).
  /// </summary>
  Legendary,

  /// <summary>
  /// Snow supertype (applies to any permanent).
  /// </summary>
  Snow,

  /// <summary>
  /// World supertype (deprecated).
  /// </summary>
  World,

  /// <summary>
  /// Ongoing supertype (applies to schemes).
  /// </summary>
  Ongoing,

  /// <summary>
  /// Elite supertype (applies to creatures).
  /// </summary>
  Elite,

  /// <summary>
  /// Token supertype (not printed on cards but used in game).
  /// </summary>
  Token,
}
