"""
Test module for Directory<T> marshalling between C# and Python.

The C# DirectoryStorageAdapter shape projects into a plain Python dict on the function
side: keys are file paths (strings), values are the inner type's natural Python
representation (bytes, scalar, DataFrame). These functions exercise round-trips and
simple structural transforms so the C# tests can assert that:

  * dict[str, bytes]      — Directory<byte[]>      survives the round-trip
  * dict[str, int]        — Directory<int>         (scalar inner) survives
  * dict[str, DataFrame]  — Directory<IEnumerable<Row>> (tabular inner) survives
"""


def echo_dir(d):
    """Return the directory unchanged. Verifies bidirectional marshalling."""
    return d


def upper_keys(d):
    """Return a new dict with the same values but uppercased keys."""
    return {k.upper(): v for k, v in d.items()}


def add_one_to_values(d):
    """Increments each scalar (int) value by 1."""
    return {k: v + 1 for k, v in d.items()}
