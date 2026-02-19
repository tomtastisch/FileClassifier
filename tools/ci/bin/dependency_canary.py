#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.request
from pathlib import Path


def load_config(config_path: Path) -> dict:
    return json.loads(config_path.read_text(encoding="utf-8"))


def get_dependency(config: dict, name: str) -> dict:
    for dep in config.get("dependencies", []):
        if dep.get("name") == name:
            return dep
    raise KeyError(name)


def read_current_version(packages_file: Path, dependency: str) -> str:
    text = packages_file.read_text(encoding="utf-8")
    pattern = re.compile(
        rf'(<PackageVersion Include="{re.escape(dependency)}" Version=")([^"]+)(" />)'
    )
    match = pattern.search(text)
    if not match:
        raise ValueError(f"dependency '{dependency}' not found in {packages_file}")
    return match.group(2)


def resolve_latest_stable_version(dependency: str) -> str:
    url = f"https://api.nuget.org/v3-flatcontainer/{dependency.lower()}/index.json"
    with urllib.request.urlopen(url, timeout=30) as response:
        payload = json.loads(response.read().decode("utf-8"))
    versions = [v for v in payload.get("versions", []) if "-" not in v]
    if not versions:
        raise ValueError(f"no stable version found for {dependency}")
    return versions[-1]


def apply_version(packages_file: Path, dependency: str, target_version: str) -> bool:
    text = packages_file.read_text(encoding="utf-8")
    pattern = re.compile(
        rf'(<PackageVersion Include="{re.escape(dependency)}" Version=")([^"]+)(" />)'
    )
    match = pattern.search(text)
    if not match:
        raise ValueError(f"dependency '{dependency}' not found in {packages_file}")

    if match.group(2) == target_version:
        return False

    updated = pattern.sub(rf"\g<1>{target_version}\g<3>", text, count=1)
    packages_file.write_text(updated, encoding="utf-8")
    return True


def run_prepare(args: argparse.Namespace) -> int:
    config = load_config(args.config)
    dep = get_dependency(config, args.dependency)
    current = read_current_version(args.packages_file, args.dependency)

    if args.requested == "latest":
        target = resolve_latest_stable_version(args.dependency)
    else:
        target = args.requested

    changed = apply_version(args.packages_file, args.dependency, target)
    print(f"CURRENT_VERSION={current}")
    print(f"TARGET_VERSION={target}")
    print(f"UPDATED={'1' if changed else '0'}")
    print(f"TEST_FILTER={dep.get('test_filter', '')}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="cmd", required=True)

    prepare = sub.add_parser("prepare")
    prepare.add_argument("--dependency", required=True)
    prepare.add_argument("--requested", default="latest")
    prepare.add_argument("--config", type=Path, required=True)
    prepare.add_argument("--packages-file", type=Path, required=True)
    prepare.set_defaults(func=run_prepare)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        return args.func(args)
    except KeyError as ex:
        print(f"ERROR: unknown dependency in config: {ex}", file=sys.stderr)
        return 2
    except Exception as ex:  # fail-closed for CI scripting
        print(f"ERROR: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
