# LanguageExt Coexistence Examples

These examples demonstrate that Flowthru's abstraction layer successfully resolves the LanguageExt v4/v5 compatibility issue.

## The Problem

LanguageExt v4 and v5 cannot coexist in the same process:
- Same assembly name: `LanguageExt.Core.dll`
- Same namespace: `LanguageExt`
- Incompatible types: v5's `IO<T>` doesn't exist in v4 (uses `Aff<T>`/`Eff<T>` instead)

This made it impossible for downstream projects using v4 to consume libraries (like Flowthru) that depend on v5.

## The Solution

Flowthru **removed its LanguageExt dependency entirely** and introduced `FlowIO<T>` as a version-independent effect abstraction. This allows:

1. Projects with **no LanguageExt** to use Flowthru directly
2. Projects with **LanguageExt v4** to use both `Aff<T>` and `FlowIO<T>` simultaneously
3. Projects with **LanguageExt v5** to use both `IO<T>` and `FlowIO<T>` simultaneously

## Test Scenarios

### Scenario 1: No LanguageExt Dependency
**Example:** `KedroSpaceflights.Pure`

```bash
cd examples/KedroSpaceflights.Pure
dotnet run
```

- ✅ No LanguageExt reference
- ✅ Uses `FlowIO<T>` directly
- ✅ Zero auxiliary dependencies for effect management

### Scenario 2: LanguageExt v4 Coexistence
**Example:** `LanguageExtV4Coexistence`

```bash
cd examples/LanguageExtV4Coexistence
dotnet run
```

**Dependencies:**
- Flowthru (no LanguageExt)
- LanguageExt.Core 4.4.9

**Demonstrates:**
- ✅ Using v4's `Aff<T>` for application logic
- ✅ Using Flowthru's `FlowIO<T>` for pipeline operations
- ✅ No type conflicts between `Aff<T>` and `FlowIO<T>`

### Scenario 3: LanguageExt v5 Coexistence
**Example:** `LanguageExtV5Coexistence`

```bash
cd examples/LanguageExtV5Coexistence
dotnet run
```

**Dependencies:**
- Flowthru (no LanguageExt)
- LanguageExt.Core 5.0.0-beta-54

**Demonstrates:**
- ✅ Using v5's `IO<T>` for application logic
- ✅ Using Flowthru's `FlowIO<T>` for pipeline operations
- ✅ No type conflicts between `IO<T>` and `FlowIO<T>`

## Validation Results

| Scenario | Build | Run | LanguageExt Version | Flowthru Dependency |
|----------|-------|-----|---------------------|---------------------|
| No LanguageExt | ✅ | ✅ | None | FlowIO<T> |
| With v4 | ✅ | ✅ | 4.4.9 (Aff<T>) | FlowIO<T> |
| With v5 | ✅ | ✅ | 5.0.0-beta-54 (IO<T>) | FlowIO<T> |

## Key Takeaways

1. **Flowthru is LanguageExt-independent** - Core library has zero LanguageExt dependencies
2. **Abstraction eliminates conflicts** - `FlowIO<T>` provides a neutral effect type that doesn't clash with v4 or v5
3. **Backward compatibility** - Existing v4 users can now adopt Flowthru without upgrading to v5
4. **Forward compatibility** - v5 users can continue using v5 alongside Flowthru
5. **Simplified dependency graph** - Projects without LanguageExt don't need to pull it in

## Future: Interop Packages

While these examples show **coexistence** (both types work side-by-side), they don't show **conversion** between types. For seamless interop, Phase 3 will introduce:

- `Flowthru.Interop.LanguageExt4` - Provides `.ToFlowIO()` and `.ToAff()` extensions
- `Flowthru.Interop.LanguageExt5` - Provides `.ToFlowIO()` and `.ToIO()` extensions

These will be separate packages that projects can opt-into when they need bidirectional conversion between LanguageExt effects and Flowthru effects.
