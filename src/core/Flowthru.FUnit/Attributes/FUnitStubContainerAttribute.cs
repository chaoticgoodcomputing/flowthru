using System.Diagnostics.CodeAnalysis;

namespace Flowthru.FUnit;

/// <summary>
/// Marks a static class as a stub-service container for FUnit-driven tests.
/// At test-fixture instantiation time, <see cref="FunitContext"/> discovers all
/// <see cref="FUnitStubContainerAttribute"/>-attributed types in the test assembly
/// via reflection and invokes their <c>Configure(IServiceCollection)</c> method to
/// populate the per-test DI container.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is convention-based: the marked class must be <c>static</c> and
/// expose a <c>public static void Configure(IServiceCollection services)</c> method.
/// Containers that don't satisfy the convention are silently ignored at runtime —
/// the <c>FunitDiagnosticAnalyzer</c> emits <c>FU100</c> when a <c>[StepTest]</c>'s
/// step has service dependencies that aren't registered in any container.
/// </para>
/// <para>
/// <strong>Mirrors ASP.NET test patterns:</strong> the
/// <c>Configure(IServiceCollection)</c> signature deliberately matches
/// <see cref="WebApplicationFactory.ConfigureWebHost"/>'s
/// <c>ConfigureTestServices(...)</c> conventions. Anyone with ASP.NET test experience
/// reads this pattern at first glance.
/// </para>
/// <para>
/// <strong>Worked example:</strong>
/// </para>
/// <code>
/// [FUnitStubContainer]
/// public static class TestStubs
/// {
///     public static void Configure(IServiceCollection services)
///     {
///         services.AddSingleton&lt;IMailchimpClient, FakeMailchimpClient&gt;();
///         services.AddSingleton&lt;ISlackClient, FakeSlackClient&gt;();
///     }
/// }
///
/// public class ApplyDeltasStepTests : FunitContext
/// {
///     [StepTest(typeof(ApplyDeltasStep))]
///     public void DeltasAreApplied()
///     {
///         var client = GetRequiredService&lt;IMailchimpClient&gt;();
///         var step = ApplyDeltasStep.Create(client);
///         // …
///     }
/// }
/// </code>
/// <para>
/// Multiple containers in the same test assembly are merged at registration time
/// (last registration wins per service type, mirroring DI semantics). Document
/// overlapping registrations across containers as discouraged — keep one container
/// per concern.
/// </para>
/// </remarks>
// Coverage: Roslyn-discovered attribute — its constructor never runs in production
// flow. The runtime side reflects on the attribute's PRESENCE; tests that assert
// auto-registration behavior cover the marker correctly.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FUnitStubContainerAttribute : Attribute { }
