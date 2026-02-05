#!/usr/bin/env python3
"""Deterministic markdown link checker for repository-local links."""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path


LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")


def iter_markdown_files(repo_root: Path) -> list[Path]:
    output = subprocess.check_output(
        ["git", "ls-files", "*.md"],
        cwd=repo_root,
        text=True,
    )
    return [repo_root / line.strip() for line in output.splitlines() if line.strip()]


def is_ignored_link(link: str) -> bool:
    return (
        not link
        or link.startswith("#")
        or link.startswith("http://")
        or link.startswith("https://")
        or link.startswith("mailto:")
        or "://" in link
    )


def resolve_link_target(markdown_file: Path, raw_link: str) -> Path:
    link = raw_link.split("#", 1)[0].strip()
    if link.startswith("<") and link.endswith(">"):
        link = link[1:-1].strip()
    return (markdown_file.parent / link).resolve()


def main() -> int:
    repo_root = Path(__file__).resolve().parents[1]
    files = iter_markdown_files(repo_root)
    errors: list[str] = []

    for markdown_file in files:
        text = markdown_file.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            raw_link = match.group(1).strip()
            if is_ignored_link(raw_link):
                continue

            target = resolve_link_target(markdown_file, raw_link)
            if not target.exists():
                rel_file = markdown_file.relative_to(repo_root).as_posix()
                rel_target = (
                    target.relative_to(repo_root).as_posix()
                    if target.is_relative_to(repo_root)
                    else str(target)
                )
                errors.append(f"{rel_file} :: {raw_link} -> {rel_target}")

    if errors:
        print("Markdown link check FAILED:")
        for line in sorted(errors):
            print(f"  - {line}")
        return 1

    print("Markdown link check PASSED.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
