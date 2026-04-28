using Flowthru.Core.Effects;
using Flowthru.Core.Graph;
using Flowthru.Tests.Helpers.Effects;

namespace Flowthru.Core.Tests.Execution.EffectExecution;

/// <summary>
/// Tests exercising <see cref="IEffect{T}"/>'s engine-dispatch surface via the reusable
/// <see cref="FakeWebhookEffect{T}"/> fixture from <c>Flowthru.Tests.Helpers</c>. Confirms:
/// <list type="bullet">
///   <item><c>Execute()</c> is invoked when the engine asks for a typed produce</item>
///   <item><c>INode&lt;T&gt;.Produce()</c> bridge DIM dispatches to <c>Execute()</c></item>
///   <item><c>INode.ProduceUntyped()</c> bridge DIM boxes the typed result</item>
///   <item><c>INode.ConsumeUntyped()</c> bridge DIM unboxes and calls <c>Consume()</c></item>
///   <item><c>INode.Traits</c> returns the configured <see cref="EffectTraits"/></item>
/// </list>
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("EffectExecution")]
public class FakeWebhookEffectTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Execute / Produce dispatch
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Execute_RecordsInvocation()
  {
    var effect = new FakeWebhookEffect<int>(result: 42);

    var result = await effect.Execute().Run();

    Assert.That(result, Is.EqualTo(42));
    Assert.That(effect.InvocationCount, Is.EqualTo(1));
    Assert.That(effect.Invocations[0], Is.EqualTo(42));
  }

  [Test]
  public async Task INodeT_Produce_BridgesToExecute()
  {
    var effect = new FakeWebhookEffect<int>(result: 99);

    // Cast to INode<T> and call Produce — this exercises the DIM bridge
    var result = await ((INode<int>)effect).Produce().Run();

    Assert.That(result, Is.EqualTo(99));
    Assert.That(effect.InvocationCount, Is.EqualTo(1), "Produce should route through Execute.");
  }

  [Test]
  public async Task INode_ProduceUntyped_BoxesTypedResult()
  {
    var effect = new FakeWebhookEffect<string>(result: "boxed");

    var boxed = await ((INode)effect).ProduceUntyped().Run();

    Assert.That(boxed, Is.EqualTo("boxed"));
    Assert.That(effect.InvocationCount, Is.EqualTo(1));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Consume dispatch
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Consume_RecordsPayload()
  {
    var effect = new FakeWebhookEffect<int>(result: 0);

    await effect.Consume(7).Run();

    Assert.That(effect.ConsumedPayloads, Has.Count.EqualTo(1));
    Assert.That(effect.ConsumedPayloads[0], Is.EqualTo(7));
  }

  [Test]
  public async Task INode_ConsumeUntyped_UnboxesAndDelegates()
  {
    var effect = new FakeWebhookEffect<string>(result: "");

    await ((INode)effect).ConsumeUntyped("payload").Run();

    Assert.That(effect.ConsumedPayloads, Has.Count.EqualTo(1));
    Assert.That(effect.ConsumedPayloads[0], Is.EqualTo("payload"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Traits dispatch
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void INode_Traits_ReturnsEffectTraits()
  {
    var customTraits = new EffectTraits
    {
      CanInspect = false,
      IsIdempotent = true,
      HasSideEffects = false,
    };
    var effect = new FakeWebhookEffect<int>(result: 0, traits: customTraits);

    var traits = ((INode)effect).Traits;

    Assert.That(traits, Is.SameAs(customTraits));
    Assert.That(traits, Is.InstanceOf<EffectTraits>());
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Validate
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Validate_NoCustomFunction_ReturnsSuccess()
  {
    var effect = new FakeWebhookEffect<int>(result: 0);

    var result = await effect.Validate().Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task Validate_WithCustomFunction_ReturnsConfiguredResult()
  {
    var effect = new FakeWebhookEffect<int>(
      result: 5,
      validateResult: v => v > 0
        ? Flowthru.Core.Data.Validation.ValidationResult.Success()
        : Flowthru.Core.Data.Validation.ValidationResult.Failure(
            "test",
            Flowthru.Core.Data.Validation.ValidationErrorType.NotFound,
            "non-positive value"
          )
    );

    var result = await effect.Validate().Run();

    Assert.That(result.IsValid, Is.True);
  }
}
