namespace Flowthru.Core.Effects;

/// <summary>
/// Applicative validation result: an accumulating list of
/// <see cref="FlowValidationFailure"/>s that collects errors across independent
/// checks rather than short-circuiting on the first one.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Applicative, not monadic.</strong> <see cref="FlowValidation"/>
/// deliberately exposes <see cref="Combine(FlowValidation[])"/> and
/// <see cref="Map(Func{FlowValidationFailure, FlowValidationFailure})"/> but
/// does <em>not</em> expose <c>Bind</c>/<c>SelectMany</c>. Monadic bind would
/// force sequential dependence between checks (and short-circuit on the first
/// failure); applicative <c>Combine</c> runs every check and accumulates all
/// failures. The type encodes that intent at compile time — a developer who
/// reaches for <c>Bind</c> and finds it absent has been steered toward the
/// right combinator.
/// </para>
/// <para>
/// Use <see cref="FlowIO{A}"/> when you need monadic sequencing of effects;
/// use <see cref="FlowValidation"/> when you need to gather independent
/// failures into one report.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// public override FlowValidation Validate(FlowExecutionContext ctx) =>
///     FlowValidation.Combine(
///         DbValidations.CanConnect(_factory),
///         FsValidations.IsWritable(_outputDirectory));
/// </code>
/// </remarks>
public readonly struct FlowValidation
{
  private readonly IReadOnlyList<FlowValidationFailure>? _failures;

  private FlowValidation(IReadOnlyList<FlowValidationFailure> failures)
  {
    _failures = failures;
  }

  /// <summary>
  /// All failures collected by this validation. Empty when <see cref="IsValid"/>
  /// is <c>true</c>.
  /// </summary>
  public IReadOnlyList<FlowValidationFailure> Failures =>
    _failures ?? Array.Empty<FlowValidationFailure>();

  /// <summary>
  /// <c>true</c> when no failures were recorded.
  /// </summary>
  public bool IsValid => Failures.Count == 0;

  /// <summary>
  /// A passing validation. Construct via this static rather than the
  /// parameterless default to make intent explicit.
  /// </summary>
  public static FlowValidation Pass { get; } =
    new(Array.Empty<FlowValidationFailure>());

  /// <summary>
  /// Builds a single-failure validation.
  /// </summary>
  public static FlowValidation Fail(string source, string message, Exception? exception = null) =>
    new(new[] { new FlowValidationFailure(source, message, exception) });

  /// <summary>
  /// Wraps a pre-built failure as a single-failure validation.
  /// </summary>
  public static FlowValidation Fail(FlowValidationFailure failure) =>
    new(new[] { failure });

  /// <summary>
  /// Combines multiple validations applicatively — every failure is preserved
  /// in source order. Returns <see cref="Pass"/> when every input passes.
  /// </summary>
  public static FlowValidation Combine(params FlowValidation[] validations) =>
    Combine((IEnumerable<FlowValidation>)validations);

  /// <summary>
  /// Combines multiple validations applicatively — every failure is preserved
  /// in source order. Returns <see cref="Pass"/> when every input passes.
  /// </summary>
  public static FlowValidation Combine(IEnumerable<FlowValidation> validations)
  {
    if (validations is null)
    {
      return Pass;
    }

    List<FlowValidationFailure>? collected = null;
    foreach (var v in validations)
    {
      var failures = v.Failures;
      if (failures.Count == 0)
      {
        continue;
      }

      collected ??= new List<FlowValidationFailure>();
      collected.AddRange(failures);
    }

    return collected is null ? Pass : new FlowValidation(collected);
  }

  /// <summary>
  /// Transforms each failure via <paramref name="f"/>. Useful for tagging
  /// failures with additional context (e.g., wrapping each failure's source
  /// with the catalog label).
  /// </summary>
  public FlowValidation Map(Func<FlowValidationFailure, FlowValidationFailure> f)
  {
    if (_failures is null || _failures.Count == 0)
    {
      return Pass;
    }

    var mapped = new FlowValidationFailure[_failures.Count];
    for (var i = 0; i < _failures.Count; i++)
    {
      mapped[i] = f(_failures[i]);
    }

    return new FlowValidation(mapped);
  }
}
