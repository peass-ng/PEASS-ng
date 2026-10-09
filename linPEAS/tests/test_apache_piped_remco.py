import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/7_software_information/Apache_nginx.sh"
SIGNATURES = ROOT / "build_lists/sensitive_files.yaml"


class ApachePipedRemcoTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.timeout = shutil.which("timeout")
        if cls.timeout is None:
            raise unittest.SkipTest("timeout is needed for bounded fixture runs")
        cls.module = MODULE.read_text(encoding="utf-8").replace("peass{Apache-Nginx}", "")

    def run_check(self, function, *args):
        script = self.module + "\n" + f'TIMEOUT="{self.timeout}"\n{function} "$@"\n'
        result = subprocess.run(
            ["sh", "-c", script, "fixture", *map(str, args)],
            text=True,
            capture_output=True,
            env={**os.environ, "SEARCH_IN_FOLDER": ""},
            timeout=8,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_piped_log_metadata_redacts_commands(self):
        with tempfile.TemporaryDirectory() as tmp:
            site = Path(tmp) / "site.conf"
            site.write_text(
                'CustomLog "|$sh -c secret_token=hidden" common\n'
                'ErrorLog "|/usr/bin/rotatelogs /tmp/error 1M"\n'
                'TransferLog /var/log/apache/access.log\n',
                encoding="utf-8",
            )
            output = self.run_check("lp_apache_piped_log_check", tmp)
            self.assertIn("CustomLog: shell pipeline", output)
            self.assertIn("ErrorLog: direct pipeline", output)
            self.assertNotIn("secret_token", output)
            self.assertNotIn("/usr/bin/rotatelogs", output)
            self.assertNotIn("TransferLog:", output)
            self.assertNotIn("privilege escalation", output.lower())
            site.write_text("customlog '|$sh -c second_secret=hidden' common\n", encoding="utf-8")
            lowered = self.run_check("lp_apache_piped_log_check", tmp)
            self.assertIn("customlog: shell pipeline", lowered)
            self.assertNotIn("second_secret", lowered)

    def test_ordinary_file_log_does_not_trigger(self):
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / "site.conf").write_text(
                "CustomLog /var/log/apache/access.log common\n", encoding="utf-8"
            )
            self.assertEqual(self.run_check("lp_apache_piped_log_check", tmp), "")

    def test_remco_raw_value_correlates_without_backend_query(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            templates = root / "templates"
            templates.mkdir()
            template = templates / "site.conf.tmpl"
            template.write_text(
                'ServerName {{ getv(printf("/customers/%s/url", customer)) }}.example\n',
                encoding="utf-8",
            )
            config = root / "config"
            config.write_text(
                "[[resource]]\n"
                'name = "apache2"\n'
                "[[resource.template]]\n"
                f'src = "{template}"\n'
                'dst = "/etc/apache2/sites-enabled/site.conf"\n'
                'reload_cmd = "systemctl restart apache2.service"\n'
                "[resource.backend]\n"
                "[resource.backend.etcd]\n"
                "version = 3\n"
                'nodes = ["http://127.0.0.1:2379"]\n'
                'keys = ["/customers"]\n'
                "watch = true\n"
                "interval = 5\n",
                encoding="utf-8",
            )
            output = self.run_check("lp_remco_apache_template_check", config, templates, "root")
            self.assertIn("Candidate: root remco process", output)
            self.assertIn("backend write permission unknown", output)
            self.assertNotIn("/customers", output)
            self.assertNotIn(str(template), output)
            self.assertIn("process privilege", self.run_check(
                "lp_remco_apache_template_check", config, templates, "unknown"
            ))

            template.write_text(
                'ServerName {{ getv("/customers/x/url") | escape }}.example\n',
                encoding="utf-8",
            )
            filtered = self.run_check(
                "lp_remco_apache_template_check", config, templates, "root"
            )
            self.assertIn("template contents", filtered)
            self.assertNotIn("Candidate:", filtered)

    def test_remco_oversize_config_is_unknown(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "config"
            config.write_text("a" * 65537, encoding="utf-8")
            output = self.run_check(
                "lp_remco_apache_template_check", config, Path(tmp) / "templates", "root"
            )
            self.assertIn("unknown", output)
            self.assertNotIn("Candidate", output)

    def test_existing_apache_selector_is_preserved(self):
        section = next(
            entry for entry in yaml.safe_load(SIGNATURES.read_text(encoding="utf-8"))["search"]
            if entry["name"] == "Apache-Nginx"
        )["value"]
        commands = section["config"]["exec"]
        self.assertIn("lp_apache_piped_log_check", commands)
        self.assertIn("lp_remco_apache_template_check", commands)
        sites = next(item for item in section["files"] if item["name"] == "sites-enabled")
        pattern = sites["value"]["files"][0]["value"]["bad_regex"]
        for directive in ("ServerName", "DocumentRoot", "ProxyPass"):
            self.assertIn(directive, pattern)

    def test_generated_file_inventory_withholds_pipe_command(self):
        section = next(
            entry for entry in yaml.safe_load(SIGNATURES.read_text(encoding="utf-8"))["search"]
            if entry["name"] == "Apache-Nginx"
        )["value"]
        default = next(item for item in section["files"] if item["name"] == "000-default.conf")
        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.fileRecord import FileRecord
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peassRecord import PEASRecord

        record = FileRecord(regex="000-default.conf", **default["value"])
        parent = PEASRecord("Apache-Nginx", True, [], [record])
        line = LinpeasBuilder.__new__(LinpeasBuilder)._LinpeasBuilder__construct_file_line(
            parent, record
        )
        with tempfile.TemporaryDirectory() as tmp:
            file = Path(tmp) / "000-default.conf"
            file.write_text(
                "ServerName example.local\n"
                "DocumentRoot /var/www/html\n"
                "ProxyPass / http://localhost:1234/\n"
                'CustomLog "|$sh -c secret_token=hidden" common\n'
                "customlog '|$sh -c second_secret=hidden' common\n",
                encoding="utf-8",
            )
            result = subprocess.run(
                ["sh", "-c", line],
                text=True,
                capture_output=True,
                env={
                    **os.environ,
                    "PSTORAGE_APACHE_NGINX": str(file),
                    "E": "E",
                    "SED_RED": "&",
                },
                timeout=4,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertNotIn("secret_token", result.stdout)
            self.assertNotIn("second_secret", result.stdout)
            for directive in ("ServerName", "DocumentRoot", "ProxyPass"):
                self.assertIn(directive, result.stdout)


if __name__ == "__main__":
    unittest.main()
