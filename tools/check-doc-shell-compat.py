#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_DIR = ROOT / "docs"
SRC_DIR = ROOT / "src"
TESTS_DIR = ROOT / "tests"
ROOT_README = ROOT / "README.md"
SECURITY_FILES = (ROOT / "SECURITY.md", ROOT / "SECURITY_ASSURANCE_INDEX.md")

BASH_FENCE_START = re.compile(r"^```bash\s*$", re.IGNORECASE)
FENCE_END = re.compile(r"^```\s*$")

# Unquoted endpoint with query string is unsafe in zsh, e.g.
# gh api repos/org/repo/code-scanning/alerts?state=open --paginate
QUOTED_GH_API_QUERY_RE = re.compile(r"""['"](?:/)?repos/[^'"]*\?[^'"]*['"]""", re.IGNORECASE)
UNQUOTED_ENDPOINT_QUERY_RE = re.compile(r"""(?:^|\s)(?:/)?repos/[^\s'"]*\?[^\s'"]*""", re.IGNORECASE)

# Fragile in zsh when no match: artifacts/nuget/*.nupkg
NUPKG_GLOB_RE = re.compile(r"\bartifacts/nuget/\*\.nupkg\b")


def has_unquoted_gh_api_query(line: str) -> bool:
    if not re.search(r"\bgh\s+api\b", line, re.IGNORECASE):
        return False
    if "?" not in line or "repos/" not in line:
        return False

    if QUOTED_GH_API_QUERY_RE.search(line):
        return False

    return UNQUOTED_ENDPOINT_QUERY_RE.search(line) is not None


def collect_markdown_files() -> list[Path]:
    files: list[Path] = []
    if ROOT_README.exists():
        files.append(ROOT_README)

    for sec in SECURITY_FILES:
        if sec.exists():
            files.append(sec)

    files.extend(sorted(DOCS_DIR.rglob("*.MD")))
    files.extend(sorted(DOCS_DIR.rglob("*.md")))
    files.extend(sorted(SRC_DIR.rglob("README.md")))
    files.extend(sorted(TESTS_DIR.rglob("README.md")))
    return files


def check_file(path: Path) -> list[str]:
    errors: list[str] = []
    in_bash = False

    for idx, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw_line.rstrip("\n")

        if not in_bash and BASH_FENCE_START.match(line):
            in_bash = True
            continue

        if in_bash and FENCE_END.match(line):
            in_bash = False
            continue

        if not in_bash:
            continue

        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue

        if has_unquoted_gh_api_query(stripped):
            errors.append(
                f"{path.relative_to(ROOT)}:{idx} [DOC-SHELL-001] Unquoted gh api endpoint with query string; quote endpoint, e.g. gh api \"repos/...?...\""
            )

        if NUPKG_GLOB_RE.search(stripped):
            errors.append(
                f"{path.relative_to(ROOT)}:{idx} [DOC-SHELL-002] Fragile nupkg glob; resolve package path first (find + test -n)"
            )

    return errors


def main() -> int:
    violations: list[str] = []
    for md in collect_markdown_files():
        violations.extend(check_file(md))

    if violations:
        print("Doc shell compatibility check failed:")
        for violation in violations:
            print(f"- {violation}")
        return 1

    print("Doc shell compatibility check OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
