# Title: LinPeasBase - check_external_hostname
# ID: check_external_hostname
# Author: Carlos Polop
# Last Update: 23-05-2025
# Description: This will check the public IP and hostname in known malicious lists and leaks to find any relevant information about the host.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $$INTERNET_SEARCH_TIMEOUT
# Fat linpeas: 0
# Small linpeas: 1


check_external_hostname(){
  INTERNET_SEARCH_TIMEOUT=15
  # wget or curl?
  if command -v curl >/dev/null 2>&1; then
    curl "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/" -H "User-Agent: linpeas" -d "{\"hostname\":\"$(hostname)\"}" -H "Content-Type: application/json" --max-time "$INTERNET_SEARCH_TIMEOUT"
  elif command -v wget >/dev/null 2>&1; then
    wget -q -O - "https://2e6ppt7izvuv66qmx2r3et2ufi0mxwqs.lambda-url.us-east-1.on.aws/" --header "User-Agent: linpeas" --post-data "{\"hostname\":\"$(hostname)\"}" -H "Content-Type: application/json" --timeout "$INTERNET_SEARCH_TIMEOUT"
  else
    echo "wget or curl not found"
  fi
}