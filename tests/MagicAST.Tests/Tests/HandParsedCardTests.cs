namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for hand-parsed card ASTs.
/// Each test case is loaded from a JSON file in the Data/HandParsedCards directory.
/// </summary>
[TestFixture]
public class HandParsedCardTests
{
  /// <summary>
  /// JSON options used for serialization during tests.
  /// </summary>
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  /// <summary>
  /// Test 1: Round-trip serialization.
  /// Deserializing the output and re-serializing it should produce semantically identical JSON.
  /// This validates that our type definitions correctly model the expected output format.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Output_RoundTrip_ProducesIdenticalJson(CardTestCase testCase)
  {
    // Arrange
    var expectedNode = testCase.OutputNode;

    // Act
    var ast = testCase.GetOutput();
    var actualJson = JsonSerializer.Serialize(ast, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);

    // Assert - compare JSON structures, not string representations
    Assert.That(
      JsonComparer.AreEqual(actualNode, expectedNode),
      Is.True,
      $"Round-trip serialization failed for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(expectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }

  /// <summary>
  /// Test 2: Parser produces expected output.
  /// Parsing the input DTO should produce an AST that serializes to the expected output.
  /// This validates the parser implementation.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  [Ignore("Parser not yet implemented")]
  public void Parser_ProducesExpectedOutput(CardTestCase testCase)
  {
    // Arrange
    var input = testCase.GetInput();
    var expectedNode = testCase.OutputNode;

    // Act
    // TODO: Replace with actual parser call once implemented
    // var result = MagicASTParser.Parse(input);
    // var actualJson = JsonSerializer.Serialize(result.Output, _testOptions);
    var actualJson = "{}"; // Placeholder
    var actualNode = JsonNode.Parse(actualJson);

    // Assert - compare JSON structures
    Assert.That(
      JsonComparer.AreEqual(actualNode, expectedNode),
      Is.True,
      $"Parser output mismatch for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(expectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }
}
