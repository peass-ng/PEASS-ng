import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/2_container/7_RW_bind_mounts_nosuid.sh"
)


class ContainerWritableMountTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.functions = MODULE.read_text().split("\ncontainerCheck\n", 1)[0]

    def run_check(self, function, contents, *arguments):
        with tempfile.TemporaryDirectory() as directory:
            fixture = Path(directory) / "fixture"
            fixture.write_text(contents)
            return subprocess.run(
                ["sh", "-c", self.functions + f'\n{function} "$1" "$2"', "test", str(fixture), *arguments],
                check=True,
                capture_output=True,
                text=True,
                timeout=10,
            ).stdout

    def test_mountinfo_fields_and_constraints(self):
        fixture = (
            "36 25 8:1 /opt/app /var/www/html/app rw,relatime - ext4 /dev/sda1 rw\n"
            "37 25 8:1 / /whole rw shared:42 - ext4 /dev/sda1 rw\n"
            "38 25 8:1 /opt/read-only /read-only ro - ext4 /dev/sda1 rw\n"
            "39 25 8:1 /opt/super-read-only /super-read-only rw - ext4 /dev/sda1 ro\n"
            "40 25 8:1 /opt/nosuid /nosuid rw,nosuid - ext4 /dev/sda1 rw\n"
            "41 25 8:1 /opt/noexec /noexec rw,noexec - ext4 /dev/sda1 rw\n"
            "42 25 0:1 /secrets /secret rw - tmpfs tmpfs rw\n"
            "43 25 0:2 / /proc rw - proc proc rw\n"
            "44 25 8:1 /opt/a\\040b /space\\040here rw master:3 - ext4 /dev/sda1 rw\n"
            "45 25 8:1 /container-only /container-only rw - ext4 /dev/sda1 rw\n"
            "46 25 8:1 /var/lib /etc/hosts rw - ext4 /dev/sda1 rw\n"
        )
        result = self.run_check("ct_rw_mountinfo_candidates", fixture)
        self.assertIn("mount(raw)=/var/www/html/app root(raw)=/opt/app", result)
        self.assertIn("mount(raw)=/whole root(raw)=/", result)
        self.assertIn("mount(raw)=/space\\040here root(raw)=/opt/a\\040b", result)
        self.assertIn("mount(raw)=/container-only", result)
        self.assertIn("mount(raw)=/nosuid", result)
        self.assertIn("opts=rw,nosuid local-suid=blocked-here", result)
        self.assertIn("opts=rw,noexec local-suid=not-blocked-here local-exec=blocked-here", result)
        self.assertNotIn("mount(raw)=/read-only", result)
        self.assertNotIn("mount(raw)=/super-read-only", result)
        self.assertNotIn("mount(raw)=/secret", result)
        self.assertNotIn("mount(raw)=/proc", result)
        self.assertNotIn("mount(raw)=/etc/hosts", result)
        self.assertLess(result.index("/var/www/html/app"), result.index("/whole"))
        self.assertNotIn("host bind", result.lower())

    def test_output_is_bounded(self):
        fixture = "".join(
            f"{i} 25 8:1 /opt/{i} /app/{i} rw - ext4 /dev/sda1 rw\n"
            for i in range(30, 55)
        )
        result = self.run_check("ct_rw_mountinfo_candidates", fixture)
        self.assertEqual(result.count("mount(raw)="), 10)
        self.assertIn("15 more ordinary writable mounts omitted", result)

    def test_uid_mapping_is_cautious(self):
        direct = self.run_check("ct_rw_uid_note", "0 0 4294967295\n", "0")
        remapped = self.run_check("ct_rw_uid_note", "0 100000 65536\n", "0")
        nonroot = self.run_check("ct_rw_uid_note", "0 0 4294967295\n", "1000")
        self.assertIn("host-root mapping is unverified", direct)
        self.assertIn("remapped", remapped)
        self.assertIn("Current effective UID is not 0", nonroot)

    def test_proc_mounts_fallback_is_generic(self):
        fixture = (
            "/dev/sda1 /shared ext4 rw,nosuid 0 0\n"
            "/dev/sda1 /read-only ext4 ro 0 0\n"
            "tmpfs /scratch tmpfs rw 0 0\n"
        )
        result = self.run_check("ct_rw_mounts_fallback", fixture)
        self.assertIn("mount(raw)=/shared", result)
        self.assertNotIn("/read-only", result)
        self.assertNotIn("/scratch", result)
        self.assertNotIn("root(raw)=", result)


if __name__ == "__main__":
    unittest.main()
