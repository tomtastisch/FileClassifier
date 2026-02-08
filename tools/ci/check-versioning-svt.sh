#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

ROOT_DIR="$(pwd)"
NAMING_SSOT=""
VERSIONING_SSOT=""
OUT_PATH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      ROOT_DIR="$2"
      shift 2
      ;;
    --naming-ssot)
      NAMING_SSOT="$2"
      shift 2
      ;;
    --versioning-ssot)
      VERSIONING_SSOT="$2"
      shift 2
      ;;
    --out)
      OUT_PATH="$2"
      shift 2
      ;;
    *)
      echo "Usage: tools/ci/check-versioning-svt.sh [--repo-root <path>] [--naming-ssot <path>] [--versioning-ssot <path>] [--out <path>]" >&2
      exit 2
      ;;
  esac
done

ROOT_DIR="$(cd -- "${ROOT_DIR}" && pwd)"
NAMING_SSOT="${NAMING_SSOT:-${ROOT_DIR}/tools/ci/policies/data/naming.json}"
VERSIONING_SSOT="${VERSIONING_SSOT:-${ROOT_DIR}/tools/ci/policies/data/versioning.json}"
OUT_PATH="${OUT_PATH:-${ROOT_DIR}/artifacts/ci/versioning-svt/versioning-svt-summary.json}"

mkdir -p "${ROOT_DIR}/artifacts" "$(dirname -- "${OUT_PATH}")"

python3 - "${ROOT_DIR}" "${NAMING_SSOT}" "${VERSIONING_SSOT}" "${OUT_PATH}" <<'PY'
import json
import os
import re
import subprocess
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

repo_root = Path(sys.argv[1]).resolve()
naming_ssot = Path(sys.argv[2]).resolve()
versioning_ssot = Path(sys.argv[3]).resolve()
out_path = Path(sys.argv[4]).resolve()
report_path = repo_root / "artifacts" / "versioning_report.json"

violations = []
checks = []
require_release_tag = os.environ.get("REQUIRE_RELEASE_TAG", "0") == "1"


def rel(p: Path) -> str:
    return p.resolve().relative_to(repo_root).as_posix()


def fail(scope: str, expected: str, actual: str, evidence: str, message: str) -> None:
    violations.append({
        "scope": scope,
        "expected": expected,
        "actual": actual,
        "evidence": evidence,
        "message": message,
    })


def check(scope: str, expected: str, actual: str, evidence: str) -> None:
    status = "pass" if expected == actual else "fail"
    checks.append({
        "scope": scope,
        "status": status,
        "expected": expected,
        "actual": actual,
        "evidence": evidence,
    })
    if status == "fail":
        fail(scope, expected, actual, evidence, f"Mismatch in {scope}")


if not naming_ssot.exists():
    fail("ssot.naming", str(naming_ssot), "missing", str(naming_ssot), "Naming SSOT missing")
if not versioning_ssot.exists():
    fail("ssot.versioning", str(versioning_ssot), "missing", str(versioning_ssot), "Versioning SSOT missing")

naming = {}
versioning = {}
if naming_ssot.exists():
    naming = json.loads(naming_ssot.read_text(encoding="utf-8"))
if versioning_ssot.exists():
    versioning = json.loads(versioning_ssot.read_text(encoding="utf-8"))

canonical = str(naming.get("canonical_name", ""))
canonical_package = str(naming.get("package_id", ""))
check("identity.canonical_vs_package", canonical, canonical_package, rel(naming_ssot) if naming_ssot.exists() else str(naming_ssot))

project_files = versioning.get("project_files", [])
required_fields = versioning.get("require_vbproj_version_fields", [])
if not isinstance(project_files, list) or len(project_files) == 0:
    fail("versioning.project_files", "non-empty list", str(project_files), rel(versioning_ssot) if versioning_ssot.exists() else str(versioning_ssot), "project_files must be configured")

if not isinstance(required_fields, list) or len(required_fields) == 0:
    fail("versioning.require_vbproj_version_fields", "non-empty list", str(required_fields), rel(versioning_ssot) if versioning_ssot.exists() else str(versioning_ssot), "require_vbproj_version_fields must be configured")

head_tags = []
try:
    raw_tags = subprocess.check_output(["git", "-C", str(repo_root), "tag", "--points-at", "HEAD"], text=True)
    for line in raw_tags.splitlines():
        t = line.strip()
        if re.match(r"^v[0-9]+\.[0-9]+\.[0-9]+$", t):
            head_tags.append(t)
except Exception as ex:
    fail("git.tag_lookup", "command succeeds", "failed", ".git", f"Failed to resolve HEAD tags: {ex}")

head_tags = sorted(set(head_tags))
if len(head_tags) == 0:
    if require_release_tag:
        fail("svt.head_tag", "exactly one tag vX.Y.Z on HEAD", "none", ".git", "No exact release tag on HEAD")
elif len(head_tags) > 1:
    fail("svt.head_tag", "exactly one tag vX.Y.Z on HEAD", ",".join(head_tags), ".git", "Multiple release tags on HEAD")

expected_version = head_tags[0][1:] if len(head_tags) == 1 else ""
# --- repo SSOT version consistency (no mixed versions) ---
def read_repo_version(props_path: Path) -> str:
    if not props_path.exists():
        fail("repo.props.exists", "Directory.Build.props present", "missing", str(props_path), "Directory.Build.props missing")
        return ""
    txt = props_path.read_text(encoding="utf-8", errors="ignore")
    m = re.search(r"<RepoVersion>\s*([^<\s]+)\s*</RepoVersion>", txt)
    return m.group(1).strip() if m else ""

def check_csproj_uses_repo_version(csproj: Path, prop_name: str) -> None:
    if not csproj.exists():
        fail(f"repo.csproj.exists.{csproj.name}", "present", "missing", rel(csproj), f"{csproj.name} missing")
        return
    txt = csproj.read_text(encoding="utf-8", errors="ignore")
    # enforce: default property value must be $(RepoVersion), not a literal
    pat = rf"<{re.escape(prop_name)}\s+Condition=\"'\\$\\({re.escape(prop_name)}\\)'\s*==\s*''\">([^<]+)</{re.escape(prop_name)}>"
    m = re.search(pat, txt)
    if not m:
        fail(f"repo.csproj.{csproj.name}.{prop_name}", "default property present", "missing", rel(csproj), f"Default {prop_name} missing")
        return
    actual = m.group(1).strip()
    check(f"repo.csproj.{csproj.name}.{prop_name}", "$(RepoVersion)", actual, rel(csproj))

repo_props = repo_root / "Directory.Build.props"
repo_version = read_repo_version(repo_props)
if repo_version == "":
    fail("repo.ssot.RepoVersion", "non-empty", "empty", rel(repo_props), "RepoVersion missing in Directory.Build.props")
else:
    # basic semver guard to avoid garbage
    if not re.match(r"^[0-9]+\.[0-9]+\.[0-9]+$", repo_version):
        fail("repo.ssot.RepoVersion.semver", "X.Y.Z", repo_version, rel(repo_props), "RepoVersion is not semver X.Y.Z")
    # if tag defines the release version, RepoVersion must match exactly
    if expected_version:
        check("repo.ssot.RepoVersion", expected_version, repo_version, rel(repo_props))

check_csproj_uses_repo_version(repo_root / "samples" / "PortableConsumer" / "PortableConsumer.csproj", "PortableConsumerPackageVersion")
check_csproj_uses_repo_version(repo_root / "tests" / "PackageBacked.Tests" / "PackageBacked.Tests.csproj", "PackageBackedVersion")
# --- end repo SSOT version consistency ---

vbproj_version = ""
vbproj_package_version = ""
project_path = None
if isinstance(project_files, list) and len(project_files) > 0:
    project_path = (repo_root / str(project_files[0])).resolve()
    if not project_path.exists():
        fail("vbproj.exists", str(project_path), "missing", str(project_path), "Configured vbproj missing")
    else:
        tree = ET.parse(project_path)
        root = tree.getroot()
        for elem in root.iter():
            tag = elem.tag.split("}")[-1]
            if tag == "Version" and elem.text:
                vbproj_version = elem.text.strip()
            if tag == "PackageVersion" and elem.text:
                vbproj_package_version = elem.text.strip()

        if "Version" in required_fields and vbproj_version == "":
            fail("vbproj.Version", "non-empty", "empty", rel(project_path), "Version field missing in vbproj")
        if "PackageVersion" in required_fields and vbproj_package_version == "":
            fail("vbproj.PackageVersion", "non-empty", "empty", rel(project_path), "PackageVersion field missing in vbproj")
        if vbproj_version and vbproj_package_version:
            check("vbproj.Version_vs_PackageVersion", vbproj_version, vbproj_package_version, rel(project_path))

if expected_version and project_path is not None and project_path.exists():
    check("svt.tag_vs_vbproj.Version", expected_version, vbproj_version, rel(project_path))
    check("svt.tag_vs_vbproj.PackageVersion", expected_version, vbproj_package_version, rel(project_path))

nupkg_dir = repo_root / "artifacts" / "nuget"
nupkg_files = sorted([p for p in nupkg_dir.glob("*.nupkg") if not p.name.endswith(".snupkg")])
if len(nupkg_files) == 0:
    if require_release_tag:
        fail("nupkg.exists", "at least one nupkg in artifacts/nuget", "none", rel(nupkg_dir), "No nupkg found for SVT verification")
    else:
        checks.append({
            "scope": "nupkg.exists",
            "status": "pass",
            "expected": "pre-pack check allows missing nupkg",
            "actual": "none",
            "evidence": rel(nupkg_dir),
        })
else:
    records = []
    for nupkg in nupkg_files:
        try:
            with zipfile.ZipFile(nupkg, "r") as zf:
                nuspec_names = sorted([n for n in zf.namelist() if n.endswith(".nuspec")])
                if not nuspec_names:
                    fail("nupkg.nuspec", "present", "missing", rel(nupkg), "nuspec missing in nupkg")
                    continue
                nuspec_content = zf.read(nuspec_names[0]).decode("utf-8", errors="ignore")
                mid = re.search(r"<id>([^<]+)</id>", nuspec_content)
                mver = re.search(r"<version>([^<]+)</version>", nuspec_content)
                nupkg_id = mid.group(1).strip() if mid else ""
                nupkg_ver = mver.group(1).strip() if mver else ""
                records.append((nupkg, nupkg_id, nupkg_ver))
        except Exception as ex:
            fail("nupkg.read", "readable", "failed", rel(nupkg), f"Failed to inspect nupkg: {ex}")

    canonical_records = [r for r in records if r[1] == canonical_package] if canonical_package else records
    if not canonical_records:
        fail("nupkg.canonical", canonical_package or "canonical package", "missing", rel(nupkg_dir), "No canonical nupkg found in artifacts/nuget")
    else:
        # Deterministic pick: highest semantic core version, then lexicographic filename.
        def sem_key(v: str):
            m = re.match(r"^([0-9]+)\\.([0-9]+)\\.([0-9]+)", v)
            if not m:
                return (-1, -1, -1, v)
            return (int(m.group(1)), int(m.group(2)), int(m.group(3)), v)

        chosen = sorted(canonical_records, key=lambda r: (sem_key(r[2]), r[0].name), reverse=True)[0]
        chosen_path, chosen_id, chosen_ver = chosen
        check("nupkg.id", canonical_package, chosen_id, rel(chosen_path))
        if expected_version:
            check("svt.tag_vs_nupkg.version", expected_version, chosen_ver, rel(chosen_path))
        elif vbproj_package_version:
            check("svt.vbproj_vs_nupkg.version", vbproj_package_version, chosen_ver, rel(chosen_path))

status = "pass" if len(violations) == 0 else "fail"
report = {
    "schema_version": 1,
    "check_id": "versioning-svt",
    "status": status,
    "expected_version": expected_version,
    "canonical_package_id": canonical_package,
    "checks": checks,
    "violations": violations,
    "mismatches": violations,
}
summary = {
    "schema_version": 1,
    "check_id": "versioning-svt",
    "status": status,
    "rule_violations": [
        {
            "rule_id": "CI-VERSION-001",
            "severity": "fail",
            "message": v["message"],
            "evidence_paths": [v["evidence"]],
        }
        for v in violations
    ],
    "evidence_paths": sorted({v["evidence"] for v in violations}),
    "artifacts": [
        "artifacts/versioning_report.json",
        out_path.resolve().relative_to(repo_root).as_posix(),
    ],
}
report_path.write_text(json.dumps(report, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
out_path.write_text(json.dumps(summary, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2, ensure_ascii=True))
if status != "pass":
    sys.exit(1)
PY
