#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import subprocess
import sys


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--service", required=True)
    parser.add_argument("--account", default=os.environ.get("USER", ""))
    args = parser.parse_args()

    try:
      result = subprocess.run(
          ["security", "find-generic-password", "-a", args.account, "-s", args.service, "-w"],
          check=False,
          capture_output=True,
          text=True,
          timeout=5,
      )
      if result.returncode == 0:
          print(result.stdout.strip())
    except Exception:
      return 0
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
