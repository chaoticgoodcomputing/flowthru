"""Per-library provenance coverage icicle chart generation step.

Reads the flat ProvenanceIcicleNode rows produced by BuildProvenanceIcicleStep,
splits them by src library, and renders one Plotly icicle SVG per project — each
tile coloured by an RGB encoding that surfaces unit, integration, and combined
coverage simultaneously.

Hierarchy mirrors the icicle node levels: Project (root) → Directory(/sub-dirs) →
File → Method. Each cell shows the node label and a three-line breakdown of the
provenance ratios.

Colour encoding (per tile, ratios in [0,1] of TotalLines):

    R = 1 − ease(IntegrationCovered / TotalLines)
    G =     ease(AnyCovered         / TotalLines)
    B = 1 − ease(UnitCovered        / TotalLines)

``ease`` is piecewise-linear: the first 80% of coverage compresses into the
first 50% of the channel range, and the last 20% expands into the last 50%.
This keeps the saturated green corner reserved for coverage ≥ 80% on all axes,
so partial coverage reads as visibly under-target rather than trending green.

Corner colours fall out as:

    green   (0, 1, 0)  — covered by BOTH unit and integration  (robust)
    yellow  (1, 1, 0)  — unit-only                              (no integration hits it)
    cyan    (0, 1, 1)  — integration-only                       (no unit hits it)
    white   (1, 1, 1)  — peer-only                              (any hits, but not unit or integration)
    magenta (1, 0, 1)  — uncovered                              (no hits at all)

A dark plot background plus thin tile borders keep the white "peer-only" case
distinguishable from the canvas.

SVG is chosen over PNG so reviewers can zoom into method-level slices that would
otherwise be unreadable at any reasonable raster resolution.

Plotly icicle docs: https://plotly.com/python/icicle-charts/
"""

import base64
import io
import logging

import numpy as np
import pandas as pd
import plotly.graph_objects as go
import plotly.io as pio
from PIL import Image, ImageDraw, ImageFont
from flowthru import step

logger = logging.getLogger(__name__)


_LEGEND_URI: str | None = None


def _ease(x):
    """Compress 0..0.8 into 0..0.5 and expand 0.8..1.0 into 0.5..1.0.

    Reserves the saturated green corner for coverage ≥ 80% on all axes —
    below that, channels stay closer to their uncovered extreme so partial
    coverage doesn't visually approximate the "robust" target state.

    Accepts scalars or numpy arrays.
    """
    return np.where(x <= 0.8, x * 0.625, 0.5 + (x - 0.8) * 2.5)


def _load_font(size: int) -> ImageFont.ImageFont:
    """Best-effort sans-serif font lookup, with Pillow's bundled fallback."""
    for path in (
        "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf",
    ):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default(size=size)


def _build_legend_image() -> Image.Image:
    """Build the complete coverage-provenance legend as a single PIL image.

    Bakes backdrop + spectrum + axis labels into one PNG so the spectrum
    renders at full brightness — there's no Plotly shape rendering on top
    of the image and washing the colours out.

    The encoding projects the three-channel provenance space onto a 2D plane
    where the X axis is Integration% (0 at left → 100 at right) and the Y axis
    is Unit% (0 at bottom → 100 at top), using ``G ≅ 1 − min(R, B)``
    (equivalently ``Any% ≅ max(Unit%, Integration%)``). The approximation
    collapses rare peer-only coverage (true white) into the magenta corner
    so the four "normal" corners stay honest with origin at no-coverage:

        bottom-left  (Int=0, Unit=0)    → magenta  uncovered
        bottom-right (Int=100, Unit=0)  → cyan     integration only
        top-left     (Int=0, Unit=100)  → yellow   unit only
        top-right    (Int=100, Unit=100)→ green    unit + integration  (robust)
    """
    # High-res so the baked image stays crisp when scaled to the chart canvas.
    W, H = 560, 560
    PAD = 28
    img = Image.new("RGB", (W, H), (26, 26, 26))
    draw = ImageDraw.Draw(img)

    title_font = _load_font(28)
    label_font = _load_font(22)
    tick_font = _load_font(18)

    draw.text(
        (W // 2, PAD + 6),
        "Coverage provenance",
        fill=(224, 224, 224),
        font=title_font,
        anchor="mm",
    )

    # Square spectrum centred horizontally, with padding for axes.
    spec_size = 380
    spec_x0 = (W - spec_size) // 2 + 30
    spec_y0 = 90
    spec_x1 = spec_x0 + spec_size
    spec_y1 = spec_y0 + spec_size

    n = 256
    # Origin at bottom-left = (Integration=0, Unit=0) = no coverage = magenta.
    # X: Integration% increases left → right. Y (PIL row 0 = top): Unit%
    # decreases top → bottom. Apply the same easing as the per-tile encoding
    # so the legend's green corner aligns with the ≥80% threshold.
    int_pct = np.tile(np.linspace(0.0, 1.0, n), (n, 1))
    unit_pct = np.tile(np.linspace(1.0, 0.0, n).reshape(-1, 1), (1, n))
    r = 1.0 - _ease(int_pct)
    b = 1.0 - _ease(unit_pct)
    g = 1.0 - np.minimum(r, b)
    rgb = (np.stack([r, g, b], axis=-1) * 255.0).astype(np.uint8)
    spec = Image.fromarray(rgb, mode="RGB").resize(
        (spec_size, spec_size), Image.Resampling.BILINEAR
    )
    img.paste(spec, (spec_x0, spec_y0))
    draw.rectangle([spec_x0, spec_y0, spec_x1, spec_y1], outline=(96, 96, 96), width=1)

    # Y axis: vertical "Unit %" label.
    y_label_img = Image.new("RGBA", (280, 32), (0, 0, 0, 0))
    ImageDraw.Draw(y_label_img).text(
        (140, 16),
        "Unit %",
        fill=(200, 200, 200),
        font=label_font,
        anchor="mm",
    )
    y_label_img = y_label_img.rotate(90, expand=True)
    img.paste(
        y_label_img,
        (spec_x0 - 70, (spec_y0 + spec_y1) // 2 - y_label_img.height // 2),
        y_label_img,
    )
    # Y ticks: 100 at top, 0 at bottom (origin convention).
    draw.text((spec_x0 - 8, spec_y0), "100", fill=(170, 170, 170), font=tick_font, anchor="rt")
    draw.text((spec_x0 - 8, spec_y1), "0", fill=(170, 170, 170), font=tick_font, anchor="rb")

    # X axis: horizontal "Integration %" label.
    draw.text(
        ((spec_x0 + spec_x1) // 2, spec_y1 + 56),
        "Integration %",
        fill=(200, 200, 200),
        font=label_font,
        anchor="mm",
    )
    # X ticks: 0 at left, 100 at right (origin convention).
    draw.text((spec_x0, spec_y1 + 8), "0", fill=(170, 170, 170), font=tick_font, anchor="lt")
    draw.text((spec_x1, spec_y1 + 8), "100", fill=(170, 170, 170), font=tick_font, anchor="rt")

    # Outer border.
    draw.rectangle([0, 0, W - 1, H - 1], outline=(64, 64, 64), width=1)

    return img


def _legend_uri() -> str:
    global _LEGEND_URI
    if _LEGEND_URI is not None:
        return _LEGEND_URI
    buf = io.BytesIO()
    _build_legend_image().save(buf, format="PNG")
    _LEGEND_URI = (
        "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii")
    )
    return _LEGEND_URI


def _add_legend(fig: go.Figure) -> None:
    """Overlay the prebuilt 2D coverage-provenance legend in the bottom-left.

    Sized to roughly the width of the icicle's project-root column so it
    doesn't clip into the directory-level tiles. The image is square in
    pixels; sizex/sizey scale chosen so the spectrum stays square once
    Plotly stretches it into paper-coords (chart is 3840×2050 effective).
    """
    fig.add_layout_image(
        dict(
            source=_legend_uri(),
            xref="paper", yref="paper",
            x=0.002, y=0.002,
            sizex=0.075,
            sizey=0.139,
            xanchor="left", yanchor="bottom",
            sizing="stretch",
            layer="above",
        )
    )


def _provenance_colors(sub: pd.DataFrame) -> list[str]:
    """Compute the per-row RGB hex colour from the four provenance counts.

    Zero-line nodes (which shouldn't reach here but are defended against) fall
    back to magenta to flag the unexpected case.
    """
    colors: list[str] = []
    for total, any_, unit, integration in zip(
        sub["TotalLines"],
        sub["AnyCovered"],
        sub["UnitCovered"],
        sub["IntegrationCovered"],
    ):
        if total <= 0:
            colors.append("#FF00FF")
            continue
        r = float(1.0 - _ease(integration / total))
        g = float(_ease(any_ / total))
        b = float(1.0 - _ease(unit / total))
        # Clamp to [0,1] before quantising — guards against tiny FP drift past 1.0.
        r = max(0.0, min(1.0, r))
        g = max(0.0, min(1.0, g))
        b = max(0.0, min(1.0, b))
        colors.append(f"#{int(r * 255):02X}{int(g * 255):02X}{int(b * 255):02X}")
    return colors


def _render_one(
    project: str,
    project_node: pd.Series,
    sub: pd.DataFrame,
) -> bytes:
    """Render one library's provenance icicle SVG.

    Args:
        project: The src package name (also the icicle root node id).
        project_node: The Project-level row for this library — supplies aggregated
                      counts used in the chart title.
        sub: All rows under this project — Project, Directory, File, Method levels.

    Returns:
        SVG image bytes.
    """
    totals = sub["TotalLines"].astype(float).replace(0.0, float("nan"))
    any_pct = (sub["AnyCovered"] / totals * 100.0).fillna(0.0)
    unit_pct = (sub["UnitCovered"] / totals * 100.0).fillna(0.0)
    integration_pct = (sub["IntegrationCovered"] / totals * 100.0).fillna(0.0)

    colors = _provenance_colors(sub)

    hover = [
        f"<b>{label}</b><br>"
        f"Level: {level}<br>"
        f"Any: {a:.1f}% ({ac:,}/{tl:,})<br>"
        f"Unit: {u:.1f}% ({uc:,}/{tl:,})<br>"
        f"Integration: {i:.1f}% ({ic:,}/{tl:,})"
        for label, level, a, ac, u, uc, i, ic, tl in zip(
            sub["Label"],
            sub["Level"],
            any_pct,
            sub["AnyCovered"],
            unit_pct,
            sub["UnitCovered"],
            integration_pct,
            sub["IntegrationCovered"],
            sub["TotalLines"],
        )
    ]

    # Each tile self-describes with label + all three percentages, so a reviewer
    # can read the chart without relying on hover.
    customdata = list(zip(any_pct, unit_pct, integration_pct))

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
            texttemplate=(
                "<b>%{label}</b><br>"
                "A %{customdata[0]:.0f}%  "
                "U %{customdata[1]:.0f}%  "
                "I %{customdata[2]:.0f}%"
            ),
            marker=dict(
                colors=colors,
                line=dict(color="#202020", width=1),
            ),
            tiling=dict(orientation="h"),
        )
    )

    project_total = int(project_node["TotalLines"])
    project_any = int(project_node["AnyCovered"])
    project_unit = int(project_node["UnitCovered"])
    project_integration = int(project_node["IntegrationCovered"])

    if project_total > 0:
        any_p = project_any / project_total * 100.0
        unit_p = project_unit / project_total * 100.0
        integration_p = project_integration / project_total * 100.0
    else:
        any_p = unit_p = integration_p = 0.0

    fig.update_layout(
        title=dict(
            text=(
                f"{project} — Provenance Coverage Icicle<br>"
                f"<sub>Any {any_p:.1f}% · Unit {unit_p:.1f}% · Integration {integration_p:.1f}%"
                f" — {project_total:,} lines</sub>"
            ),
            font=dict(size=18, color="#E0E0E0"),
            y=0.97,
        ),
        margin=dict(l=20, r=20, t=90, b=20),
        paper_bgcolor="#1A1A1A",
        plot_bgcolor="#1A1A1A",
        width=3840,
        height=2160,
    )

    _add_legend(fig)

    return pio.to_image(fig, format="svg")


@step(inputs=["ProvenanceIcicleNode"], outputs="ProvenanceCoverageIcicles")
def generate_provenance_coverage_icicle(nodes: pd.DataFrame) -> dict[str, bytes]:
    """Render one provenance-encoded icicle SVG per src library.

    Args:
        nodes: All provenance icicle nodes (Project, Directory, File, Method)
               produced by BuildProvenanceIcicleStep.

    Returns:
        Dict mapping "{project}.svg" → SVG bytes. The C# DirectoryStorageAdapter
        writes each entry to the configured icicles output directory.
    """
    projects_df = nodes[nodes["Level"] == "Project"]
    logger.info(
        f"[generate_provenance_coverage_icicle] rendering {len(projects_df)} library icicles"
    )

    out: dict[str, bytes] = {}
    for _, project_node in projects_df.iterrows():
        project = project_node["Id"]

        # All nodes belonging to this project: the project root itself, plus
        # anything whose Id starts with "{project}::" (Directory, File, Method).
        sub = nodes[
            (nodes["Id"] == project) | (nodes["Id"].str.startswith(project + "::"))
        ]
        if len(sub) <= 1:
            logger.warning(
                f"[generate_provenance_coverage_icicle] no descendants for {project}; skipping"
            )
            continue

        out[f"{project}.svg"] = _render_one(project, project_node, sub)

    return out
