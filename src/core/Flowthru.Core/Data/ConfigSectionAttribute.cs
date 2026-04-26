using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Data;

/// <summary>
/// Specifies the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> section path
/// that backs an <see cref="IItem{T}"/> property on a <see cref="FlowthruConfigAttribute"/>-marked class.
/// </summary>
/// <remarks>
/// The section path uses colon-separated segments following .NET configuration conventions
/// (e.g. <c>Flowthru:Flows:DataScience:ModelOptions</c>).
/// </remarks>
// Coverage: Roslyn-only attribute — constructor is never invoked at runtime.
// Consumed exclusively by ConfigCatalogGenerator via Roslyn semantic models.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ConfigSectionAttribute : Attribute
{
  /// <summary>
  /// Gets the colon-separated configuration section path.
  /// </summary>
  public string SectionPath { get; }

  /// <param name="sectionPath">Colon-separated configuration section path.</param>
  public ConfigSectionAttribute(string sectionPath)
  {
    SectionPath = sectionPath ?? throw new ArgumentNullException(nameof(sectionPath));
  }
}
