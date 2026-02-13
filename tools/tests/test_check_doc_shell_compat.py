from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[2]
CHECK_PATH = REPO_ROOT / "tools" / "check-doc-shell-compat.py"


def _load_module():
    spec = importlib.util.spec_from_file_location("check_doc_shell_compat_module", CHECK_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load module from {CHECK_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


check_shell = _load_module()


class DocShellCompatTests(unittest.TestCase):
    def test_flags_unquoted_gh_api_query_in_bash_fence(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md = root / "README.md"
            md.write_text(
                """# Demo\n\n```bash\ngh api repos/org/repo/code-scanning/alerts?state=open --paginate\n```\n""",
                encoding="utf-8",
            )

            with patch.object(check_shell, "ROOT", root):
                errors = check_shell.check_file(md)

            self.assertTrue(any("DOC-SHELL-001" in error for error in errors))

    def test_flags_fragile_nupkg_glob_in_bash_fence(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md = root / "README.md"
            md.write_text(
                """# Demo\n\n```bash\ngh attestation verify artifacts/nuget/*.nupkg --repo owner/repo\n```\n""",
                encoding="utf-8",
            )

            with patch.object(check_shell, "ROOT", root):
                errors = check_shell.check_file(md)

            self.assertTrue(any("DOC-SHELL-002" in error for error in errors))

    def test_allows_quoted_gh_api_query_and_resolved_nupkg(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md = root / "README.md"
            md.write_text(
                """# Demo\n\n```bash\ngh api \"repos/$REPO/code-scanning/alerts?state=open&per_page=100\" --paginate\nNUPKG=\"$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)\"\ntest -n \"$NUPKG\"\ngh attestation verify \"$NUPKG\" --repo owner/repo\n```\n""",
                encoding="utf-8",
            )

            with patch.object(check_shell, "ROOT", root):
                errors = check_shell.check_file(md)

            self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()
