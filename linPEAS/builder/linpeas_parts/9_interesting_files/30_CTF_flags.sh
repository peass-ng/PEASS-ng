# Title: Interesting Files - CTF Flags Hunter
# ID: IF_CTF_flags
# Author: mrtaichi
# Last Update: 2025-07-07
# Description: Comprehensive CTF flag hunting module that searches for common flag patterns:
#   - Standard CTF flag formats (flag{...}, FLAG{...}, etc.)
#   - Common flag locations and file types
#   - Hidden files and directories
#   - Configuration files that might contain flags
#   - Log files with potential flag information
#   - Backup files and archives
#   - Web application files
#   - Database files and dumps
#   - Environment files and variables
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, search_for_regex, print_info
# Global Variables: $HOMESEARCH, $backup_folders_row, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $ctf_flag_patterns, $ctf_flag_locations, $ppicf, $pattern, $location_pattern, $location
# Fat linpeas: 0
# Small linpeas: 1

print_2title "CTF Flag Hunting - Comprehensive Flag Search"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#interesting-files"

##-- IF) Search for standard CTF flag patterns
print_3title "Searching for standard CTF flag patterns"
  
# Standard CTF flag patterns
ctf_flag_patterns=(
    "flag{[^}]*}"           # Standard flag{...} format
    "FLAG{[^}]*}"           # Uppercase FLAG{...} format
    "ctf{[^}]*}"            # CTF{...} format
    "CTF{[^}]*}"            # Uppercase CTF{...} format
    "key{[^}]*}"            # Key{...} format
    "KEY{[^}]*}"            # Uppercase KEY{...} format
    "secret{[^}]*}"         # Secret{...} format
    "SECRET{[^}]*}"         # Uppercase SECRET{...} format
    "token{[^}]*}"          # Token{...} format
    "TOKEN{[^}]*}"          # Uppercase TOKEN{...} format
    "password{[^}]*}"       # Password{...} format
    "PASSWORD{[^}]*}"       # Uppercase PASSWORD{...} format
    "credential{[^}]*}"     # Credential{...} format
    "CREDENTIAL{[^}]*}"     # Uppercase CREDENTIAL{...} format
    "ETSCTF{[^}]*}"     # ETSCTF{...} format
    "etsctf{[^}]*}"     # etsctf{...} format
  )

# Search for each flag pattern
for pattern in "${ctf_flag_patterns[@]}"; do
    search_for_regex "CTF Flag Pattern: $pattern" "$pattern" "1"
done

##-- IF) Search for flag-like strings in common locations
print_3title "Searching for flag-like strings in common CTF locations"
  
# Common CTF flag locations and patterns
ctf_flag_locations=(
    "/home/*/.bash_history:flag"
    "/home/*/.zsh_history:flag"
    "/root/.bash_history:flag"
    "/root/.zsh_history:flag"
    "/var/log/*:flag"
    "/var/log/*:FLAG"
    "/tmp/*:flag"
    "/tmp/*:FLAG"
    "/var/tmp/*:flag"
    "/var/tmp/*:FLAG"
    "/opt/*:flag"
    "/opt/*:FLAG"
    "/usr/local/*:flag"
    "/usr/local/*:FLAG"
    "/var/www/*:flag"
    "/var/www/*:FLAG"
    "/srv/*:flag"
    "/srv/*:FLAG"
    "/etc/*:flag"
    "/etc/*:FLAG"
  )

# Search in specific locations for flag-like content
for location_pattern in "${ctf_flag_locations[@]}"; do
    location=$(echo "$location_pattern" | cut -d: -f1)
    pattern=$(echo "$location_pattern" | cut -d: -f2)
    
    if [ "$SEARCH_IN_FOLDER" ]; then
      timeout 60 find "$SEARCH_IN_FOLDER" -type f -exec grep -HnRIE "$pattern" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
    else
      timeout 60 find $location -type f -exec grep -HnRIE "$pattern" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
    fi
done
wait

##-- IF) Search for hidden files and directories that might contain flags
print_3title "Searching for hidden files and directories (common flag locations)"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name ".*" -type f 2>/dev/null | head -n 50 &
    timeout 60 find "$SEARCH_IN_FOLDER" -name ".*" -type d 2>/dev/null | head -n 20 &
else
    # Search for hidden files in common locations
    timeout 60 find /home /root /opt /usr/local /var/www /srv /etc -name ".*" -type f 2>/dev/null | head -n 50 &
    timeout 60 find /home /root /opt /usr/local /var/www /srv /etc -name ".*" -type d 2>/dev/null | head -n 20 &
    
    # Search for files with "flag" in the name
    timeout 60 find / -name "*flag*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find / -name "*FLAG*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find / -name "*ETSCTF*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find / -name "*etsctf*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find / -name "*secret*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find / -name "*SECRET*" -type f 2>/dev/null | head -n 30 &
fi
wait

##-- IF) Search for flag-like content in configuration files
print_3title "Searching for flags in configuration files"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    ppicf=$(timeout 60 find "$SEARCH_IN_FOLDER" -name "*.conf" -o -name "*.cnf" -o -name "*.config" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.env" -o -name "*.ini" -o -name "*.cfg" 2>/dev/null)
else
    ppicf=$(timeout 60 find $HOMESEARCH /var/www/ /usr/local/www/ /etc /opt /tmp /private /Applications /mnt -name "*.conf" -o -name "*.cnf" -o -name "*.config" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.env" -o -name "*.ini" -o -name "*.cfg" 2>/dev/null)
fi
  
printf "%s\n" "$ppicf" | while read f; do
    if grep -qEiI 'flag|FLAG|secret|SECRET|token|TOKEN|password|PASSWORD|credential|CREDENTIAL|ETSCTF|etsctf' "$f" 2>/dev/null; then
      echo "$ITALIC $f$NC"
      grep -HnEiIo 'flag|FLAG|secret|SECRET|token|TOKEN|password|PASSWORD|credential|CREDENTIAL|ETSCTF|etsctf' "$f" 2>/dev/null | sed -${E} "s,flag|FLAG|secret|SECRET|token|TOKEN|password|PASSWORD|credential|CREDENTIAL|ETSCTF|etsctf,${SED_RED},g"
    fi
done

##-- IF) Search for flag-like content in log files
print_3title "Searching for flags in log files"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name "*.log" -o -name "*.log.*" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 30 &
else
    timeout 60 find /var/log /var/logs /Library/Logs -name "*.log" -o -name "*.log.*" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 30 &
fi
wait

##-- IF) Search for flag-like content in backup and archive files
print_3title "Searching for flags in backup and archive files"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name "*.bak" -o -name "*.backup" -o -name "*.old" -o -name "*.orig" -o -name "*.tar" -o -name "*.tar.gz" -o -name "*.zip" -o -name "*.rar" -o -name "*.7z" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
else
    timeout 60 find $backup_folders_row /tmp /var/tmp -name "*.bak" -o -name "*.backup" -o -name "*.old" -o -name "*.orig" -o -name "*.tar" -o -name "*.tar.gz" -o -name "*.zip" -o -name "*.rar" -o -name "*.7z" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
fi
wait

##-- IF) Search for flag-like content in database files
print_3title "Searching for flags in database files"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name "*.db" -o -name "*.sqlite" -o -name "*.sqlite3" -o -name "*.sql" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
else
    timeout 60 find /var/lib /opt /usr/local /home /root -name "*.db" -o -name "*.sqlite" -o -name "*.sqlite3" -o -name "*.sql" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
fi
wait

##-- IF) Search for flag-like content in web application files
print_3title "Searching for flags in web application files"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name "*.php" -o -name "*.html" -o -name "*.htm" -o -name "*.js" -o -name "*.py" -o -name "*.rb" -o -name "*.java" -o -name "*.xml" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 30 &
else
    timeout 60 find /var/www /usr/local/www /usr/share/nginx /Library/WebServer/ -name "*.php" -o -name "*.html" -o -name "*.htm" -o -name "*.js" -o -name "*.py" -o -name "*.rb" -o -name "*.java" -o -name "*.xml" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 30 &
fi
wait

##-- IF) Search for environment variables and files that might contain flags
print_3title "Searching for flags in environment files and variables"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name ".env*" -o -name "env*" -o -name "*.env" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN|ETSCTF|etsctf" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
else
    timeout 60 find /home /root /opt /usr/local /var/www /srv /etc -name ".env*" -o -name "env*" -o -name "*.env" -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN|ETSCTF|etsctf" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 20 &
fi
wait

echo "" 