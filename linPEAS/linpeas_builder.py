import os
import yaml
import re


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


PEAS_FINDS_MARKUP = YAML_LOADED["peas_finds_markup"]
FIND_LINE_MARKUP = YAML_LOADED["find_line_markup"]
FIND_TEMPLATE = YAML_LOADED["find_template"]

PEAS_STORAGES_MARKUP = YAML_LOADED["peas_storages_markup"]
STORAGE_LINE_MARKUP = YAML_LOADED["storage_line_markup"]
STORAGE_LINE_EXTRA_MARKUP = YAML_LOADED["storage_line_extra_markup"]
STORAGE_TEMPLATE = YAML_LOADED["storage_template"]

INT_HIDDEN_FILES_MARKUP = YAML_LOADED["int_hidden_files_markup"]



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
        self.bash_name = name.upper().replace(" ","_").replace("-","_")
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
        self.hidden_files = set()
        self.bash_find_f_vars, self.bash_find_d_vars = set(), set()
        self.bash_storages = set()
        self.__get_files_to_search()
        with open(LINPEAS_BASE_PATH, 'r') as file:
            self.linpeas_sh = file.read()

    def build(self):
        find_calls = self.__generate_finds()
        self.__replace_mark(PEAS_FINDS_MARKUP, find_calls, "  ")

        storage_vars = self.__generate_storages()
        self.__replace_mark(PEAS_STORAGES_MARKUP, storage_vars, "  ")

        #Check all the expected STORAGES in linpeas have been created
        for s in re.findall(r'PSTORAGE_[\w]*', self.linpeas_sh):
            assert s in self.bash_storages, f"{s} isn't created"

        #Replace interesting hidden files markup for a list of all the serched hidden files
        self.__replace_mark(INT_HIDDEN_FILES_MARKUP, self.hidden_files, "|")

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
                
                if frecord.regex[0] == "." or frecord.regex[:2] == "*.":
                    self.hidden_files.add(frecord.regex.replace("*",""))


    def __generate_finds(self) -> list:
        """Given the regexes to search on each root folder, generate the find command"""
        finds = []
        for type,searches in self.dict_to_search.items():
            for r,regexes in searches.items():
                if regexes:
                    find_line = f"{r} "
                    
                    if type == "d": 
                        find_line += "-type d "
                        bash_find_var = f"FIND_DIR_{r[1:].replace('.','').upper()}"
                        self.bash_find_d_vars.add(bash_find_var)
                    else:
                        bash_find_var = f"FIND_{r[1:].replace('.','').upper()}"
                        self.bash_find_f_vars.add(bash_find_var)

                    find_line += '-name \\"' + '\\" -o -name \\"'.join(regexes) + '\\"'
                    find_line = FIND_TEMPLATE.replace(FIND_LINE_MARKUP, find_line)
                    find_line = f"{bash_find_var}={find_line}"
                    finds.append(find_line)
            
        return finds

    def __generate_storages(self) -> list:
        """Generate the storages to save the results per entry"""
        storages = []
        all_f_finds = "$" + "\\n$".join(self.bash_find_f_vars)
        all_d_finds = "$" + "\\n$".join(self.bash_find_d_vars)
        all_finds = "$" + "\\n$".join(list(self.bash_find_f_vars) + list(self.bash_find_d_vars))
        
        for precord in self.ploaded.peasrecords:
            bash_storage_var = f"PSTORAGE_{precord.bash_name}"
            self.bash_storages.add(bash_storage_var)
            
            #Select the FIND_ variables to search on depending on the type files
            if all(frecord.type == "f" for frecord in precord.filerecords):
                storage_line = STORAGE_TEMPLATE.replace(STORAGE_LINE_MARKUP, all_f_finds)
            elif all(frecord.type == "d" for frecord in precord.filerecords):
                storage_line = STORAGE_TEMPLATE.replace(STORAGE_LINE_MARKUP, all_d_finds)
            else:
                storage_line = STORAGE_TEMPLATE.replace(STORAGE_LINE_MARKUP, all_finds)

            #Grep by filename regex (ended in '$')
            bsp = '\\.' #A 'f' expression cannot contain a backslash, so we generate here the bs need in the line below
            grep_names = f" | grep -E \"{'|'.join([frecord.regex.replace('.',bsp).replace('*', '.*')+'$' for frecord in precord.filerecords])}\""

            #Grep extra paths. They are accumulative between files of the same PEASRecord
            grep_extra_paths = ""
            if any(True for frecord in precord.filerecords if frecord.check_extra_path):
                grep_extra_paths = f" | grep -E '{'|'.join([frecord.check_extra_path for frecord in precord.filerecords if frecord.check_extra_path])}'"
            
            #Grep to remove paths. They are accumulative between files of the same PEASRecord
            grep_remove_path = ""
            if any(True for frecord in precord.filerecords if frecord.remove_path):
                grep_remove_path = f" | grep -v -E '{'|'.join([frecord.remove_path for frecord in precord.filerecords if frecord.remove_path])}'"
            
            #Construct the final line like: STORAGE_MYSQL=$(echo "$FIND_DIR_ETC\n$FIND_DIR_USR\n$FIND_DIR_VAR\n$FIND_DIR_MNT" | grep -E '^/etc/.*mysql|/usr/var/lib/.*mysql|/var/lib/.*mysql' | grep -v "mysql/mysql")
            storage_line = storage_line.replace(STORAGE_LINE_EXTRA_MARKUP, f"{grep_remove_path}{grep_extra_paths}{grep_names}")
            storage_line = f"{bash_storage_var}={storage_line}"
            storages.append(storage_line)
        
        return storages

        

    def __generate_sections(self):
        """Generate auto_check sections"""
        pass



    def __replace_mark(self, mark: str, find_calls: list, join_char: str):
        """Substitude the markup with the actual code"""
        self.linpeas_sh = self.linpeas_sh.replace(mark, join_char.join(find_calls)) #New line char is't needed
    
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