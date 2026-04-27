using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Data;

/// <summary>
/// Marks a partial class as a Flowthru configuration catalog. The source generator
/// will emit the <see cref="CatalogAbstract"/> base class, a constructor accepting
/// <c>Microsoft.Extensions.Configuration.IConfiguration</c>, and
/// <see cref="CatalogAbstract.CreateItem{T}"/> backing for each property decorated
/// with <see cref="ConfigSectionAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The annotated class must be declared <c>partial</c> so the generator can add
/// the base class and constructor without conflicting with user-authored members.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// [FlowthruConfig]
/// public partial class FlowConfig
/// {
///     [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
///     public IItem&lt;ModelOptions&gt; ModelOptions { get; }
///
///     [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
///     public IItem&lt;ConfusionMatrixOptions&gt; ConfusionMatrixOptions { get; }
/// }
/// </code>
/// </para>
/// </remarks>
// Coverage: Roslyn-only attribute — constructor never fires at runtime.
// Consumed exclusively by ConfigCatalogGenerator via Roslyn semantic models.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class FlowthruConfigAttribute : Attribute { }
