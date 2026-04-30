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

def _render_one(project: str, project_node: pd.Series, sub: pd.DataFrame) -> bytes:
    """Render one library's icicle PNG.

    Args:
        project: The src package name (also the icicle root node id).
        project_node: The Project-level row for this library — supplies aggregated coverage
                      for the chart title.
        sub: File and Method rows for this library only.

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
                colorscale=[ 
                    [0.0,  "#0D0887"],
                    [0.2, "#B93289"],
                    [0.4,  "#F48849"],
                    [0.6,  "#F0F921"],
                    [0.79, "#F0F921"],
                    [0.8,  "#00DD00"],
                    [0.1,  "#00FF00"]
                ],
                cmin=0,
                cmax=100,
                colorbar=dict(
                    title="Coverage %",
                    ticksuffix="%",
                    tickvals=[0, 20, 40, 60, 80, 100],
                ),
            ),
            tiling=dict(orientation="h"),
        )
    )

    fig.update_layout(
        title=dict(
            text=(
                f"{project} — Coverage Icicle<br>"
                f"<sub>{project_node['CoveragePercent']:.1f}% "
                f"({project_node['CoveredLines']:,} / {project_node['TotalLines']:,} lines)</sub>"
            ),
            font=dict(size=18),
            y=0.97,
        ),
        margin=dict(l=20, r=20, t=90, b=20),
        width=1400,
        height=800,
    )

    return pio.to_image(fig, format="png", scale=2)


@step(inputs=["IcicleCoverageNode"], outputs="CoverageIcicles")
def generate_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one icicle PNG per src library.

    Args:
        nodes: All icicle nodes (Project, File, Method levels combined) produced by
               BuildIcicleCoverageStep.

    Returns:
        Dict mapping "{project}.png" → PNG bytes. The C# DirectoryStorageAdapter writes
        each entry to the configured icicles output directory.
    """
    projects_df = nodes[nodes["Level"] == "Project"]
    logger.info(f"[generate_coverage_icicle] rendering {len(projects_df)} library icicles")

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
            logger.warning(f"[generate_coverage_icicle] no file/method rows for {project}; skipping")
            continue

        out[f"{project}.png"] = _render_one(project, project_node, sub)

    return out
