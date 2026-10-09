"""The OMI cue correlates passive evidence without claiming exploitability."""

import shlex
import subprocess
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/7_software_information/Omi.sh")


class OmiReviewTests(unittest.TestCase):
    def run_function(self, body, image=None):
        source = MODULE.read_text().rsplit("\ncheck_omi_exposure", 1)[0]
        script = source + "\n" + body
        if image:
            result = subprocess.run(
                ["docker", "run", "--rm", "-i", image, "sh", "-c", script],
                capture_output=True, text=True, timeout=20,
            )
        else:
            result = subprocess.run(
                ["sh", "-c", script], capture_output=True, text=True,
                timeout=5,
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_socket_formats_and_exact_port(self):
        data = (
            "printf '%s\\n' "
            "'LISTEN 0 128 127.0.0.1:5985 0.0.0.0:*' "
            "'tcp 0 0 127.0.0.1:5986 0.0.0.0:* LISTEN' "
            "'tcp4 0 0 *.5985 *.* LISTEN' | omi_listener"
        )
        self.assertEqual(self.run_function(data).strip(), "127.0.0.1:5985")
        self.assertEqual(
            self.run_function("printf '%s\\n' 'tcp4 0 0 *.5985 *.* LISTEN' | omi_listener").strip(),
            "*.5985",
        )
        self.assertEqual(
            self.run_function("printf '%s\\n' 'LISTEN 0 128 127.0.0.1:59850 0.0.0.0:*' | omi_listener").strip(),
            "",
        )

    def test_exact_process_and_candidate_prerequisites(self):
        self.assertEqual(
            self.run_function("printf '%s\\n' 'root omiengine-helper' 'root omiengine' | omi_process_user").strip(),
            "root",
        )
        lead = self.run_function("omi_report_evidence root 127.0.0.1:5985 1.6.8-0")
        self.assertIn("review candidate", lead)
        self.assertIn("socket ownership unproven", lead)
        self.assertIn("backports", lead)
        for owner, port in (("daemon", "127.0.0.1:5985"), ("root", ""), ("root", "#limit")):
            with self.subTest(owner=owner, port=port):
                output = self.run_function(
                    f"omi_report_evidence {shlex.quote(owner)} {shlex.quote(port)} 1.6.8-0"
                )
                self.assertNotIn("review candidate", output)

    def test_input_caps(self):
        self.assertEqual(
            self.run_function("awk 'BEGIN { for (i=1; i<=4097; i++) print \"other x\" }' | omi_process_user").strip(),
            "#limit",
        )
        self.assertEqual(
            self.run_function("awk 'BEGIN { for (i=1; i<=4097; i++) print \"tcp 0 0 *.1234 *.* LISTEN\" }' | omi_listener").strip(),
            "#limit",
        )


if __name__ == "__main__":
    unittest.main()
