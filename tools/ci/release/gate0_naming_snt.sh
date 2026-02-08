#!/usr/bin/env bash
set -euo pipefail

workspace="${1:-${GITHUB_WORKSPACE:-$(pwd)}}"
bash "${workspace}/tools/ci/check-naming-snt.sh" \
  --repo-root "${workspace}" \
  --ssot "${workspace}/tools/ci/policies/data/naming.json" \
  --out "${workspace}/artifacts/nuget/naming-snt-summary.json"
