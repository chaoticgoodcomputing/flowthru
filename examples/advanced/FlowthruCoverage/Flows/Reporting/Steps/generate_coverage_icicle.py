"""Per-library coverage icicle chart generation step.

Reads the flat IcicleCoverageNode rows produced by BuildIcicleCoverageStep, splits them by
src library, and renders one Plotly icicle SVG per project. Output is a Directory<byte[]>:
a dict keyed by "{project}.svg" whose values are SVG bytes. The DirectoryStorageAdapter on
the C# side writes each entry to its file under the icicles output directory.

Hierarchy mirrors the icicle node levels: Project (root) → Directory(/sub-dirs) → File →
Method. Each cell shows the node label and its coverage percentage.

SVG was chosen over PNG so reviewers can zoom into method-level slices that would otherwise
be unreadable at any reasonable raster resolution.

Plotly icicle docs: https://plotly.com/python/icicle-charts/
"""

import logging

import pandas as pd
import plotly.graph_objects as go
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)

_COLORSCALE = [
    [0.0, "#0D0887"],
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
    """Render one library's icicle SVG.

    Args:
        project: The src package name (also the icicle root node id).
        project_node: The Project-level row for this library — supplies aggregated coverage
                      for the chart title.
        sub: All rows under this project — Project, Directory, File, and Method levels.
        title_qualifier: Suffix appended to the project name in the chart title — used to
                         distinguish between variants (default vs example-only).

    Returns:
        SVG image bytes.
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

    # texttemplate is what's drawn on each cell; we surface label + coverage% so every
    # node — Project, Directory, File, Method — self-describes without relying on hover.
    customdata = [[pct] for pct in sub["CoveragePercent"]]

    fig = go.Figure(
        go.Icicle(
            ids=sub["Id"].tolist(),
            labels=sub["Label"].tolist(),
            parents=sub["ParentId"].tolist(),
            values=sub["TotalLines"].tolist(),
            branchvalues="total",
            text=hover,
            hovertemplate="%{text}<extra></extra>",
            customdata=customdata,
            texttemplate="<b>%{label}</b><br>%{customdata[0]:.1f}%",
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

    return pio.to_image(fig, format="svg")


def _render_directory(
    nodes: pd.DataFrame, log_prefix: str, title_qualifier: str
) -> dict[str, bytes]:
    """Shared driver: split the flat node table by project root and render one SVG per
    library. Used by every ``@step`` entry point in this module — variants only differ in
    their input filtering (upstream) and chart title qualifier.

    The per-project subset includes the Project node itself (so its bar is drawn with a
    coverage label), every Directory under it, every File, and every Method.
    """
    projects_df = nodes[nodes["Level"] == "Project"]
    logger.info(f"[{log_prefix}] rendering {len(projects_df)} library icicles")

    out: dict[str, bytes] = {}
    for _, project_node in projects_df.iterrows():
        project = project_node["Id"]

        # All nodes belonging to this project: the project root itself, plus anything whose
        # Id starts with "{project}::" (Directory, File, Method).
        sub = nodes[
            (nodes["Id"] == project)
            | (nodes["Id"].str.startswith(project + "::"))
        ]
        if len(sub) <= 1:
            # Just the Project row with no descendants — the library produced no covered
            # methods under this filter (e.g. example-only variant for an unused library).
            logger.warning(f"[{log_prefix}] no descendants for {project}; skipping")
            continue

        out[f"{project}.svg"] = _render_one(
            project, project_node, sub, title_qualifier=title_qualifier
        )

    return out


@step(inputs=["IcicleCoverageNode"], outputs="CoverageIcicles")
def generate_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one icicle SVG per src library across all test runs.

    Args:
        nodes: All icicle nodes (Project, Directory, File, Method) produced by
               BuildIcicleCoverageStep.

    Returns:
        Dict mapping "{project}.svg" → SVG bytes. The C# DirectoryStorageAdapter writes
        each entry to the configured icicles output directory.
    """
    return _render_directory(
        nodes,
        log_prefix="generate_coverage_icicle",
        title_qualifier="Coverage Icicle",
    )


@step(inputs=["IcicleCoverageNode"], outputs="ExampleCoverageIcicles")
def generate_example_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one icicle SVG per src library, restricted to coverage attributed to manifest
    Example projects. The upstream filter step trims line coverage to Example test runs
    before BuildIcicleCoverageStep aggregates; this step is identical to
    ``generate_coverage_icicle`` beyond a chart title qualifier so the rendered SVG
    self-identifies as the example-only variant.
    """
    return _render_directory(
        nodes,
        log_prefix="generate_example_coverage_icicle",
        title_qualifier="Example Coverage Icicle",
    )
