#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_DIR = ROOT / "docs"
SRC_FTD_DIR = ROOT / "src" / "FileTypeDetection"
ROOT_README = ROOT / "README.md"
REPO = "https://github.com/tomtastisch/FileClassifier"

LINK_RE = re.compile(r'(?<!\!)\[(?P<text>[^\]]+)\]\((?P<url>[^)\s]+)(?:\s+"[^"]*")?\)')
RELATIVE_RE = re.compile(
    r'\]\(\s*(?!https?://|mailto:|#)(?:\.{1,2}/|/|docs/|src/|tests/|tools/|README\.md|[^)]+\.md(?:#[^)]+)?)(?:\s+"[^"]*")?\s*\)'
)
PATH_TEXT_RE = re.compile(r'(?i)^\s*(?:\.{1,2}/|/)?(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+\s*$')
FILE_TEXT_RE = re.compile(r'(?i)^\s*[A-Za-z0-9_.-]+\.(md|mdx|vb|cs|fs|java|kt|ts|js|json|yml|yaml|toml|xml|sh|ps1|sql)\s*$')
ALLOWED_REPO_URL_RE = re.compile(r'^https://github\.com/tomtastisch/FileClassifier/(blob|tree)/[0-9a-f]{7,40}/[^\s)#]+(?:#[A-Za-z0-9\-_\.]+)?$')
ANCHOR_RE = re.compile(r'^#[A-Za-z0-9\-_\.]+$')
DOC_NAME_RE = re.compile(r'^[0-9]{3}_[A-Z0-9]+_[A-Z0-9]+\.MD$')


DOC_TYPES_REQUIRE_DIAGRAM = (
    "_API_",
    "_ARCH_",
    "_FLOW_",
)
README_HEADINGS = (
    "## 1. Zweck",
    "## 2. Inhalt",
    "## 3. API und Verhalten",
    "## 4. Verifikation",
    "## 5. Diagramm",
    "## 6. Verweise",
)
RELEVANT_SUFFIXES = {".vb", ".cs", ".json", ".md", ".MD"}
EXCLUDED_DIRS = {"bin", "obj"}

def collect_targets() -> list[Path]:
    files: list[Path] = []
    if ROOT_README.exists():
        files.append(ROOT_README)
    files.extend(sorted(DOCS_DIR.rglob("*.MD")))
    files.extend(sorted(SRC_FTD_DIR.rglob("README.md")))
    return files

def check_docs_naming() -> list[str]:
    errors: list[str] = []
    for f in sorted(DOCS_DIR.rglob("*")):
        if not f.is_file():
            continue
        if f.suffix not in {".MD", ".md"}:
            continue
        if f.name.lower() == "readme.md":
            errors.append(f"docs contains forbidden README: {f.relative_to(ROOT)}")
            continue
        if f.suffix != ".MD":
            errors.append(f"docs markdown must be uppercase .MD: {f.relative_to(ROOT)}")
            continue
        if not DOC_NAME_RE.match(f.name):
            errors.append(f"docs filename does not match XXX_WORT_WORT.MD: {f.relative_to(ROOT)}")
    return errors

def check_links_and_text(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for f in files:
        text = f.read_text(encoding="utf-8")

        if RELATIVE_RE.search(text):
            errors.append(f"relative link detected in {f.relative_to(ROOT)}")

        for m in LINK_RE.finditer(text):
            ltxt = m.group("text").strip()
            url = m.group("url").strip()

            if PATH_TEXT_RE.match(ltxt):
                errors.append(f"path-like link text in {f.relative_to(ROOT)}: '{ltxt}'")
            if FILE_TEXT_RE.match(ltxt):
                errors.append(f"filename-like link text in {f.relative_to(ROOT)}: '{ltxt}'")

            if ANCHOR_RE.match(url):
                continue
            if url.startswith("mailto:"):
                continue
            if url.startswith("http://") or url.startswith("https://"):
                if url.startswith(f"{REPO}/commit/"):
                    continue
                if url.startswith(REPO):
                    if not ALLOWED_REPO_URL_RE.match(url):
                        errors.append(f"invalid internal repo URL in {f.relative_to(ROOT)}: {url}")
                        continue
                    path_part = url.split("/", 7)[7]
                    rel_path = path_part.split("#", 1)[0]
                    if rel_path.startswith("README.md"):
                        rel_path = "README.md"
                    target = ROOT / rel_path
                    if not target.exists():
                        errors.append(f"repo URL points to missing path in {f.relative_to(ROOT)}: {rel_path}")
                continue

            errors.append(f"non-absolute or unsupported URL in {f.relative_to(ROOT)}: {url}")

    return errors

def check_diagrams(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for f in files:
        if f.suffix != ".MD":
            continue
        name = f.name
        if any(t in name for t in DOC_TYPES_REQUIRE_DIAGRAM):
            text = f.read_text(encoding="utf-8")
            if "```mermaid" not in text:
                errors.append(f"diagram required but missing in {f.relative_to(ROOT)}")
    return errors

def check_readme_coverage_and_template() -> list[str]:
    errors: list[str] = []
    for d in sorted(SRC_FTD_DIR.rglob("*")):
        if not d.is_dir():
            continue
        if any(part.startswith(".") for part in d.parts):
            continue
        if any(part in EXCLUDED_DIRS for part in d.parts):
            continue

        has_relevant = any(
            f.is_file() and f.suffix in RELEVANT_SUFFIXES and f.name != "README.md"
            for f in d.iterdir()
        )
        if not has_relevant:
            continue

        readme = d / "README.md"
        if not readme.exists():
            errors.append(f"missing README.md in content directory: {d.relative_to(ROOT)}")
            continue

        text = readme.read_text(encoding="utf-8")
        for h in README_HEADINGS:
            if h not in text:
                errors.append(f"README template heading missing in {readme.relative_to(ROOT)}: {h}")
        section = text.split("## 5. Diagramm", 1)[1] if "## 5. Diagramm" in text else ""
        if "```mermaid" not in section and "N/A" not in section:
            errors.append(f"README diagram section requires mermaid or N/A in {readme.relative_to(ROOT)}")

    return errors

def main() -> int:
    errors: list[str] = []
    files = collect_targets()
    errors.extend(check_docs_naming())
    errors.extend(check_links_and_text(files))
    errors.extend(check_diagrams(files))
    errors.extend(check_readme_coverage_and_template())

    if errors:
        print("Doc check failed:")
        for e in errors:
            print(f"- {e}")
        return 1

    print("Doc check OK")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
