// Derived from LanguageExt v5 (https://github.com/louthy/language-ext) by Paul Louth.
// Copyright (c) 2014-2025 Paul Louth. MIT License — see LICENSE-LanguageExt.md.
// Simplified for Flowthru: HKT (K<F, A>) removed; the trait is Eff-specialised
// and the static accessor is collapsed to a non-generic helper.

namespace Flowthru.Prelude;

/// <summary>
/// Capability constraint trait. A runtime type implements
/// <see cref="Has{TRuntime, TCapability}"/> for each capability it provides;
/// code requiring that capability declares the constraint
/// <c>where TRuntime : Has&lt;TRuntime, TCapability&gt;</c>.
/// </summary>
/// <typeparam name="TRuntime">
/// The runtime type that provides the capability. Always self-referential
/// at use sites: <c>where TRuntime : Has&lt;TRuntime, …&gt;</c>.
/// </typeparam>
/// <typeparam name="TCapability">The capability interface or value type.</typeparam>
public interface Has<TRuntime, TCapability>
{
  /// <summary>
  /// Effect that, when run, yields the capability instance from the runtime.
  /// </summary>
  static abstract Eff<TRuntime, TCapability> Ask { get; }
}

/// <summary>
/// Static accessor for capabilities. Use <see cref="Ask{TRuntime, TCapability}"/>
/// inside an Eff combinator chain to fetch a capability from the runtime
/// environment without naming the static-abstract interface explicitly.
/// </summary>
/// <remarks>
/// When multiple <see cref="Has{TRuntime, TCapability}"/> traits are in scope,
/// calling <c>TRuntime.Ask</c> directly is ambiguous. This helper resolves the
/// ambiguity by binding both type parameters explicitly:
/// <code>
/// from runner in Has.Ask&lt;TRuntime, IStepRunner&gt;()
/// from result in runner.Run(...)
/// select result;
/// </code>
/// </remarks>
public static class Has
{
  /// <summary>
  /// Lifts the capability into an Eff. Equivalent to LanguageExt's
  /// <c>Has&lt;M, Env, VALUE&gt;.ask</c>.
  /// </summary>
  public static Eff<TRuntime, TCapability> Ask<TRuntime, TCapability>()
    where TRuntime : Has<TRuntime, TCapability> => TRuntime.Ask;
}
