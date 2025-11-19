namespace MagicAST.DTOs;

/// <summary>
/// Serializable AST representation for a Magic: The Gathering card.
/// Uses a recursive AstNodeDto structure that mirrors the visitor pattern.
/// </summary>
public record CardAstDto
{
  /// <summary>
  /// Card name. For split cards, use "Name1 // Name2" format.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Root AST node representing the complete card structure.
  /// </summary>
  public required AstNodeDto Ast { get; init; }

  /// <summary>
  /// Parse diagnostics (errors, warnings, info messages).
  /// </summary>
  public List<DiagnosticDto> Diagnostics { get; init; } = new();
}

/// <summary>
/// Recursive AST node DTO that can represent any node in the tree.
/// Mirrors the ASTNode visitor pattern structure.
/// </summary>
public record AstNodeDto
{
  /// <summary>
  /// Node type discriminator (e.g., "Card", "Ability", "Effect", "Cost", "Expression", "Filter").
  /// </summary>
  public required string NodeType { get; init; }

  /// <summary>
  /// Specific type within the category (e.g., "ActivatedAbility", "DealDamage", "StaticValue").
  /// </summary>
  public string? SubType { get; init; }

  /// <summary>
  /// Properties specific to this node type.
  /// Stores primitive values and simple structures.
  /// </summary>
  public Dictionary<string, object?> Properties { get; init; } = new();

  /// <summary>
  /// Child nodes in the AST.
  /// Enables recursive traversal matching the visitor pattern.
  /// </summary>
  public List<AstNodeDto> Children { get; init; } = new();
}
