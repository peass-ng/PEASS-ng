
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

    if [ "$SEARCH_IN_FOLDER" ]; then
        timeout 120 find "$ROOT_FOLDER" -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
    else
        # Search in home direcoties (usually the slowest)
        timeout 120 find $HOMESEARCH -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in etc
        timeout 120 find /etc -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in opt
        timeout 120 find /opt -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in possible web folders (usually only 1 will exist)
        timeout 120 find /var/www /usr/local/www /usr/share/nginx /Library/WebServer/ -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in logs
        timeout 120 find /var/log /var/logs /Library/Logs -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in backups
        timeout 120 find $backup_folders_row -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
        
        # Search in others folders (usually only /srv or /Applications will exist)
        timeout 120 find /tmp /srv /Applications -type f -not -path "*/node_modules/*" -exec grep -HnRIE$i "$regex" '{}' \; 2>/dev/null  | sed '/^.\{150\}./d' | sort | uniq | head -n 50 &
    fi
    wait
    printf "\033[2K\r"
}



if [ "$REGEXES" ] && [ "$TIMEOUT" ]; then
    peass{REGEXES}
else
    echo "Regexes to search for API keys aren't activated, use param '-r' "
fi