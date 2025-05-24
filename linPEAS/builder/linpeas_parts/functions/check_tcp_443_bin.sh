# Title: LinPeasBase - check_tcp_443_bin
# ID: check_tcp_443_bin
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if TCP Internet conns are available (via port 443) using curl or wget
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1



check_tcp_443_bin(){
  if command -v curl >/dev/null 2>&1; then
    curl -s "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/" -H "User-Agent: linpeas" -H "Content-Type: application/json" >/dev/null 2>&1 && echo "Port 443 is accessible with curl" || echo "Port 443 is not accessible with curl"
  elif command -v wget >/dev/null 2>&1; then
    wget -q -O - "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/" --header "User-Agent: linpeas" -H "Content-Type: application/json" >/dev/null 2>&1 && echo "Port 443 is accessible with wget" || echo "Port 443 is not accessible with wget"
  fi
}