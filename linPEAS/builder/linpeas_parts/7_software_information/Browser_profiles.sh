# Title: Software Information - Browser Profiles
# ID: SW_Browser_profiles
# Author: Carlos Polop
# Last Update: 10-03-2025
# Description: List browser profiles that may store credentials/cookies
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $HOMESEARCH, $SED_RED
# Initial Functions:
# Generated Global Variables: $h, $firefox_ini, $chrome_base, $profiles
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Browser Profiles"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#browser-data"

echo ""

for h in $HOMESEARCH; do
  [ -d "$h" ] || continue

  firefox_ini="$h/.mozilla/firefox/profiles.ini"
  if [ -f "$firefox_ini" ]; then
    print_3title "Firefox profiles ($h)"
    awk -F= '
      /^\[Profile/ { in_profile=1 }
      /^Path=/ { path=$2 }
      /^IsRelative=/ { isrel=$2 }
      /^$/ {
        if (path != "") {
          if (isrel == "1") {
            print base "/.mozilla/firefox/" path
          } else {
            print path
          }
        }
        path=""; isrel=""
      }
      END {
        if (path != "") {
          if (isrel == "1") {
            print base "/.mozilla/firefox/" path
          } else {
            print path
          }
        }
      }
    ' base="$h" "$firefox_ini" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
    echo ""
  fi

  for chrome_base in "$h/.config/google-chrome" "$h/.config/chromium" "$h/.config/BraveSoftware/Brave-Browser" "$h/.config/microsoft-edge" "$h/.config/microsoft-edge-beta" "$h/.config/microsoft-edge-dev"; do
    if [ -d "$chrome_base" ]; then
      profiles=$(find "$chrome_base" -maxdepth 1 -type d \( -name "Default" -o -name "Profile *" \) 2>/dev/null)
      if [ "$profiles" ]; then
        print_3title "Chromium profiles ($chrome_base)"
        printf "%s\n" "$profiles" | sed -${E} "s,.*,${SED_RED},"
        echo ""
      fi
    fi
  done

done
