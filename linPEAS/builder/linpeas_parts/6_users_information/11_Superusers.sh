# Title: Users Information - Superusers
# ID: UG_Superusers
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Check UID 0 accounts and bounded shared local UIDs
# License: GNU GPL
# Version: 1.1
# Mitre: T1087.001
# Functions Used: print_2title, print_info
# Global Variables: $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $group, $group_entry, $passwd_identity_rows, $shared_uid_rows
# Fat linpeas: 0
# Small linpeas: 1


# Collect the existing UID 0 inventory and a bounded local duplicate-UID cue
# in one read of /etc/passwd. A shared UID is a review lead, not root access.
summarize_local_passwd() {
  awk -F: '
    $3 ~ /^[0-9]+$/ && $3 + 0 == 0 { print "R\t" $0 }
    NR == 4097 { print "P" }
    NR <= 4096 && $1 != "" && $3 ~ /^[0-9]+$/ && $3 + 0 > 0 {
        uid = $3 + 0
        if (uid in first) {
            if (++duplicates <= 32) print "D\t" uid "\t" first[uid] "\t" $1
            else if (duplicates == 33) print "L"
        } else first[uid] = $1
    }
  ' "$1" 2>/dev/null
}

print_2title "Superusers and UID 0 Users" "T1087.001"
print_info "https://book.hacktricks.wiki/en/linux-hardening/user-information/interesting-groups-linux-pe/index.html"
passwd_identity_rows=$(summarize_local_passwd /etc/passwd)

echo ""
print_3title "Users with UID 0 in /etc/passwd" "T1087.001"
printf '%s\n' "$passwd_identity_rows" | awk -F '\t' '$1 == "R" { sub(/^R\t/, ""); print }' | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED_YELLOW},g" | sed "s,root,${SED_RED},g"

shared_uid_rows=$(printf '%s\n' "$passwd_identity_rows" | awk -F '\t' '
    $1 == "D" { printf "UID %s: %s, %s\n", $2, $3, $4 }
    $1 == "L" { print "Partial: first 32 shared-UID pairs shown." }
')
if [ -n "$shared_uid_rows" ]; then
    echo ""
    print_3title "Accounts sharing nonzero local UIDs (review candidate)" "T1087.001"
    printf '%s\n' "$shared_uid_rows"
    echo "Shared UIDs can be intentional; verify account ownership, authentication, and local policy."
fi
if printf '%s\n' "$passwd_identity_rows" | grep -q '^P$'; then
    echo "Shared-UID comparison partial: only the first 4096 local accounts were compared."
fi

if command -v getent >/dev/null 2>&1; then
    for group in sudo wheel adm docker lxd lxc root shadow disk video; do
        if group_entry=$(getent group "$group" 2>/dev/null) && [ -n "$group_entry" ]; then
            echo "- Users in group '$group':"
            printf '%s\n' "$group_entry" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED},g" | sed "s,root,${SED_RED},g"
        fi
    done
fi

# Check for users with sudo privileges in sudoers
echo ""
print_3title "Users with sudo privileges in sudoers" "T1087.001"
grep -v "^#" /etc/sudoers 2>/dev/null | grep -v "^$" | grep -v "^Defaults" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED_YELLOW},g" | sed "s,root,${SED_RED},g"
echo ""
