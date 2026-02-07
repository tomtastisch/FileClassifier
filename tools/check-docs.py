#!/usr/bin/env python3
from __future__ import annotations

import re
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

MARKDOWN_LINK_RE = re.compile(r"!?\[[^\]]*]\(([^)]+)\)")
MARKDOWN_HTML_LINK_RE = re.compile(r"""(?:href|src)=["']([^"']+)["']""", re.IGNORECASE)
MARKDOWN_HEADING_RE = re.compile(r"^(#{1,6})\s+(.*)$")
PLAIN_URL_RE = re.compile(r"""(?P<url>https?://[^\s<>"')\]]+)""")

TEXT_FILE_SUFFIXES = {
    ".md",
    ".txt",
    ".rst",
    ".json",
    ".yaml",
    ".yml",
    ".toml",
    ".xml",
    ".cs",
    ".vb",
    ".fs",
    ".sh",
    ".ps1",
    ".py",
    ".js",
    ".ts",
    ".csproj",
    ".vbproj",
    ".sln",
    ".props",
    ".targets",
}

SKIP_DIR_NAMES = {".git", ".idea", ".qodana", ".tmp", "bin", "obj", "qodana-results", "artifacts"}
SKIP_URL_PREFIXES = ("mailto:", "tel:")


def iter_text_files() -> list[Path]:
    files: list[Path] = []
    for path in sorted(ROOT.rglob("*")):
        if not path.is_file():
            continue
        if any(part in SKIP_DIR_NAMES for part in path.parts):
            continue
        if path.suffix.lower() in TEXT_FILE_SUFFIXES:
            files.append(path)
    return files


def try_read_text(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return None


def github_anchor_slug(heading: str) -> str:
    normalized = unicodedata.normalize("NFKD", heading.strip().lower())
    cleaned_chars: list[str] = []
    for ch in normalized:
        if ch.isalnum() or ch in {" ", "-"}:
            cleaned_chars.append(ch)
    collapsed = "".join(cleaned_chars)
    collapsed = "-".join(part for part in collapsed.split() if part)
    while "--" in collapsed:
        collapsed = collapsed.replace("--", "-")
    return collapsed.strip("-")


def markdown_anchors(path: Path) -> set[str]:
    text = try_read_text(path)
    if text is None:
        return set()

    counts: dict[str, int] = {}
    anchors: set[str] = set()
    for line in text.splitlines():
        match = MARKDOWN_HEADING_RE.match(line)
        if not match:
            continue
        heading = re.sub(r"\s+#+\s*$", "", match.group(2).strip())
        slug = github_anchor_slug(heading)
        if not slug:
            continue
        count = counts.get(slug, 0)
        counts[slug] = count + 1
        if count == 0:
            anchors.add(slug)
        else:
            anchors.add(f"{slug}-{count}")
    return anchors


def parse_markdown_targets(text: str) -> list[str]:
    targets: list[str] = []
    for match in MARKDOWN_LINK_RE.finditer(text):
        target = match.group(1).strip()
        if target.startswith("<") and target.endswith(">"):
            target = target[1:-1]
        if " " in target and not target.startswith(("http://", "https://", "#", "/")):
            target = target.split(" ", 1)[0]
        if target:
            targets.append(target)
    for match in MARKDOWN_HTML_LINK_RE.finditer(text):
        target = match.group(1).strip()
        if target:
            targets.append(target)
    return targets


def resolve_local_target(base: Path, target: str) -> tuple[Path, str]:
    path_part, _, fragment = target.partition("#")
    path_part = urllib.parse.unquote(path_part.strip())
    if path_part.startswith("/"):
        resolved = (ROOT / path_part.lstrip("/")).resolve()
    elif path_part == "":
        resolved = base.resolve()
    else:
        resolved = (base.parent / path_part).resolve()
    return resolved, fragment


def check_http_url(url: str, cache: dict[str, str | None]) -> str | None:
    if url in cache:
        return cache[url]

    def req(method: str, timeout: int) -> tuple[int, str]:
        request = urllib.request.Request(url, method=method, headers={"User-Agent": "FileClassifier-LinkCheck/1.0"})
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return int(response.getcode() or 0), str(response.geturl())

    error: str | None = None
    try:
        code, _ = req("HEAD", 12)
        if code >= 400:
            error = f"HTTP {code}"
    except urllib.error.HTTPError as ex:
        if ex.code not in {405, 403}:
            error = f"HTTP {ex.code}"
    except Exception as ex:  # noqa: BLE001
        error = str(ex).splitlines()[0][:200]

    if error is not None:
        try:
            code, _ = req("GET", 20)
            if code >= 400:
                error = f"HTTP {code}"
            else:
                error = None
        except urllib.error.HTTPError as ex:
            error = f"HTTP {ex.code}"
        except Exception as ex:  # noqa: BLE001
            error = str(ex).splitlines()[0][:200]

    cache[url] = error
    return error


def check_versioning_refs() -> list[str]:
    errors: list[str] = []
    versions = ROOT / "docs" / "versioning" / "VERSIONS.md"
    policy = ROOT / "docs" / "versioning" / "POLICY.md"

    if not versions.exists():
        errors.append("docs/versioning/VERSIONS.md is missing")
        return errors
    if not policy.exists():
        errors.append("docs/versioning/POLICY.md is missing")
        return errors

    text = try_read_text(versions) or ""
    if "docs/versioning/POLICY.md" not in text:
        errors.append("docs/versioning/VERSIONS.md does not reference docs/versioning/POLICY.md")
    return errors


def check_links() -> list[str]:
    errors: list[str] = []
    files = iter_text_files()

    markdown_files = [path for path in files if path.suffix.lower() == ".md"]
    anchor_cache = {md.resolve(): markdown_anchors(md) for md in markdown_files}
    http_cache: dict[str, str | None] = {}

    for path in files:
        rel = path.relative_to(ROOT)
        text = try_read_text(path)
        if text is None:
            continue

        if path.suffix.lower() == ".md":
            for target in parse_markdown_targets(text):
                if not target or target.startswith("#"):
                    resolved = path.resolve()
                    fragment = target[1:] if target.startswith("#") else ""
                    if fragment:
                        anchors = anchor_cache.get(resolved, set())
                        if fragment not in anchors:
                            errors.append(f"{rel} -> missing anchor: #{fragment}")
                    continue
                if target.startswith(SKIP_URL_PREFIXES):
                    continue
                if target.startswith(("http://", "https://")):
                    http_error = check_http_url(target, http_cache)
                    if http_error:
                        errors.append(f"{rel} -> broken url: {target} ({http_error})")
                    continue

                resolved, fragment = resolve_local_target(path, target)
                if not resolved.exists():
                    errors.append(f"{rel} -> missing path: {target}")
                    continue
                if fragment and resolved.suffix.lower() == ".md":
                    anchors = anchor_cache.get(resolved.resolve(), set())
                    if fragment not in anchors:
                        errors.append(f"{rel} -> missing anchor: {target}")

        for match in PLAIN_URL_RE.finditer(text):
            url = match.group("url")
            if url.startswith(SKIP_URL_PREFIXES):
                continue
            http_error = check_http_url(url, http_cache)
            if http_error:
                errors.append(f"{rel} -> broken plain-url: {url} ({http_error})")

    return sorted(set(errors))


def main() -> int:
    errors: list[str] = []
    errors.extend(check_links())
    errors.extend(check_versioning_refs())

    if errors:
        print("Doc check failed:")
        for err in sorted(set(errors)):
            print(f"- {err}")
        return 1

    print("Doc check OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
