namespace Flowthru.Hosting;

/// <summary>
/// Convenience extensions over <see cref="IFlowthruBuilder"/>. Each
/// composes with the interface's primitive methods — they're discoverable
/// affordances rather than new contracts.
/// </summary>
public static class FlowthruBuilderExtensions
{
  /// <summary>
  /// Apply several catalog (and adjacent) registrations as a single
  /// fluent call. Each <paramref name="registrations"/> entry receives
  /// the current builder; the intended idiom is to call
  /// <see cref="IFlowthruBuilder.RegisterCatalog{TCatalog}"/> inside.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a grouping affordance over repeated
  /// <c>RegisterCatalog</c> calls. Two patterns it supports cleanly:
  /// </para>
  /// <code>
  /// // Inline grouping at the call site
  /// flowthru.RegisterCatalogs(
  ///   b =&gt; b.RegisterCatalog(_ =&gt; new Catalog(...)),
  ///   b =&gt; b.RegisterCatalog(sp =&gt; new FlowConfig(sp.GetRequiredService&lt;IConfiguration&gt;())));
  ///
  /// // Reusable "catalog bundle" exported by an extension or module
  /// public static class MyCatalogBundle
  /// {
  ///   public static Action&lt;IFlowthruBuilder&gt;[] All =&gt; new Action&lt;IFlowthruBuilder&gt;[]
  ///   {
  ///     b =&gt; b.RegisterCatalog(_ =&gt; new RawCatalog()),
  ///     b =&gt; b.RegisterCatalog(_ =&gt; new IntermediateCatalog()),
  ///     b =&gt; b.RegisterCatalog(_ =&gt; new FeatureCatalog()),
  ///   };
  /// }
  /// flowthru.RegisterCatalogs(MyCatalogBundle.All);
  /// </code>
  /// <para>
  /// The method is not catalog-specific in its dispatch — any
  /// <c>IFlowthruBuilder</c>-using action composes — but the naming
  /// reflects the primary use case (bulk catalog setup) and matches
  /// how extension authors describe the affordance to consumers.
  /// </para>
  /// </remarks>
  /// <param name="builder">The builder.</param>
  /// <param name="registrations">
  /// Zero or more registration actions, each invoked in order with the
  /// builder. Null entries throw <see cref="ArgumentNullException"/>.
  /// </param>
  public static IFlowthruBuilder RegisterCatalogs(
    this IFlowthruBuilder builder,
    params Action<IFlowthruBuilder>[] registrations
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (registrations is null) throw new ArgumentNullException(nameof(registrations));
    for (var i = 0; i < registrations.Length; i++)
    {
      var register = registrations[i];
      if (register is null)
      {
        throw new ArgumentNullException(
          nameof(registrations),
          $"registrations[{i}] is null — every entry must be a non-null Action<IFlowthruBuilder>."
        );
      }
      register(builder);
    }
    return builder;
  }
}
