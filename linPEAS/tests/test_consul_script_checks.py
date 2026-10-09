"""Passive Consul configuration correlation fixtures."""

import os
import pathlib
import re
import subprocess
import tempfile
import unittest

import yaml


ROOT = pathlib.Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/7_software_information/Consul.sh"


class ConsulScriptCheckTests(unittest.TestCase):
    def run_fixture(
        self,
        configs,
        process_owner="root",
        process_path=None,
        extra_args="",
        writable_config_dir=False,
        as_root=False,
        symlink_config_dir=False,
    ):
        with tempfile.TemporaryDirectory() as tmp:
            base = pathlib.Path(tmp)
            config_dir = base / "consul"
            config_dir.mkdir()
            for name, data in configs.items():
                (config_dir / name).write_text(data)
            ps = base / "ps"
            ps.write_text('#!/bin/sh\nprintf "%s\\n" "$PS_OUTPUT"\n')
            ps.chmod(0o755)
            link = base / "consul-link"
            if symlink_config_dir:
                link.symlink_to(config_dir, target_is_directory=True)
            actual_path = process_path or str(link if symlink_config_dir else config_dir)
            config_dir.chmod(0o700 if writable_config_dir else 0o500)
            env = os.environ.copy()
            env.update(
                PATH=str(base) + os.pathsep + env.get("PATH", ""),
                PS_OUTPUT=f"{process_owner} /usr/local/bin/consul agent -config-dir={actual_path} {extra_args}",
                CONSUL_MODULE=str(MODULE),
                SEARCH_IN_FOLDER="",
                IAMROOT="1" if as_root or os.geteuid() == 0 else "",
            )
            script = (
                'print_2title() { printf "TITLE:%s\\n" "$1"; }; '
                'print_info() { :; }; '
                '. "$CONSUL_MODULE"'
            )
            try:
                result = subprocess.run(
                    ["/bin/sh", "-c", script],
                    env=env,
                    text=True,
                    capture_output=True,
                    timeout=5,
                    check=True,
                )
            finally:
                config_dir.chmod(0o700)
            self.assertEqual(result.stderr, "")
            return result.stdout

    def test_root_agent_with_enabled_script_checks_is_candidate(self):
        output = self.run_fixture(
            {
                "config.json": '{"enable_script_checks": true,\n"encrypt":"private-key-material"}\n',
            }
        )
        self.assertIn("Root-run Consul script-check review candidate", output)
        self.assertIn("enable_script_checks=true", output)
        self.assertNotIn("private-key-material", output)

    def test_false_and_local_only_are_not_candidates(self):
        self.assertEqual(
            self.run_fixture({"config.hcl": "enable_script_checks = false\nenable_local_script_checks = true\n"}),
            "",
        )

    def test_nonroot_agent_is_not_candidate(self):
        self.assertEqual(
            self.run_fixture({"config.hcl": "enable_script_checks = true\n"}, process_owner="consul"),
            "",
        )

    def test_config_not_used_by_process_is_not_candidate(self):
        self.assertEqual(
            self.run_fixture({"config.hcl": "enable_script_checks = true\n"}, process_path="/nonexistent/consul-config"),
            "",
        )

    def test_conflicting_settings_are_explicitly_unknown(self):
        output = self.run_fixture(
            {"a.json": '"enable_script_checks": true\n', "b.hcl": "enable_script_checks = false\n"}
        )
        self.assertIn("Conflicting enable_script_checks=false", output)
        self.assertIn("effective setting unknown", output)

    def test_oversized_file_is_skipped(self):
        self.assertEqual(self.run_fixture({"config.hcl": "#" * 70000 + "\nenable_script_checks = true\n"}), "")

    def test_command_line_script_check_flag_is_candidate(self):
        output = self.run_fixture({}, extra_args="-enable-script-checks")
        self.assertIn("enables script checks on its command line", output)

    @unittest.skipIf(os.geteuid() == 0, "requires non-root permission checks")
    def test_loaded_writable_directory_is_candidate_even_with_script_checks_disabled(self):
        output = self.run_fixture(
            {"config.hcl": "enable_script_checks = false\n"},
            writable_config_dir=True,
        )
        self.assertIn("Root-run Consul config-directory write review candidate", output)
        self.assertIn("Loaded Consul config-dir writable/searchable by current user:", output)
        self.assertIn("authorized reload or restart", output)
        self.assertNotIn("API script execution requires", output)

    def test_writable_directory_is_not_an_escalation_candidate_for_root(self):
        self.assertEqual(self.run_fixture({}, writable_config_dir=True, as_root=True), "")

    def test_symlinked_config_directory_is_not_reported_as_writable(self):
        self.assertEqual(self.run_fixture({}, writable_config_dir=True, symlink_config_dir=True), "")

    def test_limesurvey_selector_is_scoped_and_metadata_only(self):
        listing = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        entry = next(item for item in listing["search"] if item["name"] == "LimeSurvey")
        config = entry["value"]["files"][0]["value"]
        self.assertTrue(config["just_list_file"])
        self.assertTrue(
            re.search(config["check_extra_path"], "/var/www/limesurvey/application/config/config.php")
        )
        self.assertFalse(re.search(config["check_extra_path"], "/var/www/other/application/config/config.php"))


if __name__ == "__main__":
    unittest.main()
