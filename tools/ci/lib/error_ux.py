#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import platform
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

try:
    import tomllib
except Exception as exc:  # pragma: no cover
    print(f"FATAL: tomllib unavailable: {exc}", file=sys.stderr)
    sys.exit(2)


def _load_toml(path: Path, key: str) -> dict[str, str]:
    with path.open("rb") as f:
        data = tomllib.load(f)
    section = data.get(key)
    if not isinstance(section, dict):
        raise ValueError(f"missing [{key}] in {path.as_posix()}")
    out: dict[str, str] = {}
    for k, v in section.items():
        out[str(k)] = str(v)
    return out


def _valid_id(value: str) -> bool:
    return bool(re.fullmatch(r"\d{2}", value))


def _dotnet_version() -> str:
    try:
        proc = subprocess.run(
            ["dotnet", "--version"],
            capture_output=True,
            text=True,
            check=False,
        )
        text = (proc.stdout or proc.stderr or "").strip()
        return text if text else "unknown"
    except Exception:
        return "unknown"


def _render(template: str, values: dict[str, str]) -> str:
    return template.format(**values)


def _resolve_artifact_url(run_url: str, artifact_name: str) -> str:
    match = re.match(r"^https://github\.com/([^/]+)/([^/]+)/actions/runs/(\d+)$", run_url)
    if not match:
        raise ValueError("run_url_not_parseable")

    owner, repo, run_id = match.group(1), match.group(2), match.group(3)
    endpoint = (
        f"https://api.github.com/repos/{owner}/{repo}/actions/runs/{run_id}/artifacts"
        f"?name={urllib.parse.quote(artifact_name, safe='')}"
    )
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    headers = {
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "fileclassifier-ci-error-ux",
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"

    req = urllib.request.Request(endpoint, headers=headers)
    with urllib.request.urlopen(req, timeout=10) as resp:
        payload = json.loads(resp.read().decode("utf-8"))

    artifacts = payload.get("artifacts")
    if not isinstance(artifacts, list):
        raise ValueError("artifacts_payload_invalid")

    exact = [a for a in artifacts if isinstance(a, dict) and a.get("name") == artifact_name]
    if not exact:
        raise ValueError("artifact_not_found")

    picked = sorted(exact, key=lambda a: int(a.get("id", 0)), reverse=True)[0]
    artifact_id = picked.get("id")
    if not artifact_id:
        raise ValueError("artifact_id_missing")

    return f"https://github.com/{owner}/{repo}/actions/runs/{run_id}/artifacts/{artifact_id}"


def _fallback(
    errors: dict[str, str],
    check_id: str,
    artifact_name: str,
    artifact_url: str,
    diag_path: Path,
    reason: str,
    evidence_paths: list[str],
) -> int:
    template = errors.get("E9901", "Error mapping/artifact-link failure for check '{check_id}' ({reason}).")
    message = _render(
        template,
        {
            "check_id": check_id,
            "reason": reason,
            "rule_id": "",
            "artifact_name": artifact_name,
            "run_url": artifact_url,
        },
    )
    diag = {
        "error_code": "9901",
        "step_id": "99",
        "class_id": "01",
        "check_id": check_id,
        "rule_id": "",
        "artifact_name": artifact_name,
        "artifact_url": artifact_url,
        "message": message,
        "reason": reason,
        "timestamp_utc": dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "tool_versions": {
            "python": platform.python_version(),
            "dotnet": _dotnet_version(),
            "runner_os": os.environ.get("RUNNER_OS", "unknown"),
        },
        "evidence_paths": evidence_paths,
    }
    diag_path.parent.mkdir(parents=True, exist_ok=True)
    diag_path.write_text(json.dumps(diag, indent=2) + "\n", encoding="utf-8")

    print(f"\033[31mERROR 9901: {message}\033[0m")
    print(f"\033[34mARTIFACT {artifact_url} (artifact: {artifact_name})\033[0m")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--step-key", required=True)
    parser.add_argument("--class-key", required=True)
    parser.add_argument("--check-id", required=True)
    parser.add_argument("--rule-id", default="")
    parser.add_argument("--artifact-name", required=True)
    parser.add_argument("--run-url", default="")
    parser.add_argument("--diag-path", required=True)
    parser.add_argument("--evidence-paths", default="")
    args = parser.parse_args()

    script_dir = Path(__file__).resolve().parent
    errors_dir = script_dir.parent / "errors"
    steps_path = errors_dir / "steps.toml"
    classes_path = errors_dir / "classes.toml"
    errors_path = errors_dir / "errors.toml"
    diag_path = Path(args.diag_path)
    evidence_paths = [p for p in args.evidence_paths.split("|") if p]

    try:
        steps = _load_toml(steps_path, "steps")
        classes = _load_toml(classes_path, "classes")
        errors = _load_toml(errors_path, "errors")
    except Exception as exc:
        return _fallback(
            errors={"E9901": "Error mapping/artifact-link failure for check '{check_id}' ({reason})."},
            check_id=args.check_id,
            artifact_name=args.artifact_name,
            run_url=args.run_url or "https://github.com",
            diag_path=diag_path,
            reason=f"toml_load_failed:{exc}",
            evidence_paths=evidence_paths,
        )

    run_url = args.run_url.strip()
    if not run_url:
        return _fallback(
            errors,
            args.check_id,
            args.artifact_name,
            "https://github.com",
            diag_path,
            "missing_run_url",
            evidence_paths,
        )
    try:
        artifact_url = _resolve_artifact_url(run_url, args.artifact_name)
    except (ValueError, KeyError, urllib.error.URLError, TimeoutError) as exc:
        return _fallback(
            errors,
            args.check_id,
            args.artifact_name,
            run_url,
            diag_path,
            f"artifact_url_resolution_failed:{exc}",
            evidence_paths,
        )

    step_id = steps.get(args.step_key, "")
    class_id = classes.get(args.class_key, "")
    if not (_valid_id(step_id) and _valid_id(class_id)):
        return _fallback(
            errors,
            args.check_id,
            args.artifact_name,
            artifact_url,
            diag_path,
            f"invalid_mapping:{args.step_key}/{args.class_key}",
            evidence_paths,
        )

    error_code = f"{step_id}{class_id}"
    error_key = f"E{error_code}"
    template = errors.get(error_key, "")
    if not template:
        return _fallback(
            errors,
            args.check_id,
            args.artifact_name,
            artifact_url,
            diag_path,
            f"missing_template:{error_key}",
            evidence_paths,
        )

    values = {
        "check_id": args.check_id,
        "rule_id": args.rule_id,
        "artifact_name": args.artifact_name,
        "run_url": artifact_url,
        "reason": "",
    }
    try:
        message = _render(template, values)
    except Exception as exc:
        return _fallback(
            errors,
            args.check_id,
            args.artifact_name,
            artifact_url,
            diag_path,
            f"template_render_failed:{exc}",
            evidence_paths,
        )

    diag = {
        "error_code": error_code,
        "step_id": step_id,
        "class_id": class_id,
        "check_id": args.check_id,
        "rule_id": args.rule_id,
        "artifact_name": args.artifact_name,
        "artifact_url": artifact_url,
        "message": message,
        "timestamp_utc": dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "tool_versions": {
            "python": platform.python_version(),
            "dotnet": _dotnet_version(),
            "runner_os": os.environ.get("RUNNER_OS", "unknown"),
        },
        "evidence_paths": evidence_paths,
    }
    diag_path.parent.mkdir(parents=True, exist_ok=True)
    diag_path.write_text(json.dumps(diag, indent=2) + "\n", encoding="utf-8")

    print(f"\033[31mERROR {error_code}: {message}\033[0m")
    print(f"\033[34mARTIFACT {artifact_url} (artifact: {args.artifact_name})\033[0m")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
