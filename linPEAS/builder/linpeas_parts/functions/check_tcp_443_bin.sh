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
# Generated Global Variables: $url_lambda, $TIMEOUT_INTERNET_SECONDS_443_BIN
# Fat linpeas: 0
# Small linpeas: 1


check_tcp_443_bin () {
  local TIMEOUT_INTERNET_SECONDS_443_BIN=$1
  local url_lambda="https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/"

  if command -v curl >/dev/null 2>&1; then
    if curl -s --connect-timeout $TIMEOUT_INTERNET_SECONDS_443_BIN "$url_lambda" \
         -H "User-Agent: linpeas" -H "Content-Type: application/json" >/dev/null 2>&1
    then
      echo "Port 443 is accessible with curl"
      return 0                      # ✅ success
    else
      echo "Port 443 is not accessible with curl"
      return 1
    fi

  elif command -v wget >/dev/null 2>&1; then
    if wget -q --timeout=$TIMEOUT_INTERNET_SECONDS_443_BIN -O - "$url_lambda" \
         --header "User-Agent: linpeas" -H "Content-Type: application/json" >/dev/null 2>&1
    then
      echo "Port 443 is accessible with wget"
      return 0
    else
      echo "Port 443 is not accessible with wget"
      return 1
    fi

  else
    echo "Neither curl nor wget available"
    return 1
  fi
}
