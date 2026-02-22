#!/usr/bin/env bash
set -euo pipefail

meta_json_path="${1:-}"
release_run_url="${2:-}"

if [[ -z "${meta_json_path}" ]]; then
  echo "Missing release metadata path argument" >&2
  exit 1
fi
if [[ ! -f "${meta_json_path}" ]]; then
  echo "Release metadata file not found: '${meta_json_path}'" >&2
  exit 1
fi
if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
  echo "GITHUB_OUTPUT is not set" >&2
  exit 1
fi

release_tag="$(jq -r '.release_tag // empty' "${meta_json_path}")"
release_version="$(jq -r '.release_version // empty' "${meta_json_path}")"
package_id_from_meta="$(jq -r '.package_id // empty' "${meta_json_path}")"
release_run_url_from_meta="$(jq -r '.release_run_url // empty' "${meta_json_path}")"

if [[ -z "${release_tag}" ]]; then
  echo "release_tag is missing in '${meta_json_path}'" >&2
  exit 1
fi
if [[ ! "${release_tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$ ]]; then
  echo "Unexpected release tag: '${release_tag}'" >&2
  exit 1
fi
if [[ -z "${release_version}" ]]; then
  release_version="${release_tag#v}"
fi
if [[ "${release_version}" != "${release_tag#v}" ]]; then
  echo "Release metadata mismatch: tag='${release_tag}' version='${release_version}'" >&2
  exit 1
fi

verify_log_path="artifacts/ci/nuget-online-convergence/verify.log"
package_id="${package_id_from_meta}"
if [[ -z "${package_id}" ]]; then
  package_id="$(python3 tools/ci/bin/read_json_field.py --file tools/ci/policies/data/naming.json --field package_id)"
fi
if [[ -z "${package_id}" ]]; then
  package_id="Tomtastisch.FileClassifier"
fi
if [[ -z "${release_run_url}" ]]; then
  release_run_url="${release_run_url_from_meta}"
fi
if [[ -z "${release_run_url}" ]]; then
  echo "release_run_url is missing: provide as second argument or in '${meta_json_path}' (.release_run_url)" >&2
  exit 1
fi
mkdir -p artifacts/ci/nuget-online-convergence

{
  echo "release_tag=${release_tag}"
  echo "release_version=${release_version}"
  echo "release_run_url=${release_run_url}"
  echo "verify_log_path=${verify_log_path}"
  echo "package_id=${package_id}"
} >> "${GITHUB_OUTPUT}"
