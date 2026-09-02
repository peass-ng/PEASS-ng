import unittest

try:
    from reportlab.platypus import Paragraph

    from parsers import json2pdf

    HAS_REPORTLAB = True
except ImportError:  # pragma: no cover - depends on the environment
    HAS_REPORTLAB = False


SECTION = {
    "infos": ['see <tool> "settings" at https://book.hacktricks.xyz/x?y=1&z=2'],
    "lines": [
        {"raw_text": "a", "colors": {}, "clean_text": "line one"},
        {"raw_text": "b", "colors": {}, "clean_text": "line two"},
    ],
    "sections": {},
}


@unittest.skipUnless(HAS_REPORTLAB, "reportlab is not installed")
class Json2PdfTests(unittest.TestCase):
    def test_info_words_are_escaped_before_reaching_the_paragraph(self):
        elements = json2pdf.build_main_section(SECTION, "Escalation")
        info_paragraphs = [e for e in elements if isinstance(e, Paragraph) and e.style.name == "Italic"]
        self.assertEqual(len(info_paragraphs), 1)
        markup = info_paragraphs[0].text
        self.assertIn("&lt;tool&gt;", markup)
        self.assertNotIn("<tool>", markup)
        self.assertIn('href="https://book.hacktricks.xyz/x?y=1&amp;z=2"', markup)
        self.assertNotIn('href="https://book.hacktricks.xyz/x?y=1&z=2"', markup)

    def test_document_renders_hostile_infos_without_error(self):
        from parsers.json2pdf import MyDocTemplate

        doc = MyDocTemplate("/tmp/peas_test_hostile.pdf")
        doc.build(json2pdf.build_main_section(SECTION, "Escalation"))


if __name__ == "__main__":
    unittest.main()
