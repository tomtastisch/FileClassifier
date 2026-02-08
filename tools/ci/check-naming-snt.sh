#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
SSOT_PATH="${ROOT_DIR}/tools/ci/policies/data/naming.json"
OUT_PATH="${ROOT_DIR}/artifacts/ci/naming-snt/naming-snt-summary.json"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      ROOT_DIR="$2"
      shift 2
      ;;
    --ssot)
      SSOT_PATH="$2"
      shift 2
      ;;
    --out)
      OUT_PATH="$2"
      shift 2
      ;;
    *)
      echo "Usage: tools/ci/check-naming-snt.sh [--repo-root <path>] [--ssot <path>] [--out <path>]" >&2
      exit 2
      ;;
  esac
done

mkdir -p "$(dirname -- "${OUT_PATH}")"
mkdir -p "${ROOT_DIR}/artifacts"

python3 - "${ROOT_DIR}" "${SSOT_PATH}" "${OUT_PATH}" <<'PY'
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

repo_root = Path(sys.argv[1]).resolve()
ssot_path = Path(sys.argv[2]).resolve()
out_path = Path(sys.argv[3]).resolve()
report_json_path = repo_root / "artifacts" / "naming_snt_report.json"
report_txt_path = repo_root / "artifacts" / "naming_snt_report.txt"

checks = []
mismatches = []
notes = []


def rel(path: Path) -> str:
    return path.resolve().relative_to(repo_root).as_posix()


def add_check(name: str, expected: str, actual: str, evidence: str) -> None:
    status = "pass" if expected == actual else "fail"
    checks.append({
        "name": name,
        "status": status,
        "expected": expected,
        "actual": actual,
        "evidence": evidence,
    })
    if status == "fail":
        mismatches.append({
            "scope": name,
            "expected": expected,
            "actual": actual,
            "evidence": evidence,
            "message": f"Mismatch in {name}",
        })


def add_mismatch(scope: str, expected: str, actual: str, evidence: str, message: str) -> None:
    mismatches.append({
        "scope": scope,
        "expected": expected,
        "actual": actual,
        "evidence": evidence,
        "message": message,
    })


def file_line_hit(path: Path, needle: str) -> str:
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except Exception:
        return f"{rel(path)}:1"
    for idx, line in enumerate(lines, start=1):
        if needle in line:
            return f"{rel(path)}:{idx}"
    return f"{rel(path)}:1"


def normalize_repo_url(url: str) -> str:
    text = url.strip()
    if text.startswith("git@github.com:"):
        text = "https://github.com/" + text.split(":", 1)[1]
    text = re.sub(r"^ssh://git@github.com/", "https://github.com/", text)
    text = re.sub(r"\.git$", "", text)
    return text.rstrip("/")


if not ssot_path.exists():
    msg = f"SSOT file missing: {ssot_path}"
    result = {
        "schema_version": 1,
        "check_id": "naming-snt",
        "status": "fail",
        "checks": [],
        "mismatches": [{
            "scope": "ssot_file",
            "expected": str(ssot_path),
            "actual": "missing",
            "evidence": str(ssot_path),
            "message": msg,
        }],
        "evidence_paths": [str(ssot_path)],
    }
    out_path.write_text(json.dumps(result, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    report_json_path.write_text(json.dumps(result, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    report_txt_path.write_text(f"FAIL: {msg}\n", encoding="utf-8")
    print(msg, file=sys.stderr)
    sys.exit(1)

ssot = json.loads(ssot_path.read_text(encoding="utf-8"))
required = [
    "canonical_name",
    "package_id",
    "root_namespace",
    "assembly_name",
    "repo_slug",
    "repo_display_name",
    "repository_url",
    "deprecated_package_ids",
    "namespace_decision",
    "docs_expected_links",
]
for key in required:
    if key not in ssot:
        add_mismatch("ssot_key", key, "missing", rel(ssot_path), f"Missing SSOT key: {key}")

canonical_name = str(ssot.get("canonical_name", ""))
package_id = str(ssot.get("package_id", ""))
root_namespace = str(ssot.get("root_namespace", ""))
assembly_name = str(ssot.get("assembly_name", ""))
repo_slug = str(ssot.get("repo_slug", ""))
repo_display_name = str(ssot.get("repo_display_name", ""))
repository_url = str(ssot.get("repository_url", ""))
deprecated_package_ids = ssot.get("deprecated_package_ids", [])
namespace_decision = ssot.get("namespace_decision", {})
target_root_namespace = str(namespace_decision.get("target_root_namespace", ""))

add_check("identity.canonical_vs_package", canonical_name, package_id, rel(ssot_path))
add_check("identity.canonical_vs_root_namespace", canonical_name, root_namespace, rel(ssot_path))
add_check("identity.canonical_vs_assembly", canonical_name, assembly_name, rel(ssot_path))
add_check("identity.canonical_vs_repo_display_name", canonical_name, repo_display_name, rel(ssot_path))

if not isinstance(deprecated_package_ids, list) or len(deprecated_package_ids) == 0:
    add_mismatch("deprecated_package_ids", "non-empty list", str(deprecated_package_ids), rel(ssot_path), "deprecated_package_ids MUST exist and be non-empty")

if canonical_name in deprecated_package_ids:
    add_mismatch("deprecated_package_ids", "canonical not included", canonical_name, rel(ssot_path), "canonical_name MUST NOT be part of deprecated_package_ids")

remote_raw = ""
try:
    remote_raw = subprocess.check_output(["git", "-C", str(repo_root), "remote", "get-url", "origin"], text=True).strip()
except Exception as ex:
    add_mismatch("git_remote", "origin configured", "missing", ".git/config", f"Failed to read origin remote: {ex}")

normalized_remote = normalize_repo_url(remote_raw) if remote_raw else ""
normalized_ssot_repo = normalize_repo_url(repository_url)
notes.append("repo_url_normalization: strip .git; convert git@github.com:<org>/<repo> to https://github.com/<org>/<repo>; trim trailing slash")
add_check("repository_url.matches_origin", normalized_ssot_repo, normalized_remote, ".git/config")

remote_slug = ""
if normalized_remote:
    m = re.search(r"/([^/]+)$", normalized_remote)
    remote_slug = m.group(1) if m else ""
add_check("repo_slug.matches_remote_slug", repo_slug, remote_slug, ".git/config")

project_path = repo_root / "src" / "FileTypeDetection" / "FileTypeDetectionLib.vbproj"
if not project_path.exists():
    add_mismatch("vbproj.exists", str(project_path), "missing", rel(project_path), "Package project file missing")
else:
    tree = ET.parse(project_path)
    proj_root = tree.getroot()

    def first(tag: str) -> str:
        for elem in proj_root.iter():
            if elem.tag.endswith(tag) and elem.text:
                return elem.text.strip()
        return ""

    add_check("vbproj.PackageId", package_id, first("PackageId"), file_line_hit(project_path, "<PackageId>"))
    add_check("vbproj.RootNamespace", root_namespace, first("RootNamespace"), file_line_hit(project_path, "<RootNamespace>"))
    add_check("vbproj.AssemblyName", assembly_name, first("AssemblyName"), file_line_hit(project_path, "<AssemblyName>"))
    add_check("vbproj.RepositoryUrl", normalized_ssot_repo, normalize_repo_url(first("RepositoryUrl")), file_line_hit(project_path, "<RepositoryUrl>"))
    add_check("vbproj.PackageProjectUrl", normalized_ssot_repo, normalize_repo_url(first("PackageProjectUrl")), file_line_hit(project_path, "<PackageProjectUrl>"))

# Namespace declarations in VB code.
namespace_hits = []
for vb in sorted((repo_root / "src" / "FileTypeDetection").rglob("*.vb")):
    lines = vb.read_text(encoding="utf-8").splitlines()
    for idx, line in enumerate(lines, start=1):
        m = re.match(r"^\s*Namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*$", line)
        if m:
            raw_ns = m.group(1)
            normalized_ns = re.sub(r"^Global\.", "", raw_ns)
            namespace_hits.append((normalized_ns, f"{rel(vb)}:{idx}"))

if not namespace_hits:
    add_mismatch("code.namespace", target_root_namespace, "missing", "src/FileTypeDetection", "No namespace declarations found")
else:
    for actual_ns, evidence in namespace_hits:
        if not actual_ns.startswith(target_root_namespace):
            add_mismatch("code.namespace.prefix", target_root_namespace + "*", actual_ns, evidence, "Namespace must start with target_root_namespace")
    distinct_ns = sorted({ns for ns, _ in namespace_hits})
    checks.append({
        "name": "code.namespace.distinct",
        "status": "pass" if all(ns.startswith(target_root_namespace) for ns in distinct_ns) else "fail",
        "expected": target_root_namespace + "*",
        "actual": ",".join(distinct_ns),
        "evidence": namespace_hits[0][1],
    })

# Detect forbidden legacy namespace references in code/tests/samples.
legacy_namespace_tokens = ["using FileTypeDetection;", "Namespace FileTypeDetection", "FileTypeDetection."]
scan_roots = [repo_root / "src", repo_root / "tests", repo_root / "samples"]
for root in scan_roots:
    for path in sorted(root.rglob("*")):
        if path.suffix.lower() not in {".cs", ".vb", ".md", ".txt"}:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        for token in legacy_namespace_tokens:
            if token in text:
                add_mismatch("legacy_namespace_leftover", "absent", token, file_line_hit(path, token), "Legacy namespace token found outside migration context")

# README/docs install snippets and display name.
readme = repo_root / "README.md"
if readme.exists():
    rtext = readme.read_text(encoding="utf-8")
    if repo_display_name not in rtext:
        add_mismatch("readme.repo_display_name", repo_display_name, "missing", rel(readme), "README must show repo_display_name")
    if f"dotnet add package {package_id}" not in rtext:
        add_mismatch("readme.install", f"dotnet add package {package_id}", "missing", rel(readme), "README install snippet must use canonical package_id")

for p in [repo_root / "docs" / "021_USAGE_NUGET.MD", repo_root / "docs" / "guides" / "003_GUIDE_PORTABLE.MD"]:
    if p.exists():
        text = p.read_text(encoding="utf-8")
        if package_id not in text:
            add_mismatch("docs.package_id", package_id, "missing", rel(p), "Docs install snippets must use canonical package_id")

# Ensure deprecated package ids are not used in install snippets.
install_targets = [readme, repo_root / "docs" / "021_USAGE_NUGET.MD", repo_root / "docs" / "guides" / "003_GUIDE_PORTABLE.MD"]
for dep in deprecated_package_ids if isinstance(deprecated_package_ids, list) else []:
    for target in install_targets:
        if not target.exists():
            continue
        text = target.read_text(encoding="utf-8")
        if dep in text:
            add_mismatch("deprecated_package_in_install_docs", "absent", dep, file_line_hit(target, dep), "Deprecated package id appears in install docs")

# Samples/tests package references.
for csproj in [repo_root / "samples" / "PortableConsumer" / "PortableConsumer.csproj", repo_root / "tests" / "PackageBacked.Tests" / "PackageBacked.Tests.csproj"]:
    if csproj.exists():
        text = csproj.read_text(encoding="utf-8")
        expected_line = f"<PackageReference Include=\"{package_id}\""
        if expected_line not in text:
            add_mismatch("package_reference", expected_line, "missing", rel(csproj), "PackageReference must use canonical package_id")

status = "pass" if len(mismatches) == 0 else "fail"
all_evidence = sorted({m["evidence"] for m in mismatches} | {c.get("evidence", "") for c in checks if c.get("evidence")})

report = {
    "schema_version": 1,
    "check_id": "naming-snt",
    "status": status,
    "canonical": {
        "canonical_name": canonical_name,
        "package_id": package_id,
        "root_namespace": root_namespace,
        "assembly_name": assembly_name,
        "repo_slug": repo_slug,
        "repo_display_name": repo_display_name,
        "repository_url": repository_url,
        "deprecated_package_ids": deprecated_package_ids,
        "ssot_file": rel(ssot_path),
    },
    "normalization_notes": notes,
    "checks": checks,
    "mismatches": mismatches,
    "evidence_paths": all_evidence,
}

summary = {
    "schema_version": 1,
    "check_id": "naming-snt",
    "status": status,
    "rule_violations": [
        {
            "rule_id": "CI-NAMING-001",
            "severity": "fail",
            "message": m["message"],
            "evidence_paths": [m["evidence"]],
        }
        for m in mismatches
    ],
    "evidence_paths": all_evidence,
    "artifacts": [
        "artifacts/naming_snt_report.json",
        "artifacts/naming_snt_report.txt",
        rel(out_path),
    ],
}

report_json_path.write_text(json.dumps(report, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
lines = [
    f"Naming SNT report status={status}",
    f"canonical_name={canonical_name}",
    f"package_id={package_id}",
    f"root_namespace={root_namespace}",
    f"assembly_name={assembly_name}",
    f"repo_slug={repo_slug}",
    f"repo_display_name={repo_display_name}",
    f"repository_url={repository_url}",
    f"deprecated_package_ids={','.join(deprecated_package_ids) if isinstance(deprecated_package_ids, list) else ''}",
    f"checks={len(checks)} mismatches={len(mismatches)}",
]
for m in mismatches:
    lines.append(f"FAIL {m['scope']} expected={m['expected']} actual={m['actual']} evidence={m['evidence']} msg={m['message']}")
report_txt_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
out_path.write_text(json.dumps(summary, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")

print(f"naming-snt status={status} mismatches={len(mismatches)} summary={out_path}")
if status != "pass":
    sys.exit(1)
PY
