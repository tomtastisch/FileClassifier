#!/usr/bin/env python3
from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_DIR = ROOT / "docs"
SRC_DIR = ROOT / "src"
TESTS_DIR = ROOT / "tests"
ROOT_README = ROOT / "README.md"


@dataclass(frozen=True)
class DriftRule:
    rule_id: str
    pattern: re.Pattern[str]
    guidance: str


RULES: tuple[DriftRule, ...] = (
    DriftRule(
        rule_id="DOC-CONSISTENCY-001",
        pattern=re.compile(r"(?i)\b(?:docs/)?01_FUNCTIONS\.md\b"),
        guidance="Use docs/010_API_CORE.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-002",
        pattern=re.compile(r"(?i)\b(?:docs/)?02_ARCHITECTURE_AND_FLOWS\.md\b"),
        guidance="Use docs/020_ARCH_CORE.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-003",
        pattern=re.compile(r"(?i)\b(?:docs/)?03_REFERENCES\.md\b"),
        guidance="Use docs/references/001_REFERENCES_CORE.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-004",
        pattern=re.compile(r"(?i)\b(?:docs/)?DIN_SPECIFICATION_DE\.md\b"),
        guidance="Use docs/specs/001_SPEC_DIN.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-005",
        pattern=re.compile(r"(?i)\b(?:docs/)?guides/OPTIONS_CHANGE_GUIDE\.md\b|\bOPTIONS_CHANGE_GUIDE\.md\b"),
        guidance="Use docs/guides/001_GUIDE_OPTIONS.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-006",
        pattern=re.compile(r"(?i)\b(?:docs/)?guides/DATATYPE_EXTENSION_GUIDE\.md\b|\bDATATYPE_EXTENSION_GUIDE\.md\b"),
        guidance="Use docs/guides/002_GUIDE_DATATYPE.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-007",
        pattern=re.compile(r"(?i)\b(?:docs/)?guides/README\.md\b"),
        guidance="Use docs/guides/000_INDEX_GUIDES.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-008",
        pattern=re.compile(r"(?i)\bdocs/04_[A-Z0-9_]+\.md\b"),
        guidance="Use canonical 3-digit uppercase path under docs/ (for example docs/contracts/001_CONTRACT_HASHING.MD)",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-009",
        pattern=re.compile(r"(?i)\b(?:docs/)?04_DETERMINISTIC_HASHING_API_CONTRACT\.md\b"),
        guidance="Use docs/contracts/001_CONTRACT_HASHING.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-010",
        pattern=re.compile(r"(?i)\b(?:docs/)?04_DIN_SPECIFICATION_DE\.md\b"),
        guidance="Use docs/specs/001_SPEC_DIN.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-011",
        pattern=re.compile(r"(?i)\b(?:docs/)?04_REFERENCES\.md\b"),
        guidance="Use docs/references/001_REFERENCES_CORE.MD",
    ),
    DriftRule(
        rule_id="DOC-CONSISTENCY-012",
        pattern=re.compile(r"(?i)\b(?:docs/)?04_API_FUNCTIONS\.md\b"),
        guidance="Use docs/010_API_CORE.MD",
    ),
)


def collect_markdown_files() -> list[Path]:
    files: list[Path] = []
    if ROOT_README.exists():
        files.append(ROOT_README)
    files.extend(sorted(DOCS_DIR.rglob("*.MD")))
    files.extend(sorted(DOCS_DIR.rglob("*.md")))
    files.extend(sorted(SRC_DIR.rglob("README.md")))
    files.extend(sorted(TESTS_DIR.rglob("README.md")))
    return files


def main() -> int:
    violations: list[str] = []
    for file in collect_markdown_files():
        text = file.read_text(encoding="utf-8")
        for line_no, line in enumerate(text.splitlines(), start=1):
            for rule in RULES:
                if not rule.pattern.search(line):
                    continue
                rel = file.relative_to(ROOT)
                snippet = line.strip()
                violations.append(
                    f"{rel}:{line_no} [{rule.rule_id}] {snippet} -> {rule.guidance}"
                )

    if violations:
        print("Doc consistency check failed:")
        for violation in sorted(set(violations)):
            print(f"- {violation}")
        return 1

    print("Doc consistency check OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
