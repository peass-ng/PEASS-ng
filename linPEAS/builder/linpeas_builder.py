from .src.peasLoaded import PEASLoaded
from .src.linpeasBuilder import LinpeasBuilder
from .src.linpeasBaseBuilder import LinpeasBaseBuilder
from .src.yamlGlobals import FINAL_FAT_LINPEAS_PATH, FINAL_LINPEAS_PATH, TEMPORARY_LINPEAS_BASE_PATH

import os
import stat
import argparse

# python3 -m builder.linpeas_builder
def main(all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules, output):
    # Load configuration
    ploaded = PEASLoaded()

    # Build temporary linpeas_base.sh file
    lbasebuilder = LinpeasBaseBuilder(all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules)
    lbasebuilder.build()

    # Build final linpeas.sh
    lbuilder = LinpeasBuilder(ploaded)
    lbuilder.build()
    lbuilder.write_linpeas(output)
    os.remove(TEMPORARY_LINPEAS_BASE_PATH) # Remove the built linpeas_base_temp.sh file
    
    st = os.stat(output)
    os.chmod(output, st.st_mode | stat.S_IEXEC)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Build you own linpeas.sh')
    parser.add_argument('--all', action='store_true', help='Build linpeas with all modules (linpeas_fat).')
    parser.add_argument('--all-no-fat', action='store_true', help='Build linpeas with all modules except fat ones.')
    parser.add_argument('--no-network-scanning', action='store_true', help='Build linpeas without network scanning.')
    parser.add_argument('--small', action='store_true', help='Build small version of linpeas.')
    parser.add_argument('--include', type=str, help='Build linpeas only with the modules indicated you can indicate section names or module IDs).')
    parser.add_argument('--exclude', type=str, help='Exclude the given modules (you can indicate section names or module IDs).')
    parser.add_argument('--output', required=True, type=str, help='Parth to write the final linpeas file to.')
    args = parser.parse_args()

    all_modules = args.all
    all_no_fat_modules = args.all_no_fat
    no_network_scanning = args.no_network_scanning
    small = args.small
    include_modules = args.include.split(",") if args.include else []
    include_modules = [m.strip().lower() for m in include_modules]
    exclude_modules = args.exclude.split(",") if args.exclude else []
    exclude_modules = [m.strip().lower() for m in exclude_modules]
    output = args.output

    # If not all, all-no-fat, small or include, exit
    if not args.all and not args.all_no_fat and not args.small and not args.include:
        print("You must specify one of the following options: --all, --all-no-fat, --small or --include")
        parser.print_help()
        exit(1)
    
    main(all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules, output)
