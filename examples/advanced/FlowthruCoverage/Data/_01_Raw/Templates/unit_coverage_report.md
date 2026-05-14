<!--
Template for the unit coverage report. Placeholders of the form
double-open-brace-NAME-double-close-brace are substituted at pipeline time by
BuildUnitCoverageReportStep. Edit this file to change agent instructions or
report structure without touching pipeline source code.

Token names available (all substitute to literal markdown — the data blocks
bring their own formatting; the counts are bare integer strings):

  threshold_pct        — coverage threshold percentage (integer)
  total_libraries      — total src libraries reviewed
  failing_count        — count of libraries below threshold
  quick_wins_count     — methods exercised by integration only
  cold_spots_count     — methods with no coverage at all
  scoreboard_table     — markdown table of all libraries
  drilldown_sections   — H3 sections, one per failing library
  quick_wins_sections  — H4 sections, one per library with quick wins
  cold_spots_sections  — H4 sections, one per library with cold spots

Unknown tokens are left untouched in the rendered report. Token names not used
in the template are silently dropped from substitution.
-->

# Unit Coverage Gap Closure

You are an agent assigned to close unit-test coverage gaps in this codebase.
This report is the canonical work queue. **{{failing_count}}** of
**{{total_libraries}}** src libraries are below the **{{threshold_pct}}%**
unit-coverage threshold: **{{quick_wins_count}}** methods are quick wins,
**{{cold_spots_count}}** are cold spots.

## Your task

1. **Pick a library that no other agent is working on.** Library ownership is
   the unit of parallelism — each library has its own
   `tests/{src-dir}/{Library}.Tests/` project, so two agents working different
   libraries can't collide.
2. **Work cold spots first.** A cold spot has *no* coverage anywhere — we
   genuinely don't know whether the code works. Closing one is the only way to
   move from "untested" to "tested at all". Start with the simplest meaningful
   path; expand outward as you build a mental model of the method.
3. **Then move to quick wins.** Quick-win methods are exercised by example
   pipelines today, so we know they execute — they're a second-pass cleanup
   to lock the contract. Find the integration caller (`grep` the method name
   across `examples/`), understand the intended usage, then assert on it.

## Test conventions

- Tests live in `tests/{src-dir}/{Library}.Tests/`, mirroring the library's
  directory layout (one `{Class}Tests.cs` per source class).
- Framework: **NUnit** — `[TestFixture]`, `[Test]`, `Assert.That(actual, Is.EqualTo(expected))`.
- Naming: `Method_Scenario_ExpectedBehavior` (e.g.
  `Build_WhenEmptyInput_ReturnsEmptySequence`).
- Assert on **observable behavior**, not implementation details. Prefer
  constructing real objects to mocking them.
- One behavior per test; multiple tests per method are fine.

## Acceptance

A method is resolved when:
- `dotnet test tests/{src-dir}/{Library}.Tests/{Library}.Tests.csproj` passes.
- The test asserts on observable behavior of the method.
- Re-running `FlowthruCoverage` removes the method from this checklist.

## Coordination

The checklist is regenerated each pipeline run. To avoid stepping on other
agents:

- Claim work by library, not by individual method — the per-library test
  project is the smallest unit that can be safely owned end-to-end.
- If you finish a library before the queue is empty, claim another failing
  library from the scoreboard; don't reach into someone else's.
- Don't modify production code beyond what's strictly needed for testability
  (e.g. adding `InternalsVisibleTo` for a test project is fine; changing a
  private method to public is not).

---

## Scoreboard

Ranked by lines-to-threshold (the smallest unit-of-work to flip a library from
failing to passing). Libraries marked `✓` already pass.

{{scoreboard_table}}

## Drill-down

Sub-tree hotspots within failing libraries (top 10 each, ranked by lines
needed to reach threshold). Use these to decide where in a library to focus
first — the directory or file with the biggest gap is where each unit test
moves the project metric the most.

{{drilldown_sections}}

## Method checklist

### Cold spots ({{cold_spots_count}})

No coverage anywhere — we don't know whether these methods work. **Highest
priority.** Start with the simplest meaningful path and expand outward.

{{cold_spots_sections}}

### Quick wins ({{quick_wins_count}})

Exercised by integration tests but with no unit hit. Code paths are known to
execute — your job is to lock the contract. Pick these up after cold spots
are clear.

{{quick_wins_sections}}
