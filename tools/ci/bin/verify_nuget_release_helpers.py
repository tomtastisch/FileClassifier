#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import xml.etree.ElementTree as ET


def cmd_derive_filename(args: argparse.Namespace) -> int:
    m = re.match(r"^(?P<id>.+)\.(?P<ver>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)\.nupkg$", args.filename)
    if not m:
        print()
        print()
        return 0
    print(m.group("id"))
    print(m.group("ver"))
    return 0


def cmd_derive_nuspec(args: argparse.Namespace) -> int:
    text = args.nuspec_xml
    try:
        root = ET.fromstring(text)

        def find_first(node: ET.Element, tag_name: str) -> str:
            for elem in node.iter():
                local = elem.tag.rsplit("}", 1)[-1]
                if local == tag_name and elem.text:
                    return elem.text.strip()
            return ""

        print(find_first(root, "id"))
        print(find_first(root, "version"))
        return 0
    except ET.ParseError:
        def by_regex(tag: str) -> str:
            m = re.search(rf"<{tag}>\s*([^<]+?)\s*</{tag}>", text, flags=re.IGNORECASE)
            return m.group(1).strip() if m else ""

        print(by_regex("id"))
        print(by_regex("version"))
        return 0


def cmd_query_search(args: argparse.Namespace) -> int:
    pkg = args.pkg_id.lower()
    ver = args.pkg_ver
    data = json.loads(args.response_json)
    registration = ""
    has_id = False
    has_ver = False

    for item in data.get("data", []):
        item_id = str(item.get("id", ""))
        if item_id.lower() != pkg:
            continue
        has_id = True
        for v in item.get("versions", []):
            if isinstance(v, dict) and str(v.get("version", "")) == ver:
                has_ver = True
                break
        if item.get("registration"):
            registration = str(item["registration"])

    if not has_id:
        print("missing_id", file=sys.stderr)
        return 2
    if not has_ver:
        print("missing_version", file=sys.stderr)
        return 3
    if not registration:
        print("missing_registration", file=sys.stderr)
        return 4
    print(registration)
    return 0


def cmd_registration_contains(args: argparse.Namespace) -> int:
    target = args.pkg_ver.lower()
    obj = json.loads(args.response_json)

    def walk(node) -> bool:
        if isinstance(node, dict):
            for k, v in node.items():
                if k.lower() == "version" and isinstance(v, str) and v.lower() == target:
                    return True
                if walk(v):
                    return True
        elif isinstance(node, list):
            for item in node:
                if walk(item):
                    return True
        return False

    return 0 if walk(obj) else 2


def cmd_emit_summary(_: argparse.Namespace) -> int:
    print(json.dumps({
        "id": os.environ.get("PKG_ID", ""),
        "version": os.environ.get("PKG_VER", ""),
        "expected": os.environ.get("EXPECTED_VERSION", ""),
        "verify_online": os.environ.get("VERIFY_ONLINE", ""),
        "require_search": os.environ.get("REQUIRE_SEARCH", ""),
        "require_registration": os.environ.get("REQUIRE_REGISTRATION", ""),
        "require_flatcontainer": os.environ.get("REQUIRE_FLATCONTAINER", ""),
        "require_v2_download": os.environ.get("REQUIRE_V2_DOWNLOAD", ""),
        "registration": os.environ.get("REGISTRATION_URL", ""),
        "search": os.environ.get("SEARCH_OK", "skipped"),
        "registration_check": os.environ.get("REGISTRATION_OK", "skipped"),
        "flatcontainer": os.environ.get("FLATCONTAINER_OK", "skipped"),
        "v2_download": os.environ.get("V2_DOWNLOAD_OK", "skipped")
    }, separators=(",", ":")))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="cmd", required=True)

    p1 = sub.add_parser("derive-filename")
    p1.add_argument("--filename", required=True)
    p1.set_defaults(func=cmd_derive_filename)

    p2 = sub.add_parser("derive-nuspec")
    p2.add_argument("--nuspec-xml", required=True)
    p2.set_defaults(func=cmd_derive_nuspec)

    p3 = sub.add_parser("query-search")
    p3.add_argument("--response-json", required=True)
    p3.add_argument("--pkg-id", required=True)
    p3.add_argument("--pkg-ver", required=True)
    p3.set_defaults(func=cmd_query_search)

    p4 = sub.add_parser("registration-contains")
    p4.add_argument("--response-json", required=True)
    p4.add_argument("--pkg-ver", required=True)
    p4.set_defaults(func=cmd_registration_contains)

    p5 = sub.add_parser("emit-summary")
    p5.set_defaults(func=cmd_emit_summary)

    args = parser.parse_args()
    try:
        return args.func(args)
    except Exception as ex:
        print(f"ERROR: {ex}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
