# Sourced by ../post-install.sh.
# ── chromium ──────────────────────────────────────────────────────────────────
# Optional. Plotly's static image export (PNG/SVG via kaleido) launches a
# headless Chromium under the hood. Without a Chromium-family browser on
# PATH, Python flows that emit graph image outputs (e.g. FlowthruCoverage's
# heatmap PNG) will fail at the image-export step.

CHROMIUM_BIN=""
for candidate in chromium chromium-browser google-chrome chrome; do
  if command -v "$candidate" &>/dev/null; then
    CHROMIUM_BIN="$candidate"
    break
  fi
done

if [ -n "$CHROMIUM_BIN" ]; then
  CHROMIUM_VERSION=$("$CHROMIUM_BIN" --version 2>/dev/null | head -1)
  ok "$CHROMIUM_VERSION  (via $CHROMIUM_BIN)"
else
  warn "chromium not found. Python flows that export Plotly graphs to PNG/SVG"
  warn "(e.g. FlowthruCoverage's heatmap) may fail to produce image outputs"
  warn "without a headless Chromium on PATH."
  warn "Install: https://www.chromium.org/getting-involved/download-chromium/"
fi
