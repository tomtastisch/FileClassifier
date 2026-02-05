#!/usr/bin/env python3
"""Deterministic markdown link checker for repository-local links."""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote


LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
HEADING_PATTERN = re.compile(r"^\s{0,3}(#{1,6})\s+(.*?)\s*$")


def iter_markdown_files(repo_root: Path) -> list[Path]:
    output = subprocess.check_output(
        ["git", "ls-files", "*.md"],
        cwd=repo_root,
        text=True,
    )
    return [repo_root / line.strip() for line in output.splitlines() if line.strip()]


def normalize_link(link: str) -> str:
    normalized = link.strip()
    if normalized.startswith("<") and normalized.endswith(">"):
        normalized = normalized[1:-1].strip()
    return normalized


def is_ignored_link(link: str) -> bool:
    return (
        not link
        or link.startswith("http://")
        or link.startswith("https://")
        or link.startswith("mailto:")
        or "://" in link
    )


def split_link_target(link: str) -> tuple[str, str]:
    if "#" not in link:
        return link, ""
    path_part, anchor = link.split("#", 1)
    return path_part, unquote(anchor).strip().lower()


def slugify_heading(text: str) -> str:
    lowered = text.strip().lower()
    lowered = re.sub(r"<[^>]+>", "", lowered)
    normalized = []
    for ch in lowered:
        if ch.isalnum() or ch in {" ", "-", "_"}:
            normalized.append(ch)
    slug = "".join(normalized).replace("_", "")
    slug = re.sub(r"\s+", "-", slug)
    slug = re.sub(r"-+", "-", slug).strip("-")
    return slug


def collect_markdown_anchors(markdown_file: Path) -> set[str]:
    anchors: set[str] = set()
    seen: dict[str, int] = {}
    in_fence = False

    for line in markdown_file.read_text(encoding="utf-8").splitlines():
        stripped = line.lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue

        match = HEADING_PATTERN.match(line)
        if not match:
            continue

        heading_text = re.sub(r"\s+#+\s*$", "", match.group(2)).strip()
        base_slug = slugify_heading(heading_text)
        if not base_slug:
            continue

        count = seen.get(base_slug, 0)
        seen[base_slug] = count + 1
        anchors.add(base_slug if count == 0 else f"{base_slug}-{count}")

    return anchors


def resolve_link_target(markdown_file: Path, path_part: str) -> Path:
    return (markdown_file.parent / path_part).resolve()


def main() -> int:
    repo_root = Path(__file__).resolve().parents[1]
    files = iter_markdown_files(repo_root)
    errors: list[str] = []
    anchor_cache: dict[Path, set[str]] = {}

    for markdown_file in files:
        text = markdown_file.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            raw_link = normalize_link(match.group(1))
            if is_ignored_link(raw_link):
                continue

            path_part, anchor = split_link_target(raw_link)
            target = resolve_link_target(markdown_file, path_part) if path_part else markdown_file.resolve()
            if not target.exists():
                rel_file = markdown_file.relative_to(repo_root).as_posix()
                rel_target = (
                    target.relative_to(repo_root).as_posix()
                    if target.is_relative_to(repo_root)
                    else str(target)
                )
                errors.append(f"{rel_file} :: {raw_link} -> {rel_target}")
                continue

            if not anchor:
                continue

            if target.suffix.lower() != ".md":
                rel_file = markdown_file.relative_to(repo_root).as_posix()
                rel_target = target.relative_to(repo_root).as_posix() if target.is_relative_to(repo_root) else str(target)
                errors.append(f"{rel_file} :: {raw_link} -> anchor target is not markdown ({rel_target})")
                continue

            anchors = anchor_cache.setdefault(target, collect_markdown_anchors(target))
            if anchor not in anchors:
                rel_file = markdown_file.relative_to(repo_root).as_posix()
                rel_target = target.relative_to(repo_root).as_posix() if target.is_relative_to(repo_root) else str(target)
                errors.append(f"{rel_file} :: {raw_link} -> missing anchor '{anchor}' in {rel_target}")

    if errors:
        print("Markdown link check FAILED:")
        for line in sorted(errors):
            print(f"  - {line}")
        return 1

    print("Markdown link check PASSED.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
