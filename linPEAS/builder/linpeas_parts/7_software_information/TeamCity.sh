# Title: Software Information - TeamCity (JetBrains)
# ID: SI_TeamCity
# Author: HT Bot
# Last Update: 09-10-2025
# Description: Detect TeamCity server/agents, listeners and risky privilege context
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


# Run this section if there are hints of TeamCity around (files from storage, processes, or the default port)
if [ "$PSTORAGE_TEAMCITY" ] || ps aux 2>/dev/null | grep -Ei "teamcity|buildAgent|jetbrains.buildServer.agent|TeamCityMavenServer" | grep -v grep 1>/dev/null || ss -lntp 2>/dev/null | grep -q ":8111 " || [ "$DEBUG" ]; then
  print_2title "TeamCity (JetBrains) CI/CD detection and privilege context"

  # 1) Processes (server/agent)
  print_3title "Processes (server/agents)"
  ps axo user:20,pid,comm,args 2>/dev/null | grep -Ei "teamcity|buildAgent|jetbrains.buildServer.agent|TeamCityMavenServer" | grep -v grep | sed -${E} "s,\<root\>,${SED_RED},"
  echo ""

  # 2) Listeners (default HTTP port 8111)
  print_3title "Listeners (default HTTP 8111)"
  if command -v ss >/dev/null 2>&1; then
    ss -lntp 2>/dev/null | grep -E ":8111 .*LISTEN" | sed -${E} "s,127\.0\.0\.1|\[::ffff:127\.0\.0\.1\],${SED_BLUE}," | sed -${E} "s,0\.0\.0\.0|\[::\],${SED_GREEN},"
  else
    netstat -tulpen 2>/dev/null | grep -E "(:8111)" | sed -${E} "s,127\.0\.0\.1,${SED_BLUE}," | sed -${E} "s,0\.0\.0\.0,${SED_GREEN},"
  fi
  echo ""

  # 3) Fingerprint via loopback (if curl exists)
  if command -v curl >/dev/null 2>&1; then
    print_3title "HTTP fingerprint"
    curl -sI --max-time 2 http://127.0.0.1:8111/ 2>/dev/null | head -n 5 | sed -${E} "s,TeamCity|JetBrains,${SED_GREEN},"
    echo ""
  fi

  # 4) Files of interest (if found by the storage builder) and quick greps
  if [ "$PSTORAGE_TEAMCITY" ]; then
    print_3title "Interesting TeamCity paths (from search)"
    printf "%s\n" "$PSTORAGE_TEAMCITY" | sort | uniq | while read -r p; do
      echo "$p"
      # Common configs if running from a TeamCity root dir
      if [ -d "$p" ]; then
        if [ -r "$p/conf/teamcity-startup.properties" ]; then
          echo "  conf/teamcity-startup.properties:" | sed "s,.*,${SED_LIGHT_CYAN},"
          grep -E "(^|[^#])(ownPort|contextPath|java\.home)" "$p/conf/teamcity-startup.properties" 2>/dev/null | sed -${E} "s,.*,  &," 
        fi
        if [ -r "$p/config/database.properties" ]; then
          echo "  config/database.properties:" | sed "s,.*,${SED_LIGHT_CYAN},"
          grep -E "(^|[^#])(user|password|url)" "$p/config/database.properties" 2>/dev/null | sed -${E} "s,password|user|url,${SED_RED},"
        fi
        if [ -r "$p/buildAgent/conf/buildAgent.properties" ]; then
          echo "  buildAgent/conf/buildAgent.properties:" | sed "s,.*,${SED_LIGHT_CYAN},"
          grep -E "(^|[^#])(serverUrl|authorizationToken|name)" "$p/buildAgent/conf/buildAgent.properties" 2>/dev/null | sed -${E} "s,authorizationToken|serverUrl,${SED_RED},"
        fi
      elif [ -f "$p" ]; then
        case "$p" in
          */buildAgent.properties)
            grep -E "(^|[^#])(serverUrl|authorizationToken|name)" "$p" 2>/dev/null | sed -${E} "s,authorizationToken|serverUrl,${SED_RED}," ;;
          */teamcity-startup.properties)
            grep -E "(^|[^#])(ownPort|contextPath|java\.home)" "$p" 2>/dev/null ;;
          */database.properties)
            grep -E "(^|[^#])(user|password|url)" "$p" 2>/dev/null | sed -${E} "s,password|user|url,${SED_RED}," ;;
        esac
      fi
    done
    echo ""
  fi

  # 5) Risk note if any TeamCity component is running as root
  if ps axo user,comm,args 2>/dev/null | grep -Ei "^root .*teamcity|^root .*buildAgent|^root .*jetbrains\.buildServer\.agent" | grep -v grep 1>/dev/null; then
    echo "[!] TeamCity server/agent running as root detected. With TeamCity admin access, you can create a 'Command Line' build step to execute arbitrary commands as root on the agent (high risk)." | sed -${E} "s,\[!\].*,${SED_RED_YELLOW},"
    print_info "If 8111 is bound only to 127.0.0.1, an attacker with a low-privileged shell could port-forward via SSH (-N -L) to reach the UI."
  fi

  echo ""
fi
