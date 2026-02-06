#!/usr/bin/env python3
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_DIR = ROOT / "docs"

LINK_PATTERN = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")

IGNORE_PREFIXES = ("http://", "https://", "mailto:")


def is_local_link(target: str) -> bool:
    if not target or target.startswith("#"):
        return False
    if target.startswith(IGNORE_PREFIXES):
        return False
    return True


def resolve_target(base: Path, target: str) -> Path:
    path_part = target.split("#", 1)[0].split("?", 1)[0].strip()
    if path_part.startswith("/"):
        path_part = path_part.lstrip("/")
        return ROOT / path_part
    return (base / path_part).resolve()


def check_links() -> list[str]:
    errors: list[str] = []
    files = [ROOT / "README.md"]
    if DOCS_DIR.exists():
        files.extend(DOCS_DIR.rglob("*.md"))

    for md_file in files:
        if not md_file.exists():
            continue
        text = md_file.read_text(encoding="utf-8")
        base = md_file.parent
        for match in LINK_PATTERN.finditer(text):
            target = match.group(1).strip()
            if not is_local_link(target):
                continue
            resolved = resolve_target(base, target)
            if not resolved.exists():
                errors.append(f"{md_file.relative_to(ROOT)} -> missing: {target}")
    return errors


def check_versioning_refs() -> list[str]:
    errors: list[str] = []
    versions = DOCS_DIR / "versioning" / "VERSIONS.md"
    policy = DOCS_DIR / "versioning" / "POLICY.md"

    if not versions.exists():
        errors.append("docs/versioning/VERSIONS.md is missing")
        return errors
    if not policy.exists():
        errors.append("docs/versioning/POLICY.md is missing")
        return errors

    text = versions.read_text(encoding="utf-8")
    if "docs/versioning/POLICY.md" not in text:
        errors.append("docs/versioning/VERSIONS.md does not reference docs/versioning/POLICY.md")

    return errors


def main() -> int:
    errors = []
    errors.extend(check_links())
    errors.extend(check_versioning_refs())

    if errors:
        print("Doc check failed:")
        for err in errors:
            print(f"- {err}")
        return 1

    print("Doc check OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
