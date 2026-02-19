#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--file", required=True, type=Path)
    parser.add_argument("--field", required=True)
    args = parser.parse_args()

    try:
        obj = json.loads(args.file.read_text(encoding="utf-8"))
    except Exception as ex:
        print(f"ERROR: cannot read json file {args.file}: {ex}", file=sys.stderr)
        return 1

    value = obj.get(args.field, "")
    if isinstance(value, (dict, list)):
        print("")
        return 0

    print(str(value))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
