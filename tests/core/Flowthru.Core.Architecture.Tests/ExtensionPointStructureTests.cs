using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Architecture.Tests;

/// <summary>
/// Asserts that every closed-sum that opens to extensions does so
/// through the same shape: a sealed record case wrapping an
/// <c>IExtension*</c> interface (per §2.5). Adding an extension
/// variant must follow the same protocol everywhere — this test
/// catches drift if a future closed sum tries to define its own
/// extension-shape convention.
/// </summary>
[TestFixture]
public class ExtensionPointStructureTests
{
  [Test]
  public void RuntimeError_HasExtensionVariantWrappingIExtensionRuntimeError()
  {
    AssertExtensionVariantShape(
      umbrella: typeof(RuntimeError),
      caseTypeName: nameof(RuntimeError.ExtensionError),
      expectedPayloadInterface: typeof(IExtensionRuntimeError)
    );
  }

  [Test]
  public void PreFlightError_HasExternalVariantWrappingIExtensionPreFlightError()
  {
    AssertExtensionVariantShape(
      umbrella: typeof(PreFlightError),
      caseTypeName: nameof(PreFlightError.External),
      expectedPayloadInterface: typeof(IExtensionPreFlightError)
    );
  }

  [Test]
  public void ServiceRef_HasExternalVariantWrappingIExtensionServiceRef()
  {
    AssertExtensionVariantShape(
      umbrella: typeof(ServiceRef),
      caseTypeName: nameof(ServiceRef.External),
      expectedPayloadInterface: typeof(IExtensionServiceRef)
    );
  }

  /// <summary>
  /// The extension variant must (1) exist as a nested sealed record
  /// on the umbrella, (2) take exactly one constructor parameter
  /// whose type is the documented <c>IExtension*</c> interface.
  /// </summary>
  private static void AssertExtensionVariantShape(
    Type umbrella,
    string caseTypeName,
    Type expectedPayloadInterface
  )
  {
    var caseType = umbrella.GetNestedType(caseTypeName);
    Assert.That(caseType, Is.Not.Null,
      $"{umbrella.Name} should declare a nested case named '{caseTypeName}' as the extension variant.");
    Assert.That(caseType!.IsSealed, Is.True,
      $"{umbrella.Name}.{caseTypeName} should be a sealed record (closed-sum case shape).");

    var ctor = caseType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 1);
    Assert.That(ctor, Is.Not.Null,
      $"{umbrella.Name}.{caseTypeName} should take exactly one ctor parameter (the extension payload).");
    var paramType = ctor!.GetParameters()[0].ParameterType;
    Assert.That(paramType, Is.EqualTo(expectedPayloadInterface),
      $"{umbrella.Name}.{caseTypeName} ctor parameter must be of type {expectedPayloadInterface.Name}.");
  }
}
