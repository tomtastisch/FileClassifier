#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');

function parseJsonEnv(name, fallback) {
  const value = process.env[name];
  if (!value || !value.trim()) {
    return fallback;
  }
  return JSON.parse(value);
}

function readRacPolicy() {
  const policyPath = process.env.RAC_POLICY_PATH || 'tools/versioning/rac-policy.json';
  const abs = path.resolve(policyPath);
  return JSON.parse(fs.readFileSync(abs, 'utf-8'));
}

function startsWithAny(file, prefixes) {
  return (prefixes || []).some((p) => file.startsWith(p));
}

function isApiRelevant(file, policy) {
  const c = policy.classification || {};
  return startsWithAny(file, c.api_relevant_prefixes || []) || (c.api_relevant_files || []).includes(file);
}

function isNonApi(file, policy) {
  const c = policy.classification || {};
  return startsWithAny(file, c.non_api_prefixes || []) || (c.non_api_files || []).includes(file);
}

function evaluate(files, labels, policy) {
  const versioningAllowed = new Set(policy.labels.versioning.allowed || []);
  const versioningLabels = labels.filter((l) => versioningAllowed.has(l));

  const apiRelevantFiles = files.filter((f) => isApiRelevant(f, policy));
  const unknownFiles = files.filter((f) => !isApiRelevant(f, policy) && !isNonApi(f, policy));
  const onlyNonApiFiles = files.length > 0 && apiRelevantFiles.length === 0 && unknownFiles.length === 0;

  const violations = [];
  if (versioningLabels.length !== 1) {
    violations.push({
      code: 'VP-LABEL-COUNT',
      message: `Exactly one versioning label required, got ${versioningLabels.length}`,
      evidence: 'pull_request.labels'
    });
  }

  const selectedVersioning = versioningLabels[0] || '';
  if (apiRelevantFiles.length > 0 && selectedVersioning === 'versioning:none') {
    violations.push({
      code: 'VP-API-NONE-FORBIDDEN',
      message: 'versioning:none is forbidden when api-relevant files changed',
      evidence: 'changed_files'
    });
  }

  return {
    pass: violations.length === 0,
    selected_versioning_label: selectedVersioning,
    versioning_label_count: versioningLabels.length,
    api_relevant_files: apiRelevantFiles,
    unknown_files: unknownFiles,
    only_non_api_files: onlyNonApiFiles,
    violations
  };
}

function writeArtifacts(result, outDir) {
  fs.mkdirSync(outDir, { recursive: true });

  const decision = {
    schema_version: 1,
    status: result.pass ? 'pass' : 'fail',
    timestamp_utc: new Date().toISOString(),
    selected_versioning_label: result.selected_versioning_label,
    versioning_label_count: result.versioning_label_count,
    only_non_api_files: result.only_non_api_files,
    api_relevant_files: result.api_relevant_files,
    unknown_files: result.unknown_files,
    violations: result.violations
  };

  const summaryLines = [
    'status\tcode\tmessage\tevidence',
    ...(result.violations.length === 0
      ? ['pass\t-\tpolicy evaluation passed\t-']
      : result.violations.map((v) => `fail\t${v.code}\t${v.message}\t${v.evidence}`))
  ];

  const actionsLines = [
    `POLICY_EVAL|status=${decision.status}`,
    `POLICY_EVAL|versioning_label_count=${decision.versioning_label_count}`,
    `POLICY_EVAL|selected_versioning_label=${decision.selected_versioning_label || '-'}`,
    `POLICY_EVAL|api_relevant_files=${decision.api_relevant_files.length}`,
    `POLICY_EVAL|unknown_files=${decision.unknown_files.length}`
  ];

  fs.writeFileSync(path.join(outDir, 'decision.json'), `${JSON.stringify(decision, null, 2)}\n`, 'utf-8');
  fs.writeFileSync(path.join(outDir, 'summary.tsv'), `${summaryLines.join('\n')}\n`, 'utf-8');
  fs.writeFileSync(path.join(outDir, 'actions.log'), `${actionsLines.join('\n')}\n`, 'utf-8');
}

function main() {
  const files = parseJsonEnv('FILES_JSON', []);
  const labels = parseJsonEnv('EXISTING_LABELS_JSON', []);
  const outDir = process.env.OUT_DIR || 'artifacts/policy';

  if (!Array.isArray(files)) {
    throw new Error('FILES_JSON must be a JSON array');
  }
  if (!Array.isArray(labels)) {
    throw new Error('EXISTING_LABELS_JSON must be a JSON array');
  }

  const policy = readRacPolicy();
  const result = evaluate(files, labels, policy);
  writeArtifacts(result, outDir);

  if (!result.pass) {
    for (const violation of result.violations) {
      console.error(`versioning-policy: ${violation.code}: ${violation.message}`);
    }
    process.exit(1);
  }

  console.log('versioning-policy: pass');
}

if (require.main === module) {
  main();
}
