from .yamlGlobals import DEFAULTS, ROOT_FOLDER, COMMON_DIR_FOLDERS, COMMON_FILE_FOLDERS

class FileRecord:
    def __init__(self,
                regex: str,
                bad_regex: str=DEFAULTS["bad_regex"],
                very_bad_regex: str=DEFAULTS["very_bad_regex"],
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
        self.very_bad_regex = very_bad_regex
        self.check_extra_path = check_extra_path
        self.files = [FileRecord(regex=fr["name"],**fr["value"]) for fr in files]
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
        for r in search_in:
            assert r in ROOT_FOLDER, f"{r} not in {ROOT_FOLDER}"
        
        return search_in
