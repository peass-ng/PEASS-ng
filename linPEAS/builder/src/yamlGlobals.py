import os
import yaml

CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))

LINPEAS_BASE_PARTS = CURRENT_DIR + "/../linpeas_parts"
LINPEAS_PARTS = [
    {
        "name": "System Information",
        "name_check": "system_information",
        "file_path": LINPEAS_BASE_PARTS + "/1_system_information.sh"
    },
    {
        "name": "Container",
        "name_check": "container",
        "file_path": LINPEAS_BASE_PARTS + "/2_container.sh"
    },
    {
        "name": "Processes, Crons, Timers, Services and Sockets",
        "name_check": "procs_crons_timers_srvcs_sockets",
        "file_path": LINPEAS_BASE_PARTS + "/3_procs_crons_timers_srvcs_sockets.sh"
    },
    {
        "name": "Network Information",
        "name_check": "network_information",
        "file_path": LINPEAS_BASE_PARTS + "/4_network_information.sh"
    },
    {
        "name": "Users Information",
        "name_check": "users_information",
        "file_path": LINPEAS_BASE_PARTS + "/5_users_information.sh"
    },
    {
        "name": "Software Information",
        "name_check": "software_information",
        "file_path": LINPEAS_BASE_PARTS + "/6_software_information.sh"
    },
    {
        "name": "Interesting Files",
        "name_check": "interesting_files",
        "file_path": LINPEAS_BASE_PARTS + "/7_interesting_files.sh"
    }
]


LINPEAS_BASE_PATH = LINPEAS_BASE_PARTS + "/linpeas_base.sh"
TEMPORARY_LINPEAS_BASE_PATH = CURRENT_DIR + "/../linpeas_base.sh"
FINAL_LINPEAS_PATH = CURRENT_DIR + "/../../" + "linpeas.sh"
YAML_NAME = "sensitive_files.yaml"
YAML_REGEXES = "regexes.yaml"
FILES_YAML = CURRENT_DIR + "/../../../build_lists/" + YAML_NAME
REGEXES_YAML = CURRENT_DIR + "/../../../build_lists/" + YAML_REGEXES


with open(FILES_YAML, 'r') as file:
    YAML_LOADED = yaml.load(file, Loader=yaml.FullLoader)

with open(REGEXES_YAML, 'r') as file:
    REGEXES_LOADED = yaml.load(file, Loader=yaml.FullLoader)

ROOT_FOLDER = YAML_LOADED["root_folders"]
DEFAULTS = YAML_LOADED["defaults"]
COMMON_FILE_FOLDERS = YAML_LOADED["common_file_folders"]
COMMON_DIR_FOLDERS = YAML_LOADED["common_directory_folders"]
assert all(f in ROOT_FOLDER for f in COMMON_FILE_FOLDERS)
assert all(f in ROOT_FOLDER for f in COMMON_DIR_FOLDERS)


PEAS_CHECKS_MARKUP = YAML_LOADED["peas_checks"]
PEAS_FINDS_MARKUP = YAML_LOADED["peas_finds_markup"]
FIND_LINE_MARKUP = YAML_LOADED["find_line_markup"]
FIND_TEMPLATE = YAML_LOADED["find_template"]

REGEXES_MARKUP = YAML_LOADED["peas_regexes_markup"]
PEAS_STORAGES_MARKUP = YAML_LOADED["peas_storages_markup"]
STORAGE_LINE_MARKUP = YAML_LOADED["storage_line_markup"]
STORAGE_LINE_EXTRA_MARKUP = YAML_LOADED["storage_line_extra_markup"]
STORAGE_TEMPLATE = YAML_LOADED["storage_template"]

PEAS_VARIABLES_MARKUP = YAML_LOADED["variables_markup"]
YAML_VARIABLES = YAML_LOADED["variables"]

INT_HIDDEN_FILES_MARKUP = YAML_LOADED["int_hidden_files_markup"]

EXTRASECTIONS_MARKUP = YAML_LOADED["peas_extrasections_markup"]

SUIDVB1_MARKUP = YAML_LOADED["suidVB1_markup"]
SUIDVB2_MARKUP = YAML_LOADED["suidVB2_markup"]
SUDOVB1_MARKUP = YAML_LOADED["sudoVB1_markup"]
SUDOVB2_MARKUP = YAML_LOADED["sudoVB2_markup"]
CAP_SETUID_MARKUP = YAML_LOADED["cap_setuid_markup"]
CAP_SETGID_MARKUP = YAML_LOADED["cap_setgid_markup"]

LES_MARKUP = YAML_LOADED["les_markup"]
LES2_MARKUP = YAML_LOADED["les2_markup"]