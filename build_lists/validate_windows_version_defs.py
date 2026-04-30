#!/usr/bin/env python3
import argparse
import json
import re
from pathlib import Path
from typing import Any


CVE_RE = re.compile(r"^CVE-\d{4}-\d{4,}$")
KB_RE = re.compile(r"^\d+$")
GENERATED_RE = re.compile(r"^\d{8}$")
REQUIRED_ENTRY_KEYS = {"cve", "kb", "severity", "impact"}


def fail(message: str) -> None:
    raise SystemExit(f"Invalid windows version definitions: {message}")


def require_string(value: Any, field: str) -> str:
    if not isinstance(value, str):
        fail(f"{field} must be a string")
    return value


def validate_entry(product: str, index: int, entry: Any) -> None:
    if not isinstance(entry, dict):
        fail(f"products[{product!r}][{index}] must be an object")

    keys = set(entry)
    if keys != REQUIRED_ENTRY_KEYS:
        fail(
            f"products[{product!r}][{index}] must contain exactly "
            f"{sorted(REQUIRED_ENTRY_KEYS)}, got {sorted(keys)}"
        )

    cve = require_string(entry["cve"], f"products[{product!r}][{index}].cve").strip()
    kb = require_string(entry["kb"], f"products[{product!r}][{index}].kb").strip()
    require_string(entry["severity"], f"products[{product!r}][{index}].severity")
    require_string(entry["impact"], f"products[{product!r}][{index}].impact")

    if not cve and not kb:
        fail(f"products[{product!r}][{index}] must contain a CVE or KB")
    if cve and not CVE_RE.fullmatch(cve):
        fail(f"products[{product!r}][{index}].cve is malformed: {cve!r}")
    if kb and not KB_RE.fullmatch(kb):
        fail(f"products[{product!r}][{index}].kb must contain only digits: {kb!r}")


def validate_file(path: Path, min_products: int, min_entries: int, min_supersedes: int) -> None:
    if not path.is_file():
        fail(f"{path} does not exist")
    if path.stat().st_size == 0:
        fail(f"{path} is empty")

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        fail(f"{path} is not valid JSON: {exc}")

    if not isinstance(data, dict):
        fail("top-level JSON value must be an object")
    if set(data) != {"generated", "products", "kb_supersedes"}:
        fail(f"top-level keys must be generated, products, kb_supersedes; got {sorted(data)}")

    generated = require_string(data["generated"], "generated")
    if not GENERATED_RE.fullmatch(generated):
        fail(f"generated must be YYYYMMDD, got {generated!r}")

    products = data["products"]
    if not isinstance(products, dict):
        fail("products must be an object")
    if len(products) < min_products:
        fail(f"products has {len(products)} entries, expected at least {min_products}")

    total_entries = 0
    for product, entries in products.items():
        if not isinstance(product, str) or not product.strip():
            fail("products keys must be non-empty strings")
        if not isinstance(entries, list):
            fail(f"products[{product!r}] must be a list")
        for index, entry in enumerate(entries):
            validate_entry(product, index, entry)
        total_entries += len(entries)

    if total_entries < min_entries:
        fail(f"products contain {total_entries} vulnerabilities, expected at least {min_entries}")

    kb_supersedes = data["kb_supersedes"]
    if not isinstance(kb_supersedes, dict):
        fail("kb_supersedes must be an object")
    if len(kb_supersedes) < min_supersedes:
        fail(f"kb_supersedes has {len(kb_supersedes)} entries, expected at least {min_supersedes}")

    for kb, superseded in kb_supersedes.items():
        if not isinstance(kb, str) or not KB_RE.fullmatch(kb):
            fail(f"kb_supersedes key must contain only digits: {kb!r}")
        if not isinstance(superseded, list):
            fail(f"kb_supersedes[{kb!r}] must be a list")
        for index, child in enumerate(superseded):
            if not isinstance(child, str) or not KB_RE.fullmatch(child):
                fail(f"kb_supersedes[{kb!r}][{index}] must contain only digits: {child!r}")

    print(
        f"Validated {path}: generated={generated}, "
        f"products={len(products)}, vulnerabilities={total_entries}, "
        f"supersedence_roots={len(kb_supersedes)}"
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate WinPEAS Windows version vulnerability definitions.")
    parser.add_argument(
        "path",
        nargs="?",
        default="build_lists/windows_version_exploits.json",
        type=Path,
        help="Path to the generated definitions JSON.",
    )
    parser.add_argument("--min-products", type=int, default=50)
    parser.add_argument("--min-entries", type=int, default=500)
    parser.add_argument("--min-supersedes", type=int, default=100)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    validate_file(args.path, args.min_products, args.min_entries, args.min_supersedes)


if __name__ == "__main__":
    main()
