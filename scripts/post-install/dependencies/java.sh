# Sourced by ../post-install.sh.
# ── java ──────────────────────────────────────────────────────────────────────
# Required to run Spark-backed tests. Spark 4.1.1 requires JDK 17+.

if command -v java &>/dev/null; then
  JAVA_VERSION=$(java -version 2>&1 | head -1)
  ok "java: $JAVA_VERSION"
else
  warn "java not found. JDK 17+ is required to run Spark-backed Flowthru tests."
fi
