using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Extensions.Python.Tests.Schemas;
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using Python.Runtime;

namespace Flowthru.Extensions.Python.Tests.Compilation;

/// <summary>
/// Compilation tests verifying that Python node wiring is type-safe at compile-time.
/// </summary>
/// <remarks>
/// These tests verify that the catalog entry types passed to AddPythonStep are
/// self-consistent and correctly inferred by the compiler.
///
/// Note: Schema contract violations between C# generic types and Python @step
/// decorator declarations are caught at pre-flight, not compile-time — see
/// <see cref="Flowthru.Extensions.Python.Tests.Validation.PythonStepValidationTests"/>.
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("Compilation")]
public class PythonStepTypeSafetyTests
{
    [Test]
    public void AddPythonStep_WithMatchingTypes_CompilesSuccessfully()
    {
        // Arrange: Code with correct type matching
        var code =
          @"
            using Flowthru.Core.Data;
            using Flowthru.Extensions.Python.Steps;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Core.Flows;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var config = ItemFactory.Single.Memory<ModelConfigSchema>(label: ""config"");
                    var result = ItemFactory.Single.Memory<ModelResultSchema>(label: ""result"");
                    
                    var pipeline = FlowBuilder.CreateFlow(builder =>
                    {
                        builder.AddPythonStep(
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
    public void AddPythonStep_WithInferredMatchingTypes_CompilesSuccessfully()
    {
        // Arrange: Code that relies on type inference (no explicit generic params)
        // Type mismatch should still be caught
        var code =
          @"
            using Flowthru.Core.Data;
            using Flowthru.Extensions.Python.Steps;
            using Flowthru.Extensions.Python.Execution;
            using Flowthru.Extensions.Python.Tests.Schemas;
            using Flowthru.Core.Flows;
            
            public class TestProgram
            {
                public void TestMethod(
                    IPythonExecutor executor)
                {
                    var config = ItemFactory.Single.Memory<ModelConfigSchema>(label: ""config"");
                    var wrongOutput = ItemFactory.Single.Memory<ModelConfigSchema>(label: ""wrong_output"");
                    
                    var pipeline = FlowBuilder.CreateFlow(builder =>
                    {
                        // Relying on type inference, but types still don't match
                        builder.AddPythonStep(
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
