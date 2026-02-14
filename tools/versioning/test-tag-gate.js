#!/usr/bin/env node
'use strict';

const { spawnSync } = require('child_process');

function runCase(name, releaseTag, allTags, expectPass) {
  const proc = spawnSync('node', ['tools/versioning/tag-gate.js'], {
    env: {
      ...process.env,
      RELEASE_TAG: releaseTag,
      ALL_TAGS_JSON: JSON.stringify(allTags),
      OUT_DIR: `artifacts/tag-gate/test-${name}`
    },
    encoding: 'utf-8'
  });
  const pass = proc.status === 0;
  if (pass !== expectPass) {
    throw new Error(`${name}: expected pass=${expectPass}, got pass=${pass}\nstdout=${proc.stdout}\nstderr=${proc.stderr}`);
  }
  console.log(`tag-gate-test: OK -> ${name}`);
}

function main() {
  runCase('stable-pass', 'v5.2.0', ['v5.1.0', 'v5.0.0', 'v5.2.0'], true);
  runCase('rc-collision-fail', 'v5.1.0-rc.1', ['v5.1.0', 'v5.0.0'], false);
  runCase('rc-order-fail', 'v5.0.9-rc.1', ['v5.1.0', 'v5.0.8'], false);
  runCase('rc-pass', 'v5.2.0-rc.1', ['v5.1.0', 'v5.0.9'], true);
  console.log('tag-gate-test: completed 4 testcase(s)');
}

main();
