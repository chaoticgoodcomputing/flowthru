namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Canonical CLR-type names the Python extension's Arrow marshaller
/// supports as leaves. Compiled into both the runtime extension and
/// the Roslyn source-generator project via a linked <c>Compile</c>
/// item so the two cannot drift. Recursive shapes
/// (<see cref="System.Nullable{T}"/>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>T[]</c>, <c>enum</c>) are handled by dispatchers above this
/// list — only leaf names belong here.
/// </summary>
internal static class PythonMarshallableTypeNames
{
  internal static readonly string[] All =
  {
    "System.Int32",
    "System.Int64",
    "System.Single",
    "System.Double",
    "System.Boolean",
    "System.String",
    "System.DateTime",
    "System.DateTimeOffset",
    "System.TimeSpan",
    "System.Guid",
    "System.Decimal",
    "System.Byte[]",
  };
}
