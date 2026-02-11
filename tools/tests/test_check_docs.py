from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[2]
CHECK_DOCS_PATH = REPO_ROOT / "tools" / "check-docs.py"


def _load_check_docs_module():
    spec = importlib.util.spec_from_file_location("check_docs_module", CHECK_DOCS_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load module from {CHECK_DOCS_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


check_docs = _load_check_docs_module()


class CheckInternalRepoUrlDeterminismTests(unittest.TestCase):
    def setUp(self) -> None:
        check_docs.REF_EXISTS_CACHE.clear()
        check_docs.REF_COMMIT_CACHE.clear()

    def test_main_blob_uses_workspace_and_skips_git_ref_resolution(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            target = root / "docs" / "demo.MD"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("# demo\n", encoding="utf-8")

            url = "https://github.com/tomtastisch/FileClassifier/blob/main/docs/demo.MD"
            cache: dict[str, str | None] = {}

            with patch.object(check_docs, "ROOT", root):
                with patch.object(
                    check_docs.subprocess,
                    "run",
                    side_effect=AssertionError("subprocess.run must not be called for mutable refs"),
                ):
                    error = check_docs.check_internal_repo_url(url, cache)

            self.assertIsNone(error)
            self.assertIn(url, cache)
            self.assertIsNone(cache[url])

    def test_main_blob_missing_file_reports_workspace_error(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            url = "https://github.com/tomtastisch/FileClassifier/blob/main/docs/missing.MD"
            cache: dict[str, str | None] = {}

            with patch.object(check_docs, "ROOT", root):
                error = check_docs.check_internal_repo_url(url, cache)

            self.assertEqual("target not found in workspace (docs/missing.MD)", error)
            self.assertIn(url, cache)
            self.assertEqual(error, cache[url])

    def test_non_allowlisted_named_ref_fails_closed_when_ref_not_present(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            target = root / "docs" / "demo.MD"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("# demo\n", encoding="utf-8")
            url = "https://github.com/tomtastisch/FileClassifier/blob/maim/docs/demo.MD"
            cache: dict[str, str | None] = {}

            with patch.object(check_docs, "ROOT", root):
                with patch.object(check_docs.subprocess, "run") as run_mock:
                    run_mock.return_value.returncode = 1
                    run_mock.return_value.stdout = ""
                    run_mock.return_value.stderr = ""
                    error = check_docs.check_internal_repo_url(url, cache)

            self.assertEqual("named ref not found locally (maim)", error)
            self.assertIn(url, cache)
            self.assertEqual(error, cache[url])
            self.assertGreaterEqual(run_mock.call_count, 1)


if __name__ == "__main__":
    unittest.main()
