namespace Flowthru.Diagnostics;

/// <summary>
/// FT diagnostic-code constants — every Flowthru diagnostic, analyzer,
/// pre-flight error, and runtime error references one of these codes.
/// Per §2.5, the FT0–FT5 ranges are tied to algebra categories rather
/// than historical assignment, so the layout is stable and predictable.
/// </summary>
/// <remarks>
/// <para>
/// Code ranges:
/// <list type="bullet">
///   <item><c>FT0xxx</c> — algebra rules (effect-type discipline, exhaustive matching).</item>
///   <item><c>FT1xxx</c> — algebra shape (interpreter conformance, smart-constructor signatures).
///     Sub-ranges: <c>FT1001-1099</c> schema shape; <c>FT1101-1199</c> step shape.</item>
///   <item><c>FT2xxx</c> — composition / wiring (single-producer, type alignment, schema-format compatibility).</item>
///   <item><c>FT3xxx</c> — pre-flight failures.</item>
///   <item><c>FT4xxx</c> — runtime failures.</item>
///   <item><c>FT5xxx</c> — warnings (FUnit, suggestions).</item>
/// </list>
/// </para>
/// <para>
/// Extensions reserve sub-ranges within FT3xxx and FT4xxx for their
/// own pre-flight and runtime error categories — see the diagnostics
/// docs for the per-extension allocation.
/// </para>
/// </remarks>
public static class FlowthruDiagnosticCodes
{
  // ── FT0xxx — algebra rules ──────────────────────────────────────────────

  /// <summary>Closed sum is not exhaustively matched at a consumer.</summary>
  public const string ExhaustiveMatchRequired = "FT0001";

  // ── FT1xxx — algebra shape ──────────────────────────────────────────────

  /// <summary>A <c>[FlowthruSchema]</c>-decorated type is missing the partial keyword.</summary>
  public const string SchemaPartialRequired = "FT1001";

  /// <summary>A <c>[FlowthruSchema]</c>-decorated type has a property the planner cannot classify.</summary>
  public const string SchemaPropertyClassificationFailed = "FT1002";

  /// <summary>A step factory class is referenced by <c>FlowBuilder.AddStep</c> but lacks <c>[FlowthruStep]</c>.</summary>
  public const string StepAttributeRequired = "FT1101";

  // ── FT2xxx — composition / wiring ───────────────────────────────────────

  /// <summary>A catalog item has more than one step producing it.</summary>
  public const string SingleProducerViolated = "FT2001";

  /// <summary>A step's input/output types disagree with the catalog items it references.</summary>
  public const string StepTypeAlignmentViolated = "FT2002";

  // ── FT3xxx — pre-flight failures ────────────────────────────────────────

  public const string PreFlightDuplicateProducer = "FT3001";
  public const string PreFlightCircularDependency = "FT3002";
  public const string PreFlightMissingInput = "FT3003";
  public const string PreFlightSchemaDrift = "FT3004";
  public const string PreFlightInspectionFailed = "FT3005";
  public const string PreFlightRegistrationCheckFailed = "FT3006";

  // ── FT4xxx — runtime failures ───────────────────────────────────────────

  public const string RuntimeExternalFailure = "FT4001";
  public const string RuntimeStepFailed = "FT4002";
  public const string RuntimeCancelled = "FT4003";
  public const string RuntimeInvariantViolated = "FT4004";
  public const string RuntimeSchemaMismatch = "FT4005";
  public const string RuntimeConstraintViolated = "FT4006";

  // ── FT5xxx — warnings ───────────────────────────────────────────────────

  public const string FUnitNoFixturesDeclared = "FT5001";
}
