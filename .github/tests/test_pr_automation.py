import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
REPO = "peass-ng/PEASS-ng"
HEAD = "a" * 40
MOCK_GH = r'''
import json, os, sys
args = sys.argv[1:]
if args[0] != "api" and "--repo" not in args and os.environ.get("GH_REPO") != "peass-ng/PEASS-ng":
    sys.exit("fatal: not a git repository; GH_REPO missing")
with open(os.environ["CALLS"], "a") as log:
    log.write(json.dumps(args) + "\n")
data = json.loads(os.environ["PR_DATA"])
if args[:2] == ["pr", "list"]:
    print("775")
elif args[:2] == ["pr", "view"]:
    field = args[args.index("--json") + 1]
    print(data[field])
elif args[:2] == ["run", "list"]:
    assert args[args.index("--workflow") + 1] == "PR-tests.yml"
    assert args[args.index("--commit") + 1] == data["headRefOid"]
    assert args[args.index("--branch") + 1] == data["headRefName"]
    print(os.environ["TESTS_PASSED"])
elif args[0] == "api":
    if "--jq" in args:
        field = args[args.index("--jq") + 1]
        print({".user.login": data["author"], ".state": data["state"].lower(),
               ".head.repo.full_name": "peass-ng/PEASS-ng", ".head.ref": data["headRefName"],
               ".head.sha": data["headRefOid"], ".labels[].name": ""}[field])
    else:
        assert "sha=" + data["headRefOid"] in args
else:
    sys.exit("Unexpected gh invocation: " + repr(args))
'''


class PRAutomationTests(unittest.TestCase):
    def run_step(self, workflow, job, step_id=None, step_name=None, data=None, **overrides):
        config = yaml.safe_load((ROOT / ".github" / "workflows" / workflow).read_text())
        job_config = config["jobs"][job]
        step = next(step for step in job_config["steps"]
                    if (step_id and step.get("id") == step_id)
                    or (step_name and step.get("name") == step_name))
        fixture = dict(state="OPEN", author="carlospolop",
                       title="autoimprover: simplify linpeas checks", body="Fixture PR",
                       baseRefName="master", headRefName="autoimprover/linpeas/123",
                       baseRefOid="b" * 40, headRefOid=HEAD)
        fixture.update(data or {})
        expressions = {
            "github.repository": REPO,
            "needs.chack_agent_triage.outputs.pr_number": "775",
        }
        script = step["run"]
        for expression, value in expressions.items():
            script = script.replace("${{ " + expression + " }}", value)
        self.assertNotIn("${{", script)
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            mock = tmp / "gh"
            mock.write_text("#!" + sys.executable + "\n" + MOCK_GH)
            mock.chmod(0o755)
            env = dict(os.environ, PATH=str(tmp) + os.pathsep + os.environ["PATH"])
            env.pop("GH_REPO", None)
            for values in (config.get("env", {}), job_config.get("env", {}), step.get("env", {})):
                for key, value in values.items():
                    env[key] = REPO if value == "${{ github.repository }}" else str(value)
            env.update(GH_TOKEN="fixture", PR_NUMBER="775", HEAD_BRANCH=fixture["headRefName"],
                       SOURCE_HEAD_SHA=HEAD, REVIEWED_HEAD_SHA=HEAD, VALIDATED_HEAD_SHA=HEAD,
                       TRUSTED_AUTOMERGE="true", TRUSTED_AUTOMERGE_REASON="fixture",
                       PR_DATA=json.dumps(fixture), TESTS_PASSED="true",
                       GITHUB_OUTPUT=str(tmp / "outputs"), CALLS=str(tmp / "calls"))
            env.update(overrides)
            # Execute the actual YAML step outside any Git checkout, with no real API calls.
            result = subprocess.run(["bash", "-e", "-c", script], cwd=tmp, env=env,
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            outputs = (tmp / "outputs").read_text() if (tmp / "outputs").exists() else ""
            calls = [json.loads(line) for line in (tmp / "calls").read_text().splitlines()]
            return outputs, calls

    def gate(self, **kwargs):
        return self.run_step("chack-agent-pr-triage.yml", "chack_agent_triage", step_id="gate", **kwargs)

    def test_passing_autoimprovers_are_trusted_without_checkout(self):
        for platform in ("linpeas", "winpeas"):
            with self.subTest(platform=platform):
                outputs, _ = self.gate(data={"title": f"autoimprover: simplify {platform} checks",
                                            "headRefName": f"autoimprover/{platform}/123"})
                self.assertIn("should_run=true\n", outputs)
                self.assertIn("trusted_automerge=true\n", outputs)

    def test_autoimprover_author_and_branch_must_match(self):
        outputs, _ = self.gate(data={"author": "other-user"})
        self.assertIn("should_run=false\n", outputs)
        outputs, _ = self.gate(data={"headRefName": "unrelated-branch"})
        self.assertIn("trusted_automerge=false\n", outputs)

    def test_stale_or_closed_prs_are_skipped(self):
        for data in ({"headRefOid": "c" * 40}, {"state": "MERGED"}):
            outputs, _ = self.gate(data=data)
            self.assertIn("should_run=false\n", outputs)

    def test_manual_dispatch_requires_latest_successful_tests_at_current_head(self):
        outputs, calls = self.gate(SOURCE_HEAD_SHA="")
        self.assertIn("trusted_automerge=true\n", outputs)
        self.assertTrue(any(call[:2] == ["run", "list"] for call in calls))
        outputs, _ = self.gate(SOURCE_HEAD_SHA="", TESTS_PASSED="false")
        self.assertIn("should_run=false\n", outputs)

    def test_live_head_check_without_checkout_skips_changed_or_closed_prs(self):
        for data, expected in (({}, "true"), ({"headRefOid": "c" * 40}, "false"),
                               ({"state": "MERGED"}, "false")):
            outputs, _ = self.run_step("chack-agent-pr-triage.yml", "chack_agent_triage",
                                       step_id="live_pr", data=data)
            self.assertIn(f"open={expected}\n", outputs)

    def test_merge_uses_reviewed_sha_and_skips_changed_heads(self):
        _, calls = self.run_step("chack-agent-pr-triage.yml", "merge_or_comment",
                                 step_name="Merge PR when approved")
        self.assertTrue(any(call[0] == "api" and "sha=" + HEAD in call for call in calls))
        _, calls = self.run_step("chack-agent-pr-triage.yml", "merge_or_comment",
                                 step_name="Merge PR when approved", data={"headRefOid": "c" * 40})
        self.assertFalse(any(call[0] == "api" for call in calls))

    def test_failure_dispatch_branch_lookup_without_checkout(self):
        outputs, calls = self.run_step("pr-failure-chack-agent-dispatch.yml", "resolve_pr_context",
                                       step_id="pr_context", PR_NUMBER="")
        self.assertIn("number=775\n", outputs)
        self.assertIn("should_run=true\n", outputs)
        self.assertTrue(any(call[:2] == ["pr", "list"] for call in calls))


if __name__ == "__main__":
    unittest.main()
