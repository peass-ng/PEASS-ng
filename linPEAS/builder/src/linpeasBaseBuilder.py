from .yamlGlobals import (
    LINPEAS_PARTS,
    LINPEAS_BASE_PATH,
    TEMPORARY_LINPEAS_BASE_PATH,
    PEAS_CHECKS_MARKUP
)

class LinpeasBaseBuilder:
    def __init__(self):
        with open(LINPEAS_BASE_PATH, 'r') as file:
            self.linpeas_base = file.read()

    def build(self):
        print("[+] Building temporary linpeas_base.sh...")
        checks = []
        for part in LINPEAS_PARTS:
            name = part["name"]
            assert name, f"Name not found in {part}"
            name_check = part["name_check"]
            assert name_check, f"Name not found in {name_check}"
            file_path = part["file_path"]
            assert file_path, f"Name not found in {file_path}"

            with open(file_path, 'r') as file:
                linpeas_part = file.read()

            checks.append(name_check)
            self.linpeas_base += f"\nif echo $CHECKS | grep -q {name_check}; then\n"
            self.linpeas_base += f'print_title "{name}"\n'
            self.linpeas_base += linpeas_part
            self.linpeas_base += f"\nfi\necho ''\necho ''\n"
            self.linpeas_base += 'if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi\n'

        self.linpeas_base = self.linpeas_base.replace(PEAS_CHECKS_MARKUP, ",".join(checks))

        with open(TEMPORARY_LINPEAS_BASE_PATH, "w") as f:
            f.write(self.linpeas_base)
