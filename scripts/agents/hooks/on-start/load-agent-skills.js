#!/usr/bin/env node

/**
 * SessionStart hook: feeds the flowthru-contributing SKILL.md into agent
 * context. That file is the canonical agent entry point for this repo — it
 * carries the Agent-skills wiring (issue tracker, triage labels, domain doc
 * conventions) that other skills rely on as breadcrumbs into .claude/docs/.
 *
 * Loading it unconditionally at session start means those breadcrumbs are
 * available even outside src/ and tests/ work (where the skill would
 * otherwise auto-trigger).
 */

const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '../../../..');
const skillPath = path.join(repoRoot, '.claude/skills/flowthru-contributing/SKILL.md');

let skill;
try {
  skill = fs.readFileSync(skillPath, 'utf-8');
} catch (err) {
  process.stderr.write(`on-start/load-agent-skills: could not read flowthru-contributing SKILL.md: ${err.message}\n`);
  process.exit(0);
}

const output = {
  hookSpecificOutput: {
    hookEventName: 'SessionStart',
    additionalContext: [
      'The following is the flowthru-contributing skill — the canonical agent',
      'entry point for this repo. It points at .claude/docs/ for issue-tracker,',
      'triage-label, and domain-doc conventions.',
      '',
      skill,
    ].join('\n'),
  },
};

process.stdout.write(JSON.stringify(output));
