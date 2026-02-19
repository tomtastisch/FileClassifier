#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET

BLUE = "\033[94m"
WHITE = "\033[97m"
GREEN = "\033[32m"
RED = "\033[31m"
RESET = "\033[0m"
DIM = "\033[2m"

CHECK = "✔"
CROSS = "✘"


def strip_param_suffix(text: str) -> str:
    value = text.strip()
    while True:
        updated = re.sub(r"\s*\([^()]*\)\s*$", "", value).strip()
        if updated == value:
            return value
        value = updated


def humanize_identifier(text: str) -> str:
    value = strip_param_suffix(text)
    value = value.replace("_", " ")
    value = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", value)
    value = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1 \2", value)
    value = re.sub(r"\s+", " ", value).strip()
    return value


def normalize_title(test_name: str, scenario: str | None) -> str:
    if scenario:
        return strip_param_suffix(scenario)
    raw = strip_param_suffix(test_name)
    if "." in raw:
        raw = raw.rsplit(".", 1)[-1]
    return humanize_identifier(raw)


def iter_step_lines(stdout: str) -> list[str]:
    if not stdout:
        return []
    lines: list[str] = []
    for raw in stdout.splitlines():
        line = raw.strip()
        if not line:
            continue
        if line.startswith("[BDD]"):
            continue
        if line.startswith("-> done:"):
            continue
        if line.startswith("--- table step argument ---"):
            continue
        if line.startswith("|"):
            continue
        if line.startswith("Standardausgabemeldungen:"):
            continue
        if re.match(r"^(Angenommen|Wenn|Dann|Und|Aber)\b", line):
            lines.append(line)
    deduped: list[str] = []
    seen: set[str] = set()
    for line in lines:
        if line not in seen:
            deduped.append(line)
            seen.add(line)
    return deduped


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: bdd_readable_from_trx.py <trx_path>", file=sys.stderr)
        return 2

    trx_path = sys.argv[1]
    root = ET.parse(trx_path).getroot()
    ns = {"t": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

    def findall(path: str):
        return root.findall(path, ns) if ns else root.findall(path)

    def find(node, path: str):
        return node.find(path, ns) if ns else node.find(path)

    results: list[tuple[str, str, list[str]]] = []
    for node in findall(".//t:UnitTestResult" if ns else ".//UnitTestResult"):
        outcome = (node.attrib.get("outcome") or "").strip()
        test_name = (node.attrib.get("testName") or "").strip()
        output = find(node, "t:Output" if ns else "Output")
        stdout = ""
        if output is not None:
            std_node = find(output, "t:StdOut" if ns else "StdOut")
            if std_node is not None and std_node.text:
                stdout = std_node.text

        scenario = None
        if stdout:
            for line in stdout.splitlines():
                l = line.strip()
                m = re.match(r"^\[BDD\]\s*Szenario startet:\s*(.+)$", l)
                if m:
                    scenario = m.group(1).strip()
                    break

        title = normalize_title(test_name, scenario)
        steps = iter_step_lines(stdout)
        results.append((title, outcome, steps))

    for title, outcome, steps in results:
        passed = outcome.lower() == "passed"
        icon = CHECK if passed else CROSS
        icon_color = GREEN if passed else RED
        end_word = "FINISHED" if passed else "FAILED"

        if not steps:
            steps = ["Test erfolgreich abgeschlossen" if passed else "Test fehlgeschlagen"]

        print(f"{DIM}────────────────────────────────────────────────────────────────{RESET}")
        print(f"{BLUE}{title}{RESET}")
        for step in steps:
            print(f"{icon_color}{icon}{RESET} {WHITE}{step}{RESET}")
        print(f"{icon_color}{end_word}{RESET}")
        print("")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
