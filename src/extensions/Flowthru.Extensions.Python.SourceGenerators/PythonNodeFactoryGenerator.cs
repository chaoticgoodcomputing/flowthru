using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Extensions.Python.SourceGenerators;

/// <summary>
/// Generates strongly-typed factory methods from Python @node decorators.
/// </summary>
/// <remarks>
/// <para>
/// Discovers Python files with @node decorators at build time and generates
/// factory methods that return Func&lt;(inputs), (outputs)&gt; delegates for use
/// with the standard AddNode pipeline builder interface.
/// </para>
/// <para>
/// This moves Python node registration from runtime (stringly-typed module/function names)
/// to build-time (strongly-typed factory methods with tuple signatures).
/// </para>
/// </remarks>
[Generator]
public class PythonNodeFactoryGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // For debugging: always generate a diagnostic file to confirm generator is running
    context.RegisterPostInitializationOutput(ctx =>
    {
      ctx.AddSource(
        "_PythonNodeFactoryGenerator.Diagnostic.g.cs",
        SourceText.From("// Generator is running", Encoding.UTF8)
      );
    });

    // Find all Python files marked as AdditionalFiles
    var pythonFiles = context.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".py"));

    // Parse each Python file to extract @node decorators
    var nodeDeclarations = pythonFiles
      .Select(
        (file, ct) =>
        {
          var content = file.GetText(ct)?.ToString();
          if (content == null)
            return null;

          return ParsePythonNode(file.Path, content);
        }
      )
      .Where(node => node != null)
      .Collect();

    // Generate factory class from all discovered nodes
    context.RegisterSourceOutput(
      nodeDeclarations,
      (ctx, nodes) =>
      {
        if (nodes.Length == 0)
        {
          // Add diagnostic output showing we got no nodes
          ctx.AddSource(
            "_PythonNodeFactoryGenerator.NoNodes.g.cs",
            SourceText.From("// No Python nodes discovered", Encoding.UTF8)
          );
          return;
        }

        var validNodes = nodes.Where(n => n != null).Select(n => n!).ToList();
        if (validNodes.Count == 0)
          return;

        var source = GeneratePythonNodeFactories(validNodes);
        ctx.AddSource("PythonNodes.g.cs", SourceText.From(source, Encoding.UTF8));
      }
    );
  }

  private static PythonNodeInfo? ParsePythonNode(string filePath, string content)
  {
    // Regex to match @node decorator with inputs/outputs
    // Handles: @node(inputs=["Schema1", "Schema2"], outputs=["Schema3"])
    var decoratorPattern =
      @"@node\s*\(\s*inputs\s*=\s*\[([^\]]*)\]\s*,\s*outputs\s*=\s*(\[[^\]]*\]|None)\s*\)";
    var functionPattern = @"def\s+(\w+)\s*\(";

    var decoratorMatch = Regex.Match(content, decoratorPattern);
    if (!decoratorMatch.Success)
      return null;

    // Extract function name
    var functionMatch = Regex.Match(content.Substring(decoratorMatch.Index), functionPattern);
    if (!functionMatch.Success)
      return null;

    var functionName = functionMatch.Groups[1].Value;

    // Parse inputs
    var inputsRaw = decoratorMatch.Groups[1].Value;
    var inputs = ParseSchemaList(inputsRaw);

    // Parse outputs
    var outputsRaw = decoratorMatch.Groups[2].Value;
    var outputs = outputsRaw == "None" ? new List<string>() : ParseSchemaList(outputsRaw);

    // Derive module path from file path
    // e.g., Pipelines/DataScience/Nodes/split_data.py → Pipelines.DataScience.Nodes.split_data
    var modulePath = DeriveModulePath(filePath);

    return new PythonNodeInfo(
      functionName: functionName,
      modulePath: modulePath,
      inputs: inputs,
      outputs: outputs
    );
  }

  private static List<string> ParseSchemaList(string raw)
  {
    // Extract quoted strings: "Schema1", "Schema2", None
    var schemaPattern = @"""([^""]+)""|None";
    var matches = Regex.Matches(raw, schemaPattern);

    var result = new List<string>();
    foreach (Match match in matches)
    {
      if (match.Groups[1].Success)
      {
        result.Add(match.Groups[1].Value);
      }
      else
      {
        result.Add("object");
      }
    }

    return result.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
  }

  private static string DeriveModulePath(string filePath)
  {
    // Extract the relative path from the project and convert to Python module notation
    // Look for common markers like "Pipelines" or "Nodes"
    var parts = filePath.Replace('\\', '/').Split('/');
    var relevantParts = new List<string>();
    var startCapturing = false;

    foreach (var part in parts)
    {
      if (part == "Pipelines" || startCapturing)
      {
        startCapturing = true;
        if (part.EndsWith(".py"))
        {
          relevantParts.Add(part.Substring(0, part.Length - 3)); // Remove .py
        }
        else if (!string.IsNullOrEmpty(part))
        {
          relevantParts.Add(part);
        }
      }
    }

    return string.Join(".", relevantParts);
  }

  private static string GeneratePythonNodeFactories(List<PythonNodeInfo> nodes)
  {
    var sb = new StringBuilder();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine("using Flowthru.Extensions.Python.Execution;");
    sb.AppendLine("using Flowthru.Extensions.Python.Nodes;");
    sb.AppendLine();
    sb.AppendLine("namespace Flowthru.Extensions.Python.Generated;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Strongly-typed factory methods for Python nodes discovered at build time.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("public static class PythonNodes");
    sb.AppendLine("{");

    foreach (var node in nodes)
    {
      GenerateFactoryMethod(sb, node);
      sb.AppendLine();
    }

    sb.AppendLine("}");

    return sb.ToString();
  }

  private static void GenerateFactoryMethod(StringBuilder sb, PythonNodeInfo node)
  {
    // Generate type parameters
    var inputTypes = node.Inputs.Select(MapSchemaToType).ToList();
    var outputTypes = node.Outputs.Select(MapSchemaToType).ToList();

    // Determine input/output tuple structures
    var inputTupleType = inputTypes.Count switch
    {
      0 => "object", // No inputs (shouldn't happen)
      1 => inputTypes[0],
      _ => $"({string.Join(", ", inputTypes)})",
    };

    var outputTupleType = outputTypes.Count switch
    {
      0 => "object", // Non-tabular output
      1 => outputTypes[0],
      _ => $"({string.Join(", ", outputTypes)})",
    };

    // Generate XML documentation
    sb.AppendLine($"  /// <summary>");
    sb.AppendLine($"  /// Creates a Python node for {node.FunctionName}.");
    sb.AppendLine($"  /// </summary>");
    sb.AppendLine($"  /// <param name=\"executor\">Python executor instance.</param>");
    sb.AppendLine($"  /// <returns>");
    sb.AppendLine(
      $"  /// A function that invokes the Python node with the specified inputs and outputs."
    );
    sb.AppendLine($"  /// </returns>");

    // Generate method signature
    var methodName = ToPascalCase(node.FunctionName);
    sb.AppendLine($"  public static Func<{inputTupleType}, {outputTupleType}> {methodName}(");
    sb.AppendLine($"    IPythonExecutor executor");
    sb.AppendLine($"  )");
    sb.AppendLine("  {");

    // Create wrapper
    sb.AppendLine($"    var wrapper = new PythonNodeWrapper<{inputTupleType}, {outputTupleType}>(");
    sb.AppendLine($"      executor,");
    sb.AppendLine($"      module: \"{node.ModulePath}\",");
    sb.AppendLine($"      function: \"{node.FunctionName}\"");
    sb.AppendLine($"    );");
    sb.AppendLine();
    sb.AppendLine($"    return wrapper.GetTransform();");
    sb.AppendLine("  }");
  }

  private static string MapSchemaToType(string schemaName)
  {
    // Handle special case for non-tabular data
    if (schemaName == "object")
      return "object";

    // Remove "Schema" suffix if present and map to enumerable type
    var typeName = schemaName.EndsWith("Schema")
      ? schemaName.Substring(0, schemaName.Length - 6)
      : schemaName;

    // For now, assume all schemas are tables (IEnumerable<T>)
    // TODO: This mapping could be improved with configuration or discovery
    return $"System.Collections.Generic.IEnumerable<{typeName}>";
  }

  private static string ToPascalCase(string snakeCase)
  {
    var parts = snakeCase.Split('_');
    return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
  }
}

/// <summary>
/// Information about a Python node parsed from source.
/// </summary>
internal class PythonNodeInfo
{
  public string FunctionName { get; }
  public string ModulePath { get; }
  public List<string> Inputs { get; }
  public List<string> Outputs { get; }

  public PythonNodeInfo(
    string functionName,
    string modulePath,
    List<string> inputs,
    List<string> outputs
  )
  {
    FunctionName = functionName;
    ModulePath = modulePath;
    Inputs = inputs;
    Outputs = outputs;
  }
}
