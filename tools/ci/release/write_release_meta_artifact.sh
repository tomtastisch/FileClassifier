#!/usr/bin/env bash
set -euo pipefail

release_tag="${1:?release_tag required}"
release_version="${2:?release_version required}"
release_run_url="${3:?release_run_url required}"
out_file="${4:-artifacts/release-meta/release-meta.json}"

package_id="$(python3 tools/ci/bin/read_json_field.py --file tools/ci/policies/data/naming.json --field package_id)"
if [[ -z "${package_id}" ]]; then
  package_id="Tomtastisch.FileClassifier"
fi

mkdir -p "$(dirname -- "${out_file}")"
cat > "${out_file}" <<JSON
{
  "release_tag": "${release_tag}",
  "release_version": "${release_version}",
  "package_id": "${package_id}",
  "release_run_url": "${release_run_url}"
}
JSON
