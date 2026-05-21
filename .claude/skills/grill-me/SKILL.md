---
name: grill-me
description: Grilling session that challenges your plan against Flowthru's existing domain model, sharpens terminology, and updates per-context CONTRIBUTING.md glossaries and .claude/docs/adr/ inline as decisions crystallise. Use when you want to stress-test a plan against the project's language and documented decisions.
---

<what-to-do>

Interview the user relentlessly about every aspect of the plan until you reach a shared understanding. Walk down each branch of the design tree, resolving dependencies between decisions one-by-one. For each question, provide your recommended answer.

Ask the questions one at a time, waiting for feedback on each question before continuing.

If a question can be answered by exploring the codebase, explore the codebase instead.

</what-to-do>

<supporting-info>

## Where the docs live

Flowthru is **multi-context**. Vocabulary and conventions live in per-context CONTRIBUTING.md files:

| Context | CONTRIBUTING.md |
| --- | --- |
| Cross-cutting (philosophy, error phases, decision rules, context map) | [/CONTRIBUTING.md](/CONTRIBUTING.md) |
| Flow / Catalog Developer (writing Flows on Flowthru) | [/examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) |
| Extension Developer (extending Flowthru) | [/src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) |
| Core Developer (curating Flowthru's core engine) | [/src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md) |
| Core test author | [/tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md) |
| Extension test author | [/tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md) |

When a term resolves, update the **Glossary** section of the relevant per-context CONTRIBUTING.md. See [GLOSSARY-FORMAT.md](./GLOSSARY-FORMAT.md) for the entry format and per-context depth rules.

Architecture Decision Records live in [.claude/docs/adr/](/.claude/docs/adr/) — numbered sequentially (`0001-slug.md`, `0002-slug.md`, ...). See [ADR-FORMAT.md](./ADR-FORMAT.md) for the template and when-to-create rules.

## During the session

### Challenge against the relevant glossary

When the user uses a term that conflicts with an existing definition, call it out immediately: *"Your context's glossary defines 'Step' as a logical unit of work in a Flow, but you seem to mean a Catalog Item — which is it?"* Match the relevant context to the topic being grilled (e.g., extending an extension surface → `src/extensions/CONTRIBUTING.md`).

### Sharpen fuzzy language

When the user uses vague or overloaded terms, propose a precise canonical term. *"You're saying 'pipeline' — do you mean a Flow (Flowthru's typed implementation) or the general data-engineering concept? Flowthru-technical contexts use 'Flow'."*

### Discuss concrete scenarios

When domain relationships are being discussed, stress-test them with specific scenarios. Invent scenarios that probe edge cases and force the user to be precise about the boundaries between concepts. *"What happens when two Flows compose the same Catalog but write to the same Catalog Item — DAG-validation error, or silent merge?"*

### Cross-reference with code

When the user states how something works, check whether the code agrees. The `examples/starter/KedroSpaceflights`, `examples/advanced/FlowthruCoverage`, and `examples/advanced/SpaceflightsDistributed` projects are the canonical reference implementations; `src/core/Flowthru.Core/` is where the internal abstractions live. If you find a contradiction, surface it: *"Your code in `IStepNode.cs` calls this an 'arrow archetype,' but you just said Steps and Items are both nodes — which framing is canonical?"*

### Update the relevant CONTRIBUTING.md inline

When a term is resolved, append it to the **Glossary** section of the appropriate per-context CONTRIBUTING.md right there. **Don't batch these up** — capture them as they happen. Use the format and per-context rules in [GLOSSARY-FORMAT.md](./GLOSSARY-FORMAT.md).

Glossary entries are for *emergent patterns* and *project-specific concepts with non-obvious names*. Specific framework types (e.g., `FlowIO<T>`, individual storage adapters) self-document via XML doc comments on the classes themselves — don't enumerate them in the glossary.

### Offer ADRs sparingly

Only offer to create an ADR when all three are true:

1. **Hard to reverse** — the cost of changing your mind later is meaningful
2. **Surprising without context** — a future reader will wonder *"why did they do it this way?"*
3. **The result of a real trade-off** — there were genuine alternatives and you picked one for specific reasons

If any of the three is missing, skip the ADR. Use the template in [ADR-FORMAT.md](./ADR-FORMAT.md).

</supporting-info>
