#!/usr/bin/env bash
set -euo pipefail

summary_path="${1:-artifacts/nuget/naming-snt-summary.json}"
[[ -f "${summary_path}" ]] || { echo "Missing naming summary: ${summary_path}" >&2; exit 1; }
status="$(python3 - "${summary_path}" <<'PY'
import json
import sys
from pathlib import Path
obj = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
print(obj.get('status', ''))
PY
)"
if [[ "${status}" != "pass" ]]; then
  echo "Naming summary status is '${status}' (expected 'pass')." >&2
  exit 1
fi
echo "Naming summary status=pass (${summary_path})"
