import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path

import yaml


class SudoTerraformOverrideTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.helper = cls.root / "linPEAS/builder/linpeas_parts/functions/check_sudo_terraform_override.sh"

    def run_case(self, *, policy=None, config=None, project=None, binary=True,
                 alternate_config=False, oversized=False, writable_override=True,
                 linked_override_parent=False, linked_project_parent=False,
                 linked_home_parent=False):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp).resolve()
            home_parent = base
            if linked_home_parent:
                real_parent = base / "real-home-parent"
                real_parent.mkdir()
                home_parent = base / "home-parent-link"
                home_parent.symlink_to(real_parent, target_is_directory=True)
            home = home_parent / "home"
            home.mkdir()
            project_parent = base
            if linked_project_parent:
                real_parent = base / "real-project-parent"
                real_parent.mkdir()
                project_parent = base / "project-parent-link"
                project_parent.symlink_to(real_parent, target_is_directory=True)
            project_dir = project_parent / "project"
            project_dir.mkdir()
            override_parent = base
            if linked_override_parent:
                real_parent = base / "real-override-parent"
                real_parent.mkdir()
                override_parent = base / "override-parent-link"
                override_parent.symlink_to(real_parent, target_is_directory=True)
            override = override_parent / "override"
            override.mkdir()
            provider = override / "terraform-provider-demo"
            if binary:
                provider.write_text("#!/bin/sh\necho invoked > \"$TF_INVOKED\"\n")
                provider.chmod(0o755)
            if not writable_override:
                override.chmod(0o500)
            source = "example.com/local/demo"
            if config is None:
                config = ('provider_installation {\n  dev_overrides {\n'
                          f'    "{source}" = "{override}"\n'
                          '  }\n}\ncredentials "example.com" { token = "SECRET" }\n')
            if oversized:
                config += "# padding\n" * 6000
            (home / ".terraformrc").write_text(config)
            if project is None:
                project = f'provider "demo" {{\n  source = "{source}"\n}}\n'
            (project_dir / "main.tf").write_text(project)
            if policy is None:
                policy = ("Matching Defaults entries for user on host:\n"
                          "    !env_reset, env_delete+=PATH\n\n"
                          "User user may run the following commands on host:\n"
                          f"    (root) /usr/bin/terraform -chdir={project_dir} apply")
            policy = policy.replace("{dir}", str(project_dir))
            env = os.environ.copy()
            env.update(HOME=str(home), TF_INVOKED=str(base / "invoked"))
            env.pop("TF_CLI_CONFIG_FILE", None)
            if alternate_config:
                env["TF_CLI_CONFIG_FILE"] = str(base / "alternate.rc")
            script = (f". {shlex.quote(str(self.helper))}\n"
                      "ROOT_FOLDER=/\n"
                      f"check_sudo_terraform_override {shlex.quote(policy)} '' ''\n")
            result = subprocess.run(["sh", "-c", script], env=env,
                                    capture_output=True, text=True, timeout=5)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse((base / "invoked").exists())
            return result.stdout

    def test_matching_writable_override_is_review_candidate(self):
        output = self.run_case()
        self.assertEqual(output.count("Terraform sudo/provider override review candidate"), 1)
        self.assertIn("not proven root privilege", output)
        self.assertNotIn("SECRET", output)

    def test_missing_or_nonroot_grant_and_other_subcommand(self):
        for rule in ("", "    (daemon) /usr/bin/terraform -chdir={dir} apply",
                     "    (root) /usr/bin/terraform -chdir={dir} plan",
                     "    (root) !/usr/bin/terraform -chdir={dir} apply"):
            with self.subTest(rule=rule):
                output = self.run_case(policy="    !env_reset\n" + rule)
                self.assertNotIn("review candidate", output)

    def test_environment_and_source_mismatch(self):
        self.assertNotIn("review candidate", self.run_case(
            policy="    (root) /usr/bin/terraform -chdir={dir} apply"))
        self.assertNotIn("review candidate", self.run_case(
            policy="    !env_reset, always_set_home\n"
                   "    (root) /usr/bin/terraform -chdir={dir} apply"))
        self.assertNotIn("review candidate", self.run_case(alternate_config=True))
        self.assertNotIn("review candidate", self.run_case(
            project='provider "demo" { source = "example.com/other/demo" }'))

    def test_bounded_config_and_provider_access(self):
        self.assertNotIn("review candidate", self.run_case(oversized=True))
        self.assertNotIn("review candidate", self.run_case(writable_override=False))
        self.assertIn("caller can create executable", self.run_case(binary=False))

    def test_symlinked_parent_paths_are_suppressed(self):
        self.assertNotIn("review candidate", self.run_case(linked_home_parent=True))
        self.assertNotIn("review candidate", self.run_case(linked_override_parent=True))
        self.assertNotIn("review candidate", self.run_case(linked_project_parent=True))

    def test_yaml_lists_cli_config_without_dumping_contents(self):
        data = yaml.safe_load((self.root / "build_lists/sensitive_files.yaml").read_text())
        terraform = next(item for item in data["search"] if item["name"] == "Terraform")
        for name in (".terraformrc", "terraform.rc"):
            selector = next(item["value"] for item in terraform["value"]["files"]
                            if item["name"] == name)
            self.assertEqual(selector, {"just_list_file": True, "type": "f",
                                        "search_in": ["common"]})


if __name__ == "__main__":
    unittest.main()
