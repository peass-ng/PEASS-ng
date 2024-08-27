# Title: Software Information - Checking leaks in git repositories
# ID: SI_Leaks_git_repo
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking leaks in git repositories
# License: GNU GPL
# Version: 1.0
# Functions Used: execBin, print_2title
# Global Variables: $MACPEAS, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $git_dirname, $FAT_LINPEAS_GITLEAKS
# Fat linpeas: 1
# Small linpeas: 0


if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
  print_2title "Checking leaks in git repositories"
  printf "%s\n" "$PSTORAGE_GITHUB" | while read f; do
    if echo "$f" | grep -Eq ".git$"; then
      git_dirname=$(dirname "$f")
      if [ "$MACPEAS" ]; then
        FAT_LINPEAS_GITLEAKS="peass{https://github.com/gitleaks/gitleaks/releases/download/v8.17.0/gitleaks_8.17.0_darwin_arm64.tar.gz}"
      else
        FAT_LINPEAS_GITLEAKS="peass{https://github.com/gitleaks/gitleaks/releases/download/v8.17.0/gitleaks_8.17.0_linux_x64.tar.gz}"
      fi
      execBin "GitLeaks (checking $git_dirname)" "https://github.com/zricethezav/gitleaks" "$FAT_LINPEAS_GITLEAKS" "detect -s '$git_dirname' -v | grep -E 'Description|Match|Secret|Message|Date'"
    fi
  done
  echo ""
fi