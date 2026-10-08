import shlex
import subprocess
import tempfile
import unittest
import os
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/19_Erlang_ssh_daemons.sh"
)


class ErlangSshDaemonTests(unittest.TestCase):
    def run_check(self, root, rows, proc_args=None):
        snapshot = root / "ps.txt"
        snapshot.write_text("\n".join(rows) + "\n", encoding="utf-8")
        proc_root = root / "proc"
        proc_root.mkdir(exist_ok=True)
        for pid, args in (proc_args or {}).items():
            proc = proc_root / str(pid)
            proc.mkdir()
            (proc / "cmdline").write_bytes(b"\0".join(a.encode() for a in args) + b"\0")
        command = (
            "SEARCH_IN_FOLDER=1; print_2title() { printf 'TITLE: %s\\n' \"$1\"; }; "
            f". {shlex.quote(str(MODULE))}; "
            f"lp_check_erlang_ssh_daemons {shlex.quote(str(snapshot))} {shlex.quote(str(proc_root))}"
        )
        result = subprocess.run(["sh", "-c", command], capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_running_root_source_reports_markers_without_secret(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "service.escript"
            source.write_text(
                'ssh:daemon(0, [{user_passwords, [\n  {"operator", "private-value"}\n]}, '
                '{ip, {127,0,0,1}}]).\n', encoding="utf-8"
            )
            output = self.run_check(root, [f"41 0 escript -- -extra {source}"])
            self.assertIn("PID 41 UID 0", output)
            self.assertIn("auth=literal_user_passwords, shell=unknown", output)
            self.assertNotIn("private-value", output)
            self.assertNotIn("REPL", output)

    def test_comments_disabled_shell_and_nonroot_are_candidates(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "daemon.erl"
            source.write_text(
                '% ssh:daemon(22, [{user_passwords, []}]).\n'
                'ssh:daemon(0, [{pwdfun, F}, {shell, disabled}]).\n', encoding="utf-8"
            )
            output = self.run_check(root, [f"7 1001 /usr/bin/escript {source}"])
            self.assertIn("PID 7 UID 1001", output)
            self.assertIn("auth=auth_option, shell=disabled_marker", output)
            self.assertNotIn("literal_user_passwords", output)

    def test_comment_only_and_unlaunched_source_are_silent(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "sample.escript"
            source.write_text('% ssh:daemon(22, [{user_passwords, []}]).\n', encoding="utf-8")
            output = self.run_check(root, [f"7 0 escript {source}"])
            self.assertEqual(output, "")
            source.write_text('ssh:daemon(22, [{user_passwords, []}]).\n', encoding="utf-8")
            self.assertEqual(self.run_check(root, ["8 0 /usr/bin/sleep 1"]), "")
            source.write_text('ssh:daemon(22, Options).\n', encoding="utf-8")
            self.assertEqual(self.run_check(root, [f"9 0 escript {source}"]), "")

    def test_proc_arguments_preserve_spaces_and_symlink_is_unknown(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "space name.escript"
            source.write_text('ssh:daemon(0, [{user_passwords, []}, {ssh_cli, no_cli}]).\n', encoding="utf-8")
            output = self.run_check(
                root, ["9 0 /usr/bin/escript /unparseable source"],
                {9: ["/usr/bin/escript", str(source)]},
            )
            self.assertIn(str(source), output)
            self.assertIn("shell=disabled_marker", output)
            link = root / "link.escript"
            link.symlink_to(source)
            output = self.run_check(root, [f"10 0 escript {link}"])
            self.assertIn("symlinked", output)
            output = self.run_check(root, [f"11 0 /usr/bin/erl -extra {source}"])
            self.assertIn("absolute launched source unavailable", output)
            self.assertNotIn("auth=", output)

    def test_size_and_process_caps(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            large = root / "large.escript"
            large.write_text("x" * 131073, encoding="utf-8")
            output = self.run_check(root, [f"1 0 escript {large}"])
            self.assertIn("exceeds 128 KiB", output)
            rows = [f"{i} 0 escript" for i in range(1, 80)]
            output = self.run_check(root, rows)
            self.assertIn("PID 1", output)
            self.assertNotIn("PID 65", output)
            self.assertEqual(output.count("absolute launched source unavailable"), 8)

    def test_distinct_file_cap_and_bsd_ps_fallback(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rows = []
            for pid in range(1, 34):
                source = root / f"daemon{pid}.escript"
                source.write_text("ssh:daemon(0, [{pwdfun, F}]).\n", encoding="utf-8")
                rows.append(f"{pid} 0 /usr/bin/escript {source}")
            output = self.run_check(root, rows)
            self.assertIn("PID 32", output)
            self.assertNotIn("PID 33", output)

            bin_dir = root / "bin"
            bin_dir.mkdir()
            snapshot = root / "ps.txt"
            snapshot.write_text(rows[0] + "\n", encoding="utf-8")
            ps = bin_dir / "ps"
            ps.write_text(
                '#!/bin/sh\n'
                'case "$*" in\n'
                '  "-eo pid=,uid=,args=") exit 1 ;;\n'
                '  "-axo pid=,uid=,command=") cat "$PS_FIXTURE" ;;\n'
                '  *) exit 2 ;;\n'
                'esac\n', encoding="utf-8"
            )
            ps.chmod(0o755)
            command = (
                "SEARCH_IN_FOLDER=1; print_2title() { :; }; "
                f". {shlex.quote(str(MODULE))}; "
                f"lp_check_erlang_ssh_daemons '' {shlex.quote(str(root / 'proc'))}"
            )
            env = os.environ.copy()
            env.update(PATH=f"{bin_dir}:{env['PATH']}", PS_FIXTURE=str(snapshot))
            result = subprocess.run(["sh", "-c", command], env=env, capture_output=True, text=True, timeout=5)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("PID 1 UID 0", result.stdout)


if __name__ == "__main__":
    unittest.main()
