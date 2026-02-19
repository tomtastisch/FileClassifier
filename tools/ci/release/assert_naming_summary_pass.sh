#!/usr/bin/env bash
set -euo pipefail

summary_path="${1:-artifacts/nuget/naming-snt-summary.json}"
[[ -f "${summary_path}" ]] || { echo "Missing naming summary: ${summary_path}" >&2; exit 1; }
status="$(python3 tools/ci/release/read_summary_status.py --summary "${summary_path}")"
if [[ "${status}" != "pass" ]]; then
  echo "Naming summary status is '${status}' (expected 'pass')." >&2
  exit 1
fi
echo "Naming summary status=pass (${summary_path})"
