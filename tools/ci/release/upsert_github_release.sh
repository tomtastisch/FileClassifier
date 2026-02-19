#!/usr/bin/env bash
set -euo pipefail

RELEASE_TAG="${RELEASE_TAG:?RELEASE_TAG is required}"
REPO="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
MAX_ATTEMPTS="${RELEASE_GH_MAX_ATTEMPTS:-5}"
INITIAL_DELAY_SECONDS="${RELEASE_GH_INITIAL_DELAY_SECONDS:-2}"

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

if [[ ! "${MAX_ATTEMPTS}" =~ ^[0-9]+$ || "${MAX_ATTEMPTS}" -lt 1 ]]; then
  echo "MAX_ATTEMPTS must be a positive integer (actual='${MAX_ATTEMPTS}')" >&2
  exit 1
fi
if [[ ! "${INITIAL_DELAY_SECONDS}" =~ ^[0-9]+$ || "${INITIAL_DELAY_SECONDS}" -lt 1 ]]; then
  echo "INITIAL_DELAY_SECONDS must be a positive integer (actual='${INITIAL_DELAY_SECONDS}')" >&2
  exit 1
fi

classify_reason() {
  local msg="$1"
  if grep -qiE "401|bad credentials|requires authentication|unauthorized" <<<"${msg}"; then
    echo "auth"
    return
  fi
  if grep -qiE "429|rate limit|secondary rate limit" <<<"${msg}"; then
    echo "rate-limit"
    return
  fi
  if grep -qiE "5[0-9][0-9]|internal server error|gateway timeout|bad gateway|service unavailable" <<<"${msg}"; then
    echo "5xx"
    return
  fi
  if grep -qiE "timed out|timeout|connection reset|connection refused|temporary failure|network" <<<"${msg}"; then
    echo "network"
    return
  fi
  echo "unknown"
}

run_with_retry() {
  local attempt=1
  local delay="${INITIAL_DELAY_SECONDS}"
  local cmd=("$@")
  local output=""
  while (( attempt <= MAX_ATTEMPTS )); do
    output="$("${cmd[@]}" 2>&1)" && {
      if [[ -n "${output}" ]]; then
        printf '%s\n' "${output}"
      fi
      return 0
    }

    local reason
    reason="$(classify_reason "${output}")"
    echo "WARN: release API call failed (attempt ${attempt}/${MAX_ATTEMPTS}, reason=${reason})." >&2
    printf '%s\n' "${output}" >&2

    if [[ "${reason}" == "auth" || "${attempt}" -ge "${MAX_ATTEMPTS}" ]]; then
      echo "FAIL: release API call failed (reason=${reason})." >&2
      return 1
    fi

    sleep "${delay}"
    delay=$((delay * 2))
    attempt=$((attempt + 1))
  done
}

if gh release view "${RELEASE_TAG}" --repo "${REPO}" >/dev/null 2>&1; then
  run_with_retry gh release edit "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" "${prerelease_flag[@]}" "${latest_edit_flag[@]}"

  # gh does not support --latest=false for `release edit`. If an existing RC was
  # accidentally marked as Latest, correct it via API.
  if [[ "${is_rc}" == "true" ]]; then
    release_id="$(run_with_retry gh api -H "Accept: application/vnd.github+json" "repos/${REPO}/releases/tags/${RELEASE_TAG}" --jq .id)"
    run_with_retry gh api -X PATCH -H "Accept: application/vnd.github+json" "repos/${REPO}/releases/${release_id}" -f make_latest=false >/dev/null
  fi
else
  run_with_retry gh release create "${RELEASE_TAG}" --repo "${REPO}" --title "${RELEASE_TAG}" --notes "Automated tag-only release for ${version}" "${prerelease_flag[@]}" "${latest_create_flag[@]}"
fi
