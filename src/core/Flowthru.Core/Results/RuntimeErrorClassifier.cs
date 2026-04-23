using System.Net.Sockets;

namespace Flowthru.Core.Results;

/// <summary>
/// Classifies runtime exceptions as external/environmental or possible framework bugs
/// using heuristic type matching.
/// </summary>
public static class RuntimeErrorClassifier
{
  private static readonly HashSet<Type> ExternalExceptionTypes =
  [
    typeof(HttpRequestException),
    typeof(SocketException),
    typeof(IOException),
    typeof(OutOfMemoryException),
    typeof(OperationCanceledException),
    typeof(TaskCanceledException),
    typeof(TimeoutException),
    typeof(UnauthorizedAccessException),
  ];

  /// <summary>
  /// Classifies the given exception based on its type hierarchy.
  /// </summary>
  /// <remarks>
  /// Walks the exception type's inheritance chain and checks inner exceptions.
  /// Any match against known external/environmental exception types produces
  /// <see cref="ErrorClassification.ExternalError"/>.
  /// </remarks>
  public static ErrorClassification Classify(Exception exception)
  {
    if (IsExternal(exception))
      return ErrorClassification.ExternalError;

    if (exception.InnerException is not null && IsExternal(exception.InnerException))
      return ErrorClassification.ExternalError;

    return ErrorClassification.PossibleFrameworkBug;
  }

  private static bool IsExternal(Exception exception)
  {
    var type = exception.GetType();
    while (type is not null && type != typeof(object))
    {
      if (ExternalExceptionTypes.Contains(type))
        return true;
      type = type.BaseType;
    }

    return false;
  }
}
