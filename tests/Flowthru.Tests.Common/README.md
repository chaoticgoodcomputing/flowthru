# Flowthru.Tests.Common

Shared test infrastructure for compilation testing across Flowthru test projects.

## Overview

This library provides reusable testing utilities for verifying compile-time type safety using Roslyn (Microsoft.CodeAnalysis.CSharp). It enables negative compilation tests that assert certain code patterns fail to compile, demonstrating the value of type-safe wrappers like Flowthru.ML.Next.

## Purpose

Compilation tests serve multiple goals:

1. **Type Safety Verification**: Prove that type-safe abstractions prevent incorrect usage at compile-time
2. **Regression Prevention**: Ensure type constraints aren't accidentally weakened in refactoring
3. **Documentation**: Show developers what patterns are prevented and why
4. **Comparison**: Demonstrate advantages over raw libraries (compile-time vs runtime errors)

## Key Components

### CompilationTestHelper

Static class providing methods for compiling C# code snippets using Roslyn:

```csharp
// For testing Flowthru library
var result = CompilationTestHelper.Compile(code, includeFlowthru: true);

// For testing Flowthru.ML.Next library
var result = CompilationTestHelper.CompileWithMLExt(code);
```

### CompilationResult

Result object containing compilation diagnostics:

```csharp
public class CompilationResult {
  public bool Success { get; init; }      // True if no errors
  public List<Diagnostic> Diagnostics { get; init; }  // All diagnostics (errors, warnings)
}
```

## Usage Pattern

### 1. Write Test Code That Should NOT Compile

```csharp
[Test]
public void Schema_Mismatch_Should_Not_Compile() {
  var code = @"
    using Flowthru.ML.Next.Core.Schema;
    using Flowthru.ML.Next.Transform;

    public interface ISchema1 : ISchemaDefinition { }
    public interface ISchema2 : ISchemaDefinition { }
    public interface ISchema3 : ISchemaDefinition { }

    public class Test {
      public void Execute() {
        var step1 = new Transformer<ISchema1, ISchema2>(null!);
        var step2 = new Transformer<ISchema3, ISchema1>(null!);
        
        // This should NOT compile: ISchema2 != ISchema3
        var pipeline = step1.Append(step2);
      }
    }
  ";

  var result = CompilationTestHelper.CompileWithMLExt(code);
  
  Assert.That(result.Success, Is.False, "Code with schema mismatch should not compile");
  
  var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
  var hasTypeMismatch = errors.Any(e => e.Id == "CS1503" || e.ToString().Contains("cannot convert"));
  Assert.That(hasTypeMismatch, Is.True, "Should have type mismatch error");
}
```

### 2. Mark Tests with [Category("Compilation")]

```csharp
[TestFixture]
[Category("Compilation")]
[Category("TypeSafety")]
public class MyCompilationTests {
  // Tests here...
}
```

### 3. Run Compilation Tests

```bash
# Via dotnet
dotnet test --filter "Category=Compilation"

# Via Nx
nx test MyProject --filter "Category=Compilation"
```

## Implementation Details

### Compile() Method

For testing core Flowthru library:

```csharp
public static CompilationResult Compile(string code, bool includeFlowthru = false)
```

**References Included:**
- Basic .NET runtime assemblies (System.Runtime, System.Collections, etc.)
- When `includeFlowthru = true`:
  - Flowthru.dll
  - LanguageExt.Core.dll
  - Test assembly itself (for test fixtures)

### CompileWithMLExt() Method

For testing Flowthru.ML.Next library:

```csharp
public static CompilationResult CompileWithMLExt(string code)
```

**References Included:**
- All basic .NET runtime assemblies
- Microsoft.ML.dll
- Flowthru.ML.Next.dll
- LanguageExt.Core.dll (for Fin, Validation, etc.)

## Common Error Codes

| Code   | Meaning                                   | Usage                              |
| ------ | ----------------------------------------- | ---------------------------------- |
| CS0029 | Cannot implicitly convert type            | Verifies explicit type constraints |
| CS0266 | Cannot convert (explicit cast missing)    | Verifies no implicit conversions   |
| CS0305 | Generic method requires type arguments    | Verifies type inference failures   |
| CS0311 | Type cannot be used as type parameter     | Verifies generic constraints       |
| CS0411 | Type arguments cannot be inferred         | Verifies schema propagation        |
| CS1503 | Argument type mismatch                    | Verifies method parameter types    |
| CS1729 | Type doesn't contain matching constructor | Verifies instantiation constraints |

## Projects Using This Library

- **Flowthru.Tests**: Compilation tests for core Flowthru library
  - `01_Compilation/TypeSafety/CatalogPropertyCompilationTests.cs`
  
- **Flowthru.Tests.ML.Next.Samples**: Compilation tests for ML.Next wrappers
  - `Clustering_Iris/ClusteringIrisCompilationTests.cs` (7 tests)
  - `MulticlassClassification_Iris/MulticlassClassificationIrisCompilationTests.cs` (8 tests)

## Dependencies

- **.NET 9.0**
- **Microsoft.CodeAnalysis.CSharp 4.12.0** - Roslyn compiler API
- **NUnit 4.2.2** - Test framework
- **Microsoft.ML 4.0.3** - For ML.Next compilation tests
- **LanguageExt.Core 5.0.0-beta-54** - For monadic types
- **Flowthru** (project reference)
- **Flowthru.ML.Next** (project reference)

## Design Rationale

### Why Shared Infrastructure?

Originally, `CompilationTestHelper` existed in `Flowthru.Tests/_Fixtures/`. When adding ML.Next, we needed:

1. **Reusability**: Same Roslyn compilation pattern across multiple test projects
2. **Maintainability**: Single source of truth for compilation testing logic
3. **Extensibility**: Easy to add new `CompileWith*()` methods for other libraries
4. **Consistency**: Ensure all projects use the same testing approach

### Why Roslyn?

Roslyn (Microsoft.CodeAnalysis) provides:

- **In-memory compilation**: No need for temporary files or external compiler processes
- **Diagnostic access**: Precise error codes and messages for assertions
- **Fast execution**: Tests run in milliseconds
- **Programmatic control**: Fine-grained control over references and compilation options

## Best Practices

### ✅ Do:
- Use `[Explicit]` for positive control tests (tests that SHOULD compile)
- Assert specific error codes when possible (e.g., `CS1503`)
- Include descriptive comments explaining WHY code shouldn't compile
- Keep test code snippets focused and minimal
- Use `nameof()` for column names to test typo protection

### ❌ Don't:
- Don't test for general compilation errors (CS0246 "type not found")
- Don't assume specific error messages (they may change between compiler versions)
- Don't create overly complex test code (focus on one mistake per test)
- Don't use compilation tests for runtime behavior testing

## Example Test Suite

See `Flowthru.Tests.ML.Next.Samples` for a complete example:
- 13 negative compilation tests (verifying incorrect patterns don't compile)
- 2 positive control tests (verifying correct patterns DO compile)
- Comprehensive coverage of ML.Next type safety features

## Contributing

When adding new compilation tests:

1. Create test file in appropriate project (Flowthru.Tests or ML.Next.Samples)
2. Import `Flowthru.Tests.Common` namespace
3. Use `CompilationTestHelper.Compile()` or `.CompileWithMLExt()`
4. Mark tests with `[Category("Compilation")]`
5. Document the pattern being prevented
6. Consider adding a positive control test with `[Explicit]`

## License

See LICENSE file in repository root.
