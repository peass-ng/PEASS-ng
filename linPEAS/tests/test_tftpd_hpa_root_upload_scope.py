from pathlib import Path
import subprocess
import tempfile
import unittest


PART = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/10_Services.sh"
)


class TftpdHpaRootUploadScopeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = PART.read_text()
        begin = source.index("check_tftpd_hpa_root_upload_scope() (")
        end = source.index('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', begin)
        cls.function = source[begin:end]
        cls.module = source

    def probe(self, content=None, *, symlink=False):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "tftpd-hpa"
            if content is not None:
                target = Path(directory) / "target"
                target.write_text(content)
                if symlink:
                    path.symlink_to(target)
                else:
                    target.rename(path)
            script = (
                'print_3title() { printf "%s\\n" "$1"; }\n'
                + self.function
                + '\ncheck_tftpd_hpa_root_upload_scope "$1"\n'
            )
            result = subprocess.run(
                ["sh", "-c", script, "sh", str(path)],
                capture_output=True,
                text=True,
                check=True,
            )
            return result.stdout

    def test_exact_root_scope_candidate(self):
        output = self.probe(
            'TFTP_USERNAME="root"\nTFTP_DIRECTORY="/"\n'
            'TFTP_ADDRESS=":69"\nTFTP_OPTIONS="--secure --create"\n'
        )
        self.assertIn("Root TFTP file-creation scope", output)
        self.assertIn("effective service", output)
        self.assertNotIn('TFTP_ADDRESS', output)

    def test_irrelevant_or_disabled_scope_is_silent(self):
        good = 'TFTP_USERNAME=root\nTFTP_DIRECTORY=/\nTFTP_OPTIONS="--secure --create"\n'
        self.assertEqual("", self.probe())
        self.assertEqual("", self.probe(good.replace("root\n", "tftp\n", 1)))
        self.assertEqual("", self.probe(good.replace("DIRECTORY=/", "DIRECTORY=/srv/tftp")))
        self.assertEqual("", self.probe(good.replace("--create", "--verbose")))
        self.assertEqual("", self.probe(good.replace("--secure", "--verbose")))
        self.assertEqual("", self.probe(good.replace("--create", "--create --read-only")))
        self.assertEqual("", self.probe("# " + good.replace("\n", "\n# ")))

    def test_rejects_symlink_and_oversized_config(self):
        good = 'TFTP_USERNAME=root\nTFTP_DIRECTORY=/\nTFTP_OPTIONS="--secure --create"\n'
        self.assertEqual("", self.probe(good, symlink=True))
        self.assertEqual("", self.probe(good + "#" * 4097))

    def test_last_assignment_wins_and_output_does_not_echo_config(self):
        config = (
            'TFTP_USERNAME=tftp\nTFTP_USERNAME=root\n'
            'TFTP_DIRECTORY=/srv/tftp\nTFTP_DIRECTORY=/\n'
            'TFTP_OPTIONS="--secure --create"\nSECRET_TOKEN=do-not-print\n'
        )
        output = self.probe(config)
        self.assertIn("Root TFTP file-creation scope", output)
        self.assertNotIn("do-not-print", output)
        self.assertEqual("", self.probe(config + "TFTP_USERNAME=tftp\n"))

    def test_host_only_and_fixed_path_invocation(self):
        self.assertIn('if ! [ "$SEARCH_IN_FOLDER" ]; then', self.module)
        self.assertIn('  check_tftpd_hpa_root_upload_scope\n', self.module)
        self.assertIn('/etc/default/tftpd-hpa', self.function)
        self.assertNotIn("tftp ", self.function)


if __name__ == "__main__":
    unittest.main()
