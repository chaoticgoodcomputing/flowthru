---
title: Guides
description: Goal-oriented recipes for accomplishing a specific task with Flowthru — assumes you already know the basics from the tutorial.
review: draft
---

Guides are **task-oriented**: focused recipes for getting a specific job done. Unlike the tutorial, they assume you already know the basics and want the shortest path to a result. Each guide answers a single "how do I…?" question.

## What's here

- **[Constraining Catalog Entries](constraining-catalog-entries/)** — apply read-only, append-only, and similar constraints to catch policy violations at Flow construction rather than runtime.
- **[Customizing Schema Property Types](customizing-schema-property-types/)** — use `IScalar` to let custom types (newtypes, value objects, strong-typed IDs) appear as columns in a flat schema.
- **[Slicing Flows](slicing-pipelines/)** — run subsets of a Flow with `--from`, `--to`, and `--only` for testing and debugging.
- **[Using EFCore Catalog Entries](using-efcore-catalog-entries/)** — back catalog entries with a relational database instead of files.

### Advanced

- **[Deploying a Flow in a Container](advanced/container-deployment/)** — package and run a Flow in a container image.
- **[Custom Metadata Providers](advanced/metadata-providers/)** — emit your own run metadata alongside the built-in JSON and Mermaid providers.
- **[Using Flowthru as a Service Dependency](advanced/service-integration/)** — host Flowthru inside a longer-running application.

## Where to start

If you're new, work through the [Spaceflights tutorial](../tutorials/spaceflights/) first — the guides assume that grounding. For the reasoning behind these tasks, see the [Explanation](../explanation/) section.
