// netstandard2.0 doesn't ship System.Runtime.CompilerServices.IsExternalInit
// (added in net5.0). C# 9 record types with init-only properties need this
// type to exist somewhere — the compiler binds the marker on init-only
// setters to whatever it can find. Defining it internally in this assembly
// satisfies the compiler without taking a runtime dependency.
//
// Same workaround applies anywhere we want to use records in a Roslyn
// analyzer / source generator project — Roslyn targets netstandard2.0.
//
// See also: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init#metadata-encoding

namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit { }
}
