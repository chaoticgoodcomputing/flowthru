# Sourced by ../post-install.sh.
# ── SPARK_HOME ────────────────────────────────────────────────────────────────
# Required to run Spark-backed tests (Flowthru.Extensions.Spark.Tests).
# Tests guard themselves with Assume checks and skip gracefully if unset.

if [ -n "${SPARK_HOME:-}" ] && [ -d "$SPARK_HOME" ]; then
  if [ -f "$SPARK_HOME/RELEASE" ]; then
    SPARK_RELEASE=$(head -1 "$SPARK_HOME/RELEASE")
    ok "SPARK_HOME=$SPARK_HOME  ($SPARK_RELEASE)"
  else
    ok "SPARK_HOME=$SPARK_HOME"
  fi
else
  warn "SPARK_HOME is not set or does not point to a valid directory."
  warn "Apache Spark 4.1.1 is required to run Spark-backed Flowthru tests."
  warn "Download: https://spark.apache.org/downloads.html"
fi
