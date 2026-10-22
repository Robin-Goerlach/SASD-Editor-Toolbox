"""Unit tests for the read-only historical handbook traceability report.

The tiny synthetic inventories keep these failure cases independent from
the actual 123-entry inventory. The repository contract separately checks
the real handbook's complete page range and classifications.
"""
import json
import tempfile
import unittest
from pathlib import Path

from report_handbook_traceability import build_report


class TraceabilityReportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "docs" / "en").mkdir(parents=True)
        (self.root / "conformance" / "v1" / "cases").mkdir(parents=True)
        (self.root / "docs" / "en" / "proof.md").write_text("proof", encoding="utf-8")
        self._json("conformance/v1/manifest.json", {
            "caseFiles": ["conformance/v1/cases/cases.json"]})
        self._json("conformance/v1/cases/cases.json", {
            "cases": [{"id": "one"}, {"id": "two"}]})
        self._json("docs/handbook-procedure-index-v1.json", {
            "entries": [self.entry("A", ["one"]), self.entry("B", [])]})

    def _json(self, path, obj):
        (self.root / path).write_text(json.dumps(obj), encoding="utf-8")

    @staticmethod
    def entry(name, cases):
        return {"name": name, "handbookPage": 118,
                "status": "implemented", "evidence": "docs/en/proof.md",
                "conformanceCases": cases}

    def test_reports_linked_and_unlinked_without_claiming_parity(self):
        report = build_report(self.root)
        self.assertEqual(report["totalRoutines"], 2)
        self.assertEqual(report["routinesWithPortableFixtures"], 1)
        self.assertEqual(report["routinesWithoutPortableFixtures"], 1)
        self.assertFalse(report["projectParityEstablished"])
        self.assertTrue(all(not r["historicallyAccepted"] for r in report["routines"]))

    def test_unknown_fixture_is_rejected(self):
        self._json("docs/handbook-procedure-index-v1.json",
                   {"entries": [self.entry("A", ["nonexistent"])]})
        with self.assertRaisesRegex(ValueError, "unknown fixture"):
            build_report(self.root)

    def test_duplicate_routine_is_rejected(self):
        self._json("docs/handbook-procedure-index-v1.json",
                   {"entries": [self.entry("A", []), self.entry("A", [])]})
        with self.assertRaisesRegex(ValueError, "Duplicate historical routine"):
            build_report(self.root)

    def test_duplicate_fixture_reference_is_rejected(self):
        self._json("docs/handbook-procedure-index-v1.json",
                   {"entries": [self.entry("A", ["one", "one"])]})
        with self.assertRaisesRegex(ValueError, "duplicate referenced fixtures"):
            build_report(self.root)


if __name__ == "__main__":
    unittest.main()
