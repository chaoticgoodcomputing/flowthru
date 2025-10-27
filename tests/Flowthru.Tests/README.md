# Flowthru.Tests

Test suite for Flowthru's fail-fast pipeline construction, verifying type safety, graph validation, and execution behavior across three distinct error-detection phases.

## Table of Contents
- [Getting Started](#getting-started)
- [Testing Philosophy](#testing-philosophy)
- [Writing Tests](#writing-tests)
- [Test Structure Reference](#test-structure-reference)

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- NUnit 4.2.2 (included via package)
- Microsoft.CodeAnalysis.CSharp 4.12.0 (for compilation tests)

### Running Tests

Run all tests:
```bash
nx run test/unit
```

Run phase-specific tests using NX targets:
```bash
nx run test/unit:compilation  # Type safety tests only
nx run test/unit:validation   # Graph construction tests only
nx run test/unit:execution    # Node execution tests only
```

Run tests by category filter:
```bash
dotnet test --filter "Category=Validation"
dotnet test --filter "Category=GraphConstruction"
```

### Tutorial: Writing Tests for New Features

When adding new Flowthru features, prefer the earliest possible error detection phase:

1. **First Choice: Compilation Phase**
   
   If your feature involves types, properties, or interfaces that can be validated by the C# compiler, write compilation tests. These catch errors before any code runs.

   Example: Testing that catalog property access is type-safe:
   ```csharp
   [Test]
   [Category("Compilation")]
   [Category("TypeSafety")]
   public void CatalogProperty_WhenTypo_ProducesCompilerError() {
       var code = @"
           var catalog = new MyCatalog();
           var data = catalog.Inpt; // Typo
       ";
       
       var compilation = CompilationTestHelper.Compile(code, includeFlowthru: true);
       
       Assert.That(compilation.Success, Is.False);
       Assert.That(compilation.Diagnostics,
           Has.Some.Matches<Diagnostic>(d => d.Id == "CS1061"));
   }
   ```

2. **Second Choice: Validation Phase**
   
   If errors can't be caught at compile time but can be detected during `Pipeline.Build()`, write validation tests. These catch structural errors before execution.

   Example: Testing that multiple writers to the same catalog entry are detected:
   ```csharp
   [Test]
   [Category("Validation")]
   [Category("GraphConstruction")]
   public void Build_WhenMultipleWriters_ThrowsInvalidOperationException() {
       var pipeline = PipelineBuilder.CreatePipeline(builder => {
           builder.AddNode<NodeA>(catalog.Input, catalog.Output);
           builder.AddNode<NodeB>(catalog.Input, catalog.Output); // Conflict!
       });
       
       var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
       Assert.That(ex!.Message, Does.Contain("multiple").IgnoreCase);
   }
   ```

3. **Last Resort: Execution Phase**
   
   Only use execution tests for errors that can only be detected during `Pipeline.RunAsync()`, such as node logic failures or data processing errors.

   Example: Testing node transformation logic:
   ```csharp
   [Test]
   [Category("Execution")]
   [Category("NodeExecution")]
   public async Task Execute_WithIncrementNode_IncrementsValue() {
       var catalog = new TestCatalog();
       await catalog.Input.Save(new[] { new Data { Value = 5 } }).Run();
       
       var pipeline = PipelineBuilder.CreatePipeline(builder => {
           builder.AddNode<IncrementNode>(catalog.Input, catalog.Output);
       });
       pipeline.Build();
       
       await pipeline.RunAsync();
       
       var result = await catalog.Output.Load().Run();
       Assert.That(result.First().Value, Is.EqualTo(6));
   }
   ```

**Key Principle**: Move errors left. Compilation catches more than validation; validation catches more than execution. Prefer the earliest phase that can detect your error condition.

### Tutorial: Writing Tests for Bug Fixes

When fixing bugs, write tests that reproduce the error condition first:

1. **Identify the Error Phase**
   
   Determine when the bug manifests:
   - Does it cause compilation errors? → Compilation test
   - Does it create invalid pipeline graphs? → Validation test
   - Does it produce wrong results or crash during execution? → Execution test

2. **Write the Failing Test**
   
   Create a test that reproduces the bug. This test should initially fail, documenting the expected behavior:
   ```csharp
   [Test]
   [Category("Validation")]
   public void Build_WhenSelfLoop_ThrowsInvalidOperationException() {
       // Reproduce the bug: self-referential node
       var pipeline = PipelineBuilder.CreatePipeline(builder => {
           builder.AddNode<PassthroughNode>(catalog.Data, catalog.Data);
       });
       
       // Document expected behavior (currently fails)
       var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
       Assert.That(ex!.Message, Does.Contain("self").IgnoreCase);
   }
   ```

3. **Fix the Bug**
   
   Implement the fix in the main Flowthru library.

4. **Verify the Test Passes**
   
   Run your test to confirm it now passes:
   ```bash
   dotnet test --filter "FullyQualifiedName~Build_WhenSelfLoop"
   ```

**Test Naming for Bugs**: Use descriptive names that indicate the error condition: `Method_WhenBugCondition_ExpectedBehavior`.

## Testing Philosophy

### The Three-Phase Error Model

Flowthru's architecture enables fail-fast error detection across three distinct phases:

1. **Compilation Phase**
   - **When**: During `dotnet build` or IDE compilation
   - **What**: Type errors, missing properties, incorrect generics
   - **How**: C# compiler produces CS#### diagnostic errors
   - **Example**: Accessing a non-existent catalog property

2. **Validation Phase**
   - **When**: During `Pipeline.Build()` execution
   - **What**: Graph structure errors, DAG violations, multiple writers
   - **How**: Pipeline builder throws `InvalidOperationException`
   - **Example**: Circular dependencies in pipeline graph

3. **Execution Phase**
   - **When**: During `Pipeline.RunAsync()` execution
   - **What**: Node logic failures, data processing errors
   - **How**: Errors returned in `PipelineResult.Exception`
   - **Example**: Node transformation throws exception

### Why Test Error Conditions

Testing what *shouldn't* work is as important as testing what *should* work:

- **Fail-fast validation**: Verify errors are caught at the earliest possible phase
- **Clear error messages**: Ensure developers receive actionable feedback
- **Regression prevention**: Document expected error behavior to prevent accidental removal
- **API contract enforcement**: Validate that invalid usage is properly rejected

Each test file includes both success cases (correct usage) and error conditions (incorrect usage that should be rejected).

## Writing Tests

### Test Naming Convention

Follow the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

Examples:
- `CatalogProperty_WhenTypo_ProducesCompilerError`
- `Build_WhenCircularDependency_ThrowsException`
- `Execute_WithPassthroughNode_PreservesData`

### Test Structure (AAA Pattern)

```csharp
[Test]
public void Method_WhenCondition_ExpectedOutcome() {
    // ===========
    // Arrange: Set up test data and dependencies
    // ===========
    var catalog = new TestCatalog();
    var testData = new[] { /* ... */ };
    
    // ===========
    // Act: Execute the operation being tested
    // ===========
    var result = PerformOperation(testData);
    
    // ===========
    // Assert: Verify expected behavior
    // ===========
    Assert.That(result, Is.EqualTo(expectedValue));
}
```

### Pattern: Compilation Tests

Use `CompilationTestHelper` to verify C# compiler behavior:

```csharp
[Test]
[Category("Compilation")]
[Category("TypeSafety")]
public void Feature_WhenInvalidCode_ProducesCompilerError() {
    // Arrange: Write invalid C# code as string
    var code = @"
        using Flowthru.Data;
        var catalog = new MyCatalog();
        var invalid = catalog.NonExistentProperty;
    ";
    
    // Act: Compile the code
    var compilation = CompilationTestHelper.Compile(code, includeFlowthru: true);
    
    // Assert: Verify compilation fails with expected error
    Assert.That(compilation.Success, Is.False);
    Assert.That(compilation.Diagnostics,
        Has.Some.Matches<Diagnostic>(d => 
            d.Id == "CS1061" && 
            d.Severity == DiagnosticSeverity.Error));
}
```

**Success case example**:
```csharp
[Test]
[Category("Compilation")]
public void Feature_WhenValidCode_CompilesSuccessfully() {
    var code = @"
        var catalog = new MyCatalog();
        var valid = catalog.Input; // Correct property
    ";
    
    var compilation = CompilationTestHelper.Compile(code, includeFlowthru: true);
    
    Assert.That(compilation.Success, Is.True);
}
```

### Pattern: Validation Tests

Use `Pipeline.Build()` to verify graph construction errors:

```csharp
[Test]
[Category("Validation")]
[Category("GraphConstruction")]
public void Build_WhenInvalidGraph_ThrowsException() {
    // Arrange: Create invalid pipeline structure
    var catalog = new TestCatalog();
    var pipeline = PipelineBuilder.CreatePipeline(builder => {
        builder.AddNode<NodeA>(catalog.Input, catalog.Output);
        builder.AddNode<NodeB>(catalog.Output, catalog.Input); // Creates cycle
    });
    
    // Act & Assert: Verify Build() throws
    var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
    Assert.That(ex!.Message, Does.Contain("circular").IgnoreCase);
}
```

**Success case example**:
```csharp
[Test]
[Category("Validation")]
public void Build_WhenValidGraph_Succeeds() {
    var pipeline = PipelineBuilder.CreatePipeline(builder => {
        builder.AddNode<NodeA>(catalog.Input, catalog.StepOne);
        builder.AddNode<NodeB>(catalog.StepOne, catalog.Output);
    });
    
    Assert.DoesNotThrow(() => pipeline.Build());
}
```

### Pattern: Execution Tests

Use `Pipeline.RunAsync()` to verify runtime behavior:

```csharp
[Test]
[Category("Execution")]
[Category("NodeExecution")]
public async Task Execute_WithFailingNode_ReturnsError() {
    // Arrange: Set up pipeline with node that throws
    var catalog = new TestCatalog();
    await catalog.Input.Save(testData).Run();
    
    var pipeline = PipelineBuilder.CreatePipeline(builder => {
        builder.AddNode<FailingNode>(catalog.Input, catalog.Output);
    });
    pipeline.Build();
    
    // Act: Execute pipeline
    var result = await pipeline.RunAsync();
    
    // Assert: Verify error captured in result
    Assert.That(result.Success, Is.False);
    Assert.That(result.Exception, Is.Not.Null);
}
```

**Success case example**:
```csharp
[Test]
[Category("Execution")]
public async Task Execute_WithTransformNode_TransformsData() {
    var catalog = new TestCatalog();
    await catalog.Input.Save(new[] { new Data { Value = 5 } }).Run();
    
    var pipeline = PipelineBuilder.CreatePipeline(builder => {
        builder.AddNode<IncrementNode>(catalog.Input, catalog.Output);
    });
    pipeline.Build();
    
    await pipeline.RunAsync();
    
    var result = await catalog.Output.Load().Run();
    Assert.That(result.First().Value, Is.EqualTo(6));
}
```

### Using Test Fixtures

The `_Fixtures/` directory provides reusable components:

- **CompilationTestHelper**: Roslyn-based C# compiler for compilation tests
- **TestCatalogs**: Pre-configured data catalogs (SimpleThreeNodeCatalog, etc.)
- **TestNodes**: Reusable node implementations (PassthroughNode, IncrementNode, FailingNode, etc.)

Example usage:
```csharp
[TestFixture]
public class MyTests {
    private SimpleThreeNodeCatalog _catalog = null!;
    
    [SetUp]
    public void SetUp() {
        _catalog = new SimpleThreeNodeCatalog();
    }
    
    [Test]
    public void MyTest() {
        // Use _catalog.Input, _catalog.StepOne, etc.
    }
}
```

## Test Structure Reference

### Directory Organization

```
Flowthru.Tests/
├── Compilation/
│   └── TypeSafety/           # Compiler-level type checking
├── Validation/
│   └── GraphConstruction/    # Pipeline.Build() graph validation
├── Execution/
│   └── NodeExecution/        # Pipeline.RunAsync() runtime behavior
└── _Fixtures/
    ├── CompilationTestHelper.cs
    ├── TestCatalogs/
    └── TestNodes/
```

### Test Categories

Tests use NUnit `[Category]` attributes for filtering:

| Category            | Description                  | Phase       |
| ------------------- | ---------------------------- | ----------- |
| `Compilation`       | C# compiler behavior         | Compilation |
| `TypeSafety`        | Type system enforcement      | Compilation |
| `Validation`        | Pipeline.Build() checks      | Validation  |
| `GraphConstruction` | DAG structure validation     | Validation  |
| `Execution`         | Pipeline.RunAsync() behavior | Execution   |
| `NodeExecution`     | Node Transform() logic       | Execution   |

### NX Test Targets

Defined in `project.json`:

| Target             | Command                            | Description                 |
| ------------------ | ---------------------------------- | --------------------------- |
| `test`             | `dotnet test`                      | Run all tests               |
| `test:compilation` | `--filter "Category=Compilation"`  | Compilation phase only      |
| `test:validation`  | `--filter "Category=Validation"`   | Validation phase only       |
| `test:execution`   | `--filter "Category=Execution"`    | Execution phase only        |
| `test:fast`        | `--filter "Category!=Compilation"` | Skip slow compilation tests |

### Compilation Test Infrastructure

Uses **Microsoft.CodeAnalysis.CSharp** (Roslyn) to compile C# code dynamically:

```csharp
var compilation = CompilationTestHelper.Compile(code, includeFlowthru: true);
```

Returns:
- `compilation.Success` (bool) - Whether code compiled
- `compilation.Diagnostics` (ImmutableArray<Diagnostic>) - All compiler messages
- Diagnostics have `.Id` (e.g., "CS1061"), `.Severity`, `.GetMessage()`

### Test Data Fixtures

**TestCatalogs** provide pre-configured catalog structures:
- `SimpleThreeNodeCatalog`: Input → StepOne → StepTwo → Output
- `EmptyCatalog`: No entries
- `ComplexMultiLayerCatalog`: Multi-stage pipeline structure

**TestNodes** implement common transformation patterns:
- `PassthroughNode`: Returns input unchanged
- `IncrementNode`: Increments TestData.Id
- `DoubleValueNode`: Doubles TestData.Value
- `FailingNode`: Always throws exception

All nodes follow the pattern:
```csharp
public class MyNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams> {
    protected override async Task<IEnumerable<TestData>> Transform(
        IEnumerable<TestData> input,
        NoParams parameters) {
        // Transformation logic
    }
}
```
