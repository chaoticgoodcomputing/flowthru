# Parser Migration: String-based → Token-based Combinators

## Goal
Align MagicAST with monadic parser combinator theory by using Superpower's `TokenListParser` instead of string manipulation.

## Current State (Dec 2025)

### ✅ Completed
- **Proof-of-concept**: `Parsing/Combinators/OracleParsers.cs` (379 lines)
  - Demonstrates proper monadic composition using LINQ query syntax
  - Implements 15+ keyword parsers: Flying, Vigilance, Trample, Haste, Lifelink, Reach, Flash, FirstStrike, DoubleStrike, landwalks, Protection
  - Composite parsers: `SimpleKeyword`, `AnyKeyword`, `KeywordList`
  - Pattern: `from token in Token.EqualTo(...) select new Effect(...)`

### ⚠️ Needs Integration
- **NOT YET USED**: `OracleParsers.cs` exists but isn't wired into production code
- `StaticAbilityParser.cs` still uses old string-based approach:
  - `TryParseKeywordList()` uses `string.Split(',')`
  - `_simpleKeywordMap` dictionary for string matching
  - Regex for parentheticals
  - `String.StartsWith()` / `Contains()` pattern matching

### ❌ Failing Tests (Baseline: 61/120 fail)
Not regressions—these existed before combinator work:
- Triggered abilities: "not yet implemented"
- Complex static abilities: "AmbiguousStructure" 
- Modal abilities: "Choose one" patterns
- Activated abilities: Some effect parsing gaps

## Architecture Issue

### Before (Current - Bad)
```
Tokenizer → Token[] → string reconstruction → Regex/Split/Contains
```
Superpower used only for tokenization, then **discarded** in favor of string parsing.

### After (Target - Good)
```
Tokenizer → Token[] → TokenListParser combinators → AST
```
Tokens stay as tokens. Combinators compose via monadic bind (`from`/`select`).

## Migration Path

### Phase 1: Integrate Keywords (NEXT STEP)
1. Update `StaticAbilityParser.TryParseKeywordList()`:
   ```csharp
   // OLD: string-based
   var keywords = text.Split(',').Select(k => k.Trim());
   
   // NEW: use OracleParsers.KeywordList
   var result = OracleParsers.KeywordList(tokens);
   ```

2. Replace `_simpleKeywordMap` dictionary with `OracleParsers.AnyKeyword`

3. Run tests: Verify keyword parsing still works (expect no new failures)

### Phase 2: Migrate ActivatedAbilityParser
- Cost parsing: Already uses `ManaCostParser` (good starting point)
- Effect parsing: Needs combinator approach for common patterns
- Pattern: `{costs}: {effects}` → `CostParser.Then(EffectParser)`

### Phase 3: Migrate TriggeredAbilityParser  
- Trigger detection: "When", "Whenever", "At"
- Event parsing: Compound patterns like "at the beginning of your upkeep"
- Intervening-if clauses: "if {condition}, {effect}"

### Phase 4: Complex Static Abilities
- Enchant, Equip restrictions
- Cost reduction effects
- Targeting restrictions
- Continuous effects

### Phase 5: Modal & Spell Abilities
- "Choose one —" parsing
- Multi-mode selection
- Spell effects vs permanent abilities

## Key Patterns

### Monadic Composition (LINQ syntax)
```csharp
// Simple keyword
from keyword in Keyword("flying")
from reminder in _optionalReminder
select EffectFactory.Flying();

// Sequential composition (multi-word keywords)
from first in Keyword("first")
from strike in Keyword("strike")
from reminder in _optionalReminder
select EffectFactory.FirstStrike();

// Parameterized keywords
from keyword in Keyword("protection")
from fromWord in Keyword("from")
from qualities in _protectionQuality.ManyDelimitedBy(...)
select EffectFactory.Protection(qualities.ToList());
```

### Helper Functions
```csharp
// Case-insensitive word matching
static TokenListParser<OracleToken, Unit> Keyword(string word) =>
    Token.EqualTo(OracleToken.Word)
        .Where(t => string.Equals(t.ToStringValue(), word, 
                                  StringComparison.OrdinalIgnoreCase))
        .Value(Unit.Value);
```

### Alternatives (Or combinator)
```csharp
SimpleKeyword.Try()
    .Or(Landwalk.Try())
    .Or(Protection.Try())
```

## Common Pitfalls

1. **Naming convention**: Private static parsers need `_underscore` prefix
2. **Using directives**: Add `MagicAST.AST.Effects.*` namespaces
3. **Type conversions**: `ManyDelimitedBy` returns `T[]`, convert to `IReadOnlyList<T>` with `.Select(arr => (IReadOnlyList<T>)arr)`
4. **Try() for backtracking**: Use `.Try()` on alternatives to enable parser backtracking
5. **Token vs String**: Don't reconstruct strings—work with token sequences

## Testing Strategy

1. **Run baseline**: `dotnet test tests/MagicAST.Tests/` (current: 59 pass, 61 fail)
2. **Incremental migration**: Replace one parser at a time
3. **No new failures**: Each migration should maintain or improve pass rate
4. **Golden files**: Test cases in `tests/MagicAST.Tests/TestCards/`

## Files to Modify

### High Priority
- `src/MagicAST/Parsing/Parsers/StaticAbilityParser.cs` (349 lines) - First target
- `src/MagicAST/Parsing/Parsers/ActivatedAbilityParser.cs` - Second target
- `src/MagicAST/Parsing/Parsers/TriggeredAbilityParser.cs` - Third target

### Reference
- `src/MagicAST/Parsing/Combinators/OracleParsers.cs` - Working examples
- `src/MagicAST/Parsing/Tokens/OracleTokenizer.cs` - Token definitions
- `src/MagicAST/Parsing/Tokens/OracleToken.cs` - Token enum (274 lines)

## Success Criteria

- [ ] Zero string-based parsing (no `Split`, `Contains`, `StartsWith` on oracle text)
- [ ] All parsers use `TokenListParser<OracleToken, TResult>`
- [ ] Test pass rate improves (target: 90%+ of 120 tests)
- [ ] No regex on tokenized content (only in tokenizer itself)
- [ ] Composable grammar: small parsers → complex parsers

## Resources

- **Superpower docs**: https://github.com/datalust/superpower
- **Parser combinator theory**: Monadic composition, backtracking, left-recursion avoidance
- **LINQ as monads**: `from`/`select` = bind/return in Haskell's do-notation

---

**Status**: Ready for Phase 1 integration  
**Next Engineer**: Start with `StaticAbilityParser.TryParseKeywordList()` migration  
**Expected Impact**: Foundation for scalable, compositional grammar
