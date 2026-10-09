"""Static side-effect guard for the passive Visual Studio collector cue."""

from pathlib import Path
import unittest


SOURCE = Path(__file__).resolve().parents[1] / "winPEAS/Info/ServicesInfo/VisualStudioCollectorIndicator.cs"


class VisualStudioCollectorSafeguards(unittest.TestCase):
    def test_collector_only_reads_one_service_key_and_one_file_metadata_path(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertIn("Registry.LocalMachine.OpenSubKey(ServiceKey)", source)
        self.assertEqual(source.count("OpenSubKey("), 1)
        self.assertEqual(source.count("File.Exists"), 1)
        for active_operation in (
            "ServiceController", "Process.Start", "ProcessStartInfo", "Registry.SetValue",
            "File.Read", "File.Write", "File.Copy", "File.Delete", "Directory.Enumerate",
            "ManagementObject", "ManagementClass", "WmiQuery",
        ):
            with self.subTest(operation=active_operation):
                self.assertNotIn(active_operation, source)


if __name__ == "__main__":
    unittest.main()
