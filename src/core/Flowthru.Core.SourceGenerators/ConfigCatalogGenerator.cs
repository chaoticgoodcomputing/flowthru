using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Core.SourceGenerators;

/// <summary>
/// Incremental source generator that emits a <c>CatalogAbstract</c>-derived partial class
/// for types annotated with <c>[FlowthruConfig]</c>.
/// </summary>
/// <remarks>
/// <para>
/// For each <c>[FlowthruConfig]</c> partial class, the generator emits:
/// <list type="bullet">
/// <item>Inheritance from <c>Flowthru.Core.Data.CatalogAbstract</c>.</item>
/// <item>A constructor accepting <c>Microsoft.Extensions.Configuration.IConfiguration</c>.</item>
/// <item>Property bodies using <c>CreateItem</c> / <c>ItemFactory.Single.Configuration&lt;T&gt;</c>
///     for each property decorated with <c>[ConfigSection("path")]</c>.</item>
/// </list>
/// </para>
/// <para>
/// Emits diagnostic <c>FT3001</c> when the annotated class is not <c>partial</c>.
/// </para>
/// </remarks>
[Generator]
public class ConfigCatalogGenerator : IIncrementalGenerator
{
  private const string ConfigAttributeFullName = "Flowthru.Core.Data.FlowthruConfigAttribute";
  private const string SectionAttributeFullName = "Flowthru.Core.Data.ConfigSectionAttribute";

  // ── Diagnostics ────────────────────────────────────────────────────────────────

  private static readonly DiagnosticDescriptor _notPartialRule =
    new(
      id: "FT3001",
      title: "FlowthruConfig class must be partial",
      messageFormat: "'{0}' is annotated with [FlowthruConfig] but is not declared partial. Add the 'partial' modifier.",
      category: "Flowthru.Core.Config",
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true
    );

  private static readonly DiagnosticDescriptor _missingConfigSectionRule =
    new(
      id: "FT3002",
      title: "FlowthruConfig property missing [ConfigSection]",
      messageFormat: "Property '{0}' on '{1}' is an IItem<T> property but has no [ConfigSection] attribute. Add [ConfigSection(\"path\")] to specify the configuration section.",
      category: "Flowthru.Core.Config",
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true
    );

  // ── Initialization ──────────────────────────────────────────────────────────────

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var candidates = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        ConfigAttributeFullName,
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, _) => ExtractConfigCatalogInfo(ctx)
      )
      .Where(static info => info != null)
      .Select(static (info, _) => info!);

    context.RegisterSourceOutput(candidates, static (ctx, info) => Emit(ctx, info));
  }

  // ── Extraction ──────────────────────────────────────────────────────────────────

  private static ConfigCatalogInfo? ExtractConfigCatalogInfo(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
    {
      return null;
    }

    var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;
    var isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

    var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : typeSymbol.ContainingNamespace.ToDisplayString();

    var properties = ImmutableArray.CreateBuilder<ConfigPropertyInfo>();

    var iItemFullName = "Flowthru.Core.Data.IItem`1";

    foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
      {
        continue;
      }

      // Only process IItem<T> properties
      if (
        member.Type is not INamedTypeSymbol propType
        || propType.OriginalDefinition.ToDisplayString() != iItemFullName
        || propType.TypeArguments.Length != 1
      )
      {
        continue;
      }

      var configType = propType
        .TypeArguments[0]
        .ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
            SymbolDisplayGlobalNamespaceStyle.Omitted
          )
        );

      // Find [ConfigSection] attribute
      string? sectionPath = null;
      Location? sectionAttrLocation = null;
      foreach (var attr in member.GetAttributes())
      {
        if (
          attr.AttributeClass?.ToDisplayString() == SectionAttributeFullName
          && attr.ConstructorArguments.Length == 1
          && attr.ConstructorArguments[0].Value is string path
        )
        {
          sectionPath = path;
          sectionAttrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
          break;
        }
      }

      properties.Add(
        new ConfigPropertyInfo(
          propertyName: member.Name,
          configTypeName: configType,
          sectionPath: sectionPath,
          location: member.Locations.FirstOrDefault(),
          hasConfigSection: sectionPath != null
        )
      );
    }

    return new ConfigCatalogInfo(
      typeName: typeSymbol.Name,
      namespaceName: namespaceName,
      isPartial: isPartial,
      properties: properties.ToImmutable(),
      location: classDecl.Identifier.GetLocation()
    );
  }

  // ── Emission ────────────────────────────────────────────────────────────────────

  private static void Emit(SourceProductionContext ctx, ConfigCatalogInfo info)
  {
    // FT3001 — must be partial
    if (!info.IsPartial)
    {
      ctx.ReportDiagnostic(Diagnostic.Create(_notPartialRule, info.Location, info.TypeName));
      return;
    }

    // FT3002 — warn on IItem<T> properties without [ConfigSection]
    foreach (var prop in info.Properties)
    {
      if (!prop.HasConfigSection)
      {
        ctx.ReportDiagnostic(
          Diagnostic.Create(
            _missingConfigSectionRule,
            prop.Location,
            prop.PropertyName,
            info.TypeName
          )
        );
      }
    }

    var sb = new StringBuilder();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("using Flowthru.Core.Data;");
    sb.AppendLine("using Microsoft.Extensions.Configuration;");
    sb.AppendLine();

    var hasNamespace = !string.IsNullOrEmpty(info.NamespaceName);
    if (hasNamespace)
    {
      sb.AppendLine($"namespace {info.NamespaceName};");
      sb.AppendLine();
    }

    sb.AppendLine($"partial class {info.TypeName} : global::Flowthru.Core.Data.CatalogAbstract");
    sb.AppendLine("{");
    sb.AppendLine(
      "  private readonly global::Microsoft.Extensions.Configuration.IConfiguration _configuration;"
    );
    sb.AppendLine();
    sb.AppendLine(
      $"  public {info.TypeName}(global::Microsoft.Extensions.Configuration.IConfiguration configuration)"
    );
    sb.AppendLine($"    : base(\"{info.TypeName}\")");
    sb.AppendLine("  {");
    sb.AppendLine(
      "    _configuration = configuration ?? throw new global::System.ArgumentNullException(nameof(configuration));"
    );
    sb.AppendLine("    InitializeCatalogProperties();");
    sb.AppendLine("  }");

    foreach (var prop in info.Properties)
    {
      if (!prop.HasConfigSection)
      {
        continue;
      }

      // Label is the property name — consistent with data catalog convention.
      sb.AppendLine();
      sb.AppendLine(
        $"  public global::Flowthru.Core.Data.IItem<{prop.ConfigTypeName}> {prop.PropertyName} =>"
      );
      sb.AppendLine(
        $"    CreateItem(() => global::Flowthru.Core.Data.ItemFactory.Single.Configuration<{prop.ConfigTypeName}>("
      );
      sb.AppendLine($"      \"{prop.PropertyName}\",");
      sb.AppendLine($"      \"{prop.SectionPath}\",");
      sb.AppendLine($"      _configuration));");
    }

    sb.AppendLine("}");

    var hintName = string.IsNullOrEmpty(info.NamespaceName)
      ? $"{info.TypeName}.FlowthruConfig.g.cs"
      : $"{info.NamespaceName}.{info.TypeName}.FlowthruConfig.g.cs";

    ctx.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
  }

  // ── Data models ─────────────────────────────────────────────────────────────────

  private sealed class ConfigCatalogInfo
  {
    public ConfigCatalogInfo(
      string typeName,
      string namespaceName,
      bool isPartial,
      ImmutableArray<ConfigPropertyInfo> properties,
      Location location
    )
    {
      TypeName = typeName;
      NamespaceName = namespaceName;
      IsPartial = isPartial;
      Properties = properties;
      Location = location;
    }

    public string TypeName { get; }
    public string NamespaceName { get; }
    public bool IsPartial { get; }
    public ImmutableArray<ConfigPropertyInfo> Properties { get; }
    public Location Location { get; }
  }

  private sealed class ConfigPropertyInfo
  {
    public ConfigPropertyInfo(
      string propertyName,
      string configTypeName,
      string? sectionPath,
      Location? location,
      bool hasConfigSection
    )
    {
      PropertyName = propertyName;
      ConfigTypeName = configTypeName;
      SectionPath = sectionPath;
      Location = location;
      HasConfigSection = hasConfigSection;
    }

    public string PropertyName { get; }
    public string ConfigTypeName { get; }
    public string? SectionPath { get; }
    public Location? Location { get; }
    public bool HasConfigSection { get; }
  }
}
