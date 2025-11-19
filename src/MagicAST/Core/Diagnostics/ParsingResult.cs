using System.Collections.Immutable;
using Superpower.Model;

namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Wraps Superpower's parsing results with MagicAST diagnostics.
/// Bridges Superpower's error model to our diagnostic system.
/// </summary>
public sealed class ParsingResult<T>
{
  /// <summary>
  /// The parsed value, or default if parsing failed.
  /// </summary>
  public T? Value { get; }

  /// <summary>
  /// True if parsing succeeded.
  /// </summary>
  public bool HasValue { get; }

  /// <summary>
  /// Diagnostics collected during parsing.
  /// Empty if parsing succeeded without warnings.
  /// </summary>
  public ImmutableArray<Diagnostic> Diagnostics { get; }

  private ParsingResult(T? value, bool hasValue, ImmutableArray<Diagnostic> diagnostics)
  {
    Value = value;
    HasValue = hasValue;
    Diagnostics = diagnostics;
  }

  /// <summary>
  /// Creates a successful result.
  /// </summary>
  public static ParsingResult<T> Success(T value, ImmutableArray<Diagnostic>? warnings = null)
  {
    return new ParsingResult<T>(value, true, warnings ?? ImmutableArray<Diagnostic>.Empty);
  }

  /// <summary>
  /// Creates a failed result.
  /// </summary>
  public static ParsingResult<T> Failed(ImmutableArray<Diagnostic> diagnostics)
  {
    return new ParsingResult<T>(default, false, diagnostics);
  }

  /// <summary>
  /// Creates a failed result with a single diagnostic.
  /// </summary>
  public static ParsingResult<T> Failed(Diagnostic diagnostic)
  {
    return new ParsingResult<T>(default, false, ImmutableArray.Create(diagnostic));
  }

  /// <summary>
  /// Creates a failed result from a Superpower Result (for text parsers).
  /// </summary>
  public static ParsingResult<T> FromSuperpowerError(
    Result<T> superpowerResult,
    DiagnosticDescriptor descriptor,
    SourceText sourceText,
    params object?[] messageArgs
  )
  {
    var diagnostic = CreateDiagnosticFromResult(
      superpowerResult,
      descriptor,
      sourceText,
      messageArgs
    );

    return new ParsingResult<T>(default, false, ImmutableArray.Create(diagnostic));
  }

  /// <summary>
  /// Creates a failed result from a Superpower TokenListParserResult (for token parsers).
  /// </summary>
  public static ParsingResult<T> FromSuperpowerError<TKind>(
    TokenListParserResult<TKind, T> superpowerResult,
    DiagnosticDescriptor descriptor,
    SourceText sourceText,
    string? sourcePath = null,
    params object?[] messageArgs
  )
  {
    var diagnostic = CreateDiagnosticFromTokenResult(
      superpowerResult,
      descriptor,
      sourceText,
      sourcePath,
      messageArgs
    );

    return new ParsingResult<T>(default, false, ImmutableArray.Create(diagnostic));
  }

  private static Diagnostic CreateDiagnosticFromResult<TValue>(
    Result<TValue> result,
    DiagnosticDescriptor descriptor,
    SourceText sourceText,
    object?[] messageArgs
  )
  {
    // Convert Superpower's Position to our Location
    var position = result.ErrorPosition;
    var location = Location.FromSuperpowerPosition(position, sourceText);

    // Enhance message with Superpower's expectations
    var enhancedArgs = messageArgs.ToList();
    if (result.Expectations?.Length > 0)
    {
      var expectedList = string.Join(", ", result.Expectations);
      enhancedArgs.Add($", expected {expectedList}");
    }
    else if (!string.IsNullOrEmpty(result.ErrorMessage))
    {
      // If there's a custom error message from Superpower, append it
      enhancedArgs.Add(result.ErrorMessage);
    }

    return Diagnostic.Create(descriptor, location, enhancedArgs.ToArray());
  }

  private static Diagnostic CreateDiagnosticFromTokenResult<TKind, TValue>(
    TokenListParserResult<TKind, TValue> result,
    DiagnosticDescriptor descriptor,
    SourceText sourceText,
    string? sourcePath,
    object?[] messageArgs
  )
  {
    // Use SubTokenErrorPosition if available, otherwise token position
    var position = result.ErrorPosition;

    // Get the actual token text for better error messages
    string? tokenText = null;
    int tokenLength = 1;
    if (!result.Remainder.IsAtEnd)
    {
      var token = result.Remainder.ConsumeToken().Value;
      tokenText = token.ToStringValue();
      tokenLength = tokenText?.Length ?? 1;
    }

    var location = Location.FromSuperpowerPosition(position, sourceText, tokenLength, sourcePath);

    // Enhance with token-specific information
    var enhancedArgs = messageArgs.ToList();

    // If we have token text and the message doesn't already include it, add it
    if (tokenText != null && !messageArgs.Any(a => a?.ToString() == tokenText))
    {
      enhancedArgs.Insert(0, tokenText);
    }

    // Add expectations if available
    if (result.Expectations?.Length > 0)
    {
      var expectedList = string.Join(", ", result.Expectations);
      enhancedArgs.Add($", expected {expectedList}");
    }
    else if (!string.IsNullOrEmpty(result.ErrorMessage))
    {
      // Custom error message from Superpower
      enhancedArgs.Add(result.ErrorMessage);
    }

    return Diagnostic.Create(descriptor, location, enhancedArgs.ToArray());
  }
}
