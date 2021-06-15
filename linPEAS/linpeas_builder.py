import os
import yaml


CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
LINPEAS_BASE_PATH = CURRENT_DIR + "/base/" + "linpeas_base.sh"
FINAL_LINPEAS_PATH = CURRENT_DIR + "/" + "linpeas.sh"
YAML_NAME = "sensitive_files.yaml"
FILES_YAML = CURRENT_DIR + "/../build_lists/" + YAML_NAME

with open(FILES_YAML, 'r') as file:
    YAML_LOADED = yaml.load(file, Loader=yaml.FullLoader)

ROOT_FOLDER = YAML_LOADED["root_folders"]
DEFAULTS = YAML_LOADED["defaults"]
COMMON_FILE_FOLDERS = YAML_LOADED["common_file_folders"]
COMMON_DIR_FOLDERS = YAML_LOADED["common_directory_folders"]
assert all(f in ROOT_FOLDER for f in COMMON_FILE_FOLDERS)
assert all(f in ROOT_FOLDER for f in COMMON_DIR_FOLDERS)
PEAS_SEARCH_MARKUP = YAML_LOADED["peas_search_markup"]
FIND_SEARCH_MARKUP = YAML_LOADED["find_search_markup"]
FIND_TEMPLATE = YAML_LOADED["find_template"]


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
        self.type = type
        self.search_in = self.__resolve_search_in(search_in)

    def __resolve_search_in(self, search_in):
        """ Resolve spacial values to the correct directories """

        if "all" in search_in:
            search_in.remove("all")
            search_in = ROOT_FOLDER

        if "common" in search_in:
            search_in.remove("common")
            if self.type == "d":
                search_in = list(set(search_in + COMMON_DIR_FOLDERS))
            else:
                search_in = list(set(search_in + COMMON_FILE_FOLDERS))
        
        #Check that folders to search in are specified in ROOT_FOLDER
        assert all(r in ROOT_FOLDER for r in search_in)
        
        return search_in


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


class LinpeasBuilder:
    def __init__(self, ploaded:PEASLoaded):
        self.ploaded = ploaded
        self.__get_files_to_search()
        with open(LINPEAS_BASE_PATH, 'r') as file:
            self.linpeas_sh = file.read()

    def build(self):
        find_calls = self.__generate_finds()
        self.__write_finds(find_calls)
        self.__write_linpeas()


    def __get_files_to_search(self):
        """Given a PEASLoaded and find the files that need to be searched on each root folder"""
        self.dict_to_search = {"d": {}, "f": {}}
        self.dict_to_search["d"] = {r: set() for r in ROOT_FOLDER}
        self.dict_to_search["f"] = {r: set() for r in ROOT_FOLDER}

        for precord in self.ploaded.peasrecords:
            for frecord in precord.filerecords:
                for folder in frecord.search_in:
                    self.dict_to_search[frecord.type][folder].add(frecord.regex)


    def __generate_finds(self):
        """Given the regexes to search on each root folder, generate the find command"""
        finds = []
        for type,searches in self.dict_to_search.items():
            for r,regexes in searches.items():
                find_line = f"{r} "
                if type == "d": find_line += "-type d "
                find_line += '-name \\"' + '\\" -o -name \\"'.join(regexes) + '\\"'

                find_line = FIND_TEMPLATE.replace(FIND_SEARCH_MARKUP, find_line)
                find_line = f"FIND_{r[1:].upper()}={find_line}"
                finds.append(find_line)
        
        return finds


    def __write_finds(self, find_calls):
        """Substitude the markup with the actual find code"""
        self.linpeas_sh = self.linpeas_sh.replace(PEAS_SEARCH_MARKUP, "\n".join(find_calls))
    
    def __write_linpeas(self):
        """Write on disk the final linpeas"""
        with open(FINAL_LINPEAS_PATH, "w") as f:
            f.write(self.linpeas_sh)



def main():
    ploaded = PEASLoaded()
    lbuilder = LinpeasBuilder(ploaded)
    lbuilder.build()


if __name__ == "__main__":
    main()