// Astro middleware — server-side response transform.
//
// Starlight's Page.astro hardcodes `<html data-theme="dark">` as the static
// default attribute (it expects a client-side script to flip it to "light"
// when the user prefers light mode). Since Flowthru's site is light-only
// while we work out the base design language, we rewrite the attribute
// before the response leaves the server.
//
// Doing this in middleware instead of via a client-side script means:
//   - the static HTML ships with `data-theme="light"` from the start,
//     so Starlight's `[data-theme="light"]` component rules apply
//     consistently from the first paint;
//   - there is no FOUC race between the inline theme script and the
//     cascade resolution;
//   - the `LightThemeProvider` stub (also in this project) is kept as a
//     belt-and-suspenders safety net for any client-side state mutation,
//     but is no longer load-bearing.
//
// When dark mode is eventually added (a coordinated v2 covering both the
// marketing surface and the docs), this middleware comes out and the
// runtime theme picker comes back.

import { defineMiddleware } from "astro:middleware";

const HTML_OPEN_TAG = /<html\b[^>]*>/i;
const DARK_THEME_ATTR = /data-theme="dark"/g;

export const onRequest = defineMiddleware(async (_context, next) => {
  const response = await next();

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("text/html")) {
    return response;
  }

  const html = await response.text();
  const rewritten = html.replace(HTML_OPEN_TAG, (tag) =>
    tag.replace(DARK_THEME_ATTR, 'data-theme="light"'),
  );

  // Preserve the original headers (status, content-type, etc.) but emit
  // the rewritten body. Drop content-length since the body length changes;
  // the server will recompute it.
  const headers = new Headers(response.headers);
  headers.delete("content-length");

  return new Response(rewritten, {
    status: response.status,
    statusText: response.statusText,
    headers,
  });
});
