#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { computeDecision } = require('./compute-pr-labels');

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function runCase(testcasePath) {
  const test = JSON.parse(fs.readFileSync(testcasePath, 'utf-8'));
  const decision = computeDecision({
    files: test.files,
    existingLabels: test.existing_labels || [],
    versionRequired: test.required,
    versionActual: test.actual,
    versionReason: test.version_reason,
    guardExit: '0',
    prTitle: test.pr_title || '',
  });

  assert(decision.labels_to_add.includes(test.expect.version_label), `${test.name}: missing version label`);
  assert(decision.labels_to_add.includes(test.expect.primary), `${test.name}: missing primary label`);

  const implLabels = decision.labels_to_add.filter((l) => l.startsWith('impl:'));
  const areaLabels = decision.labels_to_add.filter((l) => l.startsWith('area:'));
  assert(implLabels.length <= 1, `${test.name}: impl cap exceeded`);
  assert(areaLabels.length <= 2, `${test.name}: area cap exceeded`);

  if (test.expect.impl === null) {
    assert(implLabels.length === 0, `${test.name}: expected no impl label`);
  } else if (test.expect.impl) {
    assert(implLabels[0] === test.expect.impl, `${test.name}: wrong impl label`);
  }

  if (Array.isArray(test.expect.area_contains)) {
    for (const area of test.expect.area_contains) {
      assert(decision.labels_to_add.includes(area), `${test.name}: missing area label ${area}`);
    }
  }

  if (Array.isArray(test.expect.area_exact)) {
    assert(areaLabels.length === test.expect.area_exact.length, `${test.name}: area count mismatch`);
    for (const area of test.expect.area_exact) {
      assert(areaLabels.includes(area), `${test.name}: missing exact area label ${area}`);
    }
  }

  console.log(`label-test: OK -> ${test.name}`);
}

function main() {
  const testDir = path.resolve('tools/versioning/testcases');
  const files = fs.readdirSync(testDir).filter((f) => f.endsWith('.json')).sort();
  for (const file of files) {
    runCase(path.join(testDir, file));
  }
  console.log(`label-test: completed ${files.length} testcase(s)`);
}

main();
