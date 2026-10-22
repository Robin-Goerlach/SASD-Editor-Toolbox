#!/usr/bin/env python3
"""Read-only traceability report for the 123-entry historical routine index.

This report checks the *presence* of declared evidence, not its semantic
adequacy. It must never use a green CI run or an implemented classification
as a substitute for historical acceptance.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def build_report(root: Path) -> dict:
    """Join the historical inventory to the actual portable test manifest.

    A routine may legitimately have zero portable fixtures (for example a
    terminal-specific helper). Missing fixtures are reported, not silently
    labelled failed. Historical sign-off is deliberately always false:
    inventory classification is conceptual traceability, not acceptance.
    """
    inventory = json.loads((root / "docs/handbook-procedure-index-v1.json").read_text(encoding="utf-8"))
    manifest = json.loads((root / "conformance/v1/manifest.json").read_text(encoding="utf-8"))

    case_ids: set[str] = set()
    for case_path in manifest["caseFiles"]:
        cases = json.loads((root / case_path).read_text(encoding="utf-8"))["cases"]
        for case in cases:
            if case["id"] in case_ids:
                raise ValueError(f"Duplicate fixture ID: {case['id']}")
            case_ids.add(case["id"])

    routines = []
    seen = set()
    for entry in inventory["entries"]:
        name = entry["name"]
        if name in seen:
            raise ValueError(f"Duplicate historical routine: {name}")
        seen.add(name)
        declared = entry.get("conformanceCases", [])
        if len(declared) != len(set(declared)):
            raise ValueError(f"{name}: duplicate referenced fixtures")
        unknown = sorted(set(declared) - case_ids)
        if unknown:
            raise ValueError(f"{name}: unknown fixture IDs: {unknown}")
        evidence_path = entry["evidence"]
        if not (root / evidence_path).is_file():
            raise ValueError(f"{name}: missing evidence document {evidence_path}")
        routines.append({
            "name": name,
            "page": entry["handbookPage"],
            "classification": entry["status"],
            "evidenceDocument": evidence_path,
            "fixtureIds": declared,
            "hasPortableFixture": bool(declared),
            # Neither an indexed routine nor a passing portable fixture
            # verifies every original behavior. Human sign-off must be
            # stored separately after its actual review takes place.
            "historicallyAccepted": False,
        })

    return {
        "schemaVersion": 1,
        "historicalScope": "printed routine index only; supplemental helpers excluded",
        "totalRoutines": len(routines),
        "routinesWithPortableFixtures": sum(x["hasPortableFixture"] for x in routines),
        "routinesWithoutPortableFixtures": sum(not x["hasPortableFixture"] for x in routines),
        "historicallyAcceptedCount": 0,
        "projectParityEstablished": False,
        "routines": routines,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT,
                        help="repository root (also useful for isolated tests)")
    parser.add_argument("--output", type=Path,
                        help="optional JSON output path; defaults to stdout")
    args = parser.parse_args()
    result = json.dumps(build_report(args.root), indent=2, ensure_ascii=False) + "\n"
    if args.output is not None:
        args.output.write_text(result, encoding="utf-8")
    else:
        print(result, end="")


if __name__ == "__main__":
    main()
