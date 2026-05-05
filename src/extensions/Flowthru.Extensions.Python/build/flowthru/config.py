"""Nested Python view over the host's .NET ``IConfiguration``.

The Flowthru Python extension flattens the configured ``IConfiguration``
section into environment variables at subprocess spawn time, using .NET's
native ``:`` → ``__`` rule. This module re-nests those env vars on import
and exposes typed accessors modelled on .NET's
``IConfiguration.GetValue<T>(...)`` API so pipeline code never has to
think about the wire format.

Usage
-----

    from flowthru import config

    # Fetch a section
    diarization = config.get_section("Diarization")

    # Typed access with explicit defaults
    model = diarization.get_str("PyannoteModel", "pyannote/speaker-diarization-3.1")
    sr = diarization.get_int("TargetSampleRate", 16000)

    # Optional access (returns None when absent — no default needed)
    token = diarization.get_optional_str("HuggingFaceToken")

    # Or materialize a frozen dataclass directly
    from dataclasses import dataclass

    @dataclass(frozen=True)
    class DiarizationConfig:
        pyannote_model: str = "pyannote/speaker-diarization-3.1"
        target_sample_rate: int = 16000

    cfg = config.bind(DiarizationConfig, "Diarization")

Semantics
---------

Mirrors .NET's ``IConfiguration`` semantics in three deliberate ways:

* ``get_section(key)`` always returns a :class:`ConfigSection` — never
  ``None``. Absent sections return an empty section so callers can chain
  without null-checks.
* ``get_<type>(key, default)`` returns the typed default when the key is
  absent. Missing keys are not failures; the caller asks for the type
  they want and pays for that intent.
* ``get_optional_<type>(key)`` returns ``None`` on absence — for cases
  where the consumer wants to distinguish "not present" from "present
  with default value."

Coercion is strict and fail-loud: a value that fails to parse as the
requested type raises :class:`ConfigCoercionError` (subclass of
``ValueError``) naming the offending key. Empty-string values are treated
as present-but-empty rather than falling through to defaults — silent
fallthrough hides typos.
"""

from __future__ import annotations

import os
from dataclasses import MISSING, fields, is_dataclass
from typing import Any, Iterator, Mapping, get_args, get_origin, get_type_hints


# ── Public exception ────────────────────────────────────────────────────


class ConfigCoercionError(ValueError):
    """Raised when a configuration value cannot be coerced to the requested type.

    The message names the offending key path and the value that failed to
    parse, plus a fragment of context about what type was expected. The
    error is a subclass of :class:`ValueError` so callers who don't want
    a Flowthru-specific dependency can catch it via the standard hierarchy.
    """


# ── Coercion helpers ────────────────────────────────────────────────────


_BOOL_TRUE = {"true", "yes", "1", "on"}
_BOOL_FALSE = {"false", "no", "0", "off"}


def _coerce_int(value: str, key: str) -> int:
    try:
        return int(value)
    except (TypeError, ValueError) as exc:
        raise ConfigCoercionError(
            f"Config key {key!r} has value {value!r} which cannot be parsed as int."
        ) from exc


def _coerce_float(value: str, key: str) -> float:
    try:
        return float(value)
    except (TypeError, ValueError) as exc:
        raise ConfigCoercionError(
            f"Config key {key!r} has value {value!r} which cannot be parsed as float."
        ) from exc


def _coerce_bool(value: str, key: str) -> bool:
    lowered = value.strip().lower()
    if lowered in _BOOL_TRUE:
        return True
    if lowered in _BOOL_FALSE:
        return False
    raise ConfigCoercionError(
        f"Config key {key!r} has value {value!r} which cannot be parsed as bool. "
        f"Expected one of: {sorted(_BOOL_TRUE | _BOOL_FALSE)}."
    )


# ── Re-nesting from env ─────────────────────────────────────────────────


def _re_nest(env: Mapping[str, str]) -> dict[str, Any]:
    """Reconstruct a nested dict from ``Section__Sub__Key=value`` env vars.

    Skips env vars without the ``__`` separator (these are unrelated
    process env vars). Conflicts where one key is both a leaf and a parent
    are resolved by keeping the deeper structure — leaves with the same
    prefix as a section are dropped on the assumption that the section
    was meant.
    """
    nested: dict[str, Any] = {}
    for key, value in env.items():
        if "__" not in key:
            continue
        parts = key.split("__")
        current: dict[str, Any] = nested
        for part in parts[:-1]:
            slot = current.get(part)
            if not isinstance(slot, dict):
                slot = {}
                current[part] = slot
            current = slot
        # Don't clobber a section with a leaf — keep the deeper structure.
        if not isinstance(current.get(parts[-1]), dict):
            current[parts[-1]] = value
    return _materialize_arrays(nested)


def _materialize_arrays(node: Any) -> Any:
    """Convert dicts whose keys are all sequential non-negative integers into lists.

    Mirrors .NET's IConfiguration array semantics: ``Foo:0``, ``Foo:1``
    flattens to env vars with numeric leaf keys, and re-binds to a
    ``List<T>``. Non-sequential numeric keys (e.g., gaps) stay as a dict.
    """
    if isinstance(node, dict):
        cooked = {k: _materialize_arrays(v) for k, v in node.items()}
        if cooked and all(k.isdigit() for k in cooked):
            indexed = sorted((int(k), v) for k, v in cooked.items())
            # Only collapse when the indices form a contiguous 0..N-1 range.
            if [i for i, _ in indexed] == list(range(len(indexed))):
                return [v for _, v in indexed]
        return cooked
    return node


# Computed lazily on first access. Test code can call _reset() to clear.
_root: dict[str, Any] | None = None


def _ensure_loaded() -> dict[str, Any]:
    global _root
    if _root is None:
        _root = _re_nest(os.environ)
    return _root


def _reset() -> None:
    """Clear the cached re-nested view. Test-only; not re-exported."""
    global _root
    _root = None


# ── Public API: ConfigSection ───────────────────────────────────────────


class ConfigSection(Mapping):
    """A view over a sub-tree of the re-nested configuration.

    Mirrors .NET's :class:`IConfigurationSection` — supports indexing by
    colon-delimited key paths, exposes typed getters, and ``get_section``
    always returns a section (never None).
    """

    def __init__(self, data: Any, path: str = ""):
        self._data: dict[str, Any] = data if isinstance(data, dict) else {}
        self._path = path

    # ── Mapping protocol ────────────────────────────────────────────

    def __getitem__(self, key: str) -> Any:
        return self._navigate(key, default=MISSING)

    def __iter__(self) -> Iterator[str]:
        return iter(self._data)

    def __len__(self) -> int:
        return len(self._data)

    def __contains__(self, key: object) -> bool:
        if not isinstance(key, str):
            return False
        try:
            self._navigate(key, default=MISSING)
            return True
        except KeyError:
            return False

    # ── Typed accessors ─────────────────────────────────────────────

    def get(self, key: str, default: Any = None) -> Any:
        try:
            return self._navigate(key, default=MISSING)
        except KeyError:
            return default

    def get_str(self, key: str, default: str = "") -> str:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return default
        return str(value)

    def get_optional_str(self, key: str) -> str | None:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return None
        return str(value)

    def get_int(self, key: str, default: int = 0) -> int:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return default
        return _coerce_int(str(value), self._qualify(key))

    def get_optional_int(self, key: str) -> int | None:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return None
        return _coerce_int(str(value), self._qualify(key))

    def get_float(self, key: str, default: float = 0.0) -> float:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return default
        return _coerce_float(str(value), self._qualify(key))

    def get_optional_float(self, key: str) -> float | None:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return None
        return _coerce_float(str(value), self._qualify(key))

    def get_bool(self, key: str, default: bool = False) -> bool:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return default
        return _coerce_bool(str(value), self._qualify(key))

    def get_optional_bool(self, key: str) -> bool | None:
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return None
        return _coerce_bool(str(value), self._qualify(key))

    def get_list(self, key: str) -> list[str]:
        """Materialized array values (post-array-detection) as a list of raw strings.

        Caller coerces individual elements if a typed list is needed.
        Returns an empty list when the key is absent.
        """
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return []
        if not isinstance(value, list):
            raise ConfigCoercionError(
                f"Config key {self._qualify(key)!r} is not a list "
                f"(got {type(value).__name__})."
            )
        return [str(v) for v in value]

    def get_section(self, key: str) -> "ConfigSection":
        """Return the named subsection. Always returns a section — empty when absent."""
        value = self._navigate(key, default=MISSING_LEAF)
        if value is MISSING_LEAF:
            return ConfigSection({}, path=self._qualify(key))
        return ConfigSection(value, path=self._qualify(key))

    # ── Internals ───────────────────────────────────────────────────

    def _navigate(self, key: str, default: Any) -> Any:
        node: Any = self._data
        for part in key.split(":"):
            if isinstance(node, dict) and part in node:
                node = node[part]
            else:
                if default is MISSING:
                    raise KeyError(self._qualify(key))
                return default
        return node

    def _qualify(self, key: str) -> str:
        return f"{self._path}:{key}" if self._path else key


# Sentinel distinct from None so optional-access can distinguish present-None
# from absent.
class _MissingLeaf:
    __slots__ = ()
    def __repr__(self) -> str: return "<missing>"
MISSING_LEAF = _MissingLeaf()


# ── Module-level conveniences ───────────────────────────────────────────


def root() -> ConfigSection:
    """Return the full re-nested configuration view as a single section."""
    return ConfigSection(_ensure_loaded(), path="")


def get_section(key: str) -> ConfigSection:
    """Get a section by .NET-style key (e.g., ``"Diarization"`` or ``"A:B"``)."""
    return root().get_section(key)


def __getitem__(key: str) -> Any:  # noqa: D105 — module-level indexing isn't supported by Python; use root().
    raise TypeError(
        "Module-level indexing is not supported. Use `flowthru.config.root()[key]` "
        "or `flowthru.config.get_section(name)[key]` instead."
    )


# ── bind() — dataclass auto-binding ─────────────────────────────────────


def bind(cls: type, section_name: str) -> Any:
    """Materialize a frozen dataclass from a named configuration section.

    Field names are converted from snake_case to PascalCase to derive the
    section key (``pyannote_model`` → ``PyannoteModel``). Each field is
    coerced based on its type hint; missing values fall back to the
    field's declared default, mirroring
    ``IConfiguration.GetValue<T>(key, default)``.

    Supported field types:

    * ``str``, ``int``, ``bool``, ``float``
    * ``X | None`` / ``Optional[X]`` for any of the above (returns
      ``None`` on absence; defaults still honored when declared)
    * ``list[str]`` (any element type — coerced to strings)

    Other types raise :class:`ConfigCoercionError`. Users with richer
    needs (custom validators, nested dataclasses, etc.) should write
    their own ``load`` classmethod using the typed accessors directly.

    Args:
        cls: The frozen dataclass type to materialize.
        section_name: The section key passed to :func:`get_section`.

    Returns:
        A new instance of ``cls`` populated from the section.

    Raises:
        TypeError: If ``cls`` is not a dataclass.
        ConfigCoercionError: If any field's value cannot be coerced.
    """
    if not is_dataclass(cls):
        raise TypeError(f"bind() requires a dataclass type; got {cls!r}.")

    section = get_section(section_name)
    type_hints = get_type_hints(cls)
    kwargs: dict[str, Any] = {}

    for f in fields(cls):
        section_key = _to_pascal(f.name)
        type_hint = type_hints.get(f.name, f.type)
        default = _field_default(f)
        kwargs[f.name] = _bind_field(section, section_key, type_hint, default, f.name)

    return cls(**kwargs)


def _to_pascal(snake: str) -> str:
    """``pyannote_model`` → ``PyannoteModel``."""
    return "".join(part[:1].upper() + part[1:] for part in snake.split("_") if part)


def _field_default(f) -> Any:
    if f.default is not MISSING:
        return f.default
    if f.default_factory is not MISSING:  # type: ignore[misc]
        return f.default_factory()  # type: ignore[misc]
    return _NO_DEFAULT


_NO_DEFAULT = object()


def _bind_field(
    section: ConfigSection,
    key: str,
    type_hint: Any,
    default: Any,
    field_name: str,
) -> Any:
    """Coerce a single field value from the section."""
    optional, inner = _unwrap_optional(type_hint)

    if inner is str:
        if optional:
            value = section.get_optional_str(key)
            return value if value is not None else (default if default is not _NO_DEFAULT else None)
        if default is _NO_DEFAULT:
            return section.get_str(key)  # default ""
        return section.get_str(key, default)

    if inner is int:
        if optional:
            value = section.get_optional_int(key)
            return value if value is not None else (default if default is not _NO_DEFAULT else None)
        if default is _NO_DEFAULT:
            return section.get_int(key)
        return section.get_int(key, default)

    if inner is bool:
        if optional:
            value = section.get_optional_bool(key)
            return value if value is not None else (default if default is not _NO_DEFAULT else None)
        if default is _NO_DEFAULT:
            return section.get_bool(key)
        return section.get_bool(key, default)

    if inner is float:
        if optional:
            value = section.get_optional_float(key)
            return value if value is not None else (default if default is not _NO_DEFAULT else None)
        if default is _NO_DEFAULT:
            return section.get_float(key)
        return section.get_float(key, default)

    if get_origin(inner) is list:
        # list[str] / list[Any] — return raw strings
        items = section.get_list(key)
        if not items and default is not _NO_DEFAULT:
            return default
        return items

    raise ConfigCoercionError(
        f"bind(): field {field_name!r} has unsupported type {type_hint!r}. "
        f"Supported: str, int, bool, float, list[str], and X | None of those."
    )


def _unwrap_optional(type_hint: Any) -> tuple[bool, Any]:
    """If ``type_hint`` is ``X | None``, return ``(True, X)``; else ``(False, type_hint)``."""
    origin = get_origin(type_hint)
    # types.UnionType (PEP 604: X | None) and typing.Union both produce a non-None origin.
    if origin is None:
        return False, type_hint
    args = [a for a in get_args(type_hint) if a is not type(None)]
    nones = [a for a in get_args(type_hint) if a is type(None)]
    if len(nones) == 1 and len(args) == 1:
        return True, args[0]
    return False, type_hint
