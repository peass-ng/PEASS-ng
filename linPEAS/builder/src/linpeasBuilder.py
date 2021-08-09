import re
import requests

from .peasLoaded import PEASLoaded
from .peassRecord import PEASRecord
from .fileRecord import FileRecord
from .yamlGlobals import (
    LINPEAS_BASE_PATH,
    PEAS_FINDS_MARKUP,
    PEAS_STORAGES_MARKUP,
    PEAS_STORAGES_MARKUP,
    INT_HIDDEN_FILES_MARKUP,
    ROOT_FOLDER,
    STORAGE_TEMPLATE,
    FIND_TEMPLATE,
    FIND_LINE_MARKUP,
    STORAGE_LINE_MARKUP,
    STORAGE_LINE_EXTRA_MARKUP,
    EXTRASECTIONS_MARKUP,
    PEAS_VARIABLES_MARKUP,
    YAML_VARIABLES,
    SUIDVB1_MARKUP,
    SUIDVB2_MARKUP,
    SUDOVB1_MARKUP,
    SUDOVB2_MARKUP,
    CAP_SETUID_MARKUP,
    CAP_SETGID_MARKUP
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
        print("[+] Building variables...")
        variables = self.__generate_variables()
        self.__replace_mark(PEAS_VARIABLES_MARKUP, variables, "")

        print("[+] Building finds...")
        find_calls = self.__generate_finds()
        self.__replace_mark(PEAS_FINDS_MARKUP, find_calls, "  ")

        print("[+] Building storages...")
        storage_vars = self.__generate_storages()
        self.__replace_mark(PEAS_STORAGES_MARKUP, storage_vars, "  ")

        #Check all the expected STORAGES in linpeas have been created
        for s in re.findall(r'PSTORAGE_[\w]*', self.linpeas_sh):
            assert s in self.bash_storages, f"{s} isn't created"

        #Replace interesting hidden files markup for a list of all the searched hidden files
        self.__replace_mark(INT_HIDDEN_FILES_MARKUP, sorted(self.hidden_files), "|")

        print("[+] Checking duplicates...")
        peass_marks = self.__get_peass_marks()
        for i,mark in enumerate(peass_marks):
            for j in range(i+1,len(peass_marks)):
                assert mark != peass_marks[j], f"Found repeated peass mark: {mark}"

        print("[+] Building autocheck sections...")
        sections = self.__generate_sections()
        for section_name, bash_lines in sections.items():
            mark = "peass{"+section_name+"}"
            if mark in peass_marks:
                self.__replace_mark(mark, list(bash_lines), "")
            else:
                self.__replace_mark(EXTRASECTIONS_MARKUP, [bash_lines, EXTRASECTIONS_MARKUP], "\n\n")
        
        self.__replace_mark(EXTRASECTIONS_MARKUP, list(""), "") #Delete extra markup

        print("[+] Building GTFOBins lists...")
        suidVB, sudoVB, capsVB = self.__get_gtfobins_lists()
        self.__replace_mark(SUIDVB1_MARKUP, suidVB[:int(len(suidVB)/2)], "|")
        self.__replace_mark(SUIDVB2_MARKUP, suidVB[int(len(suidVB)/2):], "|")
        self.__replace_mark(SUDOVB1_MARKUP, sudoVB[:int(len(sudoVB)/2)], "|")
        self.__replace_mark(SUDOVB2_MARKUP, sudoVB[int(len(sudoVB)/2):], "|")
        self.__replace_mark(CAP_SETUID_MARKUP, capsVB, "|")
        self.__replace_mark(CAP_SETGID_MARKUP, capsVB, "|")

        print("[+] Final sanity checks...")
        #Check that there arent peass marks left in linpeas
        peass_marks = self.__get_peass_marks()
        assert len(peass_marks) == 0, f"There are peass marks left: {', '.join(peass_marks)}"
        
        #Check for empty seds
        assert 'sed -${E} "s,,' not in self.linpeas_sh

    
    def __get_peass_marks(self):
        return re.findall(r'peass\{[\w\-\._ ]*\}', self.linpeas_sh)

    
    def __generate_variables(self):
        """Generate the variables from the yaml to set into linpeas bash script"""
        variables_bash = ""
        for var in YAML_VARIABLES:
            variables_bash += f"{var['name']}=\"{var['value']}\"\n"
        
        return variables_bash


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

            #Grep by searched folders
            grep_folders_searched = f" | grep -E \"^{'|^'.join(list(set([d for frecord in precord.filerecords for d in frecord.search_in])))}\"".replace("HOMESEARCH","GREPHOMESEARCH")

            #Grep extra paths. They are accumulative between files of the same PEASRecord
            grep_extra_paths = ""
            if any(True for frecord in precord.filerecords if frecord.check_extra_path):
                grep_extra_paths = f" | grep -E '{'|'.join(list(set([frecord.check_extra_path for frecord in precord.filerecords if frecord.check_extra_path])))}'"
            
            #Grep to remove paths. They are accumulative between files of the same PEASRecord
            grep_remove_path = ""
            if any(True for frecord in precord.filerecords if frecord.remove_path):
                grep_remove_path = f" | grep -v -E '{'|'.join(list(set([frecord.remove_path for frecord in precord.filerecords if frecord.remove_path])))}'"
            
            #Construct the final line like: STORAGE_MYSQL=$(echo "$FIND_DIR_ETC\n$FIND_DIR_USR\n$FIND_DIR_VAR\n$FIND_DIR_MNT" | grep -E '^/etc/.*mysql|/usr/var/lib/.*mysql|/var/lib/.*mysql' | grep -v "mysql/mysql")
            storage_line = storage_line.replace(STORAGE_LINE_EXTRA_MARKUP, f"{grep_remove_path}{grep_extra_paths}{grep_folders_searched}{grep_names}")
            storage_line = f"{bash_storage_var}={storage_line}"
            storages.append(storage_line)
        
        return storages

    def __generate_sections(self) -> dict:
        """Generate sections for records with auto_check to True"""
        sections = {}

        for precord in self.ploaded.peasrecords:
            if precord.auto_check:
                section = f'  print_2title "Analyzing {precord.name.replace("_"," ")} Files (limit 70)"\n'

                for exec_line in precord.exec:
                    if exec_line:
                        section += "    " + exec_line + "\n"

                for frecord in precord.filerecords:
                    section += "    " + self.__construct_file_line(precord, frecord) + "\n"
                
                sections[precord.name] = section

        return sections

    def __construct_file_line(self, precord: PEASRecord, frecord: FileRecord, init: bool = True) -> str:
        real_regex = frecord.regex[1:] if frecord.regex.startswith("*") and len(frecord.regex) > 1 else frecord.regex
        real_regex = real_regex.replace(".","\\.").replace("*",".*")
        real_regex += "$"
        
        analise_line = ""
        if init:
            analise_line = 'if ! [ "`echo \\\"$PSTORAGE_'+precord.bash_name+'\\\" | grep -E \\\"'+real_regex+'\\\"`" ]; then echo_not_found "'+frecord.regex+'"; fi; '
            analise_line += 'printf "%s" "$PSTORAGE_'+precord.bash_name+'" | grep -E "'+real_regex+'" | while read f; do ls -ld "$f" | sed -${E} "s,'+real_regex+',${SED_RED},"; '

        #If just list, just list the file/directory
        if frecord.just_list_file:
            if frecord.type == "d":
                analise_line += 'ls -lRA "$f";'
            analise_line += 'done; echo "";'
            return analise_line
        
        if frecord.type == "f":
            grep_empty_lines = ' | grep -IEv "^$"'
            grep_line_grep = f' | grep -E {frecord.line_grep}' if frecord.line_grep else ""
            grep_only_bad_lines = f' | grep -E "{frecord.bad_regex}"' if frecord.bad_regex else ""
            grep_remove_regex = f' | grep -Ev "{frecord.remove_regex}"' if frecord.remove_regex else ""
            sed_bad_regex = ' | sed -${E} "s,'+frecord.bad_regex+',${SED_RED},g"' if frecord.bad_regex else ""
            sed_good_regex = ' | sed -${E} "s,'+frecord.good_regex+',${SED_GOOD},g"' if frecord.good_regex else ""

            if init:
                analise_line += 'cat "$f" 2>/dev/null'
            else:
                analise_line += 'cat "$ff" 2>/dev/null'

            if grep_empty_lines:
                analise_line += grep_empty_lines

            if grep_line_grep:
                analise_line += grep_line_grep

            if frecord.only_bad_lines and not grep_line_grep:
                analise_line += grep_only_bad_lines

            if grep_remove_regex:
                analise_line += grep_remove_regex
            
            if sed_bad_regex:
                analise_line += sed_bad_regex

            if sed_good_regex:
                analise_line += sed_good_regex
            
            analise_line += '; done; echo "";'
            return analise_line

        #In case file is type "d"
        if frecord.files:
            for ffrecord in frecord.files:
                ff_real_regex = ffrecord.regex[1:] if ffrecord.regex.startswith("*") and ffrecord.regex != "*" else ffrecord.regex
                ff_real_regex = ff_real_regex.replace("*",".*")
                analise_line += 'for ff in $(find "$f" -name "'+ffrecord.regex+'"); do ls -ld "$ff" | sed -${E} "s,'+ff_real_regex+',${SED_RED},"; ' + self.__construct_file_line(precord, ffrecord, init=False)
        
        analise_line += 'done; echo "";'
        return analise_line

    def __get_gtfobins_lists(self) -> tuple:
        r = requests.get("https://github.com/GTFOBins/GTFOBins.github.io/tree/master/_gtfobins")
        bins = re.findall(r'/GTFOBins/GTFOBins.github.io/blob/master/_gtfobins/([\w_ \-]+).md', r.text)

        sudoVB = []
        suidVB = []
        capsVB = []

        for b in bins:
            rb = requests.get(f"https://raw.githubusercontent.com/GTFOBins/GTFOBins.github.io/master/_gtfobins/{b}.md")
            if "sudo:" in rb.text:
                sudoVB.append(b+"$")
            if "suid:" in rb.text:
                suidVB.append("/"+b+"$")
            if "capabilities:" in rb.text:
                capsVB.append(b)
        
        return (suidVB, sudoVB, capsVB)


    def __replace_mark(self, mark: str, find_calls: list, join_char: str):
        """Substitude the markup with the actual code"""
        self.linpeas_sh = self.linpeas_sh.replace(mark, join_char.join(find_calls)) #New line char is't needed
    
    def write_linpeas(self, path):
        """Write on disk the final linpeas"""
        with open(path, "w") as f:
            f.write(self.linpeas_sh)