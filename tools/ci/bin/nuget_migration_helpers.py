#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def cmd_read_ssot(args: argparse.Namespace) -> int:
    obj = json.loads(Path(args.ssot).read_text(encoding="utf-8"))
    canon = str(obj.get("package_id", ""))
    dep = obj.get("deprecated_package_ids", [])
    if not isinstance(dep, list):
        dep = []
    print(canon)
    for item in dep:
        print(str(item))
    return 0


def cmd_extract_versions(args: argparse.Namespace) -> int:
    obj = json.loads(args.versions_json)
    for v in obj.get("versions", []):
        if isinstance(v, str) and v.strip():
            print(v.strip())
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="cmd", required=True)

    p1 = sub.add_parser("read-ssot")
    p1.add_argument("--ssot", required=True)
    p1.set_defaults(func=cmd_read_ssot)

    p2 = sub.add_parser("extract-versions")
    p2.add_argument("--versions-json", required=True)
    p2.set_defaults(func=cmd_extract_versions)

    args = parser.parse_args()
    try:
        return args.func(args)
    except Exception as ex:
        print(f"ERROR: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
