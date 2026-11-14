import importlib.util
import json
from datetime import datetime, timezone
from pathlib import Path
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "scripts/recover_generated_pr_tests.py"
SPEC = importlib.util.spec_from_file_location("recover_generated_pr_tests", SCRIPT)
RECOVERY = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RECOVERY)
REPO = "peass-ng/PEASS-ng"
SHA = "a" * 40
NOW = datetime(2026, 11, 14, 12, tzinfo=timezone.utc)


def pr_fixture(**changes):
    pr = {
        "number": 821,
        "title": "autoimprover: simplify winpeas checks",
        "author": {"login": "carlospolop"},
        "headRepositoryOwner": {"login": "peass-ng"},
        "baseRefName": "master",
        "headRefName": "autoimprover/winpeas/42596176627",
        "headRefOid": SHA,
        "isDraft": False,
        "createdAt": "2026-11-09T09:54:10Z",
    }
    pr.update(changes)
    return pr


class FakeGh:
    def __init__(self, pr=None, existing_run=False, conclusion="success", live_head=SHA,
                 run_status="completed", updated_at="2026-11-14T11:00:00Z"):
        self.pr = pr or pr_fixture()
        self.existing_run = existing_run
        self.conclusion = conclusion
        self.live_head = live_head
        self.run_status = run_status
        self.updated_at = updated_at
        self.calls = []

    def __call__(self, *args):
        self.calls.append(args)
        if args[:2] == ("pr", "list"):
            return json.dumps([self.pr])
        if args[:2] == ("pr", "view"):
            return json.dumps(dict(self.pr, state="OPEN", headRefOid=self.live_head))
        if args[:2] == ("run", "list"):
            return json.dumps([{"databaseId": 123, "status": self.run_status,
                                "conclusion": self.conclusion,
                                "updatedAt": self.updated_at}]
                              if self.existing_run else [])
        if args[:2] == ("run", "view"):
            return json.dumps({"status": "completed", "conclusion": self.conclusion,
                               "headSha": SHA})
        if args[:3] == ("workflow", "run", "PR-tests.yml"):
            self.existing_run = True
            return "https://github.com/peass-ng/PEASS-ng/actions/runs/123"
        if args[:3] == ("workflow", "run", "chack-agent-pr-triage.yml"):
            return "https://github.com/peass-ng/PEASS-ng/actions/runs/124"
        raise AssertionError(args)


class RecoveryTests(unittest.TestCase):
    def test_trusted_pr_without_run_gets_tests_then_triage(self):
        fake = FakeGh()
        self.assertEqual(1, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        workflow_calls = [call for call in fake.calls if call[:2] == ("workflow", "run")]
        self.assertEqual(["PR-tests.yml", "chack-agent-pr-triage.yml"],
                         [call[2] for call in workflow_calls])
        self.assertIn("pr_number=821", workflow_calls[1])

    def test_existing_running_or_failed_run_is_not_repeated(self):
        fake = FakeGh(existing_run=True, run_status="in_progress")
        self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertFalse(any(call[:2] == ("workflow", "run") for call in fake.calls))
        fake = FakeGh(existing_run=True, conclusion="failure")
        self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertFalse(any(call[:2] == ("workflow", "run") for call in fake.calls))

    def test_successful_existing_run_can_recover_missing_triage(self):
        fake = FakeGh(existing_run=True)
        self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertTrue(any(call[:3] == ("workflow", "run", "chack-agent-pr-triage.yml")
                            for call in fake.calls))

    def test_recent_success_allows_normal_triage_time_to_finish(self):
        fake = FakeGh(existing_run=True, updated_at="2026-11-14T11:55:00Z")
        self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertFalse(any(call[:2] == ("workflow", "run") for call in fake.calls))

    def test_failed_checks_do_not_trigger_triage(self):
        fake = FakeGh(conclusion="failure")
        self.assertEqual(1, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertFalse(any(call[:3] == ("workflow", "run", "chack-agent-pr-triage.yml")
                             for call in fake.calls))

    def test_stale_head_is_not_tested(self):
        fake = FakeGh(live_head="b" * 40)
        self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
        self.assertFalse(any(call[:2] == ("workflow", "run") for call in fake.calls))

    def test_untrusted_or_young_prs_are_not_tested(self):
        variants = (
            {"author": {"login": "someone-else"}},
            {"headRepositoryOwner": {"login": "someone-else"}},
            {"title": "unrelated change"},
            {"isDraft": True},
            {"createdAt": "2026-11-14T11:55:00Z"},
        )
        for variant in variants:
            with self.subTest(variant=variant):
                fake = FakeGh(pr=pr_fixture(**variant))
                self.assertEqual(0, RECOVERY.recover(REPO, NOW, call=fake, pause=lambda _: None))
                self.assertFalse(any(call[:2] == ("workflow", "run") for call in fake.calls))


if __name__ == "__main__":
    unittest.main()
