#!/usr/bin/env node
'use strict';

const { spawnSync } = require('child_process');

function runCase(name, files, labels, expectPass) {
  const proc = spawnSync('node', ['tools/versioning/evaluate-versioning-policy.js'], {
    env: {
      ...process.env,
      FILES_JSON: JSON.stringify(files),
      EXISTING_LABELS_JSON: JSON.stringify(labels),
      OUT_DIR: `artifacts/policy/test-${name}`
    },
    encoding: 'utf-8'
  });

  const passed = proc.status === 0;
  if (passed !== expectPass) {
    throw new Error(`${name}: expected pass=${expectPass}, got pass=${passed}\nstdout=${proc.stdout}\nstderr=${proc.stderr}`);
  }
  console.log(`policy-test: OK -> ${name}`);
}

function main() {
  runCase('missing-versioning-label', ['docs/001_INDEX_CORE.MD'], ['docs'], false);
  runCase('multiple-versioning-labels', ['docs/001_INDEX_CORE.MD'], ['versioning:none', 'versioning:patch'], false);
  runCase('api-with-none', ['src/FileTypeDetection/FileTypeDetector.vb'], ['versioning:none'], false);
  runCase('non-api-with-none', ['docs/001_INDEX_CORE.MD', '.github/workflows/ci.yml'], ['versioning:none'], true);
  console.log('policy-test: completed 4 testcase(s)');
}

main();
