#!/usr/bin/env python3

import os
import requests
from pathlib import Path


def download_regexes():
    print("[+] Downloading regexes...")
    url = "https://raw.githubusercontent.com/JaimePolop/RExpository/main/regex.yaml"
    response = requests.get(url)
    if response.status_code == 200:
        # Save the content of the response to a file
        script_folder = Path(os.path.dirname(os.path.abspath(__file__)))
        target_file = script_folder / 'regexes.yaml'

        with open(target_file, "w") as file:
            file.write(response.text)
        print(f"Downloaded and saved in '{target_file}' successfully!")
    else:
        print("Error: Unable to download the regexes file.")
        exit(1)

download_regexes()
