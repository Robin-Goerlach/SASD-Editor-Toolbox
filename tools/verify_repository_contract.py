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
    case_count = 0

    for relative in case_files:
        if not isinstance(relative, str) or not relative:
            raise ContractError("Every conformance case file path must be a non-empty string")
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

            for field in ("area", "initial", "operation", "expect"):
                if field not in case:
                    raise ContractError(f"Case '{case_id}' is missing '{field}'")

            if not isinstance(case["area"], str) or not case["area"]:
                raise ContractError(f"Case '{case_id}' requires a non-empty string area")
            if not isinstance(case["initial"], dict):
                raise ContractError(f"Case '{case_id}' initial state must be an object")
            if not isinstance(case["expect"], dict):
                raise ContractError(f"Case '{case_id}' expected state must be an object")

            operation = case["operation"]
            if not isinstance(operation, dict) or not isinstance(operation.get("id"), str) or not operation["id"]:
                raise ContractError(f"Case '{case_id}' requires operation.id")

            case_count += 1

    return case_count


def main() -> int:
    try:
        implementation_ids = verify_implementations()
        case_count = verify_conformance()
    except ContractError as exc:
        print(f"Repository contract FAILED: {exc}", file=sys.stderr)
        return 1

    print(
        "Repository contract OK: "
        f"{len(implementation_ids)} implementation lanes "
        f"({', '.join(implementation_ids)}), {case_count} V1 conformance cases."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
