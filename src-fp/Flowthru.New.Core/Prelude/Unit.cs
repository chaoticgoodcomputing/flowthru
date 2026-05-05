// Derived from LanguageExt v5 (https://github.com/louthy/language-ext) by Paul Louth.
// Copyright (c) 2014-2025 Paul Louth. MIT License — see LICENSE-LanguageExt.md.
// Simplified for Flowthru: Monoid trait removed; collection-conversion operators
// removed (Flowthru does not depend on LanguageExt's immutable collections).

using System.Diagnostics.Contracts;

namespace Flowthru.Prelude;

/// <summary>
/// A type with exactly one value. Used as the bound type of effects that
/// produce no meaningful result — equivalent to <c>void</c>, but a real
/// value that can flow through generic combinators.
/// </summary>
[Serializable]
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
  public static readonly Unit Default = default;

  [Pure]
  public override int GetHashCode() => 0;

  [Pure]
  public override bool Equals(object? obj) => obj is Unit;

  [Pure]
  public override string ToString() => "()";

  [Pure]
  public bool Equals(Unit other) => true;

  [Pure]
  public int CompareTo(Unit other) => 0;

  [Pure]
  public static bool operator ==(Unit lhs, Unit rhs) => true;

  [Pure]
  public static bool operator !=(Unit lhs, Unit rhs) => false;

  [Pure]
  public static bool operator <(Unit lhs, Unit rhs) => false;

  [Pure]
  public static bool operator <=(Unit lhs, Unit rhs) => true;

  [Pure]
  public static bool operator >(Unit lhs, Unit rhs) => false;

  [Pure]
  public static bool operator >=(Unit lhs, Unit rhs) => true;

  [Pure]
  public static implicit operator ValueTuple(Unit _) => default;

  [Pure]
  public static implicit operator Unit(ValueTuple _) => default;
}
