#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys


def _fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def _get_token() -> str:
    # Fail-closed: do not attempt unauthenticated GitHub API calls.
    token = (os.environ.get("GITHUB_TOKEN", "") or os.environ.get("GH_TOKEN", "")).strip()
    if not token:
        _fail("GITHUB_TOKEN/GH_TOKEN is missing; cannot verify artifacts fail-closed.")
    return token


def _curl_get(url: str, token: str) -> bytes:
    # Use curl to match the workflow hardening requirement and to ensure non-2xx fails the job.
    cmd = [
        "curl",
        "--fail-with-body",
        "--location",
        "--silent",
        "--show-error",
        "--max-time",
        "30",
        "--header",
        "Accept: application/vnd.github+json",
        "--header",
        "X-GitHub-Api-Version: 2022-11-28",
        "--header",
        f"Authorization: Bearer {token}",
        "--user-agent",
        "fileclassifier-verify-run-artifact",
        url,
    ]
    try:
        proc = subprocess.run(cmd, check=False, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except FileNotFoundError:
        _fail("curl is not available; cannot verify artifacts fail-closed.")

    if proc.returncode != 0:
        # Keep error message bounded; body is in stdout for --fail-with-body.
        stdout = proc.stdout.decode("utf-8", errors="replace").strip()
        stderr = proc.stderr.decode("utf-8", errors="replace").strip()
        _fail(f"curl failed (exit={proc.returncode}). stderr={stderr!r} body_prefix={stdout[:400]!r}")

    return proc.stdout


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True, help="owner/repo")
    parser.add_argument("--run-id", required=True, help="GitHub Actions run id")
    parser.add_argument("--artifact-name", required=True)
    parser.add_argument("--out", required=True, help="Where to write the API response JSON")
    args = parser.parse_args()

    token = _get_token()

    repo = args.repo.strip()
    run_id = args.run_id.strip()
    if "/" not in repo:
        _fail(f"--repo must be owner/repo, got: {repo!r}")
    if not run_id.isdigit():
        _fail(f"--run-id must be numeric, got: {run_id!r}")

    url = f"https://api.github.com/repos/{repo}/actions/runs/{run_id}/artifacts"
    raw = _curl_get(url, token)

    out_path = args.out
    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    with open(out_path, "wb") as f:
        f.write(raw)

    try:
        payload = json.loads(raw.decode("utf-8"))
    except Exception as exc:
        _fail(f"artifact listing JSON invalid: {exc}")

    artifacts = payload.get("artifacts")
    if not isinstance(artifacts, list):
        _fail("artifact listing payload invalid: missing/invalid 'artifacts' list")

    want = args.artifact_name
    if not any(isinstance(a, dict) and a.get("name") == want for a in artifacts):
        _fail(f"required artifact {want!r} not found in run artifacts")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
