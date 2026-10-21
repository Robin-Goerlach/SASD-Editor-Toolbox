#!/usr/bin/env python3
"""Validate the repository's language lanes and shared conformance metadata.

This is intentionally dependency-free so every GitHub runner and developer
machine with Python 3 can validate the repository before any native language
toolchain is installed.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
IMPLEMENTATIONS_FILE = ROOT / "implementations.json"
CONFORMANCE_MANIFEST = ROOT / "conformance" / "v1" / "manifest.json"
HANDBOOK_INVENTORY = ROOT / "docs" / "handbook-procedure-index-v1.json"
HANDBOOK_SUPPLEMENT = ROOT / "docs" / "handbook-supplemental-symbols-v1.json"


class ContractError(RuntimeError):
    """Raised when repository-wide multi-language invariants are violated."""


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise ContractError(f"Missing required file: {path.relative_to(ROOT)}") from exc
    except json.JSONDecodeError as exc:
        raise ContractError(
            f"Invalid JSON in {path.relative_to(ROOT)}: line {exc.lineno}, column {exc.colno}: {exc.msg}"
        ) from exc


def require_repo_path(value: str, context: str) -> Path:
    path = ROOT / value
    if not path.exists():
        raise ContractError(f"{context} points to missing path: {value}")
    return path


def verify_implementations() -> list[str]:
    data = load_json(IMPLEMENTATIONS_FILE)
    if data.get("formatVersion") != 1:
        raise ContractError("implementations.json must use formatVersion 1")

    contract = data.get("contract")
    if not isinstance(contract, str):
        raise ContractError("implementations.json requires a string 'contract' path")
    require_repo_path(contract, "implementations.json contract")

    implementations = data.get("implementations")
    if not isinstance(implementations, list) or not implementations:
        raise ContractError("implementations.json requires a non-empty 'implementations' array")

    ids: list[str] = []
    for entry in implementations:
        if not isinstance(entry, dict):
            raise ContractError("Every implementation entry must be an object")

        implementation_id = entry.get("id")
        if not isinstance(implementation_id, str) or not implementation_id:
            raise ContractError("Every implementation requires a non-empty string 'id'")
        if implementation_id in ids:
            raise ContractError(f"Duplicate implementation id: {implementation_id}")
        ids.append(implementation_id)

        for field in ("language", "status", "source", "tests", "samples"):
            value = entry.get(field)
            if not isinstance(value, str) or not value:
                raise ContractError(f"Implementation '{implementation_id}' requires string '{field}'")

        expected = {
            "source": f"src/{implementation_id}",
            "tests": f"tests/{implementation_id}",
            "samples": f"samples/{implementation_id}",
        }
        for field, expected_path in expected.items():
            if entry[field] != expected_path:
                raise ContractError(
                    f"Implementation '{implementation_id}' {field} must be '{expected_path}', got '{entry[field]}'"
                )
            require_repo_path(entry[field], f"Implementation '{implementation_id}' {field}")

        ci_path = entry.get("ci")
        if ci_path is not None:
            if not isinstance(ci_path, str) or not ci_path:
                raise ContractError(f"Implementation '{implementation_id}' ci must be a non-empty string")
            require_repo_path(ci_path, f"Implementation '{implementation_id}' ci")

    return ids


def verify_conformance() -> int:
    manifest = load_json(CONFORMANCE_MANIFEST)
    if manifest.get("formatVersion") != 1:
        raise ContractError("conformance/v1/manifest.json must use formatVersion 1")
    if manifest.get("contractVersion") != "v1":
        raise ContractError("conformance/v1/manifest.json must target contractVersion 'v1'")

    contract = manifest.get("contract")
    if not isinstance(contract, str):
        raise ContractError("Conformance manifest requires a string 'contract' path")
    require_repo_path(contract, "Conformance contract")

    case_files = manifest.get("caseFiles")
    if not isinstance(case_files, list) or not case_files:
        raise ContractError("Conformance manifest requires a non-empty 'caseFiles' array")

    seen_ids: set[str] = set()
    seen_case_files: set[str] = set()
    case_count = 0

    for relative in case_files:
        if not isinstance(relative, str) or not relative:
            raise ContractError("Every conformance case file path must be a non-empty string")
        # The .NET adapter packages the contents of the shared v1/cases/
        # directory into its test output. Only files in this subtree can be
        # reached without bypassing that layout or traversing parent folders.
        prefix = "conformance/v1/cases/"
        if not relative.startswith(prefix) or any(
            part in ("", ".", "..") for part in relative.split("/")
        ) or "\\" in relative:
            raise ContractError(f"Invalid conformance case file path: {relative}")
        if relative in seen_case_files:
            raise ContractError(f"Duplicate conformance case file: {relative}")
        seen_case_files.add(relative)
        path = require_repo_path(relative, "Conformance case file")
        data = load_json(path)

        if data.get("formatVersion") != 1:
            raise ContractError(f"{relative} must use formatVersion 1")
        if data.get("contractVersion") != "v1":
            raise ContractError(f"{relative} must target contractVersion 'v1'")

        cases = data.get("cases")
        if not isinstance(cases, list) or not cases:
            raise ContractError(f"{relative} requires a non-empty 'cases' array")

        for case in cases:
            if not isinstance(case, dict):
                raise ContractError(f"{relative} contains a non-object case")

            case_id = case.get("id")
            if not isinstance(case_id, str) or not case_id:
                raise ContractError(f"{relative} contains a case without a non-empty string id")
            if case_id in seen_ids:
                raise ContractError(f"Duplicate conformance case id: {case_id}")
            seen_ids.add(case_id)

            for field in ("area", "initial", "expect"):
                if field not in case:
                    raise ContractError(f"Case '{case_id}' is missing '{field}'")

            if not isinstance(case["area"], str) or not case["area"]:
                raise ContractError(f"Case '{case_id}' requires a non-empty string area")
            if not isinstance(case["initial"], dict):
                raise ContractError(f"Case '{case_id}' initial state must be an object")
            if not isinstance(case["expect"], dict) or not case["expect"]:
                # A fixture without any expected observable result can run
                # without testing behavior. It must not count as V1 evidence.
                raise ContractError(f"Case '{case_id}' requires a non-empty expected state")

            # Preserve all original single-operation cases, but allow a
            # mutually exclusive multi-command sequence for stateful behavior.
            has_single = "operation" in case
            has_sequence = "operations" in case
            if has_single == has_sequence:
                raise ContractError(
                    f"Case '{case_id}' requires exactly one of operation or operations"
                )
            operations = [case["operation"]] if has_single else case["operations"]
            if not isinstance(operations, list) or not operations:
                raise ContractError(f"Case '{case_id}' operations must be nonempty")
            for step, operation in enumerate(operations):
                if not isinstance(operation, dict) or not isinstance(operation.get("id"), str) or not operation["id"]:
                    raise ContractError(f"Case '{case_id}' step {step} requires operation.id")
                if "commandSucceeded" in operation and type(operation["commandSucceeded"]) is not bool:
                    raise ContractError(f"Case '{case_id}' step {step} expected result must be boolean")

            case_count += 1

    return case_count


def verify_handbook_inventory() -> tuple[int, int, int]:
    """Check historical inventory integrity, not behavioral equivalence.

    The printed handbook's index has one description per page from 118 to 240.
    This contract check protects traceability from accidental omissions while
    leaving historical meaning to documentation and regression-test review.
    """
    data = load_json(HANDBOOK_INVENTORY)
    if not isinstance(data, dict) or data.get("formatVersion") != 1:
        raise ContractError("Handbook index needs formatVersion 1")

    if data.get("sourcePages") != {"first": 118, "last": 240}:
        raise ContractError("Handbook index must target printed pages 118..240")

    entries = data.get("entries")
    if not isinstance(entries, list) or len(entries) != 123:
        raise ContractError("Handbook index must contain all 123 routine entries")

    allowed = {
        "implemented", "modern-replacement", "host-sample",
        "not-applicable", "documented-divergence", "needs-review",
    }
    # Cross-reference only actual case IDs listed in the shared manifest.
    # This is a traceability check, not a claim that the cited case exhausts
    # every historical edge case of the procedure.
    manifest = load_json(CONFORMANCE_MANIFEST)
    portable_ids: set[str] = set()
    for case_file in manifest["caseFiles"]:
        case_data = load_json(require_repo_path(case_file, "Conformance case file"))
        portable_ids.update(case["id"] for case in case_data["cases"])

    seen: set[str] = set()
    pending = 0
    linked_rows = 0
    for page, entry in enumerate(entries, start=118):
        if not isinstance(entry, dict):
            raise ContractError(f"Handbook index page {page}: non-object entry")
        name = entry.get("name")
        if not isinstance(name, str) or not name.isidentifier():
            raise ContractError(f"Handbook index page {page}: invalid name")
        if name in seen:
            raise ContractError(f"Duplicate handbook routine: {name}")
        seen.add(name)
        if type(entry.get("handbookPage")) is not int or entry["handbookPage"] != page:
            raise ContractError(f"{name}: expected printed page {page}")
        if entry.get("status") not in allowed:
            raise ContractError(f"{name}: unknown classification")
        evidence = entry.get("evidence")
        if not isinstance(evidence, str) or not evidence.startswith("docs/en/"):
            raise ContractError(f"{name}: invalid evidence reference")
        # Never let a JSON path escape the repository with parent components.
        if Path(evidence).is_absolute() or ".." in Path(evidence).parts:
            raise ContractError(f"{name}: evidence path escapes repository")
        require_repo_path(evidence, f"Handbook entry {name}")
        note = entry.get("note")
        if not isinstance(note, str) or not note.strip():
            raise ContractError(f"{name}: missing classification rationale")
        if entry["status"] == "needs-review":
            pending += 1

        # Optional executable references must be real, distinct portable
        # case IDs. Evidence remains opt-in so missing coverage is visible
        # instead of being replaced with invented tests or false confidence.
        evidence_cases = entry.get("conformanceCases")
        if evidence_cases is not None:
            if not isinstance(evidence_cases, list) or not evidence_cases:
                raise ContractError(f"{name}: conformanceCases must be a non-empty list")
            if any(not isinstance(case, str) or case not in portable_ids
                   for case in evidence_cases):
                raise ContractError(f"{name}: conformanceCases contains an unknown case ID")
            if len(set(evidence_cases)) != len(evidence_cases):
                raise ContractError(f"{name}: duplicate conformance case reference")
            linked_rows += 1

    require_repo_path("docs/en/HANDBOOK-PROCEDURE-INDEX-AUDIT.md", "Handbook audit EN")
    require_repo_path("docs/de/HANDBUCH-PROZEDURENINDEX-AUDIT.md", "Handbook audit DE")
    return len(entries), pending, linked_rows


def verify_handbook_supplement() -> int:
    """Validate named handbook symbols omitted from the PRINTED index.

    The original 123 entries remain untouched and ordered by printed page.
    A separate, explicitly non-exhaustive supplement captures routines,
    extension hooks and MicroStar examples mentioned elsewhere in the book.
    Reject duplicate names across inventories: otherwise a future revision
    could silently count an indexed routine as a newly discovered feature.
    Status describes engineering traceability, NOT historical parity.
    """
    data = load_json(HANDBOOK_SUPPLEMENT)
    if not isinstance(data, dict) or data.get("formatVersion") != 1:
        raise ContractError("Handbook supplement needs formatVersion 1")
    if data.get("scope") != "non-exhaustive":
        raise ContractError("Handbook supplement must declare non-exhaustive scope")

    entries = data.get("entries")
    if not isinstance(entries, list) or not entries:
        raise ContractError("Handbook supplement needs a non-empty entries array")

    original = load_json(HANDBOOK_INVENTORY)
    printed_names = {entry["name"] for entry in original["entries"]}
    seen: set[str] = set()
    allowed_status = {
        "implemented", "modern-replacement", "host-replacement",
        "sample-missing", "example-only", "needs-review",
    }
    for entry in entries:
        if not isinstance(entry, dict):
            raise ContractError("Non-object handbook supplement entry")
        name = entry.get("name")
        if not isinstance(name, str) or not name.isidentifier():
            raise ContractError("Invalid handbook supplement symbol name")
        if name in printed_names or name in seen:
            raise ContractError(f"Duplicate handbook symbol across inventories: {name}")
        seen.add(name)
        page = entry.get("handbookPage")
        if type(page) is not int or not 1 <= page <= 244:
            raise ContractError(f"{name}: handbookPage must be a printed page in 1..244")
        if entry.get("status") not in allowed_status:
            raise ContractError(f"{name}: invalid supplement status")
        if not isinstance(entry.get("layer"), str) or not entry["layer"].strip():
            raise ContractError(f"{name}: missing historical role/layer")
        if not isinstance(entry.get("note"), str) or not entry["note"].strip():
            raise ContractError(f"{name}: missing explanation")
        evidence = entry.get("evidence")
        if not isinstance(evidence, str) or not evidence.strip():
            raise ContractError(f"{name}: missing evidence path")
        require_repo_path(evidence, f"Supplemental handbook symbol {name}")

    require_repo_path("docs/en/HANDBOOK-SUPPLEMENT-V1.md", "Handbook supplement EN")
    require_repo_path("docs/de/HANDBUCH-ERGAENZUNG-V1.md", "Handbook supplement DE")
    return len(entries)


def main() -> int:
    try:
        implementation_ids = verify_implementations()
        case_count = verify_conformance()
        handbook_count, pending, linked_rows = verify_handbook_inventory()
        supplement_count = verify_handbook_supplement()
    except ContractError as exc:
        print(f"Repository contract FAILED: {exc}", file=sys.stderr)
        return 1

    print(
        "Repository contract OK: "
        f"{len(implementation_ids)} implementation lanes "
        f"({', '.join(implementation_ids)}), {case_count} V1 conformance cases, "
        f"{handbook_count} handbook routines ({pending} pending review, "
        f"{linked_rows} linked to portable test cases), "
        f"{supplement_count} separately inventoried handbook symbols."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
