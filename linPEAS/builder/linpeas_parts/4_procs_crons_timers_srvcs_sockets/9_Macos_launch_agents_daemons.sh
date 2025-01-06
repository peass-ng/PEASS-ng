# Title: Processes & Cron & Services & Timers - Third party LaunchAgents & LaunchDemons
# ID: PR_Macos_launch_agents_daemons
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Third party LaunchAgents & LaunchDemons
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $program
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$MACPEAS" ]; then
    print_2title "Third party LaunchAgents & LaunchDemons"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#launchd"
    ls -l /Library/LaunchAgents/ /Library/LaunchDaemons/ ~/Library/LaunchAgents/ ~/Library/LaunchDaemons/ 2>/dev/null
    echo ""

    print_2title "Writable System LaunchAgents & LaunchDemons"
    find /System/Library/LaunchAgents/ /System/Library/LaunchDaemons/ /Library/LaunchAgents/ /Library/LaunchDaemons/ | grep ".plist" | while read f; do
      program=""
      program=$(defaults read "$f" Program 2>/dev/null)
      if ! [ "$program" ]; then
        program=$(defaults read "$f" ProgramArguments | grep -Ev "^\(|^\)" | cut -d '"' -f 2)
      fi
      if [ -w "$program" ]; then
        echo "$program" is writable | sed -${E} "s,.*,${SED_RED_YELLOW},";
      fi
    done
    echo ""

    print_2title "StartupItems"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#startup-items"
    ls -l /Library/StartupItems/ /System/Library/StartupItems/ 2>/dev/null
    echo ""

    print_2title "Login Items"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#startup-items"
    osascript -e 'tell application "System Events" to get the name of every login item' 2>/dev/null
    echo ""

    print_2title "SPStartupItemDataType"
    system_profiler SPStartupItemDataType
    echo ""

    print_2title "Emond scripts"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#emond"
    ls -l /private/var/db/emondClients
    echo ""
  fi
fi