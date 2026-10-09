import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
SUID_MODULE = ROOT / "linPEAS/builder/linpeas_parts/8_interesting_perms_files/1_SUID.sh"


class XwikiCatalogTests(unittest.TestCase):
    def test_live_connection_properties_only(self):
        data = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        entry = next(item for item in data["search"] if item["name"] == "XWiki")
        self.assertTrue(entry["value"]["config"]["auto_check"])
        path_pattern = entry["value"]["files"][0]["value"]["check_extra_path"]
        self.assertRegex("/etc/xwiki/hibernate.cfg.xml", path_pattern)
        self.assertNotRegex("/etc/xwiki/hibernate.cfg.xml.ucf-dist", path_pattern)
        self.assertNotRegex("/opt/example/hibernate.cfg.xml", path_pattern)

        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peasLoaded import PEASLoaded
        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        section = builder._LinpeasBuilder__generate_sections()["XWiki"]

        with tempfile.TemporaryDirectory() as tmp:
            fixture = Path(tmp) / "hibernate.cfg.xml"
            fixture.write_text(
                '<!-- <property name="hibernate.connection.password">comment-secret</property> -->\n'
                '<!--\n<property name="hibernate.connection.password">block-secret</property>\n-->\n'
                '<property name="hibernate.connection.url">jdbc:mysql://db/wiki</property>\n'
                '<property name="hibernate.connection.username">wiki-db</property>\n'
                '<property name="hibernate.connection.password">live-secret</property>\n'
                '<property name="hibernate.connection.password">   </property>\n'
                '<property name="hibernate.connection.password">changeme</property>\n'
                '<property name="unrelated.password">other-secret</property>\n'
            )
            cmd = "print_2title() { :; }; " + section
            for fast in ("", "1"):
                result = subprocess.run(
                    ["sh", "-c", cmd],
                    env={**os.environ, "PSTORAGE_XWIKI": str(fixture), "FAST": fast,
                         "E": "E", "SED_RED": ""},
                    capture_output=True, text=True,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("live-secret", result.stdout)
                self.assertIn("wiki-db", result.stdout)
                self.assertIn("database connection", result.stdout)
                for excluded in ("comment-secret", "block-secret", "changeme", "other-secret"):
                    self.assertNotIn(excluded, result.stdout)

            fixture.write_text('<property name="hibernate.connection.password"> </property>\n')
            empty = subprocess.run(
                ["sh", "-c", cmd], env={**os.environ, "PSTORAGE_XWIKI": str(fixture),
                                       "E": "E", "SED_RED": ""},
                capture_output=True, text=True,
            )
            self.assertNotIn("database connection", empty.stdout)
            fixture.chmod(0)
            if not os.access(fixture, os.R_OK):
                unreadable = subprocess.run(
                    ["sh", "-c", cmd], env={**os.environ, "PSTORAGE_XWIKI": str(fixture),
                                           "E": "E", "SED_RED": ""},
                    capture_output=True, text=True,
                )
                self.assertNotIn("database connection", unreadable.stdout)

            fixture.chmod(0o600)
            fixture.write_text(
                '<property name="hibernate.connection.url">jdbc:mysql://db/wiki</property>\n' * 25
            )
            bounded = subprocess.run(
                ["sh", "-c", cmd], env={**os.environ, "PSTORAGE_XWIKI": str(fixture),
                                       "E": "E", "SED_RED": ""},
                capture_output=True, text=True,
            )
            self.assertEqual(bounded.stdout.count("<property"), 20)


class NdsudoCandidateTests(unittest.TestCase):
    def run_fixture(self, mode=0o4755, uid="0", nnp="0", mount="rw,relatime",
                    bsd_stat=False):
        with tempfile.TemporaryDirectory() as tmp:
            helper = Path(tmp) / "netdata/plugins.d/ndsudo"
            helper.parent.mkdir(parents=True)
            marker = Path(tmp) / "executed"
            helper.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
            helper.chmod(mode)
            stat_guard = '[ "$1" = -f ] || return 1;' if bsd_stat else ''
            shell = (
                "print_2title() { :; }; print_info() { :; }; echo_not_found() { :; }; "
                "check_privileged_file_location() { :; }; "
                f"find() {{ printf '%s\\n' '{helper}'; }}; "
                f"stat() {{ {stat_guard} "
                f"printf '%s\\n' '{uid}'; }}; "
                f"findmnt() {{ printf '%s\\n' '{mount}'; }}; "
                "awk() { if [ \"$2\" = /proc/self/status ]; then "
                f"printf '%s\\n' '{nnp}'; else command awk \"$@\"; fi; }}; "
                "ROOT_FOLDER=/fixture; STRINGS=1; STRACE=1; "
                + SUID_MODULE.read_text()
            )
            run = subprocess.run(["bash", "-c", shell], capture_output=True, text=True)
            self.assertEqual(run.returncode, 0, run.stderr)
            self.assertFalse(marker.exists(), "ndsudo was executed")
            return run.stdout

    def test_candidate_and_blocking_conditions(self):
        allowed = self.run_fixture()
        self.assertIn("ndsudo candidate", allowed)
        self.assertIn("NoNewPrivs=0", allowed)
        self.assertIn("backports unknown", allowed)
        self.assertNotIn("Trying to execute", allowed)
        self.assertIn("ndsudo candidate", self.run_fixture(bsd_stat=True))

        self.assertNotIn("ndsudo candidate", self.run_fixture(uid="1000"))
        self.assertNotIn("ndsudo candidate", self.run_fixture(mode=0o0755))
        self.assertNotIn("ndsudo candidate", self.run_fixture(mode=0o4000))
        self.assertNotIn("ndsudo candidate", self.run_fixture(nnp="1"))
        self.assertNotIn("ndsudo candidate", self.run_fixture(mount="rw,nosuid,relatime"))
        unknown = self.run_fixture(nnp="", mount="")
        self.assertIn("ndsudo candidate", unknown)
        self.assertIn("NoNewPrivs unknown", unknown)
        self.assertIn("mount SUID policy unknown", unknown)


if __name__ == "__main__":
    unittest.main()
