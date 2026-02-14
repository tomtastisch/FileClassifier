#!/usr/bin/env bash
set -euo pipefail

RELEASE_TAG="${RELEASE_TAG:?RELEASE_TAG is required}"
REPO="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"

version="${RELEASE_TAG#v}"
prerelease_flag=()
if [[ "${RELEASE_TAG}" =~ -rc\.[0-9]+$ ]]; then
  prerelease_flag+=(--prerelease)
fi

if gh release view "${RELEASE_TAG}" --repo "${REPO}" >/dev/null 2>&1; then
  gh release edit "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" "${prerelease_flag[@]}"
else
  gh release create "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" --notes "Automated tag-only release for ${version}" "${prerelease_flag[@]}"
fi
