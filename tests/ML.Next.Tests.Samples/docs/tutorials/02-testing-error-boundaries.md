# Tutorial: Testing Error Boundaries with ML.Next

In this tutorial, we'll explore how to write tests that confirm ML.Next prevents common ML.NET runtime errors at compile-time. You'll learn to systematically identify error scenarios, write compilation tests, and organize them by error category.

## What You'll Learn

By the end of this tutorial, you will:
- Identify categories of runtime errors that ML.Next should prevent
- Write compilation tests using `CompilationTestHelper`
- Structure error tests in a maintainable, discoverable way
- Understand the comprehensive safety guarantees ML.Next aims to provide

## Prerequisites

- Completed [Tutorial 01: Testing Parity](./01-testing-parity.md)
- A working parity test that proves ML.Next produces correct results
- Basic understanding of how compile-time vs runtime errors differ

## Step 1: Understanding Error Categories

ML.Next prevents errors by making them **unrepresentable** in the type system. Based on real-world issues junior data engineers encounter, we organize tests into these categories.

**Critical Testing Philosophy**: All error boundary tests should assert `Is.False` - meaning the code **should not compile**. When a test fails (because `result.Success` is `True`), it documents a gap in ML.Next's current implementation - functionality we want but haven't built yet. These failures are valuable specifications for future work.

### Error Category Overview

| Category                             | ML.NET Behavior                  | ML.Next Should Prevent             | Priority |
| ------------------------------------ | -------------------------------- | ---------------------------------- | -------- |
| **01. Column Name Typos**            | ❌ Runtime error                  | ✅ Compile-time (expression trees)  | HIGH     |
| **02. Schema Mismatches**            | ❌ Runtime error                  | ✅ Compile-time (phantom types)     | CRITICAL |
| **03. Type Mismatches**              | ❌ Runtime error                  | ✅ Compile-time (type-level types)  | HIGH     |
| **04. Missing Transformations**      | ❌ Runtime error or wrong results | ✅ Compile-time (required chains)   | MEDIUM   |
| **05. Feature Column Errors**        | ❌ Runtime error                  | ✅ Compile-time (schema-aware)      | MEDIUM   |
| **06. Schema Drift**                 | ❌ Production failures            | ✅ Compile-time (versioning)        | HIGH     |
| **07. Null/Missing Values**          | ❌ Runtime error                  | ✅ Compile-time (nullable tracking) | HIGH     |
| **08. Pipeline Ordering**            | ❌ Runtime error                  | ✅ Compile-time (dependency types)  | MEDIUM   |
| **09. Prediction Engine Mismatches** | ❌ Runtime error                  | ✅ Compile-time (type parameters)   | HIGH     |

We'll create one test file per category.

## Step 2: Set Up Error Test Structure

Now that we have the Clustering_Iris parity test working, let's create error boundary tests.

Create the error test directory structure:

```bash
cd tests/ML.Next.Tests.Samples/Clustering_Iris
mkdir -p Errors
```

Each category gets its own test file:

```
Clustering_Iris/
└── Errors/
    ├── 01_ColumnNameTests.cs
    ├── 02_SchemaMismatchTests.cs
    ├── 03_TypeMismatchTests.cs
    ├── 04_MissingTransformTests.cs
    ├── 05_FeatureColumnTests.cs
    ├── 06_SchemaDriftTests.cs
    ├── 07_NullHandlingTests.cs
    ├── 08_PipelineOrderingTests.cs
    └── 09_PredictionEngineTests.cs
```

## Step 3: Category 01 - Column Name Typos

Column name typos are one of the most common errors in the Iris clustering sample. Let's write tests to confirm ML.Next prevents them.

**Create `Errors/01_ColumnNameTests.cs`:**

```csharp
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next catches column name typos in Iris clustering at compile-time.
/// 
/// Common scenario: Engineer copies clustering code and misspells "SepalLength" as "SepelLength".
/// ML.NET: Compiles fine, fails at runtime when pipeline executes.
/// ML.Next: Compilation error - column doesn't exist in schema.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("ColumnNames")]
public class ColumnNameTests
{
    [Test]
    public void TypoInColumnName_Should_Not_Compile()
    {
        // Scenario: Engineer types "SepelLength" instead of "SepalLength"
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Clustering_Iris.Schemas.IrisClusteringSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Typo: 'SepelLength' instead of 'SepalLength'
                    var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.SepelLength,  // TYPO - should not compile!
                        schema => schema.SepalWidth,
                        schema => schema.PetalLength,
                        schema => schema.PetalWidth
                    );
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // Should fail to compile
        Assert.That(result.Success, Is.False, 
            "Code with column name typo should not compile");

        // Verify we get the expected error
        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.That(errors, Is.Not.Empty, "Should have compilation errors");

        // Should have "member not found" error (CS0117)
        var hasMemberNotFound = errors.Any(e => 
            e.Id == "CS0117" || 
            e.GetMessage().Contains("does not contain a definition"));

        Assert.That(hasMemberNotFound, Is.True,
            $"Should have 'member not found' error. Got: {string.Join(", ", errors.Select(e => e.Id))}");
    }

    [Test]
    public void ColumnFromWrongSchema_Should_Not_Compile()
    {
        // Scenario: Engineer references a column that exists in a different schema
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Clustering_Iris.Schemas.IrisClusteringSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to use 'Features' column before it exists
                    var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""AllFeatures"",
                        schema => schema.Features,  // Doesn't exist in IRawSchema!
                        schema => schema.SepalLength
                    );
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False, 
            "Referencing column from wrong schema should not compile");
    }
}
```

**Key Testing Patterns:**
- Use `CompilationTestHelper.CompileWithMLExt()` to attempt compilation
- **Always** assert `result.Success` is `False` - code with errors should not compile
- Check for specific error codes (like `CS0117` for member not found) to verify the right error is caught
- When tests fail, they document ML.Next features that should exist but don't yet

**Critical: Avoid Trainer-Specific Code in Test Snippets**

ML.NET trainers (e.g., `KMeans`, `Sdca`, `LightGbm`) are in separate NuGet packages that may not be loaded by the test runner. Test code that references trainers will fail with `CS1061` errors like:

```
'ClusteringCatalog.ClusteringTrainers' does not contain a definition for 'KMeans'
```

**Instead, focus your error boundary tests on:**
- `ColumnTransforms` operations (Concatenate, Normalize, MapValueToKey, etc.)
- Data loading and schema validation
- Type system behavior (schema mismatches, type safety)
- Pipeline composition errors

**Example - ❌ AVOID (requires KMeans trainer assembly)**:
```csharp
var trainer = mlContext.Clustering.Trainers.KMeans("Features", numberOfClusters: 3);
```

**Example - ✅ PREFER (tests type system with transforms only)**:
```csharp
var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
    mlContext,
    "Features",
    schema => schema.WrongColumn  // Tests compile-time safety
);
```

This keeps tests simple, fast, and focused on ML.Next's compile-time guarantees rather than ML.NET's runtime behavior.

## Step 4: Category 02 - Schema Mismatches

Schema mismatches are the most critical errors ML.Next prevents. These tests verify that pipeline stages must have compatible input/output types.

**Create `Errors/02_SchemaMismatchTests.cs`:**

```csharp
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.YourSampleName.Errors;

/// <summary>
/// Tests verifying that ML.Next prevents schema mismatches in pipeline composition.
/// 
/// Common scenario: Engineer chains transformations where output of one stage
/// doesn't match input of next stage.
/// ML.NET: Compiles, fails at runtime during Fit() or Transform().
/// ML.Next: Compilation error - type mismatch in Append() call.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("SchemaMismatch")]
public class SchemaMismatchTests
{
    [Test]
    public void Incompatible_Pipeline_Stages_Should_Not_Compile()
    {
        // Scenario: Output of step1 doesn't match input of step2
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;

            public interface ISchemaA : ISchemaDefinition { }
            public interface ISchemaB : ISchemaDefinition { }
            public interface ISchemaC : ISchemaDefinition { }

            public class Test {
                public void Execute() {
                    // Step1: A -> B
                    var step1 = new Estimator<ISchemaA, ISchemaB>(null!);
                    
                    // Step2: C -> A (incompatible with step1's output!)
                    var step2 = new Estimator<ISchemaC, ISchemaA>(null!);
                    
                    // This should NOT compile: ISchemaB != ISchemaC
                    var pipeline = step1.Append(step2);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Appending incompatible pipeline stages should not compile");

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        // Should have type mismatch errors (CS1503 or CS0411)
        var hasTypeMismatch = errors.Any(e =>
            e.Id == "CS1503" ||  // Argument type mismatch
            e.Id == "CS0411" ||  // Type inference failed
            e.GetMessage().Contains("cannot convert") ||
            e.GetMessage().Contains("cannot be inferred"));

        Assert.That(hasTypeMismatch, Is.True,
            $"Should have type mismatch error. Got: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
    }

    [Test]
    public void Three_Stage_Pipeline_With_Break_Should_Not_Compile()
    {
        // Scenario: Middle stage breaks the type chain
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;

            public interface ISchemaA : ISchemaDefinition { }
            public interface ISchemaB : ISchemaDefinition { }
            public interface ISchemaC : ISchemaDefinition { }

            public class Test {
                public void Execute() {
                    var step1 = new Estimator<ISchemaA, ISchemaB>(null!);
                    var step2 = new Estimator<ISchemaB, ISchemaC>(null!); // OK
                    var step3 = new Estimator<ISchemaA, ISchemaB>(null!); // Incompatible!
                    
                    // step1->step2 OK, but step2->step3 fails (C != A)
                    var pipeline = step1.Append(step2).Append(step3);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Three-stage pipeline with type break should not compile");
    }

    [Test]
    public void Data_Schema_Mismatch_With_Transformer_Should_Not_Compile()
    {
        // Scenario: Trying to transform data with wrong schema type
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;

            public interface ISchemaA : ISchemaDefinition { }
            public interface ISchemaB : ISchemaDefinition { }

            public class Test {
                public void Execute() {
                    var data = new DataView<ISchemaA>(null!);
                    var transformer = new Transformer<ISchemaB, ISchemaB>(null!);
                    
                    // Should not compile: data is ISchemaA, transformer expects ISchemaB
                    var result = transformer.Transform(data);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Transforming data with mismatched schema should not compile");
    }
}
```

## Step 5: Category 03 - Type Mismatches

Type mismatches involve using columns of the wrong data type (e.g., treating string as float).

**Create `Errors/03_TypeMismatchTests.cs`:**

```csharp
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.YourSampleName.Errors;

/// <summary>
/// Tests verifying that ML.Next catches type mismatches.
/// 
/// Common scenario: Engineer assumes column is numeric when it's categorical,
/// or tries to apply numeric operations to strings.
/// ML.NET: Compiles, fails at runtime or produces wrong results.
/// ML.Next: Should prevent with type-level encoding of data types.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("TypeMismatch")]
public class TypeMismatchTests
{
    [Test]
    public void Normalizing_Scalar_Column_Should_Require_Vector()
    {
        // ML.Next should track whether column is Scalar<T> or Vector<T>
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Age { get; }  // Scalar
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // PCA requires vector input, but Age is scalar
                    var pca = mlContext.Transforms.ProjectToPrincipalComponents(
                        ""AgePCA"",
                        ""Age""  // Should not compile - need vector!
                    );
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Applying vector operations to scalar should not compile");
    }

    [Test]
    [Category("MLNet_Baseline")]
    public void MLNet_Numeric_Operation_On_String_Column_Compiles()
    {
        // Baseline: Documents that ML.NET allows this mistake
        var code = @"
            using Microsoft.ML;

            public class Data {
                public string Category { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = mlContext.Data.LoadFromEnumerable(new Data[0]);
                    
                    // Category is string, but normalizing as if numeric - compiles!
                    var pipeline = mlContext.Transforms.NormalizeMinMax(""Category"");
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.True,
            "ML.NET allows numeric operations on wrong types - runtime error");
    }
}
```

## Step 6: Category 04 - Missing Transformations

These tests verify that required preprocessing steps aren't skipped.

**Create `Errors/04_MissingTransformTests.cs`:**

```csharp
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.YourSampleName.Errors;

/// <summary>
/// Tests verifying that ML.Next catches missing required transformations.
/// 
/// Common scenario: Engineer skips essential preprocessing like converting
/// labels to keys or concatenating features.
/// ML.NET: Compiles, fails at runtime or produces wrong results.
/// ML.Next: Should prevent with required transform chains.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("MissingTransforms")]
public class MissingTransformTests
{
    [Test]
    public void Multiclass_Classification_Without_Label_Key_Conversion()
    {
        // ML.Next should catch missing required transformations
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static YourSampleName.Schemas.YourDataSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = new DataView<IRawSchema>(null!);
                    
                    // Missing: MapValueToKey for Label column!
                    var trainer = mlContext.MulticlassClassification.Trainers
                        .SdcaMaximumEntropy();
                    
                    var estimator = Estimator<IRawSchema, IPredictedSchema>.From(trainer);
                    
                    // Should not compile - missing required transform
                    var model = estimator.Fit(data);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Missing label key conversion should not compile");
    }

    [Test]
    public void Clustering_Without_Feature_Concatenation_Should_Not_Compile()
    {
        // Schema should track whether Features column exists
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static YourSampleName.Schemas.YourDataSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = new DataView<IRawSchema>(null!);
                    
                    // Skipping feature concatenation - trainer expects 'Features' column
                    var trainer = mlContext.Clustering.Trainers.KMeans(
                        featureColumnName: ""Features""  // Doesn't exist!
                    );
                    
                    var estimator = Estimator<IRawSchema, IClusteredSchema>.From(trainer);
                    
                    // Should not compile - Features column not in schema
                    var model = estimator.Fit(data);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Missing feature concatenation should not compile");
    }
}
```

## Step 7: Category 09 - Prediction Engine Mismatches

**Create `Errors/09_PredictionEngineTests.cs`:**

```csharp
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.YourSampleName.Errors;

/// <summary>
/// Tests verifying that prediction engine input/output types match the model schema.
/// 
/// Common scenario: Engineer creates prediction engine with wrong input class,
/// often from copy-pasting and modifying.
/// ML.NET: Compiles, produces wrong predictions or runtime errors.
/// ML.Next: ✅ Type parameters enforce compatibility.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("PredictionEngine")]
public class PredictionEngineTests
{
    [Test]
    public void Prediction_Engine_With_Wrong_Input_Type_Should_Not_Compile()
    {
        // Scenario: Prediction input class doesn't match training schema
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Load;
            using Microsoft.ML;

            public class TrainingData {
                public float Column1 { get; set; }
                public float Column2 { get; set; }
            }

            public class WrongPredictionInput {
                public float Column1 { get; set; }
                // Missing Column2!
            }

            public class Prediction {
                public bool PredictedLabel { get; set; }
            }

            public interface ISchema : ISchemaDefinition { }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var model = new Transformer<ISchema, ISchema>(null!);
                    
                    // Wrong input type - should not compile
                    var engineResult = PredictionEngine<WrongPredictionInput, Prediction>
                        .Create(mlContext, model);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Prediction engine with wrong input type should not compile");
    }
}
```

## Step 8: Organizing and Running Error Tests

### Run All Error Tests
```bash
# Run all compilation safety tests
dotnet test --filter Category=CompilationSafety

# Run tests for specific category
dotnet test --filter Category=ColumnNames
dotnet test --filter Category=SchemaMismatch
```

### Run Sample-Specific Tests
```bash
# Run all error tests for one sample
dotnet test --filter "FullyQualifiedName~YourSampleName.Errors"

# Run specific error category for one sample
dotnet test --filter "FullyQualifiedName~YourSampleName.Errors.ColumnNameTests"
```

### View Test Results
Tests should output clear messages about what error was caught:
```
✓ TypoInColumnName_Should_Not_Compile
  Error CS0117: 'IRawSchema' does not contain a definition for 'Colum1'
  
✓ Incompatible_Pipeline_Stages_Should_Not_Compile
  Error CS1503: Argument type mismatch - cannot convert ISchemaB to ISchemaC
```

## Step 9: Documenting Test Intent and Understanding Test Failures

Use NUnit categories to organize and document test purpose:

### Error Prevention Test Pattern
```csharp
[Test]
[Category("CompilationSafety")]
public void Schema_Mismatch_Should_Not_Compile()
{
    var code = @"
        // Code demonstrating the error scenario
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // ALL error boundary tests expect compilation to fail
    Assert.That(result.Success, Is.False,
        "ML.Next should prevent schema mismatches at compile-time");
}
```

### Test Failure Interpretation

**When an error boundary test PASSES** (code fails to compile as expected):
- ✅ ML.Next successfully prevents this error category
- ✅ Type system is working as designed
- ✅ Users will get compile-time safety for this scenario

**When an error boundary test FAILS** (code compiles when it shouldn't):
- 📋 Documents a current limitation of ML.Next
- 📋 Specifies desired behavior for future implementation
- 📋 Helps prioritize development work
- 📋 Still provides value as a specification and regression test

**Example**: If `Training_Without_Feature_Column_Should_Be_Prevented` fails because the code compiles, it means ML.Next doesn't yet enforce that the Features column exists at compile-time. This is expected - the test documents what ML.Next *should* do, not necessarily what it currently does.

## Step 10: Understanding CompilationTestHelper Requirements

### Assembly References

The `CompilationTestHelper.CompileWithMLExt()` method includes references to:
- Core .NET runtime assemblies (System.Runtime, System.Collections, System.Linq, etc.)
- netstandard.dll (required for nullable types and base types)
- Microsoft.ML assemblies (dynamically loaded from currently loaded assemblies)
- ML.Next assembly
- LanguageExt assemblies (Fin, Error, etc.)

### Writing Test Code Snippets

**Best Practices:**
1. Define schemas inline within the test code string (don't reference external test types)
2. Keep code simple - focus on testing type system, not ML.NET functionality
3. Avoid using ML.NET trainers that require specific packages (KMeans, etc.) unless necessary
4. Use `ColumnTransforms` operations which are well-supported
5. Escape quotes in strings with `""` (two double quotes)

**Example Structure:**
```csharp
var code = @"
    using ML.Next.Core.Schema;
    using ML.Next.Core.Columns;
    using ML.Next.Transform;
    using Microsoft.ML;

    public interface IRawSchema : ISchemaDefinition {
        ColumnName<float> MyColumn { get; }
    }

    public class Test {
        public void Execute() {
            var mlContext = new MLContext();
            // Test type system behavior here
        }
    }
";
```

## Step 12: Creating Error Test Checklists

For each new sample, use this checklist to ensure comprehensive error coverage:

### Error Test Checklist

- [ ] **Column Name Typos**
  - [ ] Simple typo in column selector
  - [ ] Referencing column from wrong schema
  - [ ] ML.NET baseline comparison

- [ ] **Schema Mismatches**
  - [ ] Two-stage pipeline incompatibility
  - [ ] Three-stage pipeline with break
  - [ ] Data-transformer mismatch

- [ ] **Type Mismatches** (if applicable)
  - [ ] Scalar vs vector confusion
  - [ ] Numeric operations on strings
  - [ ] Key type not tracked

- [ ] **Missing Transformations** (if applicable)
  - [ ] Missing label key conversion
  - [ ] Missing feature concatenation
  - [ ] Missing required preprocessing

- [ ] **Prediction Engine**
  - [ ] Wrong input type
  - [ ] Wrong output type
  - [ ] Schema drift between train/predict

## Troubleshooting Common Issues

### Test Code Won't Compile (CS0246, CS0012 errors)

**Symptom**: `CompileWithMLExt` returns compilation errors about missing types or assemblies.

**Common Causes**:
1. Using ML.NET trainers that require specific packages (e.g., `KMeans` requires Microsoft.ML.KMeansTrainer)
2. Missing assembly references in CompilationTestHelper
3. Using types from test project that aren't available in compiled snippet

**Solutions**:
- Simplify test code to avoid specific trainer types
- Define all required interfaces inline within the test code string
- Focus on testing ML.Next type system, not ML.NET runtime behavior
- If you need trainers, ensure CompilationTestHelper includes those assembly references

### All Tests Are Failing

**Check**: Are you asserting `Is.False` for all error boundary tests?

All error boundary tests should expect `result.Success` to be `False`. If you have tests with `Is.True`, they're likely written incorrectly. The pattern should be:
```csharp
Assert.That(result.Success, Is.False, "ML.Next should prevent this error");
```

### Lambda Expressions Not Working

**Symptom**: Code using `schema => schema.ColumnName` doesn't work as expected.

**Current Limitation**: The `ColumnExpressionExtractor` may not correctly extract column names from lambda expressions in all scenarios.

**Workaround**: Use explicit string names with `nameof()`:
```csharp
// Instead of: schema => schema.SepalLength
// Use: nameof(IrisData.SepalLength)
```

## What You've Learned

You have successfully:
- ✅ Organized error tests by category (01-09)
- ✅ Written compilation tests using `CompilationTestHelper`
- ✅ Created specifications for ML.Next's complete safety guarantees
- ✅ Used NUnit categories for discoverability
- ✅ Understood that test failures document desired features, not bugs
- ✅ Learned how to interpret passing vs failing error boundary tests

## Next Steps

- Apply this testing philosophy to your own ML.NET samples
- Contribute new error categories as you discover them
- Use failing tests to guide ML.Next implementation priorities
- Share your findings with the team to improve ML.Next's type safety!

## Reference: Complete Error Test Structure

```
YourSampleName/
├── Data/
├── Schemas/
├── Parity/
│   └── ParityTests.cs                    # From Tutorial 01
└── Errors/
    ├── 01_ColumnNameTests.cs
    ├── 02_SchemaMismatchTests.cs
    ├── 03_TypeMismatchTests.cs
    ├── 04_MissingTransformTests.cs
    ├── 05_FeatureColumnTests.cs
    ├── 06_SchemaDriftTests.cs
    ├── 07_NullHandlingTests.cs
    ├── 08_PipelineOrderingTests.cs
    └── 09_PredictionEngineTests.cs
```
