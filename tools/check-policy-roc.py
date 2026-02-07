#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
RULES = ROOT / "tools" / "ci" / "policies" / "rules"
REPO = "https://github.com/tomtastisch/FileClassifier"
LINK_RE = re.compile(r'(?<!\!)\[(?P<text>[^\]]+)\]\((?P<url>[^)\s]+)(?:\s+"[^"]*")?\)')
RULE_URL_RE = re.compile(r'^https://github\.com/tomtastisch/FileClassifier/blob/[0-9a-f]{7,40}/tools/ci/policies/rules/.+\.(?:yml|yaml)(?:#[A-Za-z0-9\-_\.]+)?$')


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="artifacts/policy_roc_matrix.tsv")
    args = parser.parse_args()

    policy_docs = sorted([p for p in DOCS.rglob("*.MD") if "POLICY" in p.name])
    rule_files = sorted([p.resolve() for p in RULES.glob("*.y*ml")])

    mappings: list[tuple[str, str]] = []
    policy_to_rules: dict[Path, set[Path]] = {p.resolve(): set() for p in policy_docs}

    for policy in policy_docs:
        text = policy.read_text(encoding="utf-8")
        for m in LINK_RE.finditer(text):
            url = m.group("url")
            if not RULE_URL_RE.match(url):
                continue
            path_part = url.split("/blob/", 1)[1].split("/", 1)[1].split("#", 1)[0]
            rule_path = (ROOT / path_part).resolve()
            policy_to_rules[policy.resolve()].add(rule_path)
            mappings.append((str(policy.relative_to(ROOT)), str(rule_path.relative_to(ROOT))))

    referenced_rules = set()
    for rs in policy_to_rules.values():
        referenced_rules.update(rs)

    orphan_policies = sorted([p for p, rs in policy_to_rules.items() if not rs])
    orphan_rules = sorted([r for r in rule_files if r not in referenced_rules])

    out = ROOT / args.out
    out.parent.mkdir(parents=True, exist_ok=True)
    lines = ["policy_doc\trule_file\tstatus"]
    for p, r in sorted(mappings):
        lines.append(f"{p}\t{r}\tmapped")
    for p in orphan_policies:
        lines.append(f"{p.relative_to(ROOT)}\t-\torphan_policy")
    for r in orphan_rules:
        lines.append(f"-\t{r.relative_to(ROOT)}\torphan_rule")
    lines.append(f"summary\torphan_policies={len(orphan_policies)};orphan_rules={len(orphan_rules)}\tresult")
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(f"policy_docs={len(policy_docs)} rule_files={len(rule_files)} orphan_policies={len(orphan_policies)} orphan_rules={len(orphan_rules)}")
    return 0 if not orphan_policies and not orphan_rules else 1


if __name__ == "__main__":
    raise SystemExit(main())
