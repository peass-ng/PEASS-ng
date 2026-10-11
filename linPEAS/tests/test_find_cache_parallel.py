"""The find cache runs independent traversals concurrently without mixing results."""

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


CACHE_MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/linpeas_base/2_caching_finds.sh"
BASE_MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/linpeas_base/0_variables_base.sh"


class FindCacheParallelTests(unittest.TestCase):
    def test_cache_guard_selects_the_same_builtin_and_custom_paths(self):
        source = CACHE_MODULE.read_text()
        source = source.replace("peass{FINDS_CUSTOM}", "printf '%s\\n' FINDS_CUSTOM")
        source = source.replace("peass{FINDS_HERE}", "printf '%s\\n' FINDS_HERE")
        source = source.replace("peass{STORAGES_HERE}", "printf '%s\\n' STORAGES_HERE")

        cases = (
            ("unrelated", "", ()),
            ("procs_crons_timers_srvcs_sockets", "", ("FINDS_HERE", "STORAGES_HERE")),
            ("software_information", "", ("FINDS_HERE", "STORAGES_HERE")),
            ("interesting_files", "", ("FINDS_HERE", "STORAGES_HERE")),
            ("unrelated", "/tmp/folder with spaces", ("FINDS_CUSTOM", "STORAGES_HERE")),
        )
        for checks, search_in_folder, expected_markers in cases:
            with self.subTest(checks=checks, search_in_folder=search_in_folder):
                env = os.environ.copy()
                env.update({
                    "CHECKS": checks, "SEARCH_IN_FOLDER": search_in_folder,
                    "GREEN": "", "YELLOW": "", "NC": "", "THREADS": "2",
                    "PSTORAGE_BACKUPS": "",
                })
                result = subprocess.run(
                    ["/bin/sh", "-c", source], env=env,
                    capture_output=True, text=True, timeout=10,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                for marker in ("FINDS_CUSTOM", "FINDS_HERE", "STORAGES_HERE"):
                    self.assertEqual(marker in result.stdout, marker in expected_markers)

    def test_portable_find_deadline_does_not_hold_the_output_pipe_open(self):
        source = BASE_MODULE.read_text()
        function = source.split("bounded_command() {", 1)[1].split("\nWF_ALL=", 1)[0]
        shell = "bounded_command() {" + function + "\nWF_TIMEOUT=; bounded_command 1 sleep 4"
        result = subprocess.run(
            ["/bin/sh", "-c", shell], capture_output=True, text=True, timeout=3,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(result.stdout, "")

    def test_two_finds_can_progress_together_and_keep_separate_results(self):
        with tempfile.TemporaryDirectory(prefix="peas-find-cache-") as temporary:
            root = Path(temporary)
            markers = root / "markers"
            results = root / "results"
            markers.mkdir()
            results.mkdir()
            stub = root / "find"
            stub.write_text("""#!/bin/sh
id=$1
: > "$MARKERS/$id"
i=0
while [ "$i" -lt 40 ]; do
  if [ -f "$MARKERS/ONE" ] && [ -f "$MARKERS/TWO" ]; then
    printf '%s\\n' "$id"
    exit 0
  fi
  sleep 0.1
  i=$((i + 1))
done
printf 'SERIAL\\n'
exit 1
""")
            stub.chmod(0o755)
            function = CACHE_MODULE.read_text().split('\nif [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
            shell = function + """
THREADS=2
CONT_THREADS=0
cache_find ONE ONE
cache_find TWO TWO
wait
cat "$FIND_CACHE_DIR/ONE" "$FIND_CACHE_DIR/TWO"
"""
            env = os.environ.copy()
            env.update({
                "PATH": f"{root}:{env.get('PATH', '')}",
                "MARKERS": str(markers),
                "FIND_CACHE_DIR": str(results),
                "YELLOW": "", "NC": "",
            })
            result = subprocess.run(
                ["/bin/sh", "-c", shell], env=env,
                capture_output=True, text=True, timeout=10,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.splitlines(), ["ONE", "TWO"])



if __name__ == "__main__":
    unittest.main()
