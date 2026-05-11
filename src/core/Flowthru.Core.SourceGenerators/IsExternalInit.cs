// Polyfill: required to use C# records and init-only setters when
// targeting netstandard2.0. The compiler emits references to this type
// for `init` accessors; netstandard2.0 does not include it.
namespace System.Runtime.CompilerServices
{
  internal sealed class IsExternalInit { }
}
