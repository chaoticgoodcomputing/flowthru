"""Coverage heatmap generation step.

Reads the section- and subgroup-annotated PivotCoverageRow data produced by the C#
ClassifyCoverage step and generates a static Plotly PNG heatmap:
  - X axis: test/example projects, ordered Library Tests (Core → Extensions → Misc)
            | Integration Tests | Examples
  - Y axis: src packages (library packages only), ordered Core → Extensions → Misc
            alphabetically within each group; subgroup derived from SrcSubgroup column
            (manifest-authoritative, set by C#).
  - Colour: CoveragePercent

Section dividers (thick) and subgroup dividers (thin, library only) are drawn on
both axes.
"""

import logging

import pandas as pd
import plotly.graph_objects as go
import plotly.io as pio
from flowthru import step

logger = logging.getLogger(__name__)

_SECTION_ORDER = {"Library Tests": 0, "Integration Tests": 1, "Examples": 2}
_SUBGROUP_ORDER = {"Core": 0, "Extensions": 1, "Misc": 2}


@step(inputs=["PivotCoverageRow"], outputs="CoverageHeatmap")
def generate_coverage_heatmap(pivot_coverage: pd.DataFrame) -> bytes:
    """Static PNG heatmap of test coverage by (TestProject, SrcPackage).

    Column (X axis) order is pre-sorted by C# ClassifyCoverageStep.
    Row (Y axis) order is sorted by SrcSubgroup (manifest-authoritative) then name.

    Args:
        pivot_coverage: Rows with columns Section, Subgroup, SrcSubgroup, TestProject,
                        SrcPackage, CoveragePercent — produced by ClassifyCoverageStep.

    Returns:
        PNG image as bytes.
    """
    logger.info(
        f"[generate_coverage_heatmap] {len(pivot_coverage)} rows, "
        f"{pivot_coverage['TestProject'].nunique()} test projects, "
        f"{pivot_coverage['SrcPackage'].nunique()} src packages"
    )

    # Split real rows from ghost anchors emitted by C# for missing packages/tests
    real = pivot_coverage[~pivot_coverage["IsGhost"]]
    ghosts = pivot_coverage[pivot_coverage["IsGhost"]]

    # X axis: column order comes directly from C# sort (stable unique preserves it).
    # Ghost anchors carry the correct TestProject names, so they're already in order.
    ordered_columns = list(dict.fromkeys(pivot_coverage["TestProject"]))

    # Lookup maps for X divider logic (ghosts contribute valid Section/Subgroup)
    section_of = dict(zip(pivot_coverage["TestProject"], pivot_coverage["Section"]))
    subgroup_of = dict(zip(pivot_coverage["TestProject"], pivot_coverage["Subgroup"]))

    # Y axis: sort src packages by SrcSubgroup (manifest-authoritative) then alphabetically
    src_subgroup_of = dict(
        zip(pivot_coverage["SrcPackage"], pivot_coverage["SrcSubgroup"])
    )
    all_src_pkgs = sorted(pivot_coverage["SrcPackage"].unique())
    ordered_rows = sorted(
        all_src_pkgs,
        key=lambda p: (_SUBGROUP_ORDER.get(src_subgroup_of.get(p, ""), 9), p),
    )

    # Ghost sets: packages/tests present ONLY in ghost rows (no real Cobertura data)
    real_src_set = set(real["SrcPackage"].unique())
    real_test_set = set(real["TestProject"].unique())
    ghost_src_set = set(ghosts["SrcPackage"].unique()) - real_src_set
    ghost_test_set = set(ghosts["TestProject"].unique()) - real_test_set

    # Build pivot from real data only; ghost rows/columns filled in below
    pivot = (
        real.pivot_table(
            index="SrcPackage",
            columns="TestProject",
            values="CoveragePercent",
            aggfunc="max",
        )
        .fillna(0.0)
        .reindex(index=ordered_rows, columns=ordered_columns, fill_value=0.0)
    )

    # Stamp entire ghost rows and columns with -1 (rendered gray)
    for src in ghost_src_set:
        if src in pivot.index:
            pivot.loc[src] = -1.0
    for test in ghost_test_set:
        if test in pivot.columns:
            pivot[test] = -1.0

    # Build hover text matrix
    hover = []
    for src_pkg in pivot.index:
        row_hover = []
        for test_proj in pivot.columns:
            pct = pivot.loc[src_pkg, test_proj]
            if src_pkg in ghost_src_set:
                row_hover.append(
                    f"<b>{src_pkg}</b><br>" f"<i>Not found in any coverage report</i>"
                )
            elif test_proj in ghost_test_set:
                row_hover.append(
                    f"<b>{test_proj}</b><br>"
                    f"<i>Test project produced no coverage data</i>"
                )
            elif pct > 0:
                row_hover.append(
                    f"<b>{src_pkg}</b><br>"
                    f"Test: {test_proj}<br>"
                    f"Coverage: {pct:.1f}%"
                )
            else:
                row_hover.append(
                    f"<b>{src_pkg}</b><br>Test: {test_proj}<br>No coverage"
                )
        hover.append(row_hover)

    # Colorscale: -1 = gray (ghost), 0 = white (no coverage), 0–100 = red→green
    # With zmin=-1 and zmax=100 the normalized position of value v is (v+1)/101.
    _t = lambda v: (v + 1) / 101
    fig = go.Figure(
        data=go.Heatmap(
            z=pivot.values.tolist(),
            x=list(pivot.columns),
            y=list(pivot.index),
            text=hover,
            hovertemplate="%{text}<extra></extra>",
            colorscale=[
                # Ghost rows/columns at -1: Gray
                [0.0, "#BBBBBB"],
                [_t(0) - 1e-4, "#BBBBBB"],
                # Real data around 0% coverage: White
                [_t(0), "#F8F8F8"],
                [_t(0) + 1e-4, "#F8F8F8"],
                # Gradient 1: Plasma
                [_t(0) + 2 * (1e-4), "#0D0887"],
                [_t(20) - 1e-4, "#0D0887"],
                [_t(20) + 1e-4, "#B93289"],
                [_t(40) - 1e-4, "#B93289"],
                [_t(40) + 1e-4, "#F48849"],
                [_t(60) - 1e-4, "#F48849"],
                [_t(60) + 1e-4, "#F0F921"],
                [_t(80) - 1e-4, "#F0F921"],
                # Gradient 2: Green
                [_t(80), "#00DD00"],
                [_t(100), "#00FF00"],
            ],
            zmin=-1,
            zmax=100,
            colorbar=dict(
                title="Coverage %", ticksuffix="%", tickvals=[0, 20, 40, 60, 80, 100]
            ),
        )
    )

    n_cols = len(pivot.columns)
    n_rows = len(pivot.index)
    cell_px = 55
    margin = dict(l=340, r=140, t=140, b=280)
    width = n_cols * cell_px + margin["l"] + margin["r"]
    height = n_rows * cell_px + margin["t"] + margin["b"]

    shapes = []
    annotations = []

    # ── X axis: section dividers + section labels + subgroup dividers + labels ──

    current_section = None
    section_start = 0
    current_subgroup = None
    subgroup_start = 0

    def _flush_section(section, start, end):
        mid = (start + end - 1) / 2
        annotations.append(
            dict(
                x=mid,
                y=1.10,
                xref="x",
                yref="paper",
                text=f"<b>{section}</b>",
                showarrow=False,
                font=dict(size=13),
                xanchor="center",
            )
        )
        if start > 0:
            shapes.append(
                dict(
                    type="line",
                    x0=start - 0.5,
                    x1=start - 0.5,
                    y0=-0.5,
                    y1=n_rows - 0.5,
                    xref="x",
                    yref="y",
                    line=dict(color="#222222", width=2.5),
                )
            )

    def _flush_subgroup(subgroup, start, end):
        mid = (start + end - 1) / 2
        annotations.append(
            dict(
                x=mid,
                y=1.05,
                xref="x",
                yref="paper",
                text=f"<i>{subgroup}</i>",
                showarrow=False,
                font=dict(size=11, color="#555555"),
                xanchor="center",
            )
        )
        if start > 0 and start != subgroup_boundaries_set.get("section_start"):
            shapes.append(
                dict(
                    type="line",
                    x0=start - 0.5,
                    x1=start - 0.5,
                    y0=-0.5,
                    y1=n_rows - 0.5,
                    xref="x",
                    yref="y",
                    line=dict(color="#999999", width=1, dash="dot"),
                )
            )

    subgroup_boundaries_set = {}

    for i, col in enumerate(pivot.columns):
        sec = section_of.get(col, "Examples")
        sub = subgroup_of.get(col, "") if sec == "Library Tests" else ""

        if sec != current_section:
            if current_section is not None:
                _flush_section(current_section, section_start, i)
                if current_subgroup is not None:
                    _flush_subgroup(current_subgroup, subgroup_start, i)
            current_section = sec
            section_start = i
            subgroup_boundaries_set["section_start"] = i
            current_subgroup = sub
            subgroup_start = i
        elif sub != current_subgroup and sec == "Library Tests":
            _flush_subgroup(current_subgroup, subgroup_start, i)
            current_subgroup = sub
            subgroup_start = i

    if current_section is not None:
        _flush_section(current_section, section_start, n_cols)
        if current_subgroup is not None and current_section == "Library Tests":
            _flush_subgroup(current_subgroup, subgroup_start, n_cols)

    # ── Y axis: subgroup dividers ──────────────────────────────────────────────

    current_y_subgroup = None

    for j, pkg in enumerate(pivot.index):
        sg = src_subgroup_of.get(pkg, "")
        if sg != current_y_subgroup:
            if current_y_subgroup is not None:
                shapes.append(
                    dict(
                        type="line",
                        x0=-0.5,
                        x1=n_cols - 0.5,
                        y0=j - 0.5,
                        y1=j - 0.5,
                        xref="x",
                        yref="y",
                        line=dict(color="#999999", width=1, dash="dot"),
                    )
                )
            current_y_subgroup = sg

    fig.update_layout(
        title=dict(text="Flowthru Coverage Heatmap", font=dict(size=20), y=0.99),
        xaxis=dict(title="Test / Example Project", tickangle=-60, automargin=False),
        yaxis=dict(title="Source Package", automargin=False, dtick=1),
        shapes=shapes,
        annotations=annotations,
        margin=margin,
        height=height,
        width=width,
    )

    return pio.to_image(fig, format="png", scale=2)
