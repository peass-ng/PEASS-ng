"""Focused fixtures for bounded, passive Logstash pipeline review."""

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/7_software_information/Logstash.sh"


def run_module(*roots):
    env = os.environ.copy()
    env.update(
        PSTORAGE_LOGSTASH="\n".join(str(root) for root in roots),
        DEBUG="",
        E="E",
        USER="",
        sh_usrs="^$",
        nosh_usrs="^$",
        knw_usrs="^$",
        SED_LIGHT_CYAN="&",
        SED_BLUE="&",
        SED_GREEN="&",
        SED_LIGHT_MAGENTA="&",
        SED_RED="&",
    )
    script = 'print_2title() { :; }\n. "$1"'
    result = subprocess.run(
        ["/bin/sh", "-c", script, "sh", str(MODULE)],
        env=env,
        text=True,
        capture_output=True,
        check=True,
        timeout=5,
    )
    return result.stdout, result.stderr


class LogstashPipelineLeadsTests(unittest.TestCase):
    def test_existing_output_and_file_input_are_visible(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "logstash"
            conf = root / "conf.d"
            conf.mkdir(parents=True)
            (root / "startup.options").write_text("LS_USER=root\nLS_GROUP=root\n")
            (conf / "input.conf").write_text(
                'input {\n  file {\n    path => "/srv/app/events_*"\n    type => "review"\n  }\n}\n'
            )
            (conf / "filter.conf").write_text(
                'filter {\n  grok {\n    match => { "message" => "%{GREEDYDATA:action}" }\n  }\n}\n'
            )
            (conf / "output.conf").write_text(
                'output {\n  exec {\n    command => "printf %s %{action}"\n  }\n}\n'
            )
            output, stderr = run_module(root)
            self.assertIn("startup.options account setting", output)
            self.assertIn("LS_USER=root", output)
            self.assertIn('path => "/srv/app/events_*"', output)
            self.assertIn('match => { "message" => "%{GREEDYDATA:action}" }', output)
            self.assertIn('command => "printf %s %{action}"', output)
            self.assertIn("input.conf", output)
            self.assertNotIn("No such file", stderr)

    def test_input_count_byte_limit_and_symlink_are_enforced(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "logstash"
            conf = root / "conf.d"
            conf.mkdir(parents=True)
            for number in range(18):
                (conf / f"input{number:02d}.conf").write_text(f'path => "/safe/{number:02d}"\n')
            (conf / "input00.conf").write_text("x" * 8192 + '\npath => "/too-late"\n')
            (conf / "input01.conf").unlink()
            (conf / "input01.conf").symlink_to(conf / "input02.conf")
            output, stderr = run_module(root)
            self.assertNotIn("/too-late", output)
            self.assertNotIn("input01.conf:", output)
            self.assertIn("16-file cap", output)
            self.assertNotIn('path => "/safe/17"', output)
            self.assertEqual(stderr, "")

    def test_missing_root_and_unrelated_lines_are_quiet(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "logstash"
            conf = root / "conf.d"
            conf.mkdir(parents=True)
            (conf / "plain.conf").write_text('# path => "/ignored"\npassword => "hidden"\n')
            output, stderr = run_module(Path(temp) / "missing", root)
            self.assertNotIn("/ignored", output)
            self.assertNotIn("hidden", output)
            self.assertNotIn("plain.conf:", output)
            self.assertEqual(stderr, "")

    def test_extensionless_output_and_filter_keep_full_file_coverage(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "logstash"
            conf = root / "conf.d"
            conf.mkdir(parents=True)
            (conf / "output-extra").write_text(
                "# harmless padding\n" * 700 + 'command => "printf %s %{action}"\n'
            )
            (conf / "filter-extra").write_text(
                "# harmless padding\n" * 700 + 'code => "event.set(\'x\', 1)"\n'
            )
            output, stderr = run_module(root)
            self.assertIn('command => "printf %s %{action}"', output)
            self.assertIn('code => "event.set(\'x\', 1)"', output)
            self.assertEqual(stderr, "")


if __name__ == "__main__":
    unittest.main()
