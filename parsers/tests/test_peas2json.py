import unittest
import warnings
from pathlib import Path

from parsers import peas2json


class Peas2JsonTests(unittest.TestCase):
    def test_module_compiles_without_syntax_warning(self):
        source = Path(peas2json.__file__).read_text()
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            compile(source, str(peas2json.__file__), "exec")
        syntax_warnings = [w for w in caught if issubclass(w.category, SyntaxWarning)]
        self.assertEqual(syntax_warnings, [])

    def test_get_colors_stops_at_color_and_reset_boundaries(self):
        line = "\x1b[1;32muser\x1b[0m\x1b[1;31mroot\x1b[0m"
        self.assertEqual(peas2json.get_colors(line), {"GREEN": ["user"], "RED": ["root"]})

    def test_get_colors_of_plain_line_is_empty(self):
        self.assertEqual(peas2json.get_colors("no colors in here"), {})


if __name__ == "__main__":
    unittest.main()
