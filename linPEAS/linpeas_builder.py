import os
import yaml


CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
YAML_NAME = "sensitive_files.yaml"
FILES_YAML = CURRENT_DIR + "/../build_lists/" + YAML_NAME

with open(FILES_YAML, 'r') as file:
    YAML_LOADED = yaml.load(file, Loader=yaml.FullLoader)

ROOT_FOLDER = YAML_LOADED["root_folders"]
DEFAULTS = YAML_LOADED["defaults"]
COMMON_FILE_FOLDERS = YAML_LOADED["common_file_folders"]
COMMON_DIR_FOLDERS = YAML_LOADED["common_directory_folders"]
assert  all(f in ROOT_FOLDER for f in COMMON_FILE_FOLDERS)
assert  all(f in ROOT_FOLDER for f in COMMON_DIR_FOLDERS)


class FileRecord:
    def __init__(self,
                regex: str,
                bad_regex: str=DEFAULTS["bad_regex"],
                check_extra_path: str =DEFAULTS["check_extra_path"],
                files: dict={},
                good_regex: str=DEFAULTS["good_regex"],
                just_list_file: bool=DEFAULTS["just_list_file"],
                line_grep: str=DEFAULTS["line_grep"],
                only_bad_lines: bool=DEFAULTS["only_bad_lines"],
                remove_empty_lines: bool=DEFAULTS["remove_empty_lines"],
                remove_path: str=DEFAULTS["remove_path"],
                remove_regex: str=DEFAULTS["remove_regex"],
                search_in: list=DEFAULTS["search_in"],
                type: str=DEFAULTS["type"],
                ):

        self.regex = regex
        self.bad_regex = bad_regex
        self.check_extra_path = check_extra_path
        self.files = [FileRecord(regex=regex,**fr) for regex,fr in files.items()]
        self.good_regex = good_regex
        self.just_list_file = just_list_file
        self.line_grep = line_grep
        self.only_bad_lines = only_bad_lines
        self.remove_regex = remove_regex
        self.remove_empty_lines = remove_empty_lines
        self.remove_path = remove_path
        self.type = search_in
        self.type = type


class PEASRecord:
    def __init__(self, name, auto_check: bool, exec: list, filerecords: list):
        self.name = name
        self.auto_check = auto_check
        self.exec = exec
        self.filerecords = filerecords


class PEASLoaded:
    def __init__(self):
        to_search = YAML_LOADED["search"]
        self.peasrecords = []
        for name,peasrecord_json in to_search.items():
            filerecords = []
            for regex,fr in peasrecord_json["files"].items():
                filerecords.append(
                    FileRecord(
                        regex=regex,
                        **fr
                    )
                )
                
            self.peasrecords.append(
                PEASRecord(
                    name=name,
                    auto_check=peasrecord_json["config"]["auto_check"],
                    exec=peasrecord_json["config"].get("exec", DEFAULTS["exec"]),
                    filerecords=filerecords
                )
            )



def main():
    ploaded = PEASLoaded()
    print(ploaded.peasrecords)

main()