#!/usr/bin/env bash
set -euo pipefail

WORKFLOW_PATH="${1:-.github/workflows/ci.yml}"
EXPECTED_PATH="${2:-tools/ci/policies/ci_graph_expected.json}"

DOTNET_CMD=(dotnet "tools/ci/checks/CiGraphValidator/bin/Release/net10.0/CiGraphValidator.dll" "$WORKFLOW_PATH" "$EXPECTED_PATH")
"${DOTNET_CMD[@]}"
