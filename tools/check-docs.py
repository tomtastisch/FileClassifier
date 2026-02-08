#!/usr/bin/env python3
from __future__ import annotations

import re
import subprocess
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_DIR = ROOT / "docs"
SRC_FTD_DIR = ROOT / "src" / "FileTypeDetection"
TESTS_DIR = ROOT / "tests"
ROOT_README = ROOT / "README.md"
REPO = "https://github.com/tomtastisch/FileClassifier"
SCAN_EXCLUDED_DIRS = {".git", "bin", "obj", "artifacts", ".qodana", ".idea", ".vscode", "node_modules", ".tmp"}

LINK_RE = re.compile(r'(?<!\!)\[(?P<text>[^\]]+)\]\((?P<url>[^)\s]+)(?:\s+"[^"]*")?\)')
RELATIVE_RE = re.compile(
    r'\]\(\s*(?!https?://|mailto:|#)(?:\.{1,2}/|/|docs/|src/|tests/|tools/|README\.md|[^)]+\.md(?:#[^)]+)?)(?:\s+"[^"]*")?\s*\)'
)
PATH_TEXT_RE = re.compile(r'(?i)^\s*(?:\.{1,2}/|/)?(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+\s*$')
FILE_TEXT_RE = re.compile(r'(?i)^\s*[A-Za-z0-9_.-]+\.(md|mdx|vb|cs|fs|java|kt|ts|js|json|yml|yaml|toml|xml|sh|ps1|sql)\s*$')
INTERNAL_REPO_URL_RE = re.compile(
    r"^https://github\.com/tomtastisch/FileClassifier/"
    r"(?P<kind>blob|tree)/"
    r"(?P<ref>[A-Za-z0-9._-]+)/"
    r"(?P<path>[^\s)#]+)"
    r"(?:#[A-Za-z0-9\-_\.]+)?$"
)
ANCHOR_RE = re.compile(r'^#[A-Za-z0-9\-_\.]+$')
DOC_NAME_RE = re.compile(r'^[0-9]{3}_(?:[A-Z0-9]+_)*[A-Z0-9]+\.MD$')
PLAIN_URL_RE = re.compile(r'(?P<url>https?://[^\s<>"\)\]]+)')
REF_EXISTS_CACHE: dict[str, bool] = {}

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
    for path in sorted(ROOT.rglob("*")):
        if not path.is_file():
            continue
        if path.suffix not in {".md", ".MD"}:
            continue
        rel = path.relative_to(ROOT)
        if any(part in SCAN_EXCLUDED_DIRS for part in rel.parts):
            continue
        files.append(path)
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


def check_http_url(url: str, cache: dict[str, str | None]) -> str | None:
    if url in cache:
        return cache[url]

    # Commit links are immutable objects; keep deterministic and avoid flaky remote checks.
    if url.startswith(f"{REPO}/commit/"):
        cache[url] = None
        return None

    def request_once(method: str, timeout: int) -> tuple[int, str]:
        req = urllib.request.Request(url, method=method, headers={"User-Agent": "FileClassifier-LinkCheck/1.0"})
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return int(resp.getcode() or 0), str(resp.geturl())

    last_error: str | None = None
    for _ in range(3):
        try:
            code, _ = request_once("HEAD", 12)
            if code < 400:
                cache[url] = None
                return None
            last_error = f"HTTP {code}"
        except urllib.error.HTTPError as ex:
            # Fallback to GET for endpoints that don't support HEAD.
            if ex.code in {405, 403}:
                try:
                    code, _ = request_once("GET", 20)
                    if code < 400:
                        cache[url] = None
                        return None
                    last_error = f"HTTP {code}"
                except urllib.error.HTTPError as ex2:
                    last_error = f"HTTP {ex2.code}"
                except Exception as ex2:  # noqa: BLE001
                    last_error = str(ex2).splitlines()[0][:200]
            else:
                last_error = f"HTTP {ex.code}"
        except Exception as ex:  # noqa: BLE001
            last_error = str(ex).splitlines()[0][:200]
        time.sleep(0.2)

    cache[url] = last_error
    return last_error


def check_internal_repo_url(url: str, cache: dict[str, str | None]) -> str | None:
    if url in cache:
        return cache[url]

    match = INTERNAL_REPO_URL_RE.match(url)
    if not match:
        cache[url] = "invalid internal repo URL format"
        return cache[url]

    kind = match.group("kind")
    ref = match.group("ref")
    rel_path = match.group("path")

    def local_ref_exists(git_ref: str) -> bool:
        local = subprocess.run(
            ["git", "rev-parse", "--verify", "--quiet", git_ref],
            cwd=ROOT,
            capture_output=True,
            text=True,
            check=False,
        )
        return local.returncode == 0

    def ref_exists_locally_or_remote(git_ref: str) -> bool:
        if git_ref in REF_EXISTS_CACHE:
            return REF_EXISTS_CACHE[git_ref]

        if local_ref_exists(git_ref):
            REF_EXISTS_CACHE[git_ref] = True
            return True

        # CI often uses shallow checkouts without local refs like "main".
        # Fallback to remote ref existence to keep checks deterministic.
        for remote_ref in (f"refs/heads/{git_ref}", f"refs/tags/{git_ref}", f"refs/tags/{git_ref}^{{}}"):
            remote = subprocess.run(
                ["git", "ls-remote", "--exit-code", "origin", remote_ref],
                cwd=ROOT,
                capture_output=True,
                text=True,
                check=False,
            )
            if remote.returncode == 0:
                REF_EXISTS_CACHE[git_ref] = True
                return True

        REF_EXISTS_CACHE[git_ref] = False
        return False

    if not ref_exists_locally_or_remote(ref):
        cache[url] = f"ref not found ({ref})"
        return cache[url]

    # In shallow CI clones, refs like "main" may exist only remotely.
    # In that case, validate path/type against current workspace content.
    if not local_ref_exists(ref):
        path = ROOT / rel_path
        if kind == "blob":
            if not path.is_file():
                cache[url] = f"target not found in workspace ({rel_path})"
                return cache[url]
        else:
            if not path.is_dir():
                cache[url] = f"target tree not found in workspace ({rel_path})"
                return cache[url]
        cache[url] = None
        return None

    spec = f"{ref}:{rel_path}"
    exists = subprocess.run(
        ["git", "cat-file", "-e", spec],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if exists.returncode != 0:
        cache[url] = f"target not found at commit ({spec})"
        return cache[url]

    type_result = subprocess.run(
        ["git", "cat-file", "-t", spec],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if type_result.returncode != 0:
        cache[url] = f"unable to resolve object type ({spec})"
        return cache[url]

    git_type = type_result.stdout.strip()
    expected = "blob" if kind == "blob" else "tree"
    if git_type != expected:
        cache[url] = f"expected {expected}, got {git_type} ({spec})"
        return cache[url]

    cache[url] = None
    return None


def check_links_and_text(files: list[Path]) -> list[str]:
    errors: list[str] = []
    http_cache: dict[str, str | None] = {}
    internal_repo_cache: dict[str, str | None] = {}

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
                if url.startswith(REPO):
                    if url.startswith(f"{REPO}/commit/"):
                        continue
                    if INTERNAL_REPO_URL_RE.match(url):
                        internal_error = check_internal_repo_url(url, internal_repo_cache)
                        if internal_error:
                            errors.append(
                                f"broken internal repo URL in {f.relative_to(ROOT)}: {url} ({internal_error})"
                            )
                            continue
                        continue
                    http_error = check_http_url(url, http_cache)
                    if http_error:
                        errors.append(f"broken URL in {f.relative_to(ROOT)}: {url} ({http_error})")
                        continue

                http_error = check_http_url(url, http_cache)
                if http_error:
                    errors.append(f"broken URL in {f.relative_to(ROOT)}: {url} ({http_error})")
                continue

            errors.append(f"non-absolute or unsupported URL in {f.relative_to(ROOT)}: {url}")

        # Also validate plain URLs in text sections/tables.
        for pm in PLAIN_URL_RE.finditer(text):
            raw_url = pm.group("url")
            cleaned = raw_url.rstrip(".,;:")
            if cleaned.startswith("mailto:"):
                continue
            if cleaned.startswith(REPO) and cleaned.startswith(f"{REPO}/commit/"):
                continue
            if cleaned.startswith(REPO):
                if INTERNAL_REPO_URL_RE.match(cleaned):
                    internal_error = check_internal_repo_url(cleaned, internal_repo_cache)
                    if internal_error:
                        errors.append(
                            f"broken internal plain-url in {f.relative_to(ROOT)}: {cleaned} ({internal_error})"
                        )
                    continue
                http_error = check_http_url(cleaned, http_cache)
                if http_error:
                    errors.append(f"broken plain-url in {f.relative_to(ROOT)}: {cleaned} ({http_error})")
                continue
            http_error = check_http_url(cleaned, http_cache)
            if http_error:
                errors.append(f"broken plain-url in {f.relative_to(ROOT)}: {cleaned} ({http_error})")

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
        for e in sorted(set(errors)):
            print(f"- {e}")
        return 1

    print("Doc check OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
