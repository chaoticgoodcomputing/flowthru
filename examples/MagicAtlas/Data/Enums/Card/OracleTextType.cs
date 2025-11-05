using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Types of oracle text entries for embedding model input.
/// </summary>
public enum OracleTextType
{
  /// <summary>
  /// Full oracle text of the card.
  /// </summary>
  [SerializedEnum("raw")]
  Full,

  /// <summary>
  /// A keyword ability with its effect.
  /// </summary>
  [SerializedEnum("kw")]
  KeywordAbility,

  /// <summary>
  /// An activated ability with costs and effect.
  /// </summary>
  [SerializedEnum("act")]
  ActivatedAbility,

  /// <summary>
  /// A triggered ability with trigger and effect.
  /// </summary>
  [SerializedEnum("trig")]
  TriggeredAbility,

  /// <summary>
  /// A passive ability or static effect.
  /// </summary>
  [SerializedEnum("pass")]
  PassiveAbility,
}
