# Title: Interesting Files - Advanced CTF Flags Hunter
# ID: IF_CTF_flags_advanced
# Author: mrtaichi 
# Last Update: 2024-12-19
# Description: Advanced CTF flag hunting using regex patterns and common CTF locations
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, search_for_regex, print_info
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $ctf_advanced_patterns, $high_value_locations, $location, $pattern
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Advanced CTF Flag Hunting - Regex-based Search"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#interesting-files"

##-- IF) Advanced CTF flag patterns using regex
print_3title "Searching for advanced CTF flag patterns"
  
# Advanced CTF flag patterns that are commonly used
ctf_advanced_patterns=(
    "flag{[a-zA-Z0-9_\-]{10,50}}"     # Standard flag format with reasonable length
    "FLAG{[a-zA-Z0-9_\-]{10,50}}"     # Uppercase FLAG format
    "ctf{[a-zA-Z0-9_\-]{10,50}}"      # CTF format
    "CTF{[a-zA-Z0-9_\-]{10,50}}"      # Uppercase CTF format
    "key{[a-zA-Z0-9_\-]{10,50}}"      # Key format
    "KEY{[a-zA-Z0-9_\-]{10,50}}"      # Uppercase KEY format
    "secret{[a-zA-Z0-9_\-]{10,50}}"   # Secret format
    "SECRET{[a-zA-Z0-9_\-]{10,50}}"   # Uppercase SECRET format
    "token{[a-zA-Z0-9_\-]{10,50}}"    # Token format
    "TOKEN{[a-zA-Z0-9_\-]{10,50}}"    # Uppercase TOKEN format
    "password{[a-zA-Z0-9_\-]{10,50}}" # Password format
    "PASSWORD{[a-zA-Z0-9_\-]{10,50}}" # Uppercase PASSWORD format
    "credential{[a-zA-Z0-9_\-]{10,50}}" # Credential format
    "CREDENTIAL{[a-zA-Z0-9_\-]{10,50}}" # Uppercase CREDENTIAL format
    "flag{[a-f0-9]{32}}"              # MD5-like flag format
    "flag{[a-f0-9]{40}}"              # SHA1-like flag format
    "flag{[a-f0-9]{64}}"              # SHA256-like flag format
    "flag{[A-Z0-9]{32}}"              # Base32-like flag format
    "flag{[A-Za-z0-9+/]{20,100}={0,2}}" # Base64-like flag format
    "ETSCTF{[a-zA-Z0-9_\-]{10,50}}"   # ETSCTF format
    "etsctf{[a-zA-Z0-9_\-]{10,50}}"   # etsctf format
  )

# Search for each advanced flag pattern
for pattern in "${ctf_advanced_patterns[@]}"; do
    search_for_regex "Advanced CTF Flag: $pattern" "$pattern" "1"
done

##-- IF) Search for flag-like strings in specific high-value locations
print_3title "Searching for flags in high-value CTF locations"
  
# High-value locations where flags are commonly found in CTFs
high_value_locations=(
    "/home/*/.bash_history"
    "/home/*/.zsh_history" 
    "/root/.bash_history"
    "/root/.zsh_history"
    "/var/log/auth.log"
    "/var/log/syslog"
    "/var/log/messages"
    "/tmp"
    "/var/tmp"
    "/opt"
    "/usr/local"
    "/var/www"
    "/srv"
    "/etc/passwd"
    "/etc/shadow"
    "/etc/hosts"
    "/etc/crontab"
  )

# Search in high-value locations for flag-like content
for location in "${high_value_locations[@]}"; do
    if [ "$SEARCH_IN_FOLDER" ]; then
      timeout 60 find "$SEARCH_IN_FOLDER" -type f -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN|ETSCTF|etsctf" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 10 &
    else
      timeout 60 find $location -type f -exec grep -HnRIE "flag|FLAG|secret|SECRET|token|TOKEN|ETSCTF|etsctf" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | head -n 10 &
    fi
done
wait

##-- IF) Search for files with flag-related names
print_3title "Searching for files with flag-related names"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name "*flag*" -o -name "*FLAG*" -o -name "*ETSCTF*" -o -name "*etsctf*" -o -name "*secret*" -o -name "*SECRET*" -o -name "*token*" -o -name "*TOKEN*" -type f 2>/dev/null | head -n 50 &
  else
    timeout 60 find / -name "*flag*" -o -name "*FLAG*" -o -name "*ETSCTF*" -o -name "*etsctf*" -o -name "*secret*" -o -name "*SECRET*" -o -name "*token*" -o -name "*TOKEN*" -type f 2>/dev/null | head -n 50 &
  fi
  wait

##-- IF) Search for hidden files and directories
print_3title "Searching for hidden files and directories"
  
if [ "$SEARCH_IN_FOLDER" ]; then
    timeout 60 find "$SEARCH_IN_FOLDER" -name ".*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find "$SEARCH_IN_FOLDER" -name ".*" -type d 2>/dev/null | head -n 15 &
  else
    timeout 60 find /home /root /opt /usr/local /var/www /srv /etc -name ".*" -type f 2>/dev/null | head -n 30 &
    timeout 60 find /home /root /opt /usr/local /var/www /srv /etc -name ".*" -type d 2>/dev/null | head -n 15 &
  fi
  wait

echo "" 