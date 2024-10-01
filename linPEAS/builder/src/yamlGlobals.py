import os
import yaml
from pathlib import Path


script_folder = Path(os.path.dirname(os.path.abspath(__file__)))
target_file = script_folder / '..' / '..' / '..' / 'build_lists' / 'download_regexes.py'
os.system(target_file)

CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))

LINPEAS_BASE_PARTS = CURRENT_DIR + "/../linpeas_parts"
LINPEAS_PARTS = {
    "functions": LINPEAS_BASE_PARTS + "/functions",
    "variables": LINPEAS_BASE_PARTS + "/variables",
    "base": LINPEAS_BASE_PARTS + "/linpeas_base",
    "modules": [
        {
            "name": "System Information",
            "name_check": "system_information",
            "folder_path": LINPEAS_BASE_PARTS + "/1_system_information"
        },
        {
            "name": "Container",
            "name_check": "container",
            "folder_path": LINPEAS_BASE_PARTS + "/2_container"
        },
        {
            "name": "Cloud",
            "name_check": "cloud",
            "folder_path": LINPEAS_BASE_PARTS + "/3_cloud"
        },
        {
            "name": "Processes, Crons, Timers, Services and Sockets",
            "name_check": "procs_crons_timers_srvcs_sockets",
            "folder_path": LINPEAS_BASE_PARTS + "/4_procs_crons_timers_srvcs_sockets"
        },
        {
            "name": "Network Information",
            "name_check": "network_information",
            "folder_path": LINPEAS_BASE_PARTS + "/5_network_information"
        },
        {
            "name": "Users Information",
            "name_check": "users_information",
            "folder_path": LINPEAS_BASE_PARTS + "/6_users_information"
        },
        {
            "name": "Software Information",
            "name_check": "software_information",
            "folder_path": LINPEAS_BASE_PARTS + "/7_software_information"
        },
        {
            "name": "Files with Interesting Permissions",
            "name_check": "interesting_perms_files",
            "folder_path": LINPEAS_BASE_PARTS + "/8_interesting_perms_files"
        },
        {
            "name": "Other Interesting Files",
            "name_check": "interesting_files",
            "folder_path": LINPEAS_BASE_PARTS + "/9_interesting_files"
        },
        {
            "name": "API Keys Regex",
            "name_check": "api_keys_regex",
            "folder_path": LINPEAS_BASE_PARTS + "/10_api_keys_regex"
        }
    ]
}


LINPEAS_BASE_PATH = LINPEAS_BASE_PARTS + "/linpeas_base.sh"
TEMPORARY_LINPEAS_BASE_PATH = CURRENT_DIR + "/../linpeas_base_tmp.sh"
FINAL_FAT_LINPEAS_PATH = CURRENT_DIR + "/../../" + "linpeas_fat.sh"
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
PEAS_FINDS_CUSTOM_MARKUP = YAML_LOADED["peas_finds_custom_markup"]
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

