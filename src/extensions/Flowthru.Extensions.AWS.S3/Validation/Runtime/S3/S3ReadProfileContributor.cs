namespace Flowthru.Validation.Runtime.S3;

/// <summary>
/// Resolves an <see cref="S3ReadDependency"/> to its <see cref="ServiceProfile"/>
/// — the read capacity the S3 medium declared (ADR-0019, issue #111). Registered
/// by <c>UseS3()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c>; it recognises only S3 read
/// dependencies and stays silent on everything else.
/// </summary>
/// <remarks>
/// The capacity rides on the dependency (the medium set it from
/// <c>S3Options.MaxConcurrentReads</c>), so this is a pure translation. When the
/// cap is unbounded the medium attaches no dependency at all, so this contributor
/// only ever sees a declared, finite read bound. Write capacity is unbounded and
/// <see cref="ServiceProfile.AffectsOutputs"/> is irrelevant — a read dependency
/// reaches the scheduler only through an item, never a step's own service set.
/// </remarks>
internal sealed class S3ReadProfileContributor : IServiceProfileContributor
{
  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.External { Cause: S3ReadDependency s3 }
      ? new ServiceProfile { Capacity = s3.WriteCapacity, ReadCapacity = s3.ReadCapacity }
      : null;
}
