#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from typing import Any


def _fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def _get_token() -> str:
    token = (os.environ.get("GITHUB_TOKEN", "") or os.environ.get("GH_TOKEN", "")).strip()
    if not token:
        _fail("GITHUB_TOKEN/GH_TOKEN is missing; cannot call GitHub API fail-closed.")
    return token


def _curl_json(method: str, url: str, token: str, payload_path: str | None = None) -> Any:
    cmd = [
        "curl",
        "--fail-with-body",
        "--location",
        "--silent",
        "--show-error",
        "--max-time",
        "30",
        "--request",
        method,
        "--header",
        "Accept: application/vnd.github+json",
        "--header",
        "X-GitHub-Api-Version: 2022-11-28",
        "--header",
        f"Authorization: Bearer {token}",
        "--user-agent",
        "fileclassifier-github-api",
        url,
    ]
    if payload_path is not None:
        cmd.extend(["--header", "Content-Type: application/json"])
        cmd.extend(["--data-binary", f"@{payload_path}"])

    try:
        proc = subprocess.run(cmd, check=False, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except FileNotFoundError:
        _fail("curl is not available; cannot call GitHub API fail-closed.")

    if proc.returncode != 0:
        stdout = proc.stdout.decode("utf-8", errors="replace").strip()
        stderr = proc.stderr.decode("utf-8", errors="replace").strip()
        _fail(f"curl failed (exit={proc.returncode}). stderr={stderr!r} body_prefix={stdout[:400]!r}")

    raw = proc.stdout
    try:
        return json.loads(raw.decode("utf-8"))
    except Exception as exc:
        _fail(f"GitHub API response JSON invalid: {exc}")
    raise AssertionError("unreachable")


def _api_base(repo: str) -> str:
    repo = repo.strip()
    if "/" not in repo:
        _fail(f"--repo must be owner/repo, got: {repo!r}")
    return f"https://api.github.com/repos/{repo}"


def _get_pr_files(repo: str, pr: int) -> list[str]:
    token = _get_token()
    base = _api_base(repo)
    files: list[str] = []
    page = 1
    while True:
        url = f"{base}/pulls/{pr}/files?per_page=100&page={page}"
        payload = _curl_json("GET", url, token)
        if not isinstance(payload, list):
            _fail("PR files payload invalid: expected list")
        if not payload:
            break
        for item in payload:
            if not isinstance(item, dict):
                _fail("PR files payload invalid: list item not object")
            name = item.get("filename")
            if not isinstance(name, str) or not name:
                _fail("PR files payload invalid: missing/invalid filename")
            files.append(name)
        if len(payload) < 100:
            break
        page += 1
        if page > 50:
            _fail("PR files pagination exceeded 50 pages; refusing to continue")
    return files


def _get_issue_labels(repo: str, issue: int) -> list[str]:
    token = _get_token()
    base = _api_base(repo)
    url = f"{base}/issues/{issue}"
    payload = _curl_json("GET", url, token)
    if not isinstance(payload, dict):
        _fail("Issue payload invalid: expected object")
    labels = payload.get("labels")
    if labels is None:
        # GitHub API should return labels, but fail-closed if not present.
        _fail("Issue payload invalid: missing labels")
    if not isinstance(labels, list):
        _fail("Issue payload invalid: labels not list")
    out: list[str] = []
    for l in labels:
        if not isinstance(l, dict):
            _fail("Issue payload invalid: label item not object")
        name = l.get("name")
        if not isinstance(name, str) or not name:
            _fail("Issue payload invalid: label name missing/invalid")
        out.append(name)
    return out


def _get_pr_title(repo: str, pr: int) -> str:
    token = _get_token()
    base = _api_base(repo)
    url = f"{base}/pulls/{pr}"
    payload = _curl_json("GET", url, token)
    if not isinstance(payload, dict):
        _fail("PR payload invalid: expected object")
    title = payload.get("title")
    if not isinstance(title, str) or not title:
        _fail("PR payload invalid: title missing/invalid")
    return title


def _put_issue_labels(repo: str, issue: int, payload_path: str) -> None:
    token = _get_token()
    base = _api_base(repo)
    if not os.path.isfile(payload_path):
        _fail(f"--payload not found: {payload_path!r}")
    url = f"{base}/issues/{issue}/labels"
    payload = _curl_json("PUT", url, token, payload_path=payload_path)
    if not isinstance(payload, list):
        # API returns label objects list on success for this endpoint.
        _fail("PUT labels response invalid: expected list")


def main() -> int:
    parser = argparse.ArgumentParser(prog="github_api.py")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_files = sub.add_parser("pr-files", help="Return JSON array of PR filenames")
    p_files.add_argument("--repo", required=True, help="owner/repo")
    p_files.add_argument("--pr", required=True, type=int)

    p_labels = sub.add_parser("issue-labels", help="Return JSON array of issue label names")
    p_labels.add_argument("--repo", required=True, help="owner/repo")
    p_labels.add_argument("--issue", required=True, type=int)
    p_labels.add_argument("--sort", action="store_true", help="Sort label names deterministically")

    p_title = sub.add_parser("pr-title", help="Return PR title as a single line (raw string)")
    p_title.add_argument("--repo", required=True, help="owner/repo")
    p_title.add_argument("--pr", required=True, type=int)

    p_put = sub.add_parser("put-issue-labels", help="PUT labels payload to /issues/{n}/labels")
    p_put.add_argument("--repo", required=True, help="owner/repo")
    p_put.add_argument("--issue", required=True, type=int)
    p_put.add_argument("--payload", required=True, help="Path to JSON payload file")

    args = parser.parse_args()

    if args.cmd == "pr-files":
        files = _get_pr_files(args.repo, args.pr)
        sys.stdout.write(json.dumps(files, separators=(",", ":")))
        return 0

    if args.cmd == "issue-labels":
        labels = _get_issue_labels(args.repo, args.issue)
        if args.sort:
            labels = sorted(labels)
        sys.stdout.write(json.dumps(labels, separators=(",", ":")))
        return 0

    if args.cmd == "pr-title":
        title = _get_pr_title(args.repo, args.pr)
        sys.stdout.write(title)
        return 0

    if args.cmd == "put-issue-labels":
        _put_issue_labels(args.repo, args.issue, args.payload)
        return 0

    _fail(f"unknown command: {args.cmd!r}")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())

