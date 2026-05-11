// netstandard2.0 doesn't ship IsExternalInit, which the C# 9+ `init`
// modifier needs. Polyfilling lets the source-gen project use modern
// language features without adding a runtime dependency.

namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }
