#!/usr/bin/env bash
set -euo pipefail

RELEASE_TAG="${RELEASE_TAG:?RELEASE_TAG is required}"
REPO="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"

version="${RELEASE_TAG#v}"
prerelease_flag=()
latest_create_flag=()
latest_edit_flag=()

is_rc="false"
if [[ "${RELEASE_TAG}" =~ -rc\.[0-9]+$ ]]; then
  is_rc="true"
  prerelease_flag+=(--prerelease)
  # Explicitly ensure RCs are never marked as Latest.
  latest_create_flag+=(--latest=false)
else
  # Stable releases must be explicitly marked as Latest.
  latest_create_flag+=(--latest)
  latest_edit_flag+=(--latest)
fi

if gh release view "${RELEASE_TAG}" --repo "${REPO}" >/dev/null 2>&1; then
  gh release edit "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" "${prerelease_flag[@]}" "${latest_edit_flag[@]}"

  # gh does not support --latest=false for `release edit`. If an existing RC was
  # accidentally marked as Latest, correct it via API.
  if [[ "${is_rc}" == "true" ]]; then
    release_id="$(gh api -H "Accept: application/vnd.github+json" "repos/${REPO}/releases/tags/${RELEASE_TAG}" --jq .id)"
    gh api -X PATCH -H "Accept: application/vnd.github+json" "repos/${REPO}/releases/${release_id}" -f make_latest=false >/dev/null
  fi
else
  gh release create "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" --notes "Automated tag-only release for ${version}" "${prerelease_flag[@]}" "${latest_create_flag[@]}"
fi
