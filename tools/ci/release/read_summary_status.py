#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main() -> int:
    # Small dedicated helper to keep bash wrappers free of inline Python blocks.
    parser = argparse.ArgumentParser()
    parser.add_argument("--summary", required=True)
    args = parser.parse_args()
    try:
        obj = json.loads(Path(args.summary).read_text(encoding="utf-8"))
    except Exception as ex:
        print(f"ERROR: {ex}", file=sys.stderr)
        return 1
    print(obj.get("status", ""))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
