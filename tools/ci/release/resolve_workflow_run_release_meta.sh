#!/usr/bin/env bash
set -euo pipefail

release_tag="${1:-}"
release_run_url="${2:-}"

if [[ -z "${release_tag}" ]]; then
  echo "Missing release tag argument" >&2
  exit 1
fi
if [[ ! "${release_tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$ ]]; then
  echo "Unexpected release tag: '${release_tag}'" >&2
  exit 1
fi
if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
  echo "GITHUB_OUTPUT is not set" >&2
  exit 1
fi

release_version="${release_tag#v}"
verify_log_path="artifacts/ci/nuget-online-convergence/verify.log"
mkdir -p artifacts/ci/nuget-online-convergence

{
  echo "release_tag=${release_tag}"
  echo "release_version=${release_version}"
  echo "release_run_url=${release_run_url}"
  echo "verify_log_path=${verify_log_path}"
} >> "${GITHUB_OUTPUT}"
