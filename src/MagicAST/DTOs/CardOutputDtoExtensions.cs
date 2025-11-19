using MagicAST.Core.AST.Nodes;
using MagicAST.Core.Diagnostics;

namespace MagicAST.DTOs;

/// <summary>
/// Extension methods for CardOutputDto.
/// </summary>
public static class CardOutputDtoExtensions
{
  /// <summary>
  /// Creates a CardOutputDto from a CardNode.
  /// </summary>
  /// <param name="cardNode">The CardNode to convert.</param>
  /// <returns>A CardOutputDto wrapping the AST and diagnostics.</returns>
  /// <remarks>
  /// This is a convenience method that combines CardNode.AsDto() with
  /// the necessary wrapping logic for CardOutputDto.
  /// </remarks>
  public static CardOutputDto FromCardNode(CardNode cardNode)
  {
    var astDto = cardNode.AsDto();
    var hasErrors = cardNode.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    return new CardOutputDto
    {
      AST = astDto,
      Diagnostics = astDto.Diagnostics,
      ParseSucceeded = !hasErrors,
    };
  }
}
