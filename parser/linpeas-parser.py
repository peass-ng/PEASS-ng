#!/usr/bin/env python3

import sys
import re
import json
import logging

# Pattern to identify main section titles
MAIN_section_PATTERN = "════════════════════════════════════╣"

# Main sections (specific for linPEAS)
BASIC_INFORMATION_SLUG = "basic-information"
SOFTWARE_INFORMATION_SLUG = "software-information"
SYSTEM_INFORMATION_SLUG = "system-information"
AVAILABLE_SOFTWARE_SLUG = "available-software"
NETWORK_INFORMATION_SLUG = "network-information"
USERS_INFORMATION_SLUG = "users-information"
INTERESTING_FILES_SLUG = "interesting-files"

try:
    linpeas_output_path = sys.argv[1]
except IndexError as err:
    # You can pipe the output to "jq" if you have setup 
    print("Error: Needs to pass the .out file\n./linpeas-parser.py <output_file>")
    sys.exit()

def basic_information_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of basic information
    """
    parsed_row = {}

    if ":" in row:
        parsed_row = {"label": row.replace(":", " ").strip()}
    elif "[+]" in row:
        parsed_row = {"label": row.replace("[+]", "").strip()}
    else: 
        parsed_row = {"label": row}

    return parsed_row
 
def software_information_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of software information
    """
    return {"row": row}

def system_information_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of system information
    """
    
    return {"row": row}

def available_software_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of available software
    """
    return {"row": row}

def network_information_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of network information
    """
    return {"row": row}

def users_information_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of network information
    """
    return {"row": row}

def interesting_files_parser(json_output: dict, row: str) -> dict:
    """Returns a dict

    Parses a row a following the boundaries of network information
    """
    return {"row": row}

def get_parser_by_slug(slug: str):
    """Returns a function

    Returns the right parser based on the slug
    """
    return parsers[slug]

parsers = {
    BASIC_INFORMATION_SLUG: basic_information_parser,
    SOFTWARE_INFORMATION_SLUG: software_information_parser,
    SYSTEM_INFORMATION_SLUG: system_information_parser,
    AVAILABLE_SOFTWARE_SLUG: available_software_parser,
    NETWORK_INFORMATION_SLUG: network_information_parser,
    USERS_INFORMATION_SLUG: users_information_parser,
    INTERESTING_FILES_SLUG: interesting_files_parser,
}

def read_file(output_path: str) -> [str]:
    """Returns a list of strings

    Reads file from a specich path and returns it as a list
    """
    return [row.strip() for row in open(output_path, 'r').readlines() if row]
    
def is_starting_section(
    row: str,
    pattern: str = MAIN_section_PATTERN
) -> bool:
    """Returns a boolean

    Checks if row matches the pattern and returns True or False
    """
    return row.find(pattern) > -1 

def extracts_title_label(row: str) -> str:
    """Returns a dict

    Extracts a strings whose rows matches the pattern
    """
    return re.findall(r"\w+\s\w+", row)

def slugify_text(title: str) -> str:
    """Returns a dict

    Returns a slugify version of the string. 
    e.g Basic Information -> basic-information
    """
    return title.lower().replace(" ", "-")

def create_new_main_entry(
    json_output: object, 
    title: str, 
    row_number: int
) -> None:
    """Returns None

    Adds a new entry based using "title" as key to return a 
    json output with the initial row number and empty info
    property where the upcoming information should be added 
    """

    slug_title = slugify_text(title)

    json_output[slug_title] = {
        "label": title, 
        "initial_row_number": row_number, 
        "items": {}
    }

def get_range_between(
    json_output: object,
    section1: str,
    section2: str
) -> list[int, int]:
    """Returns a list with two integers

    Extracts the range between one main block and the next one. 
    """
    row_number_section1 = json_output[section1]["initial_row_number"] + 1
    row_number_section2 = json_output[section2]["initial_row_number"] - 1 
    return [row_number_section1, row_number_section2]

def parse_block(
    json_output: object,
    rows: list[str],
    main_entry_key: str,
    block_range: list[int, int]
) -> None:
    """Returns None
    
    Modifies the "items" from each main section, adding information
    from the report
    """
    if len(block_range) > 1:
        initial_row, last_row = block_range
        row_range = rows[initial_row:last_row]
    elif len(block_range) == 1:
        row_range = rows[block_range[0]:]

    slug = slugify_text(main_entry_key)
    
    items = []

    for row in row_range:
        sub_section_parser = get_parser_by_slug(slug)
        items.append(sub_section_parser(json_output, row))
        
    json_output[main_entry_key]["items"] = items

def parse_initial_structure(rows: list[str]) -> object:
    """Returns an object

    Generates the initial main structure for the json ouput
    with all the main entries and additional meta properties
    """
    json_output = {}
    row_number = 0

    for row in rows:
        if is_starting_section(row, MAIN_section_PATTERN): 
            title = extracts_title_label(row)  
            if len(title) > 0:
                clean_title = title[0].replace('32m', '')
                create_new_main_entry(json_output, clean_title, row_number)
        
        row_number += 1
    
    return json_output

def main():
    rows = read_file(linpeas_output_path)
    json_output = parse_initial_structure(rows)
    json_output_keys = list(json_output.keys())
    keys_length = len(json_output_keys)

    for index in range(0, keys_length):
        next_index = index + 1
        if next_index < keys_length:
            current_label = json_output_keys[index]
            next_label = json_output_keys[index + 1]
            
            block_range = get_range_between(json_output, current_label, next_label)
            parse_block(json_output, rows, current_label, block_range)

        else:
            last_section_initial_row_number = json_output[next_label]["initial_row_number"] + 1
            parse_block(json_output, rows, next_label, [last_section_initial_row_number])

    print(json.dumps(json_output))

if __name__ == "__main__":
    # execute only if run as a script
    main()
