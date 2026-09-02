import json
import tempfile
import unittest
from pathlib import Path

from parsers import json2html

SAMPLE_JSON = {
    "System Information": {
        "sections": {
            "Operative system": {
                "sections": {},
                "lines": [
                    {
                        "raw_text": "[+] nmap is available",
                        "clean_text": "[+] nmap is available & ready <test>",
                        "colors": {"GREEN": ["nmap"]},
                    },
                    {
                        "raw_text": "[-] /usr/bin/echo",
                        "clean_text": "[-] /usr/bin/echo && chmod 2>/dev/null",
                        "colors": {},
                    },
                ],
                "infos": ["https://example.com/check?x=1&y=2"],
            }
        },
        "infos": [],
    }
}


class Json2HtmlTests(unittest.TestCase):
    def setUp(self):
        self._tmpdir = tempfile.TemporaryDirectory()
        self._json_path = Path(self._tmpdir.name) / "peas.json"
        self._html_path = Path(self._tmpdir.name) / "peas.html"

    def tearDown(self):
        self._tmpdir.cleanup()

    def _render(self, data) -> str:
        self._json_path.write_text(json.dumps(data))
        json2html.JSON_PATH = str(self._json_path)
        json2html.HTML_PATH = str(self._html_path)
        json2html.main()
        return self._html_path.read_text()

    def test_escapes_html_in_line_text(self):
        html = self._render(SAMPLE_JSON)
        self.assertIn("&lt;test&gt;", html)
        self.assertNotIn("ready <test>", html)
        self.assertIn("&amp;&amp;", html)
        self.assertNotIn("&& chmod", html)

    def test_escapes_html_in_infos(self):
        html = self._render(SAMPLE_JSON)
        self.assertIn("x=1&amp;y=2", html)
        self.assertNotIn("x=1&y=2", html)

    def test_escapes_section_names(self):
        data = {
            "A & B": {"sections": {}, "infos": []},
        }
        html = self._render(data)
        self.assertIn("A &amp; B", html)
        self.assertNotIn(">A & B</b>", html)

    def test_colored_replacement_is_escaped_and_kept(self):
        html = self._render(SAMPLE_JSON)
        self.assertIn('style="color:#008000">nmap</b>', html)
        self.assertIn('class = "green no_color"', html)

    def test_ids_are_deterministic_and_unique(self):
        first = self._render(SAMPLE_JSON)
        second = self._render(SAMPLE_JSON)
        self.assertEqual(first, second)
        self.assertEqual(first.count('id="lines1"'), 1)
        self.assertNotIn('id="lines0"', first)

    def test_top_level_infos_are_rendered(self):
        """Main sections carry infos in real linpeas output (╚ reference links) and
        must be rendered instead of being passed to parse_list as line objects."""
        data = {
            "Users Info": {
                "sections": {},
                "lines": [
                    {
                        "raw_text": "user1:x:1000:1000::/home/user1:/bin/bash",
                        "clean_text": "user1:x:1000:1000::/home/user1:/bin/bash",
                        "colors": {},
                    }
                ],
                "infos": ["https://book.hacktricks.xyz/privilege-escalation?x=1&y=2"],
            },
        }
        html = self._render(data)
        self.assertIn("https://book.hacktricks.xyz/privilege-escalation?x=1&amp;y=2", html)
        self.assertNotIn("https://book.hacktricks.xyz/privilege-escalation?x=1&y=2", html)

    def test_infos_with_markup_are_escaped_everywhere(self):
        data = {
            "Users Info": {
                "sections": {},
                "lines": [],
                "infos": ["run <script>alert(1)</script> now"],
            },
        }
        html = self._render(data)
        self.assertIn("&lt;script&gt;alert(1)&lt;/script&gt;", html)
        self.assertNotIn("<script>alert(1)</script>", html)


    def test_divs_are_balanced(self):
        html = self._render(SAMPLE_JSON)
        self.assertEqual(html.count("<div"), html.count("</div>"))


if __name__ == "__main__":
    unittest.main()
