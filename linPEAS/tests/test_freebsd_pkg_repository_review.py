"""Exercise the bounded FreeBSD pkg/hosts sudo correlation without running pkg."""

from pathlib import Path
import re
import shlex
import subprocess
import tempfile
import unittest


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
FUNCTION = re.search(
    r"(?ms)^sudo_freebsd_pkg_repo_review\(\) \{\n.*?^\}", MODULE.read_text()
).group()
POLICY = "    (ALL) NOPASSWD: /usr/sbin/pkg update\n    (ALL) NOPASSWD: /usr/sbin/pkg install *\n"
CONFIG = '''FreeBSD: {
  url: "pkg+http://packages.example.invalid:80/repo",
  mirror_type: "srv",
  signature_type: "none",
  enabled: yes
}
'''
MARKER = "Sudo FreeBSD pkg repository review candidate:"


class FreebsdPkgRepositoryReviewTests(unittest.TestCase):
    def scan(self, config=CONFIG, policy=POLICY, os_name="FreeBSD", hosts=True,
             symlink=False, parent_symlink=False):
        with tempfile.TemporaryDirectory(prefix="peas-pkg-") as root:
            root = Path(root)
            hosts_path = root / "hosts"
            if hosts:
                hosts_path.write_text("127.0.0.1 localhost\n")
            pkg_dir = root / "pkg"
            actual_dir = root / "realpkg" if parent_symlink else pkg_dir
            actual_dir.mkdir()
            if parent_symlink:
                pkg_dir.symlink_to(actual_dir, target_is_directory=True)
            target = actual_dir / "FreeBSD.conf"
            if symlink:
                real = root / "real.conf"
                real.write_text(config)
                target.symlink_to(real)
            else:
                target.write_text(config)
            source = (FUNCTION.replace("/etc/pkg/FreeBSD.conf", str(pkg_dir / "FreeBSD.conf"))
                    .replace("/etc/pkg", str(pkg_dir))
                    .replace("/etc/hosts", str(hosts_path)))
            shell = f"""
uname() {{ printf '%s\\n' {shlex.quote(os_name)}; }}
{source}
sudo_freebsd_pkg_repo_review {shlex.quote(policy)}
"""
            result = subprocess.run(["/bin/sh"], input=shell, text=True,
                                    capture_output=True, timeout=3)
            self.assertEqual(0, result.returncode, result.stderr)
            return result.stdout

    def test_exact_unsigned_http_repository_is_conditional_candidate(self):
        output = self.scan()
        self.assertEqual(1, output.count(MARKER))
        self.assertIn("effective repository overrides", output)
        self.assertNotIn("packages.example.invalid", output)
        combined = "    (root) NOPASSWD: /usr/sbin/pkg update, /usr/sbin/pkg install *\n"
        self.assertEqual(1, self.scan(policy=combined).count(MARKER))

    def test_requires_root_pkg_rules_and_writable_hosts(self):
        for policy, hosts, os_name in (
            (POLICY.replace("(ALL)", "(operator)"), True, "FreeBSD"),
            (POLICY.replace("NOPASSWD: ", ""), True, "FreeBSD"),
            (POLICY.replace("pkg install *", "pkg install trusted"), True, "FreeBSD"),
            (POLICY.replace("pkg update", "pkg update -f"), True, "FreeBSD"),
            (POLICY + "    (ALL) ! /usr/sbin/pkg install *\n", True, "FreeBSD"),
            (POLICY + "    (ALL) ! /usr/sbin/pkg update\n", True, "FreeBSD"),
            ("    (root) NOPASSWD: /usr/sbin/pkg update, ! /usr/sbin/pkg install *\n", True, "FreeBSD"),
            ("    (root) NOPASSWD: /usr/sbin/pkg update, PASSWD: /usr/sbin/pkg install *\n", True, "FreeBSD"),
            (POLICY.replace("NOPASSWD: /usr/sbin/pkg install", "NOEXEC: NOPASSWD: /usr/sbin/pkg install"), True, "FreeBSD"),
            (POLICY, False, "FreeBSD"),
            (POLICY, True, "OpenBSD"),
        ):
            with self.subTest(policy=policy, hosts=hosts, os_name=os_name):
                self.assertNotIn(MARKER, self.scan(policy=policy, hosts=hosts,
                                                    os_name=os_name))

    def test_rejects_signed_secure_disabled_or_mixed_repositories(self):
        for config in (
            CONFIG.replace('"none"', '"fingerprints"'),
            CONFIG.replace("pkg+http://", "pkg+https://"),
            CONFIG.replace("pkg+http://packages.example.invalid", "http://192.0.2.1"),
            CONFIG.replace("enabled: yes", "enabled: no"),
            CONFIG.replace("enabled: yes", ""),
            CONFIG.replace("signature_type: \"none\"", ""),
            CONFIG.replace("url: \"pkg+http://packages.example.invalid:80/repo\"", ""),
            '''Signed: { url: "pkg+http://packages.example.invalid/r" }
Unsigned: {
  url: "pkg+https://other.example.invalid/r",
  signature_type: "none",
  enabled: yes
}
''',
        ):
            with self.subTest(config=config):
                self.assertNotIn(MARKER, self.scan(config=config))

    def test_rejects_symlinks_and_oversize_config(self):
        self.assertNotIn(MARKER, self.scan(symlink=True))
        self.assertNotIn(MARKER, self.scan(parent_symlink=True))
        self.assertNotIn(MARKER, self.scan(config=CONFIG + "# padding\n" * 2100))


if __name__ == "__main__":
    unittest.main()
