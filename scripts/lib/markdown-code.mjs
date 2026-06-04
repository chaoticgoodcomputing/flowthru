/**
 * Shared markdown code-masking for the docs link/terminology tooling.
 *
 * Replaces every code span/block with spaces, preserving length and newlines,
 * so scanners can ignore code while keeping byte offsets and line numbers
 * aligned with the original source. Used by:
 *   - scripts/lint-doc-links.mjs       (the gating link audit)
 *   - scripts/lint-docs-warnings.mjs   (terminology + review-state warnings)
 *   - src/website/scripts/ingest-docs.mjs (the link interceptor)
 *
 * Handles fenced blocks (``` or ~~~, 3+ delimiters, up to 3 leading spaces,
 * closing fence of ≥ the opening length) and inline code (`…`). Indented
 * (4-space) code blocks are intentionally NOT masked — they're ambiguous with
 * list continuations, and masking them risks hiding real links in list items;
 * use fenced blocks, which are unambiguous.
 *
 * A prose link whose TEXT contains inline code — e.g. [`FlowIO`](/src/…) — is
 * preserved at its `](` token (only the backtick span inside the text is
 * blanked), so consumers that key off the `](` position still see it.
 */
export function maskCode(src) {
  const blank = (s) => s.replace(/[^\n]/g, " ");
  let fence = null; // { char, len } when inside a fenced block
  return src
    .split("\n")
    .map((line) => {
      if (fence) {
        const close = line.match(/^ {0,3}([`~]{3,})\s*$/);
        const masked = blank(line);
        if (close && close[1][0] === fence.char && close[1].length >= fence.len) {
          fence = null;
        }
        return masked;
      }
      const open = line.match(/^ {0,3}([`~]{3,})/);
      if (open) {
        fence = { char: open[1][0], len: open[1].length };
        return blank(line);
      }
      return line.replace(/`[^`\n]*`/g, blank);
    })
    .join("\n");
}
