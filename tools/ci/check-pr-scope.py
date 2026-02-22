#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import json
import subprocess
import sys
from pathlib import Path


def run_git(repo_root: Path, args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
    )


def base_ref_exists(repo_root: Path, base_ref: str) -> bool:
    result = run_git(repo_root, ["rev-parse", "--verify", "--quiet", f"{base_ref}^{{commit}}"])
    return result.returncode == 0


def collect_pr_diff_files(repo_root: Path, base_ref: str) -> set[str]:
    result = run_git(repo_root, ["diff", "--name-only", "--diff-filter=ACMR", f"{base_ref}...HEAD"])
    if result.returncode != 0:
        return set()
    return {line.strip() for line in result.stdout.splitlines() if line.strip()}


def parse_status_line(line: str) -> str | None:
    # Porcelain v1 examples:
    #  M path
    # A  path
    # R  old -> new
    # ?? path
    if not line:
        return None

    if line.startswith("?? "):
        return line[3:].strip() or None

    if len(line) < 4:
        return None

    payload = line[3:].strip()
    if not payload:
        return None

    if " -> " in payload:
        return payload.split(" -> ", 1)[1].strip() or None

    return payload


def collect_worktree_files(repo_root: Path) -> set[str]:
    result = run_git(repo_root, ["status", "--porcelain=v1", "--untracked-files=all"])
    if result.returncode != 0:
        return set()

    files: set[str] = set()
    for raw in result.stdout.splitlines():
        path = parse_status_line(raw.rstrip("\n"))
        if path:
            files.add(path)
    return files


def load_manifest(manifest_path: Path) -> list[str]:
    patterns: list[str] = []
    for raw in manifest_path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        patterns.append(line)
    return patterns


def matches_any(path: str, patterns: list[str]) -> bool:
    for pattern in patterns:
        if fnmatch.fnmatch(path, pattern):
            return True
        # convenience: allow directory-prefix patterns ending with '/'
        if pattern.endswith("/") and path.startswith(pattern):
            return True
    return False


def main() -> int:
    parser = argparse.ArgumentParser(description="Deterministic PR scope guard.")
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--base-ref", default="origin/main")
    parser.add_argument("--mode", choices=["combined", "pr-diff", "working-tree"], default="combined")
    parser.add_argument("--out", default="")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    manifest_path = Path(args.manifest).resolve()

    if not manifest_path.is_file():
        print(f"PR scope manifest missing: {manifest_path}", file=sys.stderr)
        return 2

    patterns = load_manifest(manifest_path)
    if not patterns:
        print(f"PR scope manifest has no active patterns: {manifest_path}", file=sys.stderr)
        return 2

    changed_files: set[str] = set()

    if args.mode in {"combined", "pr-diff"}:
        if base_ref_exists(repo_root, args.base_ref):
            changed_files.update(collect_pr_diff_files(repo_root, args.base_ref))
        elif args.mode == "pr-diff":
            print(f"Base ref not found for pr-diff mode: {args.base_ref}", file=sys.stderr)
            return 2

    if args.mode in {"combined", "working-tree"}:
        changed_files.update(collect_worktree_files(repo_root))

    changed = sorted(changed_files)
    unmatched = [path for path in changed if not matches_any(path, patterns)]

    result = {
        "base_ref": args.base_ref,
        "mode": args.mode,
        "manifest": str(manifest_path.relative_to(repo_root)),
        "changed_count": len(changed),
        "unmatched_count": len(unmatched),
        "changed_files": changed,
        "unmatched_files": unmatched,
    }

    if args.out:
        out_path = Path(args.out).resolve()
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(result, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")

    if unmatched:
        print("PR scope check failed: out-of-scope changes detected.")
        for path in unmatched:
            print(f"- {path}")
        return 1

    print(f"PR scope check passed: {len(changed)} changed files, all within manifest scope.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
