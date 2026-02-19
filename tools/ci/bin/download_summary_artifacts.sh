#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

if [[ "$#" -ne 1 ]]; then
  echo "Usage: $0 <run_id>" >&2
  exit 2
fi

run_id="$1"

bash "${SCRIPT_DIR}/download_artifacts_with_retry.sh" "${run_id}" \
  "ci-preflight=artifacts/ci/preflight" \
  "ci-build=artifacts/ci/build" \
  "ci-api-contract=artifacts/ci/api-contract" \
  "ci-pack=artifacts/ci/pack" \
  "ci-consumer-smoke=artifacts/ci/consumer-smoke" \
  "ci-package-backed-tests=artifacts/ci/package-backed-tests" \
  "ci-security-nuget=artifacts/ci/security-nuget" \
  "ci-docs-links-full=artifacts/ci/docs-links-full" \
  "ci-naming-snt=artifacts/ci/naming-snt" \
  "ci-versioning-svt=artifacts/ci/versioning-svt" \
  "ci-version-convergence=artifacts/ci/version-convergence" \
  "ci-tests-bdd-coverage=artifacts/ci/tests-bdd-coverage"
