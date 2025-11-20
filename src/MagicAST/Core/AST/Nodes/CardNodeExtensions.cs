using MagicAST.Core.AST.Visitors;
using MagicAST.DTOs;

namespace MagicAST.Core.AST.Nodes;

/// <summary>
/// Extension methods for CardNode.
/// </summary>
public static class CardNodeExtensions
{
  /// <summary>
  /// Converts a CardNode to a serialization-friendly CardAstDto.
  /// </summary>
  /// <param name="cardNode">The CardNode to convert.</param>
  /// <returns>A CardAstDto representing the serialized form of the AST.</returns>
  /// <remarks>
  /// This method uses the DtoSerializationVisitor to convert the rich domain model
  /// into a DTO structure suitable for JSON serialization with System.Text.Json.
  /// The resulting DTO can be serialized without any custom converters.
  /// </remarks>
  public static CardAstDto AsDto(this CardNode cardNode)
  {
    var visitor = new DtoSerializationVisitor();
    var astNodeDto = cardNode.Accept(visitor);

    return new CardAstDto
    {
      Name = cardNode.Name,
      Ast = astNodeDto,
      Diagnostics = cardNode.Diagnostics.Select(DiagnosticDto.FromDiagnostic).ToList(),
    };
  }

  /// <summary>
  /// Converts a CardNode to a CardOutputDto for pipeline output.
  /// </summary>
  /// <param name="cardNode">The CardNode to convert.</param>
  /// <returns>A CardOutputDto with AST and diagnostics.</returns>
  public static CardOutputDto ToCardOutputDto(this CardNode cardNode)
  {
    var hasErrors = cardNode.Diagnostics.Any(d =>
      d.Severity == Core.Diagnostics.DiagnosticSeverity.Error
    );

    return new CardOutputDto
    {
      AST = cardNode.AsDto(),
      Diagnostics = cardNode.Diagnostics.Select(DiagnosticDto.FromDiagnostic).ToList(),
      ParseSucceeded = !hasErrors,
    };
  }
}
