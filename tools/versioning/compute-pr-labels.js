#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');

const PRIMARY_PRIORITY = [
  'breaking',
  'feature',
  'fix',
  'refactor',
  'ci',
  'test',
  'docs',
  'tooling',
  'chore',
];

const AREA_RULES = [
  { prefix: '.github/workflows/', label: 'area:pipeline' },
  { prefix: '.qodana/', label: 'area:qodana' },
  { exact: 'qodana.yaml', label: 'area:qodana' },
  { exact: '.github/workflows/qodana.yml', label: 'area:qodana' },
  { exact: 'tools/ci/check-code-scanning-tools-zero.sh', label: 'area:qodana' },
  { prefix: 'src/FileTypeDetection/Infrastructure/Archive', label: 'area:archive' },
  { prefix: 'src/FileTypeDetection/ArchiveProcessing', label: 'area:archive' },
  { exact: 'src/FileTypeDetection/EvidenceHashing.vb', label: 'area:hashing' },
  { prefix: 'src/FileTypeDetection/Detection/', label: 'area:detection' },
  { prefix: 'src/FileTypeDetection/FileTypeDetector', label: 'area:detection' },
  { prefix: 'src/FileTypeDetection/FileMaterializer', label: 'area:materializer' },
  { prefix: 'docs/versioning/', label: 'area:versioning' },
  { exact: 'Directory.Build.props', label: 'area:versioning' },
  { prefix: 'tests/', label: 'area:tests' },
  { prefix: 'docs/', label: 'area:docs' },
  { exact: 'README.md', label: 'area:docs' },
  { prefix: 'tools/', label: 'area:tooling' },
];

function loadRacPolicy() {
  const policyPath = process.env.RAC_POLICY_PATH || 'tools/versioning/rac-policy.json';
  const absPath = path.resolve(policyPath);
  const raw = fs.readFileSync(absPath, 'utf-8');
  const policy = JSON.parse(raw);
  const versionMap = policy?.labels?.versioning?.map;
  if (!versionMap || !versionMap.none || !versionMap.patch || !versionMap.minor || !versionMap.major) {
    throw new Error(`Invalid RaC policy version label map in ${policyPath}`);
  }
  return policy;
}

function parseJsonEnv(name, fallback) {
  const value = process.env[name];
  if (!value || !value.trim()) {
    return fallback;
  }
  try {
    return JSON.parse(value);
  } catch (error) {
    throw new Error(`Invalid JSON in ${name}: ${error.message}`);
  }
}

function firstMatchingPrimary(candidates) {
  for (const label of PRIMARY_PRIORITY) {
    if (candidates.has(label)) {
      return label;
    }
  }
  return 'chore';
}

function hasAnyPrefix(files, prefixes) {
  return files.some((file) => prefixes.some((prefix) => file.startsWith(prefix)));
}

function containsAny(text, tokens) {
  return tokens.some((token) => text.includes(token));
}

function computeVersionReason(required, files) {
  if (required === 'major') {
    return 'breaking-change-required';
  }
  const docsOnly = files.length > 0 && files.every((f) => f.startsWith('docs/') || f.endsWith('.md') || f.startsWith('.github/'));
  if (docsOnly) {
    return 'docs-or-meta-only';
  }
  if (hasAnyPrefix(files, ['src/'])) {
    return 'code-surface-changed';
  }
  if (hasAnyPrefix(files, ['tests/'])) {
    return 'test-surface-changed';
  }
  if (hasAnyPrefix(files, ['.github/workflows/', 'tools/'])) {
    return 'pipeline-or-tooling-changed';
  }
  return 'meta-change';
}

function computePrimary(required, files, prTitle) {
  const candidates = new Set();
  const title = (prTitle || '').toLowerCase();

  if (required === 'major') {
    candidates.add('breaking');
  }
  if (containsAny(title, ['feat', 'feature']) || hasAnyPrefix(files, ['src/'])) {
    candidates.add('feature');
  }
  if (containsAny(title, ['fix', 'bug', 'hotfix'])) {
    candidates.add('fix');
  }
  if (containsAny(title, ['refactor', 'cleanup'])) {
    candidates.add('refactor');
  }
  if (hasAnyPrefix(files, ['.github/'])) {
    candidates.add('ci');
  }
  if (hasAnyPrefix(files, ['tests/'])) {
    candidates.add('test');
  }
  if (hasAnyPrefix(files, ['docs/']) || files.includes('README.md')) {
    candidates.add('docs');
  }
  if (hasAnyPrefix(files, ['tools/'])) {
    candidates.add('tooling');
  }
  if (files.length === 0 || hasAnyPrefix(files, ['Directory.Build.props', 'Directory.Packages.props'])) {
    candidates.add('chore');
  }

  return firstMatchingPrimary(candidates);
}

function computeImpl(files) {
  if (files.some((f) => f.toLowerCase().includes('security') || f.toLowerCase().includes('vuln'))) {
    return 'impl:security';
  }
  if (hasAnyPrefix(files, ['docs/']) && !hasAnyPrefix(files, ['src/', 'tests/', '.github/', 'tools/'])) {
    return 'impl:docs';
  }
  if (hasAnyPrefix(files, ['.github/', 'tools/']) || files.includes('Directory.Build.props') || files.includes('Directory.Packages.props')) {
    return 'impl:config';
  }
  if (hasAnyPrefix(files, ['src/', 'tests/'])) {
    return 'impl:quality';
  }
  return null;
}

function computeAreas(files) {
  const result = [];
  for (const file of files) {
    for (const rule of AREA_RULES) {
      const matches = (rule.exact && file === rule.exact) || (rule.prefix && file.startsWith(rule.prefix));
      if (matches && !result.includes(rule.label)) {
        result.push(rule.label);
        if (result.length === 2) {
          return result;
        }
      }
    }
  }
  return result;
}

function resolveVersionLabel(required, racPolicy) {
  const allowed = new Set(['major', 'minor', 'patch', 'none']);
  const normalized = allowed.has(required) ? required : 'none';
  return racPolicy.labels.versioning.map[normalized];
}

function readAutoLabelScope() {
  const labelsPath = 'tools/versioning/labels.json';
  const raw = fs.readFileSync(labelsPath, 'utf-8');
  const labels = JSON.parse(raw);
  return Object.keys(labels);
}

function computeDecision(input) {
  const files = input.files;
  const required = input.versionRequired;
  const actual = input.versionActual;
  const guardExit = input.guardExit;
  const prTitle = input.prTitle;
  const existingLabels = input.existingLabels;

  const racPolicy = loadRacPolicy();
  const versionLabel = resolveVersionLabel(required, racPolicy);
  const primaryLabel = computePrimary(required, files, prTitle);
  const implLabel = computeImpl(files);
  const areaLabels = computeAreas(files);
  const versionReason = input.versionReason || computeVersionReason(required, files);

  const labelsToAdd = [versionLabel, primaryLabel];
  if (implLabel) {
    labelsToAdd.push(implLabel);
  }
  for (const areaLabel of areaLabels) {
    labelsToAdd.push(areaLabel);
  }

  const autoLabelScope = readAutoLabelScope();
  const labelsToRemove = existingLabels.filter((label) => autoLabelScope.includes(label) && !labelsToAdd.includes(label));

  return {
    labels_to_add: labelsToAdd,
    labels_to_remove: labelsToRemove,
    version_required: required,
    version_actual: actual,
    version_reason: versionReason,
    decision_trace: {
      guard_exit: guardExit,
      changed_files_count: files.length,
      changed_files: files,
      primary_priority: PRIMARY_PRIORITY,
      selected_primary: primaryLabel,
      selected_impl: implLabel,
      selected_areas: areaLabels,
    },
  };
}

function main() {
  const output = process.env.OUTPUT_PATH || 'artifacts/labels/decision.json';
  const files = parseJsonEnv('FILES_JSON', []);
  const existingLabels = parseJsonEnv('EXISTING_LABELS_JSON', []);

  if (!Array.isArray(files)) {
    throw new Error('FILES_JSON must be a JSON array');
  }
  if (!Array.isArray(existingLabels)) {
    throw new Error('EXISTING_LABELS_JSON must be a JSON array');
  }

  const decision = computeDecision({
    files,
    existingLabels,
    versionRequired: process.env.VERSION_REQUIRED || 'none',
    versionActual: process.env.VERSION_ACTUAL || 'none',
    versionReason: process.env.VERSION_REASON || '',
    guardExit: process.env.VERSION_GUARD_EXIT || '0',
    prTitle: process.env.PR_TITLE || '',
  });

  fs.mkdirSync(require('path').dirname(output), { recursive: true });
  fs.writeFileSync(output, `${JSON.stringify(decision, null, 2)}\n`, 'utf-8');

  console.log(`versioning: required=${decision.version_required}`);
  console.log(`versioning: actual=${decision.version_actual}`);
  console.log(`versioning: reason=${decision.version_reason}`);
  console.log(`labels: add=${decision.labels_to_add.join(',')}`);
  if (decision.labels_to_remove.length > 0) {
    console.log(`labels: remove=${decision.labels_to_remove.join(',')}`);
  }
}

if (require.main === module) {
  main();
}

module.exports = {
  computeDecision,
};
