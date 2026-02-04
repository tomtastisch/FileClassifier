#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
LIB="${ROOT_DIR}/tools/lib/filetypedetection-lib.sh"
# shellcheck source=/dev/null
source "${LIB}"

SRC_DIR="${ROOT_DIR}/src/FileTypeDetection"
PORTABLE_DIR="${ROOT_DIR}/portable/FileTypeDetection"

usage() {
  cat <<'USAGE'
Usage:
  bash tools/filetypedetection.sh portable sync
  bash tools/filetypedetection.sh portable validate
  bash tools/filetypedetection.sh test [--] [dotnet test args...]
  bash tools/filetypedetection.sh portable check [--workdir PATH] [--clean] [--tfm TFM]

Commands:
  portable sync
    Creates a strict 1:1 mirror of src/FileTypeDetection -> portable/FileTypeDetection
    for *.vb + *.md (excluding *.vbproj) and mirrors src/README.md -> portable/README.md.

  portable validate
    Non-destructive: verifies mirror equivalence via manifests and README mirror.

  test
    Runs dotnet test with detailed console verbosity (readable in CI/terminal).
    Any additional args after -- are passed to dotnet test.

  portable check
    Optional integration smoke-check: creates isolated VB host project, copies portable
    sources, restores/builds/runs once.

Examples:
  bash tools/filetypedetection.sh portable sync
  bash tools/filetypedetection.sh portable validate
  bash tools/filetypedetection.sh test
  bash tools/filetypedetection.sh test -- -c Release
  bash tools/filetypedetection.sh portable check --clean
USAGE
}

cmd="${1:-}"
sub="${2:-}"

if [[ -z "${cmd}" || "${cmd}" == "-h" || "${cmd}" == "--help" ]]; then
  usage
  exit 0
fi

portable_sync() {
  [[ -d "${SRC_DIR}" ]] || ftd_die "Source directory not found: ${SRC_DIR}"

  local tmp_src tmp_port
  tmp_src="$(mktemp "/tmp/ftd-src-manifest.XXXXXX")"
  tmp_port="$(mktemp "/tmp/ftd-port-manifest.XXXXXX")"
  trap "rm -f '${tmp_src}' '${tmp_port}'" EXIT

  ftd_info "Building source manifest..."
  ftd_build_manifest "${SRC_DIR}" "${tmp_src}" "source"

  ftd_assert_safe_rmrf_target "${PORTABLE_DIR}" "/portable/FileTypeDetection"
  rm -rf "${PORTABLE_DIR}"
  mkdir -p "${PORTABLE_DIR}"

  ftd_info "Copying files to portable mirror..."
  while IFS=$'\t' read -r rel _hash; do
    mkdir -p "${PORTABLE_DIR}/$(dirname "${rel}")"
    cp "${SRC_DIR}/${rel}" "${PORTABLE_DIR}/${rel}"
  done < "${tmp_src}"

  # Strict README mirror contract:
  cp "${ROOT_DIR}/src/README.md" "${ROOT_DIR}/portable/README.md"

  ftd_info "Building portable manifest..."
  ftd_build_manifest "${PORTABLE_DIR}" "${tmp_port}" "portable"

  if ! diff -u "${tmp_src}" "${tmp_port}" >/dev/null; then
    echo "FAIL  Portable sync failed: manifests differ." >&2
    diff -u "${tmp_src}" "${tmp_port}" || true
    exit 1
  fi

  ftd_ok "Portable sync complete: ${PORTABLE_DIR}"
}

portable_validate() {
  [[ -d "${SRC_DIR}" ]] || ftd_die "Source directory not found: ${SRC_DIR}"
  [[ -d "${PORTABLE_DIR}" ]] || ftd_die "Portable directory not found: ${PORTABLE_DIR}"

  local tmp_src tmp_port
  tmp_src="$(mktemp "/tmp/ftd-src-manifest.XXXXXX")"
  tmp_port="$(mktemp "/tmp/ftd-port-manifest.XXXXXX")"
  trap "rm -f '${tmp_src}' '${tmp_port}'" EXIT

  ftd_build_manifest "${SRC_DIR}" "${tmp_src}" "source"
  ftd_build_manifest "${PORTABLE_DIR}" "${tmp_port}" "portable"

  if ! diff -u "${tmp_src}" "${tmp_port}" >/dev/null; then
    echo "FAIL  Mismatch between src/FileTypeDetection and portable/FileTypeDetection (*.vb + *.md)." >&2
    echo "INFO  Fix: bash tools/filetypedetection.sh portable sync" >&2
    diff -u "${tmp_src}" "${tmp_port}" || true
    exit 1
  fi

  if [[ ! -f "${ROOT_DIR}/portable/README.md" ]]; then
    ftd_die "Missing portable/README.md (expected strict mirror of src/README.md). Run sync."
  fi

  if ! cmp -s "${ROOT_DIR}/src/README.md" "${ROOT_DIR}/portable/README.md"; then
    ftd_die "portable/README.md is not 1:1 with src/README.md. Run sync."
  fi

  ftd_ok "Portable mirror validated (1:1 manifests + README mirror)."
}

run_tests_readable() {
  ftd_require_cmd dotnet

  local test_project="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj"
  [[ -f "${test_project}" ]] || ftd_die "Test project not found: ${test_project}"

  shift || true  # remove "test" command
  if [[ "${1:-}" == "--" ]]; then shift; fi

  dotnet test "${test_project}" \
    --logger "console;verbosity=detailed" \
    "$@"
}

portable_check() {
  ftd_require_cmd dotnet

  local work_dir="/tmp/filetypedetection-portable-check"
  local clean=0
  local tfm=""
  shift 2 || true

  while [[ $# -gt 0 ]]; do
    case "$1" in
    --workdir) work_dir="${2:-}"; shift 2 ;;
    --clean) clean=1; shift ;;
    --tfm) tfm="${2:-}"; shift 2 ;;
    -h|--help)
      cat <<'USAGE'
Usage: bash tools/filetypedetection.sh portable check [--workdir PATH] [--clean] [--tfm TFM]

Creates an isolated VB solution, copies portable sources, restores/builds/runs once.

Options:
  --workdir PATH   temp directory (default: /tmp/filetypedetection-portable-check)
  --clean          remove workdir after success
  --tfm TFM        override TargetFramework (default: auto-detect, fallback net10.0)
USAGE
      exit 0
      ;;
    *) ftd_die "Unknown argument: $1" ;;
    esac
  done

  [[ -d "${PORTABLE_DIR}" ]] || ftd_die "Portable directory not found: ${PORTABLE_DIR}"

  if [[ -z "${tfm}" ]]; then
    tfm="$(ftd_detect_first_tfm "${ROOT_DIR}")"
  fi

  ftd_info "Preparing isolated check workspace: ${work_dir}"
  rm -rf "${work_dir}"
  mkdir -p "${work_dir}"

  pushd "${work_dir}" >/dev/null

  dotnet new sln -n PortableCheck >/dev/null
  dotnet new console -lang VB -n PortableHost --framework "${tfm}" >/dev/null

  local sln_file
  sln_file="$(find . -maxdepth 1 -type f \( -name 'PortableCheck.sln' -o -name 'PortableCheck.slnx' \) | head -n 1)"
  [[ -n "${sln_file}" ]] || ftd_die "No solution file generated by 'dotnet new sln'."

  dotnet sln "${sln_file}" add PortableHost/PortableHost.vbproj >/dev/null

  mkdir -p PortableHost/FileTypeDetection
  cp -R "${PORTABLE_DIR}/." PortableHost/FileTypeDetection/

  cat > PortableHost/PortableHost.vbproj <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>${tfm}</TargetFramework>
    <RootNamespace></RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <OptionStrict>On</OptionStrict>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="DocumentFormat.OpenXml" Version="3.4.1" />
    <PackageReference Include="Mime" Version="3.8.0" />
    <PackageReference Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
    <PackageReference Include="SharpCompress" Version="0.39.0" />
  </ItemGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
EOF

  cat > PortableHost/Program.vb <<'EOF'
Option Strict On
Option Explicit On

Imports System
Imports FileTypeDetection

Module Program
    Sub Main(args As String())
        Dim detector As New FileTypeDetector()
        Dim detected = detector.Detect("does-not-exist.bin")
        Console.WriteLine(detected.Kind.ToString())
    End Sub
End Module
EOF

  ftd_info "Restoring..."
  dotnet restore "${sln_file}" -v minimal

  ftd_info "Building..."
  dotnet build "${sln_file}" -v minimal

  ftd_info "Running smoke check..."
  dotnet run --project PortableHost/PortableHost.vbproj --no-build

  popd >/dev/null

  if [[ "${clean}" == "1" ]]; then
    rm -rf "${work_dir}"
    ftd_ok "Portable integration check passed. Workdir removed."
  else
    ftd_ok "Portable integration check passed. Workspace kept at: ${work_dir}"
  fi
}

case "${cmd}:${sub}" in
portable:sync) portable_sync ;;
portable:validate) portable_validate ;;
portable:check) portable_check ;;
test:*) run_tests_readable "$@" ;;
*)
  usage
  exit 2
  ;;
esac