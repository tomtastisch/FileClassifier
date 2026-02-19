#!/usr/bin/env bash
set -euo pipefail

PR_NUMBER="${PR_NUMBER:-}"
REPO="${REPO:-}"

if [[ -z "${PR_NUMBER}" ]]; then
  echo "versioning-policy: pull_request context required" >&2
  exit 1
fi
if [[ -z "${REPO}" ]]; then
  echo "versioning-policy: REPO is required" >&2
  exit 1
fi

mkdir -p artifacts/policy
files_json="$(python3 tools/ci/bin/github_api.py pr-files --repo "${REPO}" --pr "${PR_NUMBER}")"
labels_json="[]"
for _ in {1..12}; do
  labels_json="$(python3 tools/ci/bin/github_api.py issue-labels --repo "${REPO}" --issue "${PR_NUMBER}")"
  count="$(python3 tools/versioning/count_prefixed_labels.py --labels-json "${labels_json}" --prefix "versioning:")"
  if [[ "${count}" -eq 1 ]]; then
    break
  fi
  sleep 10
done

FILES_JSON="${files_json}" EXISTING_LABELS_JSON="${labels_json}" OUT_DIR="artifacts/policy" node tools/versioning/evaluate-versioning-policy.js
