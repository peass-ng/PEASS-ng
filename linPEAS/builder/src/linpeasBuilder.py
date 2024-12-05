import re
import requests
import base64
import os
from pathlib import Path

from .peasLoaded import PEASLoaded
from .peassRecord import PEASRecord
from .fileRecord import FileRecord
from .linpeasModule import LinpeasModule
from .yamlGlobals import (
    TEMPORARY_LINPEAS_BASE_PATH,
    PEAS_FINDS_MARKUP,
    PEAS_FINDS_CUSTOM_MARKUP,
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
    CAP_SETGID_MARKUP,
    REGEXES_LOADED,
    REGEXES_MARKUP
)


class LinpeasBuilder:
    def __init__(self, ploaded:PEASLoaded):
        self.ploaded = ploaded
        self.hidden_files = set()
        self.bash_find_f_vars, self.bash_find_d_vars = set(), set()
        self.bash_storages = set()
        self.__get_files_to_search()
        with open(TEMPORARY_LINPEAS_BASE_PATH, 'r') as file:
            self.linpeas_sh = file.read()

    def build(self):
        print("[+] Building variables...")
        variables = self.__generate_variables()
        self.__replace_mark(PEAS_VARIABLES_MARKUP, variables, "")
        
        if len(re.findall(r"PSTORAGE_[a-zA-Z0-9_]+", self.linpeas_sh)) > 1: #Only add storages if there are storages (PSTORAGE_BACKUPS is always there so it doesn't count)
            print("[+] Building finds...")
            find_calls, find_custom_calls = self.__generate_finds()
            self.__replace_mark(PEAS_FINDS_MARKUP, find_calls, "  ")
            self.__replace_mark(PEAS_FINDS_CUSTOM_MARKUP, find_custom_calls, "  ")

            print("[+] Building storages...")
            storage_vars = self.__generate_storages()
            self.__replace_mark(PEAS_STORAGES_MARKUP, storage_vars, "  ")
        
        else:
            lm = LinpeasModule(os.path.join(os.path.dirname(__file__), "..", "linpeas_parts", "linpeas_base", "2_caching_finds.sh"))
            self.linpeas_sh = self.linpeas_sh.replace(lm.sh_code, "")

        #Check all the expected STORAGES in linpeas have been created
        for s in re.findall(r'PSTORAGE_[a-zA-Z0-9_]+', self.linpeas_sh):
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

        print("[+] Building regexes searches...")
        section = self.__generate_regexes_search()
        self.__replace_mark(REGEXES_MARKUP, list(section), "")


        print("[+] Downloading external tools...")
        urls = re.findall(r'peass\{(https://[^\}]+)\}', self.linpeas_sh)
        for orig_url in urls:
            tar_gz_bin_name = ""
            if ",,," in orig_url:
                tar_gz_bin_name = url.split(",,,")[1]
                url = orig_url.split(",,,")[0]
            else:
                url = orig_url
            
            print(f"Downloading {url}...")
            
            bin_b64 = self.__get_bin(url, tar_gz_bin_name)

            assert len(bin_b64) > 15000, f"Len of downloaded {url} is {len(bin_b64)}"
            
            self.__replace_mark("peass{"+orig_url+"}", list(bin_b64), "")
        
        if any(v in self.linpeas_sh for v in [SUIDVB1_MARKUP, SUIDVB2_MARKUP, SUDOVB1_MARKUP, SUDOVB2_MARKUP, CAP_SETUID_MARKUP, CAP_SETGID_MARKUP]):
            print("[+] Building GTFOBins lists...")
            suidVB, sudoVB, capsVB = self.__get_gtfobins_lists()
            assert len(suidVB) > 185, f"Len suidVB is {len(suidVB)}"
            assert len(sudoVB) > 250, f"Len sudo is {len(sudoVB)}"
            assert len(capsVB) > 10, f"Len suidVB is {len(capsVB)}"

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
        return re.findall(r'peass\{[a-zA-Z0-9\-\._ ]*\}', self.linpeas_sh)

    
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
        
        finds_custom = []
        all_folder_regexes = []
        all_file_regexes = []
        
        for type,searches in self.dict_to_search.items():
            for r,regexes in searches.items():
                if regexes:
                    find_line = f"{r} "
                    
                    if type == "d": 
                        find_line += "-type d "
                        bash_find_var = f"FIND_DIR_{r[1:].replace('.','').replace('-','_').replace('{ROOT_FOLDER}','').upper()}"
                        self.bash_find_d_vars.add(bash_find_var)
                        all_folder_regexes += regexes
                    else:
                        bash_find_var = f"FIND_{r[1:].replace('.','').replace('-','_').replace('{ROOT_FOLDER}','').upper()}"
                        self.bash_find_f_vars.add(bash_find_var)
                        all_file_regexes += regexes

                    find_line += '-name \\"' + '\\" -o -name \\"'.join(regexes) + '\\"'
                    find_line = FIND_TEMPLATE.replace(FIND_LINE_MARKUP, find_line)
                    find_line = f"{bash_find_var}={find_line}"
                    finds.append(find_line)
        
        # Buid folder and files finds when searching in a custom folder
        all_folder_regexes = list(set(all_folder_regexes))
        find_line = '$SEARCH_IN_FOLDER -type d -name \\"' + '\\" -o -name \\"'.join(all_folder_regexes) + '\\"'
        find_line = FIND_TEMPLATE.replace(FIND_LINE_MARKUP, find_line)
        find_line = f"FIND_DIR_CUSTOM={find_line}"
        finds_custom.append(find_line)
        
        all_file_regexes = list(set(all_file_regexes))
        find_line = '$SEARCH_IN_FOLDER -name \\"' + '\\" -o -name \\"'.join(all_file_regexes) + '\\"'
        find_line = FIND_TEMPLATE.replace(FIND_LINE_MARKUP, find_line)
        find_line = f"FIND_CUSTOM={find_line}"
        finds_custom.append(find_line)
            
        return finds, finds_custom

    def __generate_storages(self) -> list:
        """Generate the storages to save the results per entry"""
        storages = []
        custom_storages = ["FIND_CUSTOM", "FIND_DIR_CUSTOM"]
        all_f_finds = "$" + "\\n$".join(list(self.bash_find_f_vars) + custom_storages)
        all_d_finds = "$" + "\\n$".join(list(self.bash_find_d_vars) + custom_storages)
        all_finds = "$" + "\\n$".join(list(self.bash_find_f_vars) + list(self.bash_find_d_vars) + custom_storages)
        
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
                section = f'if [ "$PSTORAGE_{precord.bash_name}" ] || [ "$DEBUG" ]; then\n'
                section += f'  print_2title "Analyzing {precord.name.replace("_"," ")} Files (limit 70)"\n'

                for exec_line in precord.exec:
                    if exec_line:
                        section += "    " + exec_line + "\n"

                for frecord in precord.filerecords:
                    section += "    " + self.__construct_file_line(precord, frecord) + "\n"
                
                section += "fi\n"
                
                sections[precord.name] = section

        return sections

    def __construct_file_line(self, precord: PEASRecord, frecord: FileRecord, init: bool = True) -> str:
        real_regex = frecord.regex[1:] if frecord.regex.startswith("*") and len(frecord.regex) > 1 else frecord.regex
        real_regex = real_regex.replace(".","\\.").replace("*",".*")
        real_regex += "$"
        
        analise_line = ""
        if init:
            analise_line = 'if ! [ "`echo \\\"$PSTORAGE_'+precord.bash_name+'\\\" | grep -E \\\"'+real_regex+'\\\"`" ]; then if [ "$DEBUG" ]; then echo_not_found "'+frecord.regex+'"; fi; fi; '
            analise_line += 'printf "%s" "$PSTORAGE_'+precord.bash_name+'" | grep -E "'+real_regex+'" | while read f; do ls -ld "$f" 2>/dev/null | sed -${E} "s,'+real_regex+',${SED_RED},"; '

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
            sed_very_bad_regex = ' | sed -${E} "s,'+frecord.very_bad_regex+',${SED_RED_YELLOW},g"' if frecord.very_bad_regex else ""
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
            
            if sed_very_bad_regex:
                analise_line += sed_very_bad_regex

            if sed_good_regex:
                analise_line += sed_good_regex
            
            analise_line += '; done; echo "";'
            return analise_line

        #In case file is type "d"
        if frecord.files:
            for ffrecord in frecord.files:
                ff_real_regex = ffrecord.regex[1:] if ffrecord.regex.startswith("*") and ffrecord.regex != "*" else ffrecord.regex
                ff_real_regex = ff_real_regex.replace("*",".*")
                #analise_line += 'for ff in $(find "$f" -name "'+ffrecord.regex+'"); do ls -ld "$ff" | sed -${E} "s,'+ff_real_regex+',${SED_RED},"; ' + self.__construct_file_line(precord, ffrecord, init=False)
                analise_line += 'find "$f" -name "'+ffrecord.regex+'" | while read ff; do ls -ld "$ff" | sed -${E} "s,'+ff_real_regex+',${SED_RED},"; ' + self.__construct_file_line(precord, ffrecord, init=False)

        analise_line += 'done; echo "";'
        return analise_line
    
    def __get_bin(self, url, tar_gz="") -> str:
        os.system(f"wget -q '{url}' -O /tmp/bin_builder")
        if tar_gz:
            os.system(f"cd /tmp; tar -xvzf /tmp/bin_builder 2> /dev/null; rm /tmp/bin_builder; mv {tar_gz} /tmp/bin_builder")
        
        with open("/tmp/bin_builder", "rb") as bin:
            bin_b64 = base64.b64encode(bin.read()).decode('utf-8')

        os.remove("/tmp/bin_builder")
                
        return bin_b64
    
    def __get_gtfobins_lists(self) -> tuple:
        r = requests.get("https://github.com/GTFOBins/GTFOBins.github.io/tree/master/_gtfobins")
        bins = re.findall(r'_gtfobins/([a-zA-Z0-9_ \-]+).md', r.text)

        sudoVB = []
        suidVB = []
        capsVB = []

        for b in bins:
            try:
                rb = requests.get(f"https://raw.githubusercontent.com/GTFOBins/GTFOBins.github.io/master/_gtfobins/{b}.md", timeout=5)
            except:
                try:
                    rb = requests.get(f"https://raw.githubusercontent.com/GTFOBins/GTFOBins.github.io/master/_gtfobins/{b}.md", timeout=5)
                except:
                    rb = requests.get(f"https://raw.githubusercontent.com/GTFOBins/GTFOBins.github.io/master/_gtfobins/{b}.md", timeout=5)
            if "sudo:" in rb.text:
                if len(b) <= 3:
                    sudoVB.append("[^a-zA-Z0-9]"+b+"$") # Less false possitives applied to small names
                else:
                    sudoVB.append(b+"$")
            if "suid:" in rb.text:
                suidVB.append("/"+b+"$")
            if "capabilities:" in rb.text:
                capsVB.append(b)
        
        return (suidVB, sudoVB, capsVB)
    
    def __generate_regexes_search(self) -> str:
        regexes = REGEXES_LOADED["regular_expresions"]

        regexes_search_section = ""

        for values in regexes:
            section_name = values["name"]
            regexes_search_section += f'    print_2title "Searching {section_name}"\n'

            for entry in values["regexes"]:
                name = entry["name"]
                caseinsensitive = entry.get("caseinsensitive", False)
                regex = entry["regex"]
                regex = regex.replace('"', '\\"').strip()
                falsePositives = entry.get("falsePositives", False)

                if falsePositives:
                    continue
                
                regexes_search_section += f"    search_for_regex \"{name}\" \"{regex}\" {'1' if caseinsensitive else ''}\n"
                
            regexes_search_section += "    echo ''\n\n"

        return regexes_search_section


    def __replace_mark(self, mark: str, find_calls: list, join_char: str):
        """Substitude the markup with the actual code"""
        
        self.linpeas_sh = self.linpeas_sh.replace(mark, join_char.join(find_calls)) #New line char is't needed
    
    def write_linpeas(self, path):
        """Write on disk the final linpeas"""
        
        with open(path, "w") as f:
            f.write(self.linpeas_sh)
    
