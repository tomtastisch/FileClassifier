#!/usr/bin/env node
'use strict';

const fs = require('fs');

function fail(message) {
  console.error(`label-validation: ${message}`);
  process.exit(1);
}

function readJson(path) {
  return JSON.parse(fs.readFileSync(path, 'utf-8'));
}

function validateArray(name, value) {
  if (!Array.isArray(value)) {
    fail(`${name} must be an array`);
  }
}

function validate() {
  const schemaPath = process.argv[2] || 'tools/versioning/label-schema.json';
  const decisionPath = process.argv[3] || 'artifacts/labels/decision.json';

  const schema = readJson(schemaPath);
  const decision = readJson(decisionPath);

  const requiredFields = [
    'labels_to_add',
    'labels_to_remove',
    'version_required',
    'version_actual',
    'version_reason',
    'decision_trace',
  ];

  for (const field of requiredFields) {
    if (!(field in decision)) {
      fail(`missing field: ${field}`);
    }
  }

  validateArray('labels_to_add', decision.labels_to_add);
  validateArray('labels_to_remove', decision.labels_to_remove);

  const allowedLabels = new Set(schema.allowed_labels || []);
  for (const label of decision.labels_to_add) {
    if (!allowedLabels.has(label)) {
      fail(`labels_to_add contains unknown label: ${label}`);
    }
  }

  const versionLabels = decision.labels_to_add.filter((label) => label.startsWith('versioning:'));
  if (versionLabels.length !== 1) {
    fail(`expected exactly 1 versioning:* label, got ${versionLabels.length}`);
  }

  const primaryLabels = decision.labels_to_add.filter((label) => (schema.primary_labels || []).includes(label));
  if (primaryLabels.length !== 1) {
    fail(`expected exactly 1 primary label, got ${primaryLabels.length}`);
  }

  const implLabels = decision.labels_to_add.filter((label) => label.startsWith('impl:'));
  if (implLabels.length > 1) {
    fail(`expected max 1 impl:* label, got ${implLabels.length}`);
  }

  const areaLabels = decision.labels_to_add.filter((label) => label.startsWith('area:'));
  if (areaLabels.length > 2) {
    fail(`expected max 2 area:* labels, got ${areaLabels.length}`);
  }

  const validVersionValues = new Set(['major', 'minor', 'patch', 'none']);
  if (!validVersionValues.has(decision.version_required)) {
    fail(`invalid version_required: ${decision.version_required}`);
  }
  if (!validVersionValues.has(decision.version_actual)) {
    fail(`invalid version_actual: ${decision.version_actual}`);
  }

  if (!decision.version_reason || typeof decision.version_reason !== 'string') {
    fail('version_reason must be a non-empty string');
  }

  console.log('label-validation: OK');
}

validate();
