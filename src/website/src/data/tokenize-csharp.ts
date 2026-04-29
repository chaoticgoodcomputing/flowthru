// Minimal C# tokenizer ported from the Notebook prototype's flow-data.jsx.
// Run at build time inside Astro components — emits typed tokens that the
// CodeBlock component renders into <span class="tok-…"> elements.

export type TokenKind =
  | "keyword"
  | "type"
  | "string"
  | "comment"
  | "number"
  | "func"
  | "punct"
  | "ws"
  | "ident";

export interface Token {
  text: string;
  kind: TokenKind;
}

const KEYWORDS = new Set([
  "public", "static", "return", "var", "new", "if", "else", "for", "while",
  "void", "namespace", "using", "class", "private", "internal", "readonly",
  "true", "false", "null", "this",
]);

const TYPES = new Set([
  "Flow", "Catalog", "FlowBuilder", "Pipeline",
  "PreprocessCompaniesStep", "PreprocessShuttlesStep", "CreateModelInputTableStep",
  "IStep", "IEnumerable", "RawShuttle", "PreprocessedShuttle",
]);

export function tokenizeCSharp(src: string): Token[] {
  const out: Token[] = [];
  let i = 0;
  while (i < src.length) {
    const ch = src[i];

    // Triple-quoted raw strings.
    if (src.slice(i, i + 3) === '"""') {
      const end = src.indexOf('"""', i + 3);
      const stop = end === -1 ? src.length : end + 3;
      out.push({ text: src.slice(i, stop), kind: "string" });
      i = stop;
      continue;
    }

    // Double-quoted strings.
    if (ch === '"') {
      let j = i + 1;
      while (j < src.length && src[j] !== '"') j++;
      out.push({ text: src.slice(i, j + 1), kind: "string" });
      i = j + 1;
      continue;
    }

    // Line comments.
    if (src.slice(i, i + 2) === "//") {
      const nl = src.indexOf("\n", i);
      const stop = nl === -1 ? src.length : nl;
      out.push({ text: src.slice(i, stop), kind: "comment" });
      i = stop;
      continue;
    }

    // Identifiers / keywords / types / function calls.
    if (/[A-Za-z_]/.test(ch)) {
      let j = i;
      while (j < src.length && /[A-Za-z0-9_]/.test(src[j])) j++;
      const word = src.slice(i, j);
      let kind: TokenKind = "ident";
      if (KEYWORDS.has(word)) kind = "keyword";
      else if (TYPES.has(word)) kind = "type";
      else if (j < src.length && src[j] === "(") kind = "func";
      else if (/^[A-Z]/.test(word)) kind = "type";
      out.push({ text: word, kind });
      i = j;
      continue;
    }

    // Numbers.
    if (/[0-9]/.test(ch)) {
      let j = i;
      while (j < src.length && /[0-9.]/.test(src[j])) j++;
      out.push({ text: src.slice(i, j), kind: "number" });
      i = j;
      continue;
    }

    // Whitespace.
    if (/\s/.test(ch)) {
      let j = i;
      while (j < src.length && /\s/.test(src[j])) j++;
      out.push({ text: src.slice(i, j), kind: "ws" });
      i = j;
      continue;
    }

    // Punctuation fallthrough.
    out.push({ text: ch, kind: "punct" });
    i++;
  }
  return out;
}

export function tokenizeLine(line: string): Token[] {
  return tokenizeCSharp(line);
}
