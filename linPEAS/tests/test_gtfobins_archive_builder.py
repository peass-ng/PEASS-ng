"""One-download GTFOBins builder and offline completeness fixtures."""

import io
import json
import sys
import tarfile
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

import requests


LINPEAS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(LINPEAS))
from builder.src.linpeasBuilder import LinpeasBuilder  # noqa: E402


class FakeResponse:
    def __init__(self, content):
        self.content = content
        self.closed = False
        self.chunks_yielded = 0

    def raise_for_status(self):
        pass

    def iter_content(self, chunk_size):
        for offset in range(0, len(self.content), chunk_size):
            self.chunks_yielded += 1
            yield self.content[offset:offset + chunk_size]

    def close(self):
        self.closed = True


class GtfobinsArchiveBuilderTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with (LINPEAS / "builder/gtfobins_snapshot.json").open() as handle:
            cls.snapshot = json.load(handle)

    def archive_from_snapshot(self):
        labels = {}
        for category in ("sudo", "suid", "capabilities"):
            for name in self.snapshot[category]:
                labels.setdefault(name, []).append(category)
        output = io.BytesIO()
        with tarfile.open(fileobj=output, mode="w:gz") as archive:
            for name in sorted(labels):
                payload = ("\n".join(category + ":" for category in labels[name]) + "\n").encode()
                entry = tarfile.TarInfo("repo/_gtfobins/" + name)
                entry.size = len(payload)
                archive.addfile(entry, io.BytesIO(payload))
        return output.getvalue()

    def test_single_archive_request_matches_complete_offline_snapshot(self):
        archive = self.archive_from_snapshot()
        builder = object.__new__(LinpeasBuilder)
        response = FakeResponse(archive)
        with patch("builder.src.linpeasBuilder.requests.get", return_value=response) as get:
            online = builder._LinpeasBuilder__get_gtfobins_lists()
        self.assertTrue(response.closed)
        self.assertTrue(get.call_args.kwargs["stream"])
        get.assert_called_once()
        self.assertEqual((3, 8), get.call_args.kwargs["timeout"])

        with patch("builder.src.linpeasBuilder.requests.get", side_effect=requests.Timeout) as get:
            offline = builder._LinpeasBuilder__get_gtfobins_lists()
        get.assert_called_once()
        self.assertEqual(online, offline)
        self.assertGreater(len(online[0]), 185)
        self.assertGreater(len(online[1]), 250)
        self.assertGreater(len(online[2]), 2)
        self.assertIn("[^a-zA-Z0-9]R([[:space:]]*[,]|$)", online[1])

    def test_rejects_incomplete_or_corrupt_archive_before_fallback(self):
        builder = object.__new__(LinpeasBuilder)
        with patch("builder.src.linpeasBuilder.requests.get", return_value=FakeResponse(b"bad")) as get:
            lists = builder._LinpeasBuilder__get_gtfobins_lists()
        get.assert_called_once()
        self.assertGreater(len(lists[1]), 250)

    def test_archive_download_limit_stops_reading_and_falls_back(self):
        response = FakeResponse(b"x" * (3 * 1024 * 1024))
        builder = object.__new__(LinpeasBuilder)
        with patch("builder.src.linpeasBuilder.requests.get", return_value=response):
            lists = builder._LinpeasBuilder__get_gtfobins_lists()
        self.assertGreater(len(lists[1]), 250)
        self.assertTrue(response.closed)
        self.assertEqual(response.chunks_yielded, 33)

    def test_archive_http_error_closes_response_before_fallback(self):
        response = FakeResponse(b"")
        builder = object.__new__(LinpeasBuilder)
        with patch("builder.src.linpeasBuilder.requests.get", return_value=response), patch.object(
            response, "raise_for_status", side_effect=requests.HTTPError
        ):
            lists = builder._LinpeasBuilder__get_gtfobins_lists()
        self.assertGreater(len(lists[1]), 250)
        self.assertTrue(response.closed)
        self.assertEqual(response.chunks_yielded, 0)

    def test_expanded_size_budget_includes_skipped_members(self):
        # Mock metadata only; no large archive payload is created or expanded.
        for names in (("repo/unused-a", "repo/unused-b"),
                      ("repo/_gtfobins/large-a", "repo/_gtfobins/large-b")):
            with self.subTest(names=names):
                entries = [tarfile.TarInfo(name) for name in names]
                entries[0].size = 8 * 1024 * 1024
                entries[1].size = 8 * 1024 * 1024 + 1
                archive = MagicMock()
                archive.__iter__.return_value = iter(entries)
                archive.__enter__.return_value = archive
                with patch("builder.src.linpeasBuilder.tarfile.open", return_value=archive):
                    with self.assertRaisesRegex(ValueError, "expanded size limit"):
                        LinpeasBuilder._LinpeasBuilder__gtfobins_archive_categories(b"")
                archive.extractfile.assert_not_called()

    def test_archive_parser_ignores_nested_and_oversized_members(self):
        output = io.BytesIO()
        with tarfile.open(fileobj=output, mode="w:gz") as archive:
            for name, body in (
                ("repo/_gtfobins/good", b"sudo:\nsuid:\n"),
                ("repo/_gtfobins/nested/skip", b"sudo:\n"),
                ("repo/_gtfobins/evil[", b"sudo:\n"),
            ):
                entry = tarfile.TarInfo(name)
                entry.size = len(body)
                archive.addfile(entry, io.BytesIO(body))
        categories = LinpeasBuilder._LinpeasBuilder__gtfobins_archive_categories(output.getvalue())
        self.assertEqual(["good"], categories["sudo"])
        self.assertEqual(["good"], categories["suid"])
        self.assertEqual([], categories["capabilities"])

    def test_archive_entry_limit_bounds_repository_walk(self):
        output = io.BytesIO()
        with tarfile.open(fileobj=output, mode="w:gz") as archive:
            for index in range(2049):
                entry = tarfile.TarInfo("repo/unused-" + str(index))
                entry.type = tarfile.DIRTYPE
                archive.addfile(entry)
        with self.assertRaisesRegex(ValueError, "too many entries"):
            LinpeasBuilder._LinpeasBuilder__gtfobins_archive_categories(output.getvalue())


if __name__ == "__main__":
    unittest.main()
