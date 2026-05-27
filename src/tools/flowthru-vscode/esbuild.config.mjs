import { build, context } from 'esbuild';

const isWatch = process.argv.includes('--watch');
const isProduction = process.env.NODE_ENV === 'production';

/** @type {import('esbuild').BuildOptions} */
const config = {
  entryPoints: [ 'src/extension.ts' ],
  bundle: true,
  outfile: 'out/extension.js',
  external: [ 'vscode' ],
  platform: 'node',
  format: 'cjs',
  target: 'node20',
  sourcemap: !isProduction,
  minify: isProduction,
  logLevel: 'info',
};

if (isWatch) {
  const ctx = await context(config);
  await ctx.watch();
  console.log('esbuild: watching for changes...');
} else {
  await build(config);
}
