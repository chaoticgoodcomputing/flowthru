# Customizing Schema Property Types

Use `IScalar` to allow custom types — NewTypes, value objects, strong-typed identifiers — to appear as columns in flat schemas without being misclassified as nested objects.

## How `[FlowthruSchema]` Classifies Properties

When you annotate a schema with `[FlowthruSchema]`, the source generator inspects every public property and decides: does this schema serialize to a flat table of columns (`IFlatSchema`), or does it contain structure that requires a hierarchical format (`INestedSchema`)?

The decision rule for any individual property type is the **JSON test**: would this value appear as `"key": value` in JSON, or as `"key": {...}`?

- `"customer_id": "abc-123"` — scalar, flat
- `"address": { "street": "...", "city": "..." }` — object, nested
- `"tags": [...]` — array, nested

A schema is flat if and only if every property passes the JSON test. One nested property makes the whole schema nested.

### What is automatically recognized as flat

The generator recognizes five categories of flat property types, in order:

**1. CLR primitives**

`bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `char`, `string`, `DateTime`

Note that `string` is technically `IEnumerable<char>` at the CLR level, but it is a compiler-known primitive type and always treated as a single scalar value. It does not behave like a collection for schema classification purposes.

**2. Enums**

Any `enum` type, regardless of its underlying integer type.

**3. `byte[]`**

Structurally an array, but treated as an opaque binary blob — for example, a compressed image or a serialized document stored in a single column. It is not treated as a traversable collection.

**4. Known BCL scalar structs**

`Guid`, `TimeSpan`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `Half`, `Int128`, `UInt128`

These are value types defined in the .NET BCL that cannot opt in via `IScalar` (they're not your types to modify), so the generator recognizes them by name.

**5. User-defined types implementing `IScalar`**

Any type you write that explicitly declares it serializes to a single value. See below.

If a property type doesn't match any of these five categories, it is treated as a nested object, and the schema becomes `INestedSchema`.

## Using `IScalar` for Custom Types

If you have a NewType or value-object wrapper that backs a single primitive value, implement `IScalar` to declare its scalar intent:

```csharp
// A strong-typed identifier backed by a single string value
public readonly record struct CustomerId(string Value) : IScalar;

// A domain primitive backed by a single decimal
public readonly record struct Price(decimal Amount) : IScalar;
```

Schemas using these types are now classified as flat:

```csharp
[FlowthruSchema]
public partial record OrderSchema
{
    public required CustomerId Id { get; init; }    // flat — implements IScalar
    public required string Name { get; init; }      // flat — string primitive
    public required Price UnitPrice { get; init; }  // flat — implements IScalar
}
```

`OrderSchema` gets `IFlatSchema`, `ITextSerializable`, and all the flat-format markers. It can be stored in CSV.

## When NOT to Use `IScalar`

`IScalar` is a declaration of intent. Implementing it on a type that doesn't actually serialize to a single value will cause silent data loss or serialization failures in flat formats — the generator trusts the declaration without inspecting your serializer.

```csharp
// ❌ Multi-property struct — NOT a scalar
public readonly record struct Address(string Street, string City) : IScalar; // wrong

// ❌ Collection wrapper — NOT a scalar
public record TagList(List<string> Values) : IScalar; // wrong
```

Implement `IScalar` only when your type round-trips through a single string, numeric, or boolean column. If it requires more than one column, it's not a scalar — the schema should be nested, or the type should be flattened at the node level before writing.

## Nesting Two Flat Schemas

A common misconception: if two schemas are both individually flat, does nesting one inside the other produce a flat schema?

No. The moment a schema appears as a property of another schema — rather than as a column value — that property serializes as `{...}` in JSON, not as a single value. The parent schema becomes nested regardless of what the child schema looks like on its own.

```csharp
[FlowthruSchema]
public partial record AddressSchema
{
    public required string Street { get; init; }
    public required string City { get; init; }
}

// ❌ AddressSchema as a property produces "address": {...} — nested, not flat
[FlowthruSchema]
public partial record PersonSchema
{
    public required string Name { get; init; }
    public required AddressSchema Address { get; init; }  // produces INestedSchema
}
```

If you need `PersonSchema` to be flat for CSV storage, the node that produces it is responsible for projecting the address fields into top-level columns — for example, `AddressStreet` and `AddressCity` as `string` properties directly on `PersonSchema`.

## Quick Reference

| Property type                              | Flat? | Reason                                             |
| ------------------------------------------ | ----- | -------------------------------------------------- |
| `int`, `string`, `bool`, `double`, etc.    | ✅     | CLR primitives                                     |
| `byte`                                     | ✅     | CLR primitive                                      |
| `byte[]`                                   | ✅     | Opaque binary blob                                 |
| `string`                                   | ✅     | CLR primitive (not treated as `IEnumerable<char>`) |
| Any `enum`                                 | ✅     | Single-value by definition                         |
| `Guid`, `TimeSpan`, `DateTimeOffset`, etc. | ✅     | Known BCL scalar structs                           |
| `record struct CustomerId(...) : IScalar`  | ✅     | Declared scalar via `IScalar`                      |
| `record struct Address(string, string)`    | ❌     | Multi-property struct, no `IScalar`                |
| `List<T>`, `T[]`, `IEnumerable<T>`         | ❌     | Collection                                         |
| `Dictionary<K, V>`                         | ❌     | Collection                                         |
| Any class or record without `IScalar`      | ❌     | Assumed nested object                              |
| Another `[FlowthruSchema]` type            | ❌     | Nested row, not a scalar value                     |
