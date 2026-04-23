using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests verifying that catalog property access is type-safe at compile-time.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("TypeSafety")]
public class CatalogPropertyCompilationTests
{
  [Test]
  public void CatalogProperty_WhenTypo_ProducesNonExistentError()
  {
    // ===========
    // Arrange: Code with typo in catalog property name
    // ===========
    var code =
      @"
            using Flowthru.Core.Data;
            using Flowthru.Tests.Fixtures.TestCatalogs;
            
            public class TestProgram
            {
                public void TestMethod()
                {
                    var catalog = new SimpleThreeStepCatalog();
                    var data = catalog.Inpt; // Typo: should be 'Input'
                }
            }
        ";

    // ===========
    // Act
    // ===========
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(SimpleThreeStepCatalog)
    );

    // ===========
    // Assert
    // ===========
    Assert.That(
      compilation.Success,
      Is.False,
      "Code with typo in catalog property should not compile"
    );
    Assert.That(
      compilation.Diagnostics,
      Has.Some.Matches<Diagnostic>(d => d.Id == "CS1061" && d.Severity == DiagnosticSeverity.Error),
      "Should produce CS1061 error (member does not exist)"
    );
  }

  [Test]
  public void CatalogProperty_WhenCorrect_CompilesSuccessfully()
  {
    // ===========
    // Arrange: Correct code
    // ===========
    var code =
      @"
            using Flowthru.Core.Data;
            using Flowthru.Tests.Fixtures.TestCatalogs;
            
            public class TestProgram
            {
                public void TestMethod()
                {
                    var catalog = new SimpleThreeStepCatalog();
                    var data = catalog.Input; // Correct property name
                }
            }
        ";

    // ===========
    // Act
    // ===========
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(SimpleThreeStepCatalog)
    );

    // ===========
    // Assert
    // ===========
    Assert.That(
      compilation.Success,
      Is.True,
      "Correct catalog property access should compile successfully"
    );
    Assert.That(
      compilation.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error),
      Is.Empty,
      "Should have no compilation errors"
    );
  }

  [Test]
  public void CatalogProperty_WhenAccessingNonExistent_ProducesCompileError()
  {
    // ===========
    // Arrange: Code accessing non-existent property
    // ===========
    var code =
      @"
            using Flowthru.Core.Data;
            using Flowthru.Tests.Fixtures.TestCatalogs;
            
            public class TestProgram
            {
                public void TestMethod()
                {
                    var catalog = new SimpleThreeStepCatalog();
                    var data = catalog.NonExistentProperty;
                }
            }
        ";

    // ===========
    // Act
    // ===========
    var compilation = CompilationTestHelper.Compile(
      code,
      includeFlowthru: true,
      typeof(SimpleThreeStepCatalog)
    );

    // ===========
    // Assert
    // ===========
    Assert.That(compilation.Success, Is.False);
    Assert.That(
      compilation.Diagnostics,
      Has.Some.Matches<Diagnostic>(d =>
        d.Id == "CS1061" && d.GetMessage().Contains("NonExistentProperty")
      )
    );
  }
}
