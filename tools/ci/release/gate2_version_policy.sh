#!/usr/bin/env bash
set -euo pipefail

mode="${1:-release}"
tag="${2:?tag required}"
nupkg_path="${3:?nupkg path required}"
if [[ "${mode}" != "release" ]]; then
  echo "Unsupported mode '${mode}' for gate2_version_policy.sh (expected release)" >&2
  exit 2
fi

# Gate 2 enforces tag->vbproj->nupkg equality on release tags.
REQUIRE_RELEASE_TAG=1 bash tools/ci/check-versioning-svt.sh \
  --repo-root . \
  --naming-ssot tools/ci/policies/data/naming.json \
  --versioning-ssot tools/ci/policies/data/versioning.json \
  --out artifacts/nuget/versioning-svt-summary.json
