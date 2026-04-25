#!/usr/bin/env node

/**
 * Stop hook: Confirms stop hooks are running.
 */
process.stdout.write(JSON.stringify({
  systemMessage: "Stop hooks started!"
}));
process.exit(0);
