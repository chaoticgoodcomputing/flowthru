using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Serialization;

namespace Flowthru.Core.Tests.Execution.Serialization;

/// <summary>
/// Unit tests for <see cref="PropertyMappingPlanner"/>. Exercises every Tier 1–5 case in
/// the property-classification cascade plus nullable wrappers, nested types,
/// <see cref="SerializedLabelAttribute"/> field-name mapping, and
/// <see cref="IScalar"/>-wrapper edge cases. Format extensions consume the planner's
/// output to wire their serializers; tests here confirm the planner's output is correct
/// before format-side migrations begin (Phase B2 onward).
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("Serialization")]
public class PropertyMappingPlannerTests
{
  // ── Test schemas ─────────────────────────────────────────────────────────────

  // Tier 1 / Tier 4 primitives + BCL scalar structs + DateTime variants.
  private record AllPrimitivesSchema
  {
    public bool BoolValue { get; init; }
    public byte ByteValue { get; init; }
    public sbyte SByteValue { get; init; }
    public short ShortValue { get; init; }
    public ushort UShortValue { get; init; }
    public int IntValue { get; init; }
    public uint UIntValue { get; init; }
    public long LongValue { get; init; }
    public ulong ULongValue { get; init; }
    public float FloatValue { get; init; }
    public double DoubleValue { get; init; }
    public decimal DecimalValue { get; init; }
    public char CharValue { get; init; }
    public string StringValue { get; init; } = string.Empty;
    public DateTime DateTimeValue { get; init; }
    public Guid GuidValue { get; init; }
    public TimeSpan TimeSpanValue { get; init; }
    public DateTimeOffset DateTimeOffsetValue { get; init; }
    public DateOnly DateOnlyValue { get; init; }
    public TimeOnly TimeOnlyValue { get; init; }
  }

  private record ByteArraySchema
  {
    public byte[] Blob { get; init; } = Array.Empty<byte>();
  }

  private enum Status
  {
    Active,
    Inactive,
  }

  private record EnumPropertySchema
  {
    public Status Status { get; init; }
  }

  private readonly record struct CustomerId(string Value) : IScalar;

  private readonly record struct OrderCount(int Value) : IScalar;

  private record IScalarPropertySchema
  {
    public CustomerId Customer { get; init; }
    public OrderCount Count { get; init; }
  }

  private record NullablePropertySchema
  {
    public int? OptionalInt { get; init; }
    public string? OptionalString { get; init; }
    public DateTime? OptionalDateTime { get; init; }
    public Status? OptionalStatus { get; init; }
    public CustomerId? OptionalCustomer { get; init; }
  }

  private record InnerNestedRecord(string Name, int Count);

  private record NestedPropertySchema
  {
    public string TopLevel { get; init; } = string.Empty;
    public InnerNestedRecord Inner { get; init; } = new(string.Empty, 0);
  }

  private record LabeledFieldSchema
  {
    [SerializedLabel("entity_id")]
    public Guid EntityId { get; init; }

    [SerializedLabel("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    public int UnlabeledCount { get; init; }
  }

  // Negative case — IScalar declared but with multiple public properties (kit's
  // "❌ Multi-property struct" anti-pattern). Should fall through to Nested.
  private readonly record struct InvalidIScalar(string A, string B) : IScalar;

  private record InvalidIScalarSchema
  {
    public InvalidIScalar Bad { get; init; }
  }

  // ── Primitive classification ─────────────────────────────────────────────────

  [Test]
  public void Build_AllPrimitives_AllBindingsArePrimitiveAndNonNullable()
  {
    var plan = PropertyMappingPlanner.Build<AllPrimitivesSchema>();

    Assert.That(plan.Bindings, Has.Count.EqualTo(20));
    foreach (var binding in plan.Bindings)
    {
      Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Primitive), $"{binding.Property.Name}");
      Assert.That(binding.IsNullable, Is.False, $"{binding.Property.Name}");
      Assert.That(binding.NullSentinels, Is.Empty, $"{binding.Property.Name}");
      Assert.That(binding.Enum, Is.Null);
      Assert.That(binding.IScalar, Is.Null);
    }
  }

  [Test]
  public void Build_ByteArray_IsClassifiedAsPrimitive()
  {
    var plan = PropertyMappingPlanner.Build<ByteArraySchema>();

    Assert.That(plan.Bindings, Has.Count.EqualTo(1));
    var binding = plan.Bindings[0];
    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Primitive));
    Assert.That(binding.EffectiveType, Is.EqualTo(typeof(byte[])));
  }

  // ── Enum classification ──────────────────────────────────────────────────────

  [Test]
  public void Build_EnumProperty_IsClassifiedAsEnum()
  {
    var plan = PropertyMappingPlanner.Build<EnumPropertySchema>();

    var binding = plan.Bindings.Single();
    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Enum));
    Assert.That(binding.Enum, Is.Not.Null);
    Assert.That(binding.Enum!.EnumType, Is.EqualTo(typeof(Status)));
    Assert.That(binding.IScalar, Is.Null);
    Assert.That(binding.EffectiveType, Is.EqualTo(typeof(Status)));
  }

  // ── IScalar classification ───────────────────────────────────────────────────

  [Test]
  public void Build_IScalarProperty_PopulatesIScalarInfo()
  {
    var plan = PropertyMappingPlanner.Build<IScalarPropertySchema>();

    Assert.That(plan.Bindings, Has.Count.EqualTo(2));

    var customerBinding = plan.Bindings.Single(b => b.Property.Name == "Customer");
    Assert.That(customerBinding.Kind, Is.EqualTo(PropertyKind.IScalar));
    Assert.That(customerBinding.IScalar, Is.Not.Null);
    Assert.That(customerBinding.IScalar!.ScalarType, Is.EqualTo(typeof(CustomerId)));
    Assert.That(customerBinding.IScalar.BackingType, Is.EqualTo(typeof(string)));
    Assert.That(customerBinding.IScalar.ValueProperty.Name, Is.EqualTo("Value"));
    Assert.That(customerBinding.IScalar.WrappingConstructor.GetParameters().Single().ParameterType, Is.EqualTo(typeof(string)));

    var countBinding = plan.Bindings.Single(b => b.Property.Name == "Count");
    Assert.That(countBinding.Kind, Is.EqualTo(PropertyKind.IScalar));
    Assert.That(countBinding.IScalar!.BackingType, Is.EqualTo(typeof(int)));
  }

  [Test]
  public void Build_MultiPropertyIScalar_IsRejectedAndFallsThroughToNested()
  {
    // An IScalar declared on a multi-property struct is an anti-pattern (the IScalar XML
    // doc warns against it). The planner rejects the IScalar classification and falls
    // through to Nested.
    var plan = PropertyMappingPlanner.Build<InvalidIScalarSchema>();

    var binding = plan.Bindings.Single();
    Assert.That(binding.Kind, Is.EqualTo(PropertyKind.Nested));
    Assert.That(binding.IScalar, Is.Null);
  }

  // ── Nullable handling ────────────────────────────────────────────────────────

  [Test]
  public void Build_NullableProperties_AreFlaggedNullableWithUnwrappedEffectiveType()
  {
    var plan = PropertyMappingPlanner.Build<NullablePropertySchema>();

    var optInt = plan.Bindings.Single(b => b.Property.Name == "OptionalInt");
    Assert.That(optInt.IsNullable, Is.True);
    Assert.That(optInt.EffectiveType, Is.EqualTo(typeof(int)));
    Assert.That(optInt.Kind, Is.EqualTo(PropertyKind.Primitive));

    var optString = plan.Bindings.Single(b => b.Property.Name == "OptionalString");
    Assert.That(optString.IsNullable, Is.True);
    Assert.That(optString.EffectiveType, Is.EqualTo(typeof(string)));
    Assert.That(optString.Kind, Is.EqualTo(PropertyKind.Primitive));

    var optStatus = plan.Bindings.Single(b => b.Property.Name == "OptionalStatus");
    Assert.That(optStatus.IsNullable, Is.True);
    Assert.That(optStatus.EffectiveType, Is.EqualTo(typeof(Status)));
    Assert.That(optStatus.Kind, Is.EqualTo(PropertyKind.Enum));

    var optCustomer = plan.Bindings.Single(b => b.Property.Name == "OptionalCustomer");
    Assert.That(optCustomer.IsNullable, Is.True);
    Assert.That(optCustomer.EffectiveType, Is.EqualTo(typeof(CustomerId)));
    Assert.That(optCustomer.Kind, Is.EqualTo(PropertyKind.IScalar));
  }

  [Test]
  public void Build_NullableProperties_CarryConfiguredNullSentinels()
  {
    var options = new PropertyMappingPlannerOptions
    {
      NullSentinels = new[] { "", "NA", "N/A" },
    };
    var plan = PropertyMappingPlanner.Build<NullablePropertySchema>(options);

    foreach (var binding in plan.Bindings)
    {
      Assert.That(binding.IsNullable, Is.True);
      Assert.That(binding.NullSentinels, Is.EqualTo(options.NullSentinels));
    }
  }

  [Test]
  public void Build_NonNullableProperties_HaveEmptyNullSentinels()
  {
    var options = new PropertyMappingPlannerOptions
    {
      NullSentinels = new[] { "", "NA" },
    };
    var plan = PropertyMappingPlanner.Build<AllPrimitivesSchema>(options);

    foreach (var binding in plan.Bindings)
    {
      Assert.That(binding.IsNullable, Is.False);
      Assert.That(binding.NullSentinels, Is.Empty);
    }
  }

  // ── Nested classification ────────────────────────────────────────────────────

  [Test]
  public void Build_NonPrimitiveNonScalarType_IsClassifiedAsNested()
  {
    var plan = PropertyMappingPlanner.Build<NestedPropertySchema>();

    var inner = plan.Bindings.Single(b => b.Property.Name == "Inner");
    Assert.That(inner.Kind, Is.EqualTo(PropertyKind.Nested));
    Assert.That(inner.Enum, Is.Null);
    Assert.That(inner.IScalar, Is.Null);

    var topLevel = plan.Bindings.Single(b => b.Property.Name == "TopLevel");
    Assert.That(topLevel.Kind, Is.EqualTo(PropertyKind.Primitive));
  }

  // ── Field-name mapping ───────────────────────────────────────────────────────

  [Test]
  public void Build_SerializedLabel_AppliesToFieldName()
  {
    var plan = PropertyMappingPlanner.Build<LabeledFieldSchema>();

    var entity = plan.Bindings.Single(b => b.Property.Name == "EntityId");
    Assert.That(entity.FieldName, Is.EqualTo("entity_id"));

    var display = plan.Bindings.Single(b => b.Property.Name == "DisplayName");
    Assert.That(display.FieldName, Is.EqualTo("display_name"));

    var unlabeled = plan.Bindings.Single(b => b.Property.Name == "UnlabeledCount");
    Assert.That(unlabeled.FieldName, Is.EqualTo("UnlabeledCount"));
  }

  [Test]
  public void Build_ByFieldName_IsCaseInsensitive()
  {
    var plan = PropertyMappingPlanner.Build<LabeledFieldSchema>();

    Assert.That(plan.TryGetByFieldName("entity_id", out var lower), Is.True);
    Assert.That(plan.TryGetByFieldName("ENTITY_ID", out var upper), Is.True);
    Assert.That(lower!.Property.Name, Is.EqualTo("EntityId"));
    Assert.That(upper!.Property.Name, Is.EqualTo("EntityId"));
  }

  [Test]
  public void Build_ByFieldName_MissingKey_ReturnsFalse()
  {
    var plan = PropertyMappingPlanner.Build<LabeledFieldSchema>();

    Assert.That(plan.TryGetByFieldName("nonexistent_field", out var binding), Is.False);
    Assert.That(binding, Is.Null);
  }

  // ── Defaults and option handling ─────────────────────────────────────────────

  [Test]
  public void Build_DefaultOptions_UsesEmptyStringAsOnlyNullSentinel()
  {
    var plan = PropertyMappingPlanner.Build<NullablePropertySchema>();

    foreach (var binding in plan.Bindings.Where(b => b.IsNullable))
    {
      Assert.That(binding.NullSentinels, Has.Count.EqualTo(1));
      Assert.That(binding.NullSentinels[0], Is.EqualTo(string.Empty));
    }
  }

  [Test]
  public void Build_NullOptions_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(
      () => PropertyMappingPlanner.Build<AllPrimitivesSchema>(null!)
    );
  }

  // ── Plan structure ───────────────────────────────────────────────────────────

  [Test]
  public void Build_PreservesDeclarationOrder()
  {
    var plan = PropertyMappingPlanner.Build<AllPrimitivesSchema>();

    var orderedNames = plan.Bindings.Select(b => b.Property.Name).ToList();
    Assert.That(orderedNames[0], Is.EqualTo("BoolValue"));
    Assert.That(orderedNames[^1], Is.EqualTo("TimeOnlyValue"));
  }

  [Test]
  public void Build_MultipleCallsForSameType_ProduceEquivalentPlans()
  {
    // No caching is contractual, but multiple calls should produce structurally
    // equivalent plans (same bindings, same field names, same kinds).
    var p1 = PropertyMappingPlanner.Build<AllPrimitivesSchema>();
    var p2 = PropertyMappingPlanner.Build<AllPrimitivesSchema>();

    Assert.That(p2.Bindings.Count, Is.EqualTo(p1.Bindings.Count));
    for (var i = 0; i < p1.Bindings.Count; i++)
    {
      Assert.That(p2.Bindings[i].Property, Is.EqualTo(p1.Bindings[i].Property));
      Assert.That(p2.Bindings[i].FieldName, Is.EqualTo(p1.Bindings[i].FieldName));
      Assert.That(p2.Bindings[i].Kind, Is.EqualTo(p1.Bindings[i].Kind));
    }
  }
}
