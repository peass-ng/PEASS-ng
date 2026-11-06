# Title: API Keys Regex - search_for_regex
# ID: search_for_regex
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Search for a given regex in the file system
# License: GNU GPL
# Version: 1.0
# Functions Used: print_3title_no_nl
# Global Variables: $backup_folders_row, $HOMESEARCH, $ROOT_FOLDER, $SEARCH_IN_FOLDER, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $regex, $title, $caseSensitive, $i, $regex_pids, $regex_pid
# Fat linpeas: 0
# Small linpeas: 1


search_for_regex(){
    title=$1
    regex=$2
    caseSensitive=$3
    
    if [ "$caseSensitive" ]; then
        i="i"
    else
        i=""
    fi

    print_3title_no_nl "Searching $title..."
    regex_pids=

    if [ "$SEARCH_IN_FOLDER" ]; then
        "$TIMEOUT" 120 find "$ROOT_FOLDER" -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
    else
        # Search in home direcoties (usually the slowest)
        if [ "$HOMESEARCH" ]; then
            "$TIMEOUT" 120 find $HOMESEARCH -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
            regex_pids="$regex_pids $!"
        fi
        
        # Search in etc
        "$TIMEOUT" 120 find /etc -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
        
        # Search in opt
        "$TIMEOUT" 120 find /opt -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
        
        # Search in possible web folders (usually only 1 will exist)
        "$TIMEOUT" 120 find /var/www /usr/local/www /usr/share/nginx /Library/WebServer/ -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
        
        # Search in logs
        "$TIMEOUT" 120 find /var/log /var/logs /Library/Logs -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
        
        # Search in backups
        if [ "$backup_folders_row" ]; then
            "$TIMEOUT" 120 find $backup_folders_row -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
            regex_pids="$regex_pids $!"
        fi
        
        # Search in others folders (usually only /srv or /Applications will exist)
        "$TIMEOUT" 120 find /tmp /srv /Applications -type d -name node_modules -prune -o -type f -exec grep -HnE$i -I -e "$regex" '{}' + 2>/dev/null | sed '/^.\{150\}./d' | sort -u | head -n 50 &
        regex_pids="$regex_pids $!"
    fi
    for regex_pid in $regex_pids; do wait "$regex_pid"; done
    printf "\033[2K\r"
}
