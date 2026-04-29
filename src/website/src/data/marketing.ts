// Single source of truth for the marketing homepage's content. Mirrors the
// data shapes from the original Notebook prototype (flow-data.jsx) so the
// Astro components remain visually faithful.

export const FLOWTHRU_VERSION = "v0.1.36";

export const FLOWTHRU_CODE = `public static Flow Create(Catalog catalog)
{
  return FlowBuilder.CreateFlow(pipeline =>
  {
    pipeline.AddStep(
      label: "PreprocessCompanies",
      transform: PreprocessCompaniesStep.Create(),
      input: catalog.Companies,
      output: catalog.PreprocessedCompanies
    );

    pipeline.AddStep(
      label: "PreprocessShuttles",
      transform: PreprocessShuttlesStep.Create(),
      input: catalog.Shuttles,
      output: catalog.PreprocessedShuttles
    );

    pipeline.AddStep(
      label: "CreateModelInputTable",
      transform: CreateModelInputTableStep.Create(),
      input: (
        catalog.PreprocessedShuttles,
        catalog.PreprocessedCompanies,
        catalog.Reviews
      ),
      output: catalog.ModelInputTable
    );
  });
}`;

export type CatalogKind = "external" | "intermediate" | "output";

export interface DagCatalog {
  id: string;
  label: string;
  x: number;
  y: number;
  kind: CatalogKind;
}

export interface DagStep {
  id: string;
  label: string;
  x: number;
  y: number;
}

export interface DagEdge {
  from: string;
  to: string;
}

export interface DagSubgraph {
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface FlowDagData {
  width: number;
  height: number;
  catalogs: DagCatalog[];
  steps: DagStep[];
  edges: DagEdge[];
  subgraph: DagSubgraph;
}

export const FLOW_DAG: FlowDagData = {
  width: 600,
  height: 560,
  catalogs: [
    { id: "shuttles", label: "Catalog.Shuttles", x: 90, y: 30, kind: "external" },
    { id: "companies", label: "Catalog.Companies", x: 290, y: 30, kind: "external" },
    { id: "preShuttles", label: "Catalog.PreprocessedShuttles", x: 90, y: 230, kind: "intermediate" },
    { id: "preCompanies", label: "Catalog.PreprocessedCompanies", x: 290, y: 230, kind: "intermediate" },
    { id: "reviews", label: "Catalog.Reviews", x: 490, y: 230, kind: "external" },
    { id: "modelInput", label: "Catalog.ModelInputTable", x: 290, y: 430, kind: "output" },
  ],
  steps: [
    { id: "preShuttlesStep", label: "PreprocessShuttles", x: 90, y: 130 },
    { id: "preCompaniesStep", label: "PreprocessCompanies", x: 290, y: 130 },
    { id: "buildModelInput", label: "CreateModelInputTable", x: 290, y: 330 },
  ],
  edges: [
    { from: "shuttles", to: "preShuttlesStep" },
    { from: "preShuttlesStep", to: "preShuttles" },
    { from: "companies", to: "preCompaniesStep" },
    { from: "preCompaniesStep", to: "preCompanies" },
    { from: "preShuttles", to: "buildModelInput" },
    { from: "preCompanies", to: "buildModelInput" },
    { from: "reviews", to: "buildModelInput" },
    { from: "buildModelInput", to: "modelInput" },
  ],
  subgraph: { label: "DataProcessing", x: 30, y: 95, width: 460, height: 365 },
};

export interface Extension {
  name: string;
  title: string;
  blurb: string;
  code: string;
  status: "stable" | "preview";
  href?: string;
}

export const EXTENSIONS: Extension[] = [
  {
    name: "Flowthru.EFCore",
    title: "EF Core",
    blurb: "Use any EF Core DbContext as a Catalog source. Tables become typed Catalog items.",
    code: "catalog.Use<EFCoreSource<AppDb>>();",
    status: "stable",
    href: "/docs/extensions/efcore/",
  },
  {
    name: "Flowthru.Spark",
    title: "Spark",
    blurb: "Run steps against a Spark context. DataFrames flow through the same typed Catalog.",
    code: 'step.WithSpark(spark => spark.Read.Parquet("..."));',
    status: "preview",
    href: "/docs/extensions/spark/",
  },
  {
    name: "Flowthru.Python",
    title: "Python",
    blurb: "Drop into Python for steps where pandas / sklearn / numpy is the right tool.",
    code: 'transform: PythonStep.From("steps/clean.py")',
    status: "preview",
    href: "/docs/extensions/python/",
  },
  {
    name: "Flowthru.Csv",
    title: "CSV / Parquet",
    blurb: "File-format adapters that participate in compile-time schema gating.",
    code: 'catalog.Companies.AsCsv("data/companies.csv");',
    status: "stable",
  },
  {
    name: "Flowthru.Mermaid",
    title: "Mermaid Docs",
    blurb: "Auto-generate DAG diagrams from any Flow. The same engine that powers our docs.",
    code: 'flow.ExportMermaid("docs/flow.md");',
    status: "stable",
  },
  {
    name: "Flowthru.Cli",
    title: "CLI Runner",
    blurb: "dotnet run --pipelines DataEngineering. Filter, dry-run, and inspect from the shell.",
    code: "dotnet run --pipelines DataScience --dry-run",
    status: "stable",
  },
];

export interface DocSection {
  key: "docs" | "examples" | "extensions";
  label: string;
  audience: string;
  blurb: string;
  cta: string;
  href: string;
}

export const DOC_SECTIONS: DocSection[] = [
  {
    key: "docs",
    label: "Docs",
    audience: "Core Flowthru",
    blurb: "Tutorials, how-to guides, explanations, and the API reference for the core framework.",
    cta: "Open the docs",
    href: "/docs/",
  },
  {
    key: "examples",
    label: "Examples",
    audience: "Real Flows, end to end",
    blurb: "Worked examples — Spaceflights, Iris, and more. Each ships with its own walkthrough.",
    cta: "Browse examples",
    href: "/docs/tutorials/spaceflights/",
  },
  {
    key: "extensions",
    label: "Extensions",
    audience: "EFCore · Spark · Python · …",
    blurb: "Each extension brings its own docs and a focused guide for adopting it in an existing pipeline.",
    cta: "Browse extensions",
    href: "#extensions",
  },
];

// Error-surface scenarios — each maps to a tab in the ScenarioTabs component.

export interface ScenarioLine {
  n: number;
  text: string;
  error?: boolean;
  warn?: boolean;
  squigglyOn?: string;
}

export interface Scenario {
  key: "schema" | "contract" | "preflight";
  label: string;
  phase: "Build-time" | "Pre-flight";
  filename: string;
  kind: "error" | "warn";
  code: string;
  phaseLabel: string;
  time: string;
  bodyHtml: string;
  hintHtml: string;
  outcome: string;
  lines: ScenarioLine[];
}

export const SCENARIOS: Scenario[] = [
  {
    key: "schema",
    label: "Schema rename",
    phase: "Build-time",
    filename: "DataProcessingFlow.cs — modified",
    kind: "error",
    code: "CS0117",
    phaseLabel: "Compile error",
    time: "Found in 0.2s.",
    bodyHtml: `<span class="tok-keyword">Catalog</span> does not contain a definition for <span class="tok-error">'PreprocessedCompany'</span>`,
    hintHtml: `Did you mean <span class="pill-suggestion">'PreprocessedCompanies'</span>?`,
    outcome: "Your Flow never started. Zero rows processed. Zero compute wasted.",
    lines: [
      { n: 1, text: "pipeline.AddStep(" },
      { n: 2, text: '  label: "CreateModelInputTable",' },
      { n: 3, text: "  transform: CreateModelInputTableStep.Create()," },
      { n: 4, text: "  input: (" },
      { n: 5, text: "    catalog.PreprocessedShuttles," },
      { n: 6, text: "    catalog.PreprocessedCompany,", error: true, squigglyOn: "PreprocessedCompany" },
      { n: 7, text: "    catalog.Reviews" },
      { n: 8, text: "  )," },
      { n: 9, text: "  output: catalog.ModelInputTable" },
      { n: 10, text: ");" },
    ],
  },
  {
    key: "contract",
    label: "Step contract violation",
    phase: "Build-time",
    filename: "PreprocessShuttlesStep.cs — modified",
    kind: "error",
    code: "CS0029",
    phaseLabel: "Compile error",
    time: "Found in 0.2s.",
    bodyHtml: `Cannot implicitly convert <span class="tok-error">'IEnumerable&lt;RawShuttle&gt;'</span> to <span class="tok-accent">'IEnumerable&lt;PreprocessedShuttle&gt;'</span>`,
    hintHtml: `The Step declared <span class="pill-suggestion">IStep&lt;RawShuttle, PreprocessedShuttle&gt;</span>. The compiler is holding it to the contract.`,
    outcome: "A Step that lies about its output is impossible to ship. The contract is enforced by the type system, every build, no exceptions.",
    lines: [
      { n: 1, text: "public class PreprocessShuttlesStep" },
      { n: 2, text: "  : IStep<RawShuttle, PreprocessedShuttle>" },
      { n: 3, text: "{" },
      { n: 4, text: "  public IEnumerable<PreprocessedShuttle> Run(" },
      { n: 5, text: "    IEnumerable<RawShuttle> input)" },
      { n: 6, text: "  {" },
      { n: 7, text: "    return input" },
      { n: 8, text: "      .Where(s => s.Active)" },
      { n: 9, text: "      .Select(s => new RawShuttle", error: true, squigglyOn: "RawShuttle" },
      { n: 10, text: "      {" },
      { n: 11, text: "        Id = s.Id," },
      { n: 12, text: "        Name = s.Name.Trim()" },
      { n: 13, text: "      });" },
      { n: 14, text: "  }" },
      { n: 15, text: "}" },
    ],
  },
  {
    key: "preflight",
    label: "Database unreachable",
    phase: "Pre-flight",
    filename: "$ dotnet run --pipelines DataProcessing --dry-run",
    kind: "warn",
    code: "PRE-FLIGHT",
    phaseLabel: "Pre-flight check failed",
    time: "Aborted in 1.8s.",
    bodyHtml: `Catalog destination <span class="tok-warn">'postgres://prod/analytics'</span> is unreachable.`,
    hintHtml: `Even though this Catalog is only written at the <em>end</em> of the Flow, every external connection is verified <span class="pill-suggestion">before Step #1 runs</span>.`,
    outcome: "No partial writes. No 90-minute job that fails on the last save. Fix the connection string and re-run.",
    lines: [
      { n: 1, text: "[14:02:11] flowthru: pre-flight checks" },
      { n: 2, text: "[14:02:11] ✓ DAG validated · 0 cycles · 0 duplicate producers" },
      { n: 3, text: "[14:02:11] ✓ catalog.Companies     csv://data/companies.csv" },
      { n: 4, text: "[14:02:11] ✓ catalog.Shuttles      csv://data/shuttles.csv" },
      { n: 5, text: "[14:02:12] ✓ catalog.Reviews       csv://data/reviews.csv" },
      { n: 6, text: "[14:02:13] ✗ catalog.ModelInputTable  postgres://prod/analytics", warn: true, squigglyOn: "postgres://prod/analytics" },
      { n: 7, text: "[14:02:13]   └─ connection refused (after 2 retries)" },
      { n: 8, text: "[14:02:13] aborted. 0 Steps executed." },
    ],
  },
];

// Anatomy section's three callout cards.
export const ANATOMY_CALLOUTS = [
  {
    title: "The compiler is the source of truth",
    body: "Schemas, wiring, and Flows are real C# values. The build tells you the structure of your pipeline before you ever run it.",
  },
  {
    title: "A team can split the work",
    body: "Data engineers own Catalog entries. Data scientists write Steps. Analysts wire Steps and Catalogs into reusable Flows. Everyone meets at the type system.",
  },
  {
    title: "Reusable, not rewritten",
    body: "Steps and Flows compose. The exploratory Flow that an analyst built becomes the production Flow an engineer ships — same code, same contracts.",
  },
];

// Error-surface phase cards (the row above the scenario tabs).
export const PHASE_CARDS = [
  {
    tag: "Build-time",
    tone: "ok" as const,
    title: "Caught by the compiler",
    weight: "The gold standard",
    body: "Schema mismatches, missing inputs, broken Step contracts, wrong serializers. Squigglies in your IDE before you save.",
    shareLabel: "Most errors land here",
  },
  {
    tag: "Pre-flight",
    tone: "warn" as const,
    title: "Caught before the first Step runs",
    weight: "Tolerable",
    body: "DAG validation, duplicate producers, missing files, unreachable databases, header drift. Caught with zero side effects.",
  },
  {
    tag: "Runtime",
    tone: "err" as const,
    title: "Network drops & true surprises",
    weight: "Rare, and recoverable",
    body: "The unpredictable few — transient outages, OOM. Captured in structured Flow results so the job halts cleanly and tells you exactly which Step failed and why.",
  },
];

// Footer columns.
export const FOOTER_COLUMNS = [
  {
    h: "Docs",
    items: [
      { label: "Tutorials", href: "/docs/tutorials/spaceflights/" },
      { label: "Guides", href: "/docs/guides/slicing-pipelines/" },
      { label: "Explanation", href: "/docs/explanation/anatomy-of-a-flow/" },
      { label: "Reference", href: "/docs/reference/" },
    ],
  },
  {
    h: "Project",
    items: [
      { label: "GitHub", href: "https://github.com/chaoticgoodcomputing/flowthru" },
      { label: "Releases", href: "https://github.com/chaoticgoodcomputing/flowthru/releases" },
      { label: "Roadmap", href: "https://github.com/chaoticgoodcomputing/flowthru/issues" },
      { label: "Changelog", href: "https://github.com/chaoticgoodcomputing/flowthru/blob/main/CHANGELOG.md" },
    ],
  },
  {
    h: "Community",
    items: [
      { label: "Discussions", href: "https://github.com/chaoticgoodcomputing/flowthru/discussions" },
      { label: "Contributing", href: "https://github.com/chaoticgoodcomputing/flowthru/blob/main/CONTRIBUTING.md" },
      { label: "Code of Conduct", href: "https://github.com/chaoticgoodcomputing/flowthru/blob/main/CODE_OF_CONDUCT.md" },
      { label: "License", href: "https://github.com/chaoticgoodcomputing/flowthru/blob/main/LICENSE" },
    ],
  },
];
