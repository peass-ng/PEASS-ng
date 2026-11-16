"""Keep the UMP config classifier equivalent to its six original grep tests."""

import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


SOURCE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/functions/checkUMPConversionCVE202537891.sh"


def original_states(lines):
    patterns = (
        (r"^CONFIG_SND_UMP=[ym]$", r"^# CONFIG_SND_UMP is not set$|^CONFIG_SND_UMP=n$"),
        (r"^CONFIG_SND_UMP_LEGACY_RAWMIDI=[ym]$", r"^# CONFIG_SND_UMP_LEGACY_RAWMIDI is not set$|^CONFIG_SND_UMP_LEGACY_RAWMIDI=n$"),
        (r"^CONFIG_SND_USB_AUDIO_MIDI_V2=[ym]$", r"^# CONFIG_SND_USB_AUDIO_MIDI_V2 is not set$|^CONFIG_SND_USB_AUDIO_MIDI_V2=n$"),
    )
    return tuple(
        "disabled" if re.search(disabled, lines, re.MULTILINE) else
        "enabled" if re.search(enabled, lines, re.MULTILINE) else "unknown"
        for enabled, disabled in patterns
    )


class UmpConfigStatesTests(unittest.TestCase):
    def test_original_classifications_and_output(self):
        source = SOURCE.read_text()
        classifier = '  if command -v awk >/dev/null 2>&1; then\n' + source.split(
            '  if command -v awk >/dev/null 2>&1; then\n', 1
        )[1].split('  echo "UMP config:', 1)[0]
        script = (
            'ump37891_lines=$1\n' + classifier +
            'printf "%s;%s;%s\\n" "$ump37891_ump" "$ump37891_legacy" "$ump37891_usb"\n'
        )
        fixtures = (
            "",
            "CONFIG_SND_UMP=y\nCONFIG_SND_UMP_LEGACY_RAWMIDI=m\nCONFIG_SND_USB_AUDIO_MIDI_V2=y",
            "# CONFIG_SND_UMP is not set\nCONFIG_SND_UMP_LEGACY_RAWMIDI=n\n# CONFIG_SND_USB_AUDIO_MIDI_V2 is not set",
            "CONFIG_SND_UMP=n\nCONFIG_SND_UMP=y\nCONFIG_SND_USB_AUDIO_MIDI_V2=m\nCONFIG_SND_USB_AUDIO_MIDI_V2=n",
            "CONFIG_SND_UMP=y\nCONFIG_SND_UMP=n\nCONFIG_SND_UMP_LEGACY_RAWMIDI=m\n# CONFIG_SND_UMP_LEGACY_RAWMIDI is not set",
            " CONFIG_SND_UMP=y\nCONFIG_SND_UMP=Y\nCONFIG_SND_UMP=ym\nCONFIG_SND_UMP=y extra\nCONFIG_SND_USB_AUDIO_MIDI_V2=m\r",
            "CONFIG_SND_UMP=y\n" * 200 + "CONFIG_SND_UMP=n\n" + "CONFIG_SND_USB_AUDIO_MIDI_V2=m\n" * 200,
        )
        with tempfile.TemporaryDirectory() as tmp:
            # Exercise the grep fallback without hiding grep itself.
            grep = shutil.which("grep")
            self.assertIsNotNone(grep)
            (Path(tmp) / "grep").symlink_to(grep)
            for shell in ("dash", "bash"):
                executable = shutil.which(shell)
                if not executable:
                    continue
                for lines in fixtures:
                    for path in (None, tmp):
                        with self.subTest(shell=shell, fallback=bool(path), lines=lines[:40]):
                            result = subprocess.run(
                                [executable, "-c", script, "ump-config-test", lines],
                                env={"PATH": path} if path else None,
                                capture_output=True, text=True, check=True,
                            )
                            self.assertEqual(";".join(original_states(lines)) + "\n", result.stdout)


if __name__ == "__main__":
    unittest.main()
