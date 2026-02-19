#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--labels-json", required=True)
    parser.add_argument("--prefix", required=True)
    args = parser.parse_args()

    try:
        labels = json.loads(args.labels_json)
    except Exception as ex:
        print(f"ERROR: {ex}", file=sys.stderr)
        return 1

    count = sum(1 for label in labels if isinstance(label, str) and label.startswith(args.prefix))
    print(count)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
