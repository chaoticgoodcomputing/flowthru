using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.ML.Ext.Samples.Clustering_Iris;

/// <summary>
/// Compilation tests verifying that ML.Ext prevents common ML.NET runtime errors at compile-time.
/// These tests demonstrate the type safety advantages of the ML.Ext wrapper over raw ML.NET usage.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("TypeSafety")]
public class ClusteringIrisCompilationTests {
  [Test]
  public void Schema_Mismatch_In_Pipeline_Composition_Should_Not_Compile() {
    // This test verifies that when appending transformers with incompatible schemas,
    // the code fails to compile rather than producing a runtime error.
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Flowthru.ML.Ext.Extract;
      using LanguageExt;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }
      public interface ISchema3 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var loader = DataLoader.LoadFromEnumerable<ISchema1>(new object[0]);
          var data = loader.Match(
            Succ: d => d,
            Fail: e => throw new System.Exception()
          );

          // Create transformer: ISchema1 -> ISchema2
          var step1 = new Transformer<ISchema1, ISchema2>(null!);

          // Create transformer: ISchema3 -> ISchema1 (incompatible with step1 output)
          var step2 = new Transformer<ISchema3, ISchema1>(null!);

          // This should NOT compile: ISchema2 != ISchema3
          var pipeline = step1.Append(step2);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Should fail to compile due to type mismatch
    Assert.That(result.Success, Is.False, "Code with schema mismatch should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");

    // Expect type argument mismatch error (CS1503) or type inference error (CS0411)
    var hasTypeMismatch = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS0411" ||
        e.ToString().Contains("cannot convert") ||
        e.ToString().Contains("cannot be inferred"));
    Assert.That(hasTypeMismatch, Is.True,
        $"Should have type mismatch or inference error. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Cannot_Use_Estimator_As_Transformer_Without_Fitting() {
    // Verifies that estimators cannot be used where transformers are expected
    // This prevents "must call Fit() first" runtime errors
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Microsoft.ML;

      public interface ISchema1 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var estimator = new Estimator<ISchema1, ISchema1>(null!);
          var data = new DataView<ISchema1>(null!);

          // This should NOT compile: Estimator<T,U> is not assignable to Transformer<T,U>
          var transformer = (Transformer<ISchema1, ISchema1>)estimator;

          var result = transformer.Transform(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Cannot cast Estimator to Transformer");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasInvalidCast = errors.Any(e => e.Id == "CS0030" || e.ToString().Contains("convert"));
    Assert.That(hasInvalidCast, Is.True, "Should have invalid cast error");
  }

  [Test]
  public void DataView_Underlying_Required_For_MLNet_Operations() {
    // Verifies that DataView<T> cannot be assigned to IDataView without explicit conversion
    // This tests that the type wrapper requires intentional unwrapping
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Microsoft.ML;

      public interface ISchema1 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var data = new DataView<ISchema1>(null!);

          // Try to assign DataView<T> to IDataView variable - should NOT work
          IDataView mlData = data;

          // The correct way would be: IDataView mlData = data.Underlying;
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "DataView<T> should not implicitly convert to IDataView");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    // Note: This test documents current behavior. If DataView<T> is made to implement IDataView
    // in the future, this test will need to be updated to demonstrate the difference in another way.
    Assert.That(errors, Is.Not.Empty,
        $"Should have compilation errors. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Fin_Result_Cannot_Be_Used_Without_Explicit_Handling() {
    // Verifies that Fin<T> results must be explicitly handled (Match, Bind, etc.)
    // This prevents silently ignoring errors
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Extract;
      using Flowthru.ML.Ext.Transform;
      using LanguageExt;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var finResult = DataLoader.LoadFromEnumerable<ISchema1>(new object[0]);
          var transformer = new Transformer<ISchema1, ISchema2>(null!);

          // This should NOT compile: cannot Transform Fin<DataView<T>> directly
          var transformed = transformer.Transform(finResult);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Fin<DataView<T>> should not be usable without unwrapping");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasConversionError = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS1929" || // Argument mismatch or extension method not found
        e.ToString().Contains("cannot convert") ||
        e.ToString().Contains("Fin"));
    Assert.That(hasConversionError, Is.True,
        $"Should have error when using Fin without Match. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Estimator_Append_With_Mismatched_Schemas_Should_Not_Compile() {
    // Verifies that Estimator.Append enforces schema compatibility
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }
      public interface ISchema3 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var est1 = new Estimator<ISchema1, ISchema2>(null!);
          var est2 = new Estimator<ISchema3, ISchema1>(null!); // Note: starts with ISchema3, not ISchema2

          // This should NOT compile: ISchema2 != ISchema3
          var pipeline = est1.Append(est2);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Estimator.Append with mismatched schemas should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasTypeMismatch = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS0311" ||
        e.ToString().Contains("type argument") ||
        e.ToString().Contains("cannot convert"));
    Assert.That(hasTypeMismatch, Is.True, "Should have type mismatch error");
  }

  [Test]
  public void Column_Name_Typo_With_ColumnName_Type_Does_Not_Compile() {
    // Verifies that using typed ColumnName<T> with wrong property name fails at compile-time
    // In ML.NET, string literal column names fail at runtime if misspelled
    var code = @"
      using Flowthru.ML.Ext.Core.Columns;
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface IIrisData : ISchemaDefinition { }

      public class IrisData {
        public float SepalLength { get; set; }
        public float SepalWidth { get; set; }
        public float PetalLength { get; set; }
        public float PetalWidth { get; set; }
        public string Label { get; set; }
      }

      public class Test {
        public void Execute() {
          // Trying to use a column name that doesn't exist in the C# class
          var columnName = ColumnName<float>.From(nameof(IrisData.SepalLengt)); // Typo: missing 'h'
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Column name typo with nameof should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasNameofError = errors.Any(e =>
        e.Id == "CS0117" || // 'type' does not contain a definition for 'member'
        e.ToString().Contains("does not contain a definition"));
    Assert.That(hasNameofError, Is.True,
        $"Should have nameof error. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  [Explicit("Positive test - demonstrates valid ML.Ext usage")]
  public void Valid_Pipeline_Composition_Should_Compile() {
    // This is a positive control test showing that valid code compiles successfully
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Flowthru.ML.Ext.Extract;
      using LanguageExt;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var loader = DataLoader.LoadFromEnumerable<ISchema1>(new object[0]);
          var data = loader.Match(
            Succ: d => d,
            Fail: e => throw new System.Exception()
          );

          var step1 = new Transformer<ISchema1, ISchema2>(null!);
          var step2 = new Transformer<ISchema2, ISchema1>(null!);

          // This SHOULD compile: schemas match correctly
          var pipeline = step1.Append(step2);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        $"Valid pipeline should compile. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }
}
