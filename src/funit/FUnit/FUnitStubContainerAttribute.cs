using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Step.Testing;

/// <summary>
/// Marks a static class as a stub-service container for FUnit-driven
/// tests. At test-fixture instantiation time,
/// <see cref="FUnitContext"/> discovers all
/// <see cref="FUnitStubContainerAttribute"/>-attributed types in the
/// test assembly via reflection and invokes their
/// <c>Configure(IServiceCollection)</c> method to populate the
/// per-test DI container.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is convention-based: the marked class must be
/// <c>static</c> and expose a
/// <c>public static void Configure(IServiceCollection services)</c>
/// method. Containers that don't satisfy the convention are silently
/// ignored at runtime — the FUnit diagnostic analyzer emits FU100
/// when a <c>[FUnitStepTest]</c>'s step has service dependencies
/// that aren't registered in any container.
/// </para>
/// <example>
/// <code>
/// [FUnitStubContainer]
/// public static class TestStubs
/// {
///   public static void Configure(IServiceCollection services)
///   {
///     services.AddSingleton&lt;IClock, FixedClock&gt;();
///   }
/// }
///
/// public class FooStepTests : FUnitContext
/// {
///   [FUnitStepTest(typeof(FooStep))]
///   public void DoesTheRightThing()
///   {
///     var clock = GetRequiredService&lt;IClock&gt;();
///     // …
///   }
/// }
/// </code>
/// </example>
/// </remarks>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FUnitStubContainerAttribute : Attribute { }
