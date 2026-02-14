#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function parseSemverCore(tag) {
  const m = /^v(\d+)\.(\d+)\.(\d+)$/.exec(tag);
  if (!m) return null;
  return [Number(m[1]), Number(m[2]), Number(m[3])];
}

function parseRc(tag) {
  const m = /^v(\d+)\.(\d+)\.(\d+)-rc\.(\d+)$/.exec(tag);
  if (!m) return null;
  return [Number(m[1]), Number(m[2]), Number(m[3]), Number(m[4])];
}

function cmpCore(a, b) {
  for (let i = 0; i < 3; i += 1) {
    if (a[i] !== b[i]) return a[i] - b[i];
  }
  return 0;
}

function stableTags() {
  if (process.env.ALL_TAGS_JSON && process.env.ALL_TAGS_JSON.trim()) {
    const parsed = JSON.parse(process.env.ALL_TAGS_JSON);
    if (!Array.isArray(parsed)) {
      throw new Error('ALL_TAGS_JSON must be a JSON array');
    }
    return parsed.map((s) => String(s));
  }
  const out = execSync("git tag -l 'v*'", { encoding: 'utf-8' });
  return out.split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
}

function loadPolicy() {
  const p = process.env.RAC_POLICY_PATH || 'tools/versioning/rac-policy.json';
  return JSON.parse(fs.readFileSync(path.resolve(p), 'utf-8'));
}

function writeArtifacts(outDir, decision) {
  fs.mkdirSync(outDir, { recursive: true });
  fs.writeFileSync(path.join(outDir, 'decision.json'), `${JSON.stringify(decision, null, 2)}\n`, 'utf-8');
  const tsv = ['status\tcode\tmessage'];
  if (decision.violations.length === 0) {
    tsv.push('pass\t-\ttag gate passed');
  } else {
    for (const v of decision.violations) {
      tsv.push(`fail\t${v.code}\t${v.message}`);
    }
  }
  fs.writeFileSync(path.join(outDir, 'summary.tsv'), `${tsv.join('\n')}\n`, 'utf-8');
  const log = [
    `TAG_GATE|status=${decision.status}`,
    `TAG_GATE|tag=${decision.tag}`,
    `TAG_GATE|channel=${decision.channel}`,
    `TAG_GATE|violations=${decision.violations.length}`
  ];
  fs.writeFileSync(path.join(outDir, 'actions.log'), `${log.join('\n')}\n`, 'utf-8');
}

function main() {
  const policy = loadPolicy();
  const stableRegex = new RegExp(policy.channels.stable.tag_regex);
  const rcRegex = new RegExp(policy.channels.rc.tag_regex);
  const tag = process.env.RELEASE_TAG || process.env.GITHUB_REF_NAME || '';
  const outDir = process.env.OUT_DIR || 'artifacts/tag-gate';

  const violations = [];
  if (!tag) {
    violations.push({ code: 'TG-TAG-MISSING', message: 'release tag missing' });
  }

  let channel = 'invalid';
  if (stableRegex.test(tag)) channel = 'stable';
  if (rcRegex.test(tag)) channel = 'rc';
  if (channel === 'invalid') {
    violations.push({ code: 'TG-TAG-FORMAT', message: `tag '${tag}' does not match stable/rc policy regex` });
  }

  const allTags = stableTags();
  const stable = allTags.filter((t) => stableRegex.test(t)).map((t) => ({ tag: t, core: parseSemverCore(t) })).filter((x) => x.core);
  stable.sort((a, b) => cmpCore(a.core, b.core));

  if (channel === 'rc') {
    const rc = parseRc(tag);
    if (!rc) {
      violations.push({ code: 'TG-RC-PARSE', message: `unable to parse rc tag '${tag}'` });
    } else {
      const coreTag = `v${rc[0]}.${rc[1]}.${rc[2]}`;
      if (policy.enforcement.fail_if_rc_matches_any_existing_stable_same_xyz && stable.some((s) => s.tag === coreTag)) {
        violations.push({ code: 'TG-RC-COLLISION', message: `rc tag collides with existing stable ${coreTag}` });
      }
      if (policy.enforcement.fail_if_rc_version_lte_latest_stable && stable.length > 0) {
        const latestStable = stable[stable.length - 1].core;
        const rcCore = [rc[0], rc[1], rc[2]];
        if (cmpCore(rcCore, latestStable) <= 0) {
          violations.push({ code: 'TG-RC-ORDER', message: `rc core ${coreTag} must be greater than latest stable v${latestStable.join('.')}` });
        }
      }
    }
  }

  const decision = {
    schema_version: 1,
    status: violations.length === 0 ? 'pass' : 'fail',
    timestamp_utc: new Date().toISOString(),
    tag,
    channel,
    stable_tag_count: stable.length,
    violations
  };

  writeArtifacts(outDir, decision);

  if (violations.length > 0) {
    for (const v of violations) {
      console.error(`tag-gate: ${v.code}: ${v.message}`);
    }
    process.exit(1);
  }

  console.log(`tag-gate: pass (${tag}, channel=${channel})`);
}

if (require.main === module) {
  main();
}
