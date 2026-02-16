#!/usr/bin/env python3
"""Insert or update deterministic DE/EN language switch headers in markdown docs."""

from __future__ import annotations

import argparse
import json
import os
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional, Tuple


LANG_BEGIN = "<!-- LANG_SWITCH:BEGIN -->"
LANG_END = "<!-- LANG_SWITCH:END -->"
HEADER_PATTERN = re.compile(
    r"(?ms)^<!-- LANG_SWITCH:BEGIN -->\r?\n.*?\r?\n<!-- LANG_SWITCH:END -->\r?\n?"
)
PREFIX_PATTERN = re.compile(r"^(?P<prefix>[01])(?P<nn>\d{2})_(?P<rest>.+)$")
SUFFIX_LANG_PATTERN = re.compile(r"_(?P<lang>DE|EN)\.md$", re.IGNORECASE)


@dataclass(frozen=True)
class FileRecord:
    path: Path
    rel_path: str


@dataclass
class Resolution:
    current: FileRecord
    counterpart: Path
    source: str
    status: str
    needs_map: bool
    counterpart_exists: bool
    de_target: Path
    en_target: Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Add/update language switch headers for docs markdown files."
    )
    parser.add_argument(
        "--repo-root",
        default=".",
        help="Repository root directory (default: current directory).",
    )
    parser.add_argument(
        "--docs-dir",
        default="docs",
        help="Documentation directory relative to repo root (default: docs).",
    )
    parser.add_argument(
        "--include-readme",
        action="store_true",
        help="Also include README*.md files from repository root.",
    )
    parser.add_argument(
        "--write-report-always",
        action="store_true",
        help="Always rewrite reports even when no markdown header changed.",
    )
    return parser.parse_args()


def to_rel(repo_root: Path, path: Path) -> str:
    try:
        return path.relative_to(repo_root).as_posix()
    except ValueError:
        return os.path.relpath(path, repo_root).replace(os.sep, "/")


def collect_markdown_files(
    repo_root: Path,
    docs_dir: Path,
    include_readme: bool,
) -> List[FileRecord]:
    records: List[FileRecord] = []

    for path in docs_dir.rglob("*"):
        if not path.is_file() or path.suffix.lower() != ".md":
            continue
        if path.name == "lang_switch_report.md":
            continue
        records.append(FileRecord(path=path, rel_path=to_rel(repo_root, path)))

    if include_readme:
        for path in repo_root.glob("README*.md"):
            if path.is_file():
                records.append(FileRecord(path=path, rel_path=to_rel(repo_root, path)))

    return sorted(records, key=lambda item: item.rel_path)


def load_lang_map(lang_map_path: Path) -> Dict[str, str]:
    if not lang_map_path.exists():
        return {}
    data = json.loads(lang_map_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"{lang_map_path} must contain a JSON object.")
    normalized: Dict[str, str] = {}
    for key, value in data.items():
        if not isinstance(key, str) or not isinstance(value, str):
            raise ValueError(
                f"{lang_map_path} contains non-string key/value entries: {key!r}:{value!r}"
            )
        normalized[key] = value
    return normalized


def detect_lang(path: Path) -> Optional[str]:
    prefix_match = PREFIX_PATTERN.match(path.name)
    if prefix_match:
        return "DE" if prefix_match.group("prefix") == "0" else "EN"

    suffix_match = SUFFIX_LANG_PATTERN.search(path.name)
    if suffix_match:
        return suffix_match.group("lang").upper()
    return None


def schema_counterpart(path: Path) -> Optional[Path]:
    match = PREFIX_PATTERN.match(path.name)
    if not match:
        return None
    switched_prefix = "1" if match.group("prefix") == "0" else "0"
    counterpart_name = f"{switched_prefix}{match.group('nn')}_{match.group('rest')}"
    return path.with_name(counterpart_name)


def map_counterpart(
    repo_root: Path, rel_path: str, lang_map: Dict[str, str]
) -> Optional[Path]:
    mapped = lang_map.get(rel_path)
    if not mapped:
        return None
    mapped_path = (repo_root / mapped).resolve()
    return mapped_path


def resolve_targets(
    current_path: Path, counterpart_path: Path, current_lang: Optional[str], counterpart_lang: Optional[str]
) -> Tuple[Path, Path]:
    if current_lang == "DE":
        return current_path, counterpart_path
    if current_lang == "EN":
        return counterpart_path, current_path
    if counterpart_lang == "DE":
        return counterpart_path, current_path
    if counterpart_lang == "EN":
        return current_path, counterpart_path
    # Fallback for unknown language markers.
    return current_path, counterpart_path


def resolve_file(
    repo_root: Path,
    record: FileRecord,
    lang_map: Dict[str, str],
) -> Resolution:
    schema_target = schema_counterpart(record.path)
    source = "schema"
    needs_map = False

    if schema_target is not None:
        counterpart = schema_target
    else:
        source = "map"
        mapped = map_counterpart(repo_root, record.rel_path, lang_map)
        if mapped is None:
            needs_map = True
            counterpart = record.path
        else:
            counterpart = mapped

    counterpart_exists = counterpart.exists() and counterpart.is_file()
    status = "ok"
    if needs_map:
        status = "needs_map"
    elif not counterpart_exists:
        status = "missing_counterpart"

    current_lang = detect_lang(record.path)
    counterpart_lang = detect_lang(counterpart)
    de_target, en_target = resolve_targets(
        current_path=record.path,
        counterpart_path=counterpart,
        current_lang=current_lang,
        counterpart_lang=counterpart_lang,
    )

    return Resolution(
        current=record,
        counterpart=counterpart,
        source=source,
        status=status,
        needs_map=needs_map,
        counterpart_exists=counterpart_exists,
        de_target=de_target,
        en_target=en_target,
    )


def make_relative_link(from_file: Path, to_file: Path) -> str:
    rel = os.path.relpath(to_file, from_file.parent)
    return rel.replace(os.sep, "/")


def build_header(from_file: Path, de_target: Path, en_target: Path, newline: str) -> str:
    rel_de = make_relative_link(from_file, de_target)
    rel_en = make_relative_link(from_file, en_target)
    return (
        f"{LANG_BEGIN}{newline}"
        f"[DE]({rel_de}) | [EN]({rel_en}){newline}"
        f"{LANG_END}"
    )


def normalize_with_header(path: Path, de_target: Path, en_target: Path) -> Tuple[str, bool]:
    original = path.read_text(encoding="utf-8")
    newline = "\r\n" if "\r\n" in original else "\n"
    header = build_header(path, de_target, en_target, newline)

    body = HEADER_PATTERN.sub("", original)
    body = re.sub(r"^(?:\r?\n)+", "", body)
    if body:
        updated = f"{header}{newline}{newline}{body}"
    else:
        updated = f"{header}{newline}"

    return updated, updated != original


def write_lang_map_if_needed(
    lang_map_path: Path,
    lang_map: Dict[str, str],
    missing_map_keys: List[str],
) -> bool:
    if not missing_map_keys and not lang_map_path.exists():
        return False

    changed = False
    for key in missing_map_keys:
        if key not in lang_map:
            lang_map[key] = ""
            changed = True

    if changed or not lang_map_path.exists():
        lang_map_path.parent.mkdir(parents=True, exist_ok=True)
        serialized = json.dumps(dict(sorted(lang_map.items())), indent=2, ensure_ascii=True)
        lang_map_path.write_text(f"{serialized}\n", encoding="utf-8")
        return True
    return False


def report_payload(repo_root: Path, resolutions: List[Resolution], changed_files: List[str]) -> Dict[str, object]:
    ok = [r for r in resolutions if r.status == "ok"]
    missing = [r for r in resolutions if r.status == "missing_counterpart"]
    needs_map = [r for r in resolutions if r.status == "needs_map"]

    records = []
    for item in resolutions:
        records.append(
            {
                "file": item.current.rel_path,
                "counterpart": to_rel(repo_root, item.counterpart),
                "source": item.source,
                "status": item.status,
                "counterpart_exists": item.counterpart_exists,
            }
        )

    return {
        "summary": {
            "total_files": len(resolutions),
            "changed_files": len(changed_files),
            "ok": len(ok),
            "missing_counterpart": len(missing),
            "needs_map": len(needs_map),
        },
        "changed_files": changed_files,
        "ok": [item.current.rel_path for item in ok],
        "missing_counterpart": [item.current.rel_path for item in missing],
        "needs_map": [item.current.rel_path for item in needs_map],
        "records": records,
    }


def markdown_report(
    repo_root: Path,
    payload: Dict[str, object],
    report_md_path: Path,
) -> str:
    summary = payload["summary"]
    records = payload["records"]

    header = build_header(report_md_path, report_md_path, report_md_path, "\n")
    lines = [
        header,
        "",
        "# Language Switch Report",
        "",
        "## Summary",
        f"- Total files: {summary['total_files']}",
        f"- Changed files: {summary['changed_files']}",
        f"- OK: {summary['ok']}",
        f"- Missing counterpart: {summary['missing_counterpart']}",
        f"- Needs map: {summary['needs_map']}",
        "",
        "## Records",
        "| File | Counterpart | Source | Status |",
        "| --- | --- | --- | --- |",
    ]
    for record in records:
        lines.append(
            f"| `{record['file']}` | `{record['counterpart']}` | `{record['source']}` | `{record['status']}` |"
        )

    return "\n".join(lines) + "\n"


def write_reports(
    repo_root: Path,
    payload: Dict[str, object],
    report_md_path: Path,
    report_json_path: Path,
) -> None:
    report_md_path.parent.mkdir(parents=True, exist_ok=True)
    report_json_path.parent.mkdir(parents=True, exist_ok=True)
    report_json_path.write_text(
        json.dumps(payload, indent=2, ensure_ascii=True) + "\n", encoding="utf-8"
    )
    report_md_path.write_text(
        markdown_report(repo_root, payload, report_md_path), encoding="utf-8"
    )


def main() -> int:
    args = parse_args()
    repo_root = Path(args.repo_root).resolve()
    docs_dir = (repo_root / args.docs_dir).resolve()
    lang_map_path = (docs_dir / "lang_map.json").resolve()
    report_md_path = (docs_dir / "lang_switch_report.md").resolve()
    report_json_path = (docs_dir / "lang_switch_report.json").resolve()

    if not docs_dir.exists() or not docs_dir.is_dir():
        raise SystemExit(f"Documentation directory not found: {docs_dir}")

    files = collect_markdown_files(
        repo_root=repo_root, docs_dir=docs_dir, include_readme=args.include_readme
    )
    lang_map = load_lang_map(lang_map_path)

    resolutions: List[Resolution] = []
    changed_files: List[str] = []
    missing_map_keys: List[str] = []

    for record in files:
        resolution = resolve_file(repo_root=repo_root, record=record, lang_map=lang_map)
        resolutions.append(resolution)

        if resolution.needs_map:
            missing_map_keys.append(record.rel_path)

        updated, changed = normalize_with_header(
            path=record.path,
            de_target=resolution.de_target,
            en_target=resolution.en_target,
        )
        if changed:
            record.path.write_text(updated, encoding="utf-8")
            changed_files.append(record.rel_path)

    map_changed = write_lang_map_if_needed(lang_map_path, lang_map, sorted(set(missing_map_keys)))
    payload = report_payload(repo_root=repo_root, resolutions=resolutions, changed_files=changed_files)

    report_exists = report_md_path.exists() and report_json_path.exists()
    should_write_report = (
        bool(changed_files)
        or map_changed
        or args.write_report_always
        or not report_exists
    )

    if should_write_report:
        write_reports(
            repo_root=repo_root,
            payload=payload,
            report_md_path=report_md_path,
            report_json_path=report_json_path,
        )

    print(
        json.dumps(
            {
                "total_files": len(files),
                "changed_files": len(changed_files),
                "missing_counterpart": payload["summary"]["missing_counterpart"],
                "needs_map": payload["summary"]["needs_map"],
                "report_written": should_write_report,
            },
            ensure_ascii=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
