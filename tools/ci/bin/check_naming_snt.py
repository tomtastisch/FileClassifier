#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def main() -> int:
    if len(sys.argv) != 4:
        print("Usage: check_naming_snt.py <repo_root> <ssot_path> <out_path>", file=sys.stderr)
        return 2

    repo_root = Path(sys.argv[1]).resolve()
    ssot_path = Path(sys.argv[2]).resolve()
    out_path = Path(sys.argv[3]).resolve()
    report_json_path = repo_root / "artifacts" / "naming_snt_report.json"
    report_tsv_path = repo_root / "artifacts" / "naming_snt_report.tsv"

    violations: list[dict[str, str]] = []
    deprecated_hits: list[dict[str, str]] = []
    checked_paths: list[str] = []

    def rel(path: Path) -> str:
        return path.resolve().relative_to(repo_root).as_posix()

    def add_violation(scope: str, expected: str, actual: str, evidence: str, message: str) -> None:
        violations.append({
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
        add_violation("ssot_file", str(ssot_path), "missing", str(ssot_path), "SSOT file missing")
        report = {
            "schema_version": 1,
            "check_id": "naming-snt",
            "status": "fail",
            "canonical": {},
            "violations": violations,
            "deprecated_hits": deprecated_hits,
            "file_counts": {},
            "checked_paths": [],
            "mismatches": violations,
        }
        report_json_path.write_text(json.dumps(report, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
        out_path.write_text(json.dumps({
            "schema_version": 1,
            "check_id": "naming-snt",
            "status": "fail",
            "rule_violations": [{
                "rule_id": "CI-NAMING-001",
                "severity": "fail",
                "message": "SSOT file missing",
                "evidence_paths": [str(ssot_path)],
            }],
            "evidence_paths": [str(ssot_path)],
            "artifacts": ["artifacts/naming_snt_report.json", "artifacts/naming_snt_report.tsv"],
        }, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
        return 1

    ssot = json.loads(ssot_path.read_text(encoding="utf-8"))
    canonical_name = str(ssot.get("canonical_name", ""))
    package_id = str(ssot.get("package_id", ""))
    root_namespace = str(ssot.get("root_namespace", ""))
    assembly_name = str(ssot.get("assembly_name", ""))
    repo_identity = str(ssot.get("repo_identity", ssot.get("repo_slug", "")))
    repo_slug = str(ssot.get("repo_slug", repo_identity))
    repo_display_name = str(ssot.get("repo_display_name", ""))
    repository_url = str(ssot.get("repository_url", ""))
    deprecated_package_ids = ssot.get("deprecated_package_ids", [])
    namespace_decision = ssot.get("namespace_decision", {})
    target_root_namespace = str(namespace_decision.get("target_root_namespace", ""))
    docs_expected_links = ssot.get("docs_expected_links", [])

    for scope, expected, actual in [
        ("canonical_vs_package_id", canonical_name, package_id),
        ("canonical_vs_root_namespace", canonical_name, root_namespace),
        ("canonical_vs_assembly_name", canonical_name, assembly_name),
    ]:
        if expected != actual:
            add_violation(scope, expected, actual, rel(ssot_path), f"{scope} mismatch")

    if not isinstance(deprecated_package_ids, list) or len(deprecated_package_ids) == 0:
        add_violation("deprecated_package_ids", "non-empty list", str(deprecated_package_ids), rel(ssot_path), "deprecated_package_ids must be non-empty")

    if canonical_name in deprecated_package_ids:
        add_violation("deprecated_package_ids", "canonical absent", canonical_name, rel(ssot_path), "canonical_name must not be deprecated")

    remote_raw = ""
    try:
        remote_raw = subprocess.check_output(["git", "-C", str(repo_root), "remote", "get-url", "origin"], text=True).strip()
    except Exception as ex:
        add_violation("git_remote", "origin configured", "missing", ".git/config", f"cannot read origin remote: {ex}")

    normalized_remote = normalize_repo_url(remote_raw) if remote_raw else ""
    normalized_repo = normalize_repo_url(repository_url)
    if normalized_remote and normalized_repo and normalized_remote != normalized_repo:
        add_violation("repository_url.matches_origin", normalized_repo, normalized_remote, ".git/config", "repository_url differs from origin URL")

    if normalized_remote:
        m = re.search(r"/([^/]+)$", normalized_remote)
        remote_slug = m.group(1) if m else ""
        if repo_slug and remote_slug != repo_slug:
            add_violation("repo_slug.matches_remote_slug", repo_slug, remote_slug, ".git/config", "repo_slug differs from remote slug")

    project_path = repo_root / "src" / "FileTypeDetection" / "FileTypeDetectionLib.vbproj"
    checked_paths.append(rel(project_path))
    if not project_path.exists():
        add_violation("vbproj.exists", rel(project_path), "missing", rel(project_path), "vbproj missing")
    else:
        tree = ET.parse(project_path)
        proj_root = tree.getroot()
        values: dict[str, str] = {}
        for elem in proj_root.iter():
            tag = elem.tag.split("}")[-1]
            if tag in {"PackageId", "RootNamespace", "AssemblyName"} and elem.text and tag not in values:
                values[tag] = elem.text.strip()
        if values.get("PackageId", "") != package_id:
            add_violation("vbproj.PackageId", package_id, values.get("PackageId", ""), file_line_hit(project_path, "<PackageId>"), "vbproj PackageId mismatch")
        if values.get("RootNamespace", "") != root_namespace:
            add_violation("vbproj.RootNamespace", root_namespace, values.get("RootNamespace", ""), file_line_hit(project_path, "<RootNamespace>"), "vbproj RootNamespace mismatch")
        if values.get("AssemblyName", "") != assembly_name:
            add_violation("vbproj.AssemblyName", assembly_name, values.get("AssemblyName", ""), file_line_hit(project_path, "<AssemblyName>"), "vbproj AssemblyName mismatch")

    namespace_files = sorted((repo_root / "src" / "FileTypeDetection").rglob("*.vb"))
    for vb in namespace_files:
        checked_paths.append(rel(vb))
        lines = vb.read_text(encoding="utf-8").splitlines()
        for idx, line in enumerate(lines, start=1):
            m = re.match(r"^\s*Namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*$", line)
            if not m:
                continue
            ns = re.sub(r"^Global\.", "", m.group(1))
            if target_root_namespace and not ns.startswith(target_root_namespace):
                add_violation("code.namespace.prefix", target_root_namespace + "*", ns, f"{rel(vb)}:{idx}", "Namespace must start with target_root_namespace")

    scan_paths = [
        repo_root / "README.md",
        repo_root / "docs",
        repo_root / "samples",
        repo_root / "tests",
    ]

    docs_required = {"README.md"}
    if isinstance(docs_expected_links, list):
        for link in docs_expected_links:
            if not isinstance(link, str):
                continue
            marker = "/blob/main/"
            if marker in link:
                docs_required.add(link.split(marker, 1)[1])

    canonical_required_paths = set(docs_required) | {
        "samples/PortableConsumer/PortableConsumer.csproj",
        "tests/PackageBacked.Tests/PackageBacked.Tests.csproj",
    }
    for base in scan_paths:
        if not base.exists():
            continue
        files = [base] if base.is_file() else sorted(p for p in base.rglob("*") if p.is_file())
        for path in files:
            if path.suffix.lower() not in {".md", ".csproj", ".cs", ".vb", ".txt", ".json", ".yml", ".yaml"}:
                continue
            rpath = rel(path)
            checked_paths.append(rpath)
            text = path.read_text(encoding="utf-8", errors="ignore")
            if package_id not in text and rpath in canonical_required_paths:
                add_violation("canonical_reference", package_id, "missing", rpath, "Canonical package reference missing in required install/consumer file")

    install_targets = set(docs_required)
    migration_doc_rel = "docs/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD"
    for rpath in sorted(install_targets):
        path = repo_root / rpath
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        lines = text.splitlines()
        for dep in deprecated_package_ids if isinstance(deprecated_package_ids, list) else []:
            if not isinstance(dep, str) or not dep:
                continue
            in_install_snippet = False
            for line in lines:
                if dep in line and ("dotnet add package" in line or "PackageReference" in line):
                    in_install_snippet = True
                    break
            if in_install_snippet:
                hit = {
                    "id": dep,
                    "evidence": file_line_hit(path, dep),
                }
                deprecated_hits.append(hit)
                if rpath != migration_doc_rel:
                    add_violation("deprecated_id_in_install_docs", "absent", dep, hit["evidence"], "Deprecated package id appears in install docs")

    file_counts = {
        "checked_paths": len(sorted(set(checked_paths))),
        "violations": len(violations),
        "deprecated_hits": len(deprecated_hits),
    }
    status = "pass" if len(violations) == 0 else "fail"

    report = {
        "schema_version": 1,
        "check_id": "naming-snt",
        "status": status,
        "canonical": {
            "canonical_name": canonical_name,
            "package_id": package_id,
            "root_namespace": root_namespace,
            "assembly_name": assembly_name,
            "repo_identity": repo_identity,
            "repo_slug": repo_slug,
            "repo_display_name": repo_display_name,
            "repository_url": repository_url,
            "deprecated_package_ids": deprecated_package_ids,
            "ssot_file": rel(ssot_path),
        },
        "violations": violations,
        "deprecated_hits": deprecated_hits,
        "file_counts": file_counts,
        "checked_paths": sorted(set(checked_paths)),
        "mismatches": violations,
    }

    summary = {
        "schema_version": 1,
        "check_id": "naming-snt",
        "status": status,
        "rule_violations": [
            {
                "rule_id": "CI-NAMING-001",
                "severity": "fail",
                "message": v["message"],
                "evidence_paths": [v["evidence"]],
            }
            for v in violations
        ],
        "evidence_paths": sorted({v["evidence"] for v in violations}),
        "artifacts": [
            "artifacts/naming_snt_report.json",
            "artifacts/naming_snt_report.tsv",
            out_path.resolve().relative_to(repo_root).as_posix(),
        ],
    }

    report_json_path.write_text(json.dumps(report, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    out_path.write_text(json.dumps(summary, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")

    rows = ["scope\texpected\tactual\tevidence\tmessage"]
    for v in violations:
        rows.append(f"{v['scope']}\t{v['expected']}\t{v['actual']}\t{v['evidence']}\t{v['message']}")
    for d in deprecated_hits:
        rows.append(f"deprecated_hit\t{d['id']}\t{d['id']}\t{d['evidence']}\tdeprecated id hit")
    report_tsv_path.write_text("\n".join(rows) + "\n", encoding="utf-8")

    print(json.dumps(report, indent=2, ensure_ascii=True))
    return 0 if status == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main())
