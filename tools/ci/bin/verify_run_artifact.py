#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.request


def _fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True, help="owner/repo")
    parser.add_argument("--run-id", required=True, help="GitHub Actions run id")
    parser.add_argument("--artifact-name", required=True)
    parser.add_argument("--out", required=True, help="Where to write the API response JSON")
    args = parser.parse_args()

    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not token:
        _fail("GITHUB_TOKEN is missing; cannot verify artifacts fail-closed.")

    repo = args.repo.strip()
    run_id = args.run_id.strip()
    if "/" not in repo:
        _fail(f"--repo must be owner/repo, got: {repo!r}")
    if not run_id.isdigit():
        _fail(f"--run-id must be numeric, got: {run_id!r}")

    url = f"https://api.github.com/repos/{repo}/actions/runs/{run_id}/artifacts"
    req = urllib.request.Request(
        url,
        headers={
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "Authorization": f"Bearer {token}",
            "User-Agent": "fileclassifier-verify-run-artifact",
        },
        method="GET",
    )

    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            raw = resp.read()
    except (urllib.error.URLError, TimeoutError) as exc:
        _fail(f"artifact listing request failed: {exc}")

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
