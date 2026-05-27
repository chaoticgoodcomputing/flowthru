import { describe, it, expect } from 'vitest';

describe('extension scaffold', () => {
  it('placeholder test runs', () => {
    // Smoke test — ensures the vitest harness is wired up.
    // Real tests for activate() will need a `vscode` module mock; deferred
    // until there is non-trivial activation logic to exercise.
    expect(true).toBe(true);
  });
});
