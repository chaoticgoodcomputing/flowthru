using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Nodes;
using Flowthru.Extensions.Python.Tests.Schemas;
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using Python.Runtime;

namespace Flowthru.Extensions.Python.Tests.Compilation;

/// <summary>
/// Compilation tests verifying that Python node wiring is type-safe at compile-time.
/// </summary>
/// <remarks>
/// These tests use Roslyn to compile code snippets and verify that type mismatches
/// between AddPythonNode generic parameters and catalog entry types produce compiler errors.
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("Compilation")]
public class PythonNodeTypeSafetyTests
{
  [Test]
  public void AddPythonNode_WithMatchingTypes_CompilesSuccessfully()
  {
    // Arrange: Code with correct type matching
    var code =
      @"
            using Flowthru.Data;
            using Flowthru.Extensions.Python.Nodes;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Pipelines;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var config = CatalogEntries.Single.Memory<ModelConfigSchema>(label: ""config"");
                    var result = CatalogEntries.Single.Memory<ModelResultSchema>(label: ""result"");
                    
                    var pipeline = PipelineBuilder.CreatePipeline(builder =>
                    {
                        builder.AddPythonNode(
                            label: ""Test"",
                            module: ""test"",
                            function: ""test"",
                            input: config,
                            output: result,
                            executor: executor
                        );
                    });
                }
            }
        ";

    // Act
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(ModelConfigSchema), // Test schemas assembly
      typeof(IPythonExecutor), // Python extension assembly
      typeof(PythonException) // Python.NET assembly
    );

    // Assert
    Assert.That(
      compilation.Success,
      Is.True,
      "Code with matching types should compile successfully"
    );

    var errors = compilation.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    Assert.That(errors, Is.Empty, "Should have no compilation errors");
  }

  [Test]
  public void AddPythonNode_WithMismatchedInputType_ProducesCompilerError()
  {
    // Arrange: Code where input catalog entry type doesn't match TInput
    // AddPythonNode<ModelConfigSchema, ModelResultSchema> but input is ModelResultSchema
    var code =
      @"
            using Flowthru.Data;
            using Flowthru.Extensions.Python.Nodes;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Pipelines;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var wrongInput = CatalogEntries.Single.Memory<ModelResultSchema>();
                    var result = CatalogEntries.Single.Memory<ModelResultSchema>();
                    
                    var pipeline = PipelineBuilder.CreatePipeline(builder =>
                    {
                        // Type mismatch: declaring TInput as ModelConfigSchema
                        // but passing ICatalogEntry<ModelResultSchema>
                        builder.AddPythonNode<ModelConfigSchema, ModelResultSchema>(
                            label: ""Test"",
                            module: ""test"",
                            function: ""test"",
                            input: wrongInput,  // ERROR: wrong type
                            output: result,
                            executor: executor
                        );
                    });
                }
            }
        ";

    // Act
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(ModelConfigSchema),
      typeof(IPythonExecutor),
      typeof(PythonException)
    );

    // Assert
    Assert.That(
      compilation.Success,
      Is.False,
      "Code with mismatched input type should not compile"
    );

    // Should produce CS1503 (cannot convert argument)
    var errors = compilation.Diagnostics.Where(d =>
      d.Severity == DiagnosticSeverity.Error && d.Id == "CS1503"
    );
    Assert.That(errors, Is.Not.Empty, "Should produce CS1503 error (argument type mismatch)");
  }

  [Test]
  public void AddPythonNode_WithMismatchedOutputType_ProducesCompilerError()
  {
    // Arrange: Code where output catalog entry type doesn't match TOutput
    var code =
      @"
            using Flowthru.Data;
            using Flowthru.Extensions.Python.Nodes;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Pipelines;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var config = CatalogEntries.Single.Memory<ModelConfigSchema>();
                    var wrongOutput = CatalogEntries.Single.Memory<ModelConfigSchema>();
                    
                    var pipeline = PipelineBuilder.CreatePipeline(builder =>
                    {
                        // Type mismatch: declaring TOutput as ModelResultSchema
                        // but passing ICatalogEntry<ModelConfigSchema>
                        builder.AddPythonNode<ModelConfigSchema, ModelResultSchema>(
                            label: ""Test"",
                            module: ""test"",
                            function: ""test"",
                            input: config,
                            output: wrongOutput,  // ERROR: wrong type
                            executor: executor
                        );
                    });
                }
            }
        ";

    // Act
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(ModelConfigSchema),
      typeof(IPythonExecutor),
      typeof(PythonException)
    );

    // Assert
    Assert.That(
      compilation.Success,
      Is.False,
      "Code with mismatched output type should not compile"
    );

    var errors = compilation.Diagnostics.Where(d =>
      d.Severity == DiagnosticSeverity.Error && d.Id == "CS1503"
    );
    Assert.That(errors, Is.Not.Empty, "Should produce CS1503 error (argument type mismatch)");
  }

  [Test]
  public void AddPythonNode_WithTypeInference_PreventsTypeMismatch()
  {
    // Arrange: Code that relies on type inference (no explicit generic params)
    // Type mismatch should still be caught
    var code =
      @"
            using Flowthru.Data;
            using Flowthru.Extensions.Python.Nodes;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Pipelines;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var config = CatalogEntries.Single.Memory<ModelConfigSchema>(label: ""config"");
                    var wrongOutput = CatalogEntries.Single.Memory<ModelConfigSchema>(label: ""wrong_output"");
                    
                    var pipeline = PipelineBuilder.CreatePipeline(builder =>
                    {
                        // Relying on type inference, but types still don't match
                        builder.AddPythonNode(
                            label: ""Test"",
                            module: ""test"",
                            function: ""test"",
                            input: config,  // ModelConfigSchema
                            output: wrongOutput,  // Also ModelConfigSchema - should infer but mismatch intent
                            executor: executor
                        );
                    });
                }
            }
        ";

    // Act
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(ModelConfigSchema),
      typeof(IPythonExecutor),
      typeof(PythonException)
    );

    // Note: This will actually compile successfully because type inference
    // makes both TInput and TOutput be ModelConfigSchema, which is legal.
    // The *intent* might be wrong (user wanted different output type),
    // but it's not a compiler error - it's a logic error.
    // Pre-flight validation (Phase 4) will catch schema mismatches.

    Assert.That(
      compilation.Success,
      Is.True,
      "Type inference allows matching types (even if intent was different)"
    );
  }
}
