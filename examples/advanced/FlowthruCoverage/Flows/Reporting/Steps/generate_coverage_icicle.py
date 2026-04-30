"""Per-library coverage icicle chart generation step.

Reads the flat IcicleCoverageNode rows produced by BuildIcicleCoverageStep, splits them by
src library, and renders one Plotly icicle PNG per project. Output is a Directory<byte[]>:
a dict keyed by relative file path (e.g. "Flowthru.Core.png") whose values are PNG bytes.
The DirectoryStorageAdapter on the C# side writes each entry to its file under the icicles
output directory.

For a single-library icicle, only File and Method nodes are kept (the Project node would
just be the root cell with no comparative information). The library's overall coverage
appears in the chart title.

Plotly icicle docs: https://plotly.com/python/icicle-charts/
"""

import logging

import pandas as pd
import plotly.graph_objects as go
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)

_COLORSCALE = [
    [0.0, "#F8F8F8"],
    [0.0 + 1e-4, "#F8F8F8"],
    [0.0 + 2 * (1e-4), "#0D0887"],
    [0.2 - 1e-4, "#0D0887"],
    [0.2 + 1e-4, "#B93289"],
    [0.4 - 1e-4, "#B93289"],
    [0.4 + 1e-4, "#F48849"],
    [0.6 - 1e-4, "#F48849"],
    [0.6 + 1e-4, "#F0F921"],
    [0.8 - 1e-4, "#F0F921"],
    [0.8, "#00DD00"],
    [1.0, "#00FF00"],
]


def _render_one(
    project: str,
    project_node: pd.Series,
    sub: pd.DataFrame,
    title_qualifier: str = "Coverage Icicle",
) -> bytes:
    """Render one library's icicle PNG.

    Args:
        project: The src package name (also the icicle root node id).
        project_node: The Project-level row for this library — supplies aggregated coverage
                      for the chart title.
        sub: File and Method rows for this library only.
        title_qualifier: Suffix appended to the project name in the chart title — used to
                         distinguish between variants (default vs example-only).

    Returns:
        PNG image bytes.
    """
    hover = [
        f"<b>{label}</b><br>"
        f"Level: {level}<br>"
        f"Coverage: {pct:.1f}%<br>"
        f"Lines: {covered:,} / {total:,}"
        for label, level, pct, covered, total in zip(
            sub["Label"],
            sub["Level"],
            sub["CoveragePercent"],
            sub["CoveredLines"],
            sub["TotalLines"],
        )
    ]

    fig = go.Figure(
        go.Icicle(
            ids=sub["Id"].tolist(),
            labels=sub["Label"].tolist(),
            parents=sub["ParentId"].tolist(),
            values=sub["TotalLines"].tolist(),
            branchvalues="total",
            text=hover,
            hovertemplate="%{text}<extra></extra>",
            marker=dict(
                colors=sub["CoveragePercent"].tolist(),
                colorscale=_COLORSCALE,
                cmin=0,
                cmax=100,
                colorbar=dict(
                    title="Coverage %",
                    ticksuffix="%",
                    tickvals=[0, 25, 50, 75, 100],
                ),
            ),
            tiling=dict(orientation="h"),
        )
    )

    fig.update_layout(
        title=dict(
            text=(
                f"{project} — {title_qualifier}<br>"
                f"<sub>{project_node['CoveragePercent']:.1f}% "
                f"({project_node['CoveredLines']:,} / {project_node['TotalLines']:,} lines)</sub>"
            ),
            font=dict(size=18),
            y=0.97,
        ),
        margin=dict(l=20, r=20, t=90, b=20),
        width=3840,
        height=2160,
    )

    return pio.to_image(fig, format="png", scale=2)


def _render_directory(
    nodes: pd.DataFrame, log_prefix: str, title_qualifier: str
) -> dict[str, bytes]:
    """Shared driver: split the flat node table by project root and render one PNG per
    library. Used by every <c>@step</c> entry point in this module — variants only differ
    in their input filtering (upstream) and chart title qualifier."""
    projects_df = nodes[nodes["Level"] == "Project"]
    logger.info(f"[{log_prefix}] rendering {len(projects_df)} library icicles")

    out: dict[str, bytes] = {}
    for _, project_node in projects_df.iterrows():
        project = project_node["Id"]
        sub = nodes[
            (nodes["Level"] != "Project")
            & (
                (nodes["Id"].str.startswith(project + "::"))
                | (nodes["ParentId"].str.startswith(project + "::"))
            )
        ]
        if sub.empty:
            logger.warning(f"[{log_prefix}] no file/method rows for {project}; skipping")
            continue

        out[f"{project}.png"] = _render_one(
            project, project_node, sub, title_qualifier=title_qualifier
        )

    return out


@step(inputs=["IcicleCoverageNode"], outputs="CoverageIcicles")
def generate_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one icicle PNG per src library across all test runs.

    Args:
        nodes: All icicle nodes (Project, File, Method levels combined) produced by
               BuildIcicleCoverageStep.

    Returns:
        Dict mapping "{project}.png" → PNG bytes. The C# DirectoryStorageAdapter writes
        each entry to the configured icicles output directory.
    """
    return _render_directory(
        nodes,
        log_prefix="generate_coverage_icicle",
        title_qualifier="Coverage Icicle",
    )


@step(inputs=["IcicleCoverageNode"], outputs="ExampleCoverageIcicles")
def generate_example_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one icicle PNG per src library, restricted to coverage attributed to manifest
    Example projects. The upstream filter step trims line coverage to Example test runs
    before BuildIcicleCoverageStep aggregates; this step is identical to
    <c>generate_coverage_icicle</c> beyond a chart title qualifier so the rendered PNG
    self-identifies as the example-only variant.
    """
    return _render_directory(
        nodes,
        log_prefix="generate_example_coverage_icicle",
        title_qualifier="Example Coverage Icicle",
    )
