"""Coverage heatmap generation step.

Reads the section-annotated PivotCoverageRow data produced by the C# ClassifyCoverage
step and generates a static Plotly PNG heatmap: test/example projects on the X axis
(ordered by Section then name), src packages on the Y axis, colour-encoded by
CoveragePercent.
"""

import logging

import pandas as pd
import plotly.graph_objects as go
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)

_SECTION_ORDER = {"Library Tests": 0, "Integration Tests": 1, "Examples": 2}


@step(inputs=["PivotCoverageRow"], outputs="CoverageHeatmap")
def generate_coverage_heatmap(pivot_coverage: pd.DataFrame) -> bytes:
    """Static PNG heatmap of test coverage by (TestProject, SrcPackage).

    Column (X axis) order is determined by the Section field pre-computed in C#:
    Library Tests | Integration Tests | Examples, alphabetically within each.

    Args:
        pivot_coverage: Rows with columns Section, TestProject, SrcPackage,
                        CoveragePercent — produced by ClassifyCoverageStep.

    Returns:
        PNG image as bytes.
    """
    logger.info(
        f"[generate_coverage_heatmap] {len(pivot_coverage)} rows, "
        f"{pivot_coverage['TestProject'].nunique()} test projects, "
        f"{pivot_coverage['SrcPackage'].nunique()} src packages"
    )

    # Derive ordered column list directly from the Section values in the data.
    # The C# step already sorts by section order then name, so a stable unique
    # preserves that order without re-sorting here.
    ordered_columns = list(dict.fromkeys(pivot_coverage["TestProject"]))

    # Section membership for dividers/annotations
    section_of = dict(zip(pivot_coverage["TestProject"], pivot_coverage["Section"]))

    pivot = (
        pivot_coverage
        .pivot_table(
            index="SrcPackage",
            columns="TestProject",
            values="CoveragePercent",
            aggfunc="max",
        )
        .fillna(0.0)
        .sort_index(axis=0)
        .reindex(columns=ordered_columns, fill_value=0.0)
    )

    # Build hover text matrix
    hover = []
    lookup = pivot_coverage.set_index(["SrcPackage", "TestProject"])["CoveragePercent"]
    for src_pkg in pivot.index:
        row_hover = []
        for test_proj in pivot.columns:
            pct = pivot.loc[src_pkg, test_proj]
            if pct > 0:
                row_hover.append(
                    f"<b>{src_pkg}</b><br>"
                    f"Test: {test_proj}<br>"
                    f"Coverage: {pct:.1f}%"
                )
            else:
                row_hover.append(f"<b>{src_pkg}</b><br>Test: {test_proj}<br>No coverage")
        hover.append(row_hover)

    fig = go.Figure(
        data=go.Heatmap(
            z=pivot.values.tolist(),
            x=list(pivot.columns),
            y=list(pivot.index),
            text=hover,
            hovertemplate="%{text}<extra></extra>",
            colorscale=[
                [0.0, "#f8f8f8"],
                [0.001, "#fee0d2"],
                [0.25, "#fc9272"],
                [0.50, "#fb6a4a"],
                [0.75, "#de2d26"],
                [1.0, "#1a9641"],
            ],
            zmin=0,
            zmax=100,
            colorbar=dict(title="Coverage %", ticksuffix="%"),
        )
    )

    cell_px = 55
    n_cols = len(pivot.columns)
    n_rows = len(pivot.index)
    margin = dict(l=340, r=140, t=120, b=280)
    width = n_cols * cell_px + margin["l"] + margin["r"]
    height = n_rows * cell_px + margin["t"] + margin["b"]

    # Section dividers and header annotations
    shapes = []
    annotations = []
    current_section = None
    section_start = 0

    def _flush_section(section, start, end):
        mid = (start + end - 1) / 2
        annotations.append(dict(
            x=mid, y=1.06, xref="x", yref="paper",
            text=f"<b>{section}</b>",
            showarrow=False, font=dict(size=13), xanchor="center",
        ))
        if start > 0:
            shapes.append(dict(
                type="line",
                x0=start - 0.5, x1=start - 0.5,
                y0=-0.5, y1=n_rows - 0.5,
                xref="x", yref="y",
                line=dict(color="#333333", width=2),
            ))

    for i, col in enumerate(pivot.columns):
        sec = section_of.get(col, "Examples")
        if sec != current_section:
            if current_section is not None:
                _flush_section(current_section, section_start, i)
            current_section = sec
            section_start = i
    if current_section is not None:
        _flush_section(current_section, section_start, n_cols)

    fig.update_layout(
        title=dict(text="Flowthru Coverage Heatmap", font=dict(size=20), y=0.98),
        xaxis=dict(title="Test / Example Project", tickangle=-60, automargin=False),
        yaxis=dict(title="Source Package", automargin=False, dtick=1),
        shapes=shapes,
        annotations=annotations,
        margin=margin,
        height=height,
        width=width,
    )

    return pio.to_image(fig, format="png", scale=2)
