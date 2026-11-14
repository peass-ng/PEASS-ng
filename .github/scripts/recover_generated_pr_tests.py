"""Recover generated PRs missing PR-tests or merge triage."""

import json
import os
import re
import subprocess
import time
from datetime import datetime, timedelta, timezone


MIN_AGE = timedelta(minutes=10)
TRIAGE_AGE = timedelta(minutes=10)
MAX_DISPATCHES = 2


def gh(*args):
    return subprocess.run(
        ["gh", *args], check=True, capture_output=True, text=True
    ).stdout.strip()


def gh_json(*args, call=gh):
    return json.loads(call(*args))


def trusted_generated(pr, repo):
    if (pr.get("isDraft") or pr.get("baseRefName") != "master"
            or (pr.get("author") or {}).get("login") != "carlospolop"
            or (pr.get("headRepositoryOwner") or {}).get("login") != repo.split("/")[0]):
        return False
    branch = pr.get("headRefName", "")
    title = pr.get("title", "")
    match = re.fullmatch(r"autoimprover/(linpeas|winpeas)/[0-9]+", branch)
    if match:
        return title == f"autoimprover: simplify {match.group(1)} checks"
    if re.fullmatch(r"wordlist-updater/linpeas/[0-9]+", branch):
        return title == "wordlist-updater: refresh linpeas vulnerability lists"
    match = re.fullmatch(r"new-check/(linpeas|winpeas)/[0-9]+-[0-9]+", branch)
    if match:
        return title == f"new-check({match.group(1)}): add high-impact vulnerability detection"
    if re.fullmatch(r"chack-agent/ci-master-fix-[0-9]+", branch):
        return title.startswith("Fix CI-master_test failure (run #")
    if branch.startswith("peass_linpeas_"):
        return title.startswith("[LINPEAS] Add privilege escalation check: ")
    if branch.startswith("peass_winpeas_"):
        return title.startswith("[WINPEAS] Add privilege escalation check: ")
    return False


def live_pr(number, repo, call):
    return gh_json(
        "pr", "view", str(number), "--repo", repo,
        "--json", "state,isDraft,baseRefName,headRefName,headRefOid,headRepositoryOwner,author,title",
        call=call,
    )


def current_head(pr, repo, call):
    live = live_pr(pr["number"], repo, call)
    return (live.get("state") == "OPEN" and trusted_generated(live, repo)
            and live.get("headRefName") == pr["headRefName"]
            and live.get("headRefOid") == pr["headRefOid"])


def matching_runs(pr, repo, call):
    return gh_json(
        "run", "list", "--repo", repo, "--workflow", "PR-tests.yml",
        "--branch", pr["headRefName"], "--commit", pr["headRefOid"],
        "--limit", "1", "--json", "databaseId,status,conclusion,updatedAt",
        call=call,
    )


def wait_for_run(pr, repo, call, pause):
    for _ in range(12):
        runs = matching_runs(pr, repo, call)
        if runs:
            run_id = str(runs[0]["databaseId"])
            break
        pause(5)
    else:
        raise RuntimeError(f"PR #{pr['number']}: dispatched PR-tests run did not appear")

    for _ in range(135):  # At most 45 minutes for the full platform suite.
        run = gh_json("run", "view", run_id, "--repo", repo,
                      "--json", "status,conclusion,headSha", call=call)
        if run.get("headSha") != pr["headRefOid"]:
            raise RuntimeError(f"PR #{pr['number']}: test run used a different commit")
        if run.get("status") == "completed":
            return run.get("conclusion") == "success"
        pause(20)
    raise RuntimeError(f"PR #{pr['number']}: PR-tests did not finish in 45 minutes")


def recover(repo, now, call=gh, pause=time.sleep):
    prs = gh_json(
        "pr", "list", "--repo", repo, "--state", "open", "--limit", "200",
        "--json", "number,title,author,isDraft,createdAt,baseRefName,headRefName,headRefOid,headRepositoryOwner",
        call=call,
    )
    dispatched = 0
    for pr in sorted(prs, key=lambda item: item["createdAt"]):
        if not trusted_generated(pr, repo):
            continue
        created = datetime.fromisoformat(pr["createdAt"].replace("Z", "+00:00"))
        if now - created < MIN_AGE:
            continue
        runs = matching_runs(pr, repo, call)
        if runs:
            run = runs[0]
            updated = datetime.fromisoformat(run["updatedAt"].replace("Z", "+00:00"))
            if (run["status"] == "completed" and run["conclusion"] == "success"
                    and now - updated >= TRIAGE_AGE and current_head(pr, repo, call)):
                print(f"PR #{pr['number']}: tests passed but PR remains open; dispatching triage")
                call("workflow", "run", "chack-agent-pr-triage.yml", "--repo", repo,
                     "--ref", "master", "-f", f"pr_number={pr['number']}")
            continue
        if not current_head(pr, repo, call):
            continue

        print(f"PR #{pr['number']}: no PR-tests run at {pr['headRefOid']}; dispatching")
        call("workflow", "run", "PR-tests.yml", "--repo", repo,
             "--ref", pr["headRefName"])
        dispatched += 1
        if wait_for_run(pr, repo, call, pause) and current_head(pr, repo, call):
            print(f"PR #{pr['number']}: checks passed; dispatching merge triage")
            # A github.token-dispatched PR-tests run does not reliably emit the
            # workflow_run event consumed by triage, so dispatch triage directly.
            call("workflow", "run", "chack-agent-pr-triage.yml", "--repo", repo,
                 "--ref", "master", "-f", f"pr_number={pr['number']}")
        else:
            print(f"PR #{pr['number']}: checks failed or PR changed; leaving open")
        if dispatched >= MAX_DISPATCHES:
            break
    return dispatched


if __name__ == "__main__":
    if not os.environ.get("GH_TOKEN") or not os.environ.get("GH_REPO"):
        raise SystemExit("GH_TOKEN and GH_REPO are required")
    recover(os.environ["GH_REPO"], datetime.now(timezone.utc))
