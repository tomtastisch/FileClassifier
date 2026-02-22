#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path


DIM_PATTERN = re.compile(
    r"^(?P<indent>[ \t]*)Dim[ \t]+(?P<name>.+?)[ \t]+As[ \t]+(?P<type>.+?)(?:[ \t]*=[ \t]*(?P<init>.*))?$"
)


@dataclass
class DimDecl:
    line_index: int
    indent: str
    name: str
    type_name: str
    initializer: str | None
    original: str


def iter_vb_files(root: Path) -> list[Path]:
    return sorted(
        p
        for p in root.rglob("*.vb")
        if "/obj/" not in p.as_posix() and "/bin/" not in p.as_posix()
    )


def parse_dim_decl(line_index: int, line: str) -> DimDecl | None:
    if not line.strip() or line.lstrip().startswith("'"):
        return None

    match = DIM_PATTERN.match(line)
    if match is None:
        return None

    name = match.group("name").strip()
    type_name = match.group("type").strip()
    initializer = match.group("init")
    if initializer is not None:
        initializer = initializer.strip()

    # Multi-variable declarations are not part of the policy and remain untouched.
    if "," in name:
        return None

    return DimDecl(
        line_index=line_index,
        indent=match.group("indent"),
        name=name,
        type_name=type_name,
        initializer=initializer,
        original=line,
    )


def format_block(block: list[DimDecl]) -> list[str]:
    max_name = max(len(item.name) for item in block)
    max_type = max(len(item.type_name) for item in block)

    formatted: list[str] = []
    for item in block:
        row = f"{item.indent}Dim {item.name.ljust(max_name)} As {item.type_name.ljust(max_type)}"
        if item.initializer is not None:
            row += f" = {item.initializer}"
        formatted.append(row.rstrip())
    return formatted


def normalize_file(path: Path, write: bool) -> tuple[int, int]:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()

    violations = 0
    fixed = 0
    i = 0
    while i < len(lines):
        decl = parse_dim_decl(i, lines[i])
        if decl is None:
            i += 1
            continue

        block: list[DimDecl] = [decl]
        j = i + 1
        while j < len(lines):
            next_decl = parse_dim_decl(j, lines[j])
            if next_decl is None:
                break
            if next_decl.indent != decl.indent:
                break
            block.append(next_decl)
            j += 1

        if len(block) >= 2:
            expected = format_block(block)
            for idx, item in enumerate(block):
                if item.original != expected[idx]:
                    violations += 1
                    if write:
                        lines[item.line_index] = expected[idx]
                        fixed += 1

        i = j

    if write and fixed > 0:
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")

    return violations, fixed


def main() -> int:
    parser = argparse.ArgumentParser(description="Checks/enforces column alignment for VB Dim blocks.")
    parser.add_argument("--root", type=Path, default=Path("src/FileTypeDetection"))
    parser.add_argument("--write", action="store_true", help="Rewrite files in-place.")
    args = parser.parse_args()

    root = args.root.resolve()
    if not root.exists():
        print(f"Root path does not exist: {root}")
        return 1

    total_violations = 0
    total_fixed = 0
    impacted_files = 0

    for file in iter_vb_files(root):
        violations, fixed = normalize_file(file, write=args.write)
        if violations > 0:
            impacted_files += 1
            rel = file.relative_to(Path.cwd().resolve())
            print(f"{rel}: violations={violations}, fixed={fixed}")
            total_violations += violations
            total_fixed += fixed

    if total_violations == 0:
        print("VB Dim alignment check OK")
        return 0

    if args.write:
        print(
            f"VB Dim alignment fixed: files={impacted_files}, "
            f"violations={total_violations}, fixed={total_fixed}"
        )
        return 0

    print(
        f"VB Dim alignment check failed: files={impacted_files}, "
        f"violations={total_violations}"
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
