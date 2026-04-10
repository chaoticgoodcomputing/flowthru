namespace Flowthru.Core.Meta;

/// <summary>
/// Specifies the builder type for a metadata provider.
/// </summary>
/// <remarks>
/// <para>
/// This attribute enables type-safe provider registration via the generic
/// <see cref="FlowthruMetadataBuilder.AddProvider{TProvider}(Action{object}?)"/> method.
/// </para>
/// <para>
/// The builder type must:
/// </para>
/// <list type="bullet">
/// <item>Have a public parameterless constructor</item>
/// <item>Expose a public <c>Build()</c> method returning <see cref="Providers.IMetadataProvider"/></item>
/// </list>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// [MetadataProviderBuilder(typeof(JsonMetadataProviderBuilder))]
/// public class JsonMetadataProvider : IMetadataProvider
/// {
///   // ...
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MetadataProviderBuilderAttribute : Attribute
{
  /// <summary>
  /// Gets the builder type for this metadata provider.
  /// </summary>
  public Type BuilderType { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="MetadataProviderBuilderAttribute"/> class.
  /// </summary>
  /// <param name="builderType">The type of the provider's builder class</param>
  public MetadataProviderBuilderAttribute(Type builderType)
  {
    BuilderType = builderType ?? throw new ArgumentNullException(nameof(builderType));
  }
}
