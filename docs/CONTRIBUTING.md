# Contributing to Flowthru Docs

First things first — thanks for being willing to contribute to the Flowthru docs! The overall promise of Flowthru is to make datapipelines straightforward to write, and reliable to run. The docs are a huge part of that.

Before starting to plan docs updates, please take a look at the [core contributor guide](../CONTRIBUTING.md) to understand the philosophy for Flowthru to better line up new documentation with the two core promises of Flowthru:

> 1. End-users can easily write data pipelines, and have a development experience focused on what *their* pipelines will do, not how Flowthru is handling the pipeline.
> 2. If an error can occur in the pipeline they've created, it will occur as soon in the development process as possible.

## The Question-Driven Process

The documentation for Flowthru follows the [Diátaxis philosophy](https://diataxis.fr/) for technical writing. When users or contributors have questions, the nature of the question helps us sort it into a category:

| Category        | Question Pattern                | Context               |
| --------------- | ------------------------------- | --------------------- |
| **Tutorials**   | "How do I get started with...?" | Learning (study)      |
| **Guides**      | "How do I accomplish...?"       | Working (action)      |
| **Explanation** | "Why...?" / "How does... work?" | Understanding (study) |
| **Reference**   | "What are the details of...?"   | Working (information) |

When writing or editing documentation, ask yourself: **"What question does this answer?"**

This question reveals both your audience and the type of documentation you're creating. The answer determines not just where the content lives, but how you write it, what tone you use, and what you include or exclude.

Consider these examples:

- **"How do I start writing my first pipeline?"** → This is someone learning. They need a tutorial.
- **"How can I read data from a database instead of local files?"** → This is someone working with a specific need. They need a guide.
- **"Why does Flowthru use so many types?"** → This is someone studying the framework's design. They need an explanation.
- **"What parameters does `Item` accept?"** → This is someone looking up technical details. They need reference documentation.

The same topic can yield different documentation depending on the question:

**Topic: Schemas**
- "How do I create my first schema?" → Tutorial
- "How do I handle optional fields in my schema?" → Guide
- "Why do I have to use schemas on my nodes and catalog entries?" → Explanation
- "What attributes can I use on schema properties?" → Reference

Understanding this pattern means understanding your audience. Flowthru has two primary audiences:

1. **End-users** — data engineers building pipelines with Flowthru
2. **Contributors** — developers extending or maintaining Flowthru itself

Both audiences move through the same cycle: learning → working → studying → working. The question they're asking tells you where they are in that cycle.

### Directory Structure

Each documentation category has a top-level directory for end-user content and an `advanced/` subdirectory for contributor-focused content:

```
docs/
├── tutorials/
│   └── advanced/           # Contributor onboarding tutorials
├── guides/
│   └── advanced/           # Extension authoring guides
├── explanation/
│   ├── anatomy-of-a-pipeline.md   # End-user: how pipelines work
│   └── advanced/
│       └── storage-composition.md # Contributor: internal architecture
└── reference/              # Generated from code — see below
```

**End-user content** lives at the category root. It answers questions from data engineers building pipelines.

**Contributor content** lives in `advanced/`. It answers questions from developers extending or maintaining Flowthru itself — storage adapters, source generators, validation layers.

### Tutorials: Learning-Oriented

**Questions answered:** "How do I get started?" / "How do I learn X?"

Tutorials are lessons. They guide someone through their first encounter with a concept by having them build something concrete. The goal isn't the artifact they create — it's the learning that happens while creating it.

A tutorial answers: *"How do I write my first Flowthru pipeline?"*

Not: *"How do I build a production data pipeline?"* (too broad, too advanced)

**Characteristics:**
- Step-by-step, guaranteed to succeed
- Concrete examples, not abstract principles
- Welcoming tone — the user is learning
- Straightforward explanation — link to explanations instead of embedding theory

### Guides: Task-Oriented

**Questions answered:** "How do I accomplish this specific task?"

Guides help familiar users introduce specific features or fixes to their current work. The user already knows Flowthru basics; they have a specific goal and need practical direction. Guides are defined by user needs, not by what the API offers.

A guide answers: *"How do I use database tables for my catalog entries?"*

Not: *"How does the storage adapter work?"* (Too theoretical for a concrete guide)

**Characteristics:**
- Focused on a specific, achievable goal
- Assumes tutorial-level knowledge — no hand-holding
- Direct, efficient tone — the user is working
- Action-oriented — no theory or background

### Explanations: Understanding-Oriented

**Questions answered:** "Why?" / "How does this work conceptually?"

Explanations help users understand design decisions, architectural patterns, and trade-offs. It connects Flowthru's features to broader software engineering principles. Users read explanation when they're studying, not when they're working — they may be away from their code entirely.

Explanation answers: *"Why does Flowthru require so many schema types?"*

Not: *"How do I fix a type error?"* (that's a guide)

**Characteristics:**
- Explores context, trade-offs, alternatives
- Makes connections between concepts
- Analytical tone — the user is studying
- No action required — pure understanding

For contributors, explanation documentation should connect to concepts in the main `CONTRIBUTING.md`: the three error phases, type-system enforcement, the functional programming concepts, and the actual high-level architecture of Flowthru.

### Reference: Information-Oriented

**Questions answered:** "What exactly is this?" / "What are the technical details?"

Reference provides neutral, complete, authoritative technical descriptions. It's a map of the system — users consult it while working to verify details. Reference should mirror the structure of what it describes.

**Reference documentation is programmatically generated from code.** Manual reference contributions are not accepted — instead, improve XML documentation comments in the source code, and the reference docs will update automatically.

This ensures:
- Reference always matches the actual implementation
- No drift between code and documentation
- Contributors focus on tutorials, guides, and explanations where human authorship adds value

Reference answers: *"What are all of the storage strategies available for Data Catalog entries?"*

Not: *"How do I create a new data catalog entry?"* (that's a guide) or *"Why do data catalog entries need schema types?"* (that's explanation)

**Characteristics:**
- Factual, complete, structured
- Mirrors the architecture of the code
- Neutral, austere tone — no storytelling
- Pure description — no instruction or motivation
- **Generated from source code** — not manually authored

## Applying the Process

When you're ready to write or edit documentation:

1. **Identify the question** — Write it out explicitly. Be specific.
2. **Identify the audience state** — Are they learning, working, or studying? Are they end-users or contributors?
3. **Choose the category** — Match the question pattern to the category.
4. **Adopt the appropriate tone** — Welcoming for tutorials, direct for guides, analytical for explanations, neutral for reference.
5. **Link between categories** — Tutorial mentions schemas? Link to schema explanation. Guide uses an API? Link to reference.

### Example: Working Through a Topic

Let's say you want to document "catalog entries." Start by listing questions:

- "How do I create my first catalog entry?" → Tutorial
- "How do I configure a catalog entry for Parquet files?" → Guide
- "Why are catalog entries properties rather than string keys?" → Explanation
- "What methods are available on `IItem<T>`?" → Reference

Each question becomes a separate piece of documentation in its appropriate category. Together, they serve users at every stage of their journey with catalog entries.

### Example: Recognizing Mismatched Content

You're editing a guide titled "How to configure storage adapters" and notice it includes:

- A section explaining the three-layer composition pattern → Move to explanation
- Complete API signatures for every adapter method → Move to reference
- A step-by-step walkthrough of creating your first adapter → Move to tutorial

The guide should focus only on: *"I need to read from X source — what do I configure?"*

## Quality Standards

Regardless of category:

- **Technical accuracy** — Wrong documentation is worse than none
- **Cross-category linking** — Help users move between learning, working, and studying
- **Consistent terminology** — Use the same terms the code uses
- **Runnable examples** — Code samples must compile and execute
- **Synchronization** — Documentation drift violates Flowthru's fail-fast principle

---

The goal isn't perfect documentation. The goal is documentation that meets users where they are, answers the question they're asking, and helps them move forward. Start with the question, and the rest follows.
