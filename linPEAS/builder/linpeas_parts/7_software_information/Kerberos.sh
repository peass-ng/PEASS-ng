# Title: Software Information - Kerberos
# ID: SI_Kerberos
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Kerberos
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $DEBUG, $ITALIC
# Initial Functions:
# Generated Global Variables: $kadmin_exists, $klist_exists, $kinit_exists, $ptrace_scope
# Fat linpeas: 0
# Small linpeas: 1


kadmin_exists="$(command -v kadmin || echo -n '')"
klist_exists="$(command -v klist || echo -n '')"
kinit_exists="$(command -v kinit || echo -n '')"
if [ "$kadmin_exists" ] || [ "$klist_exists" ] || [ "$kinit_exists" ] || [ "$PSTORAGE_KERBEROS" ] || [ "$DEBUG" ]; then
  print_2title "Searching kerberos conf files and tickets"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/linux-active-directory.html#linux-active-directory"

  if [ "$kadmin_exists" ]; then echo "kadmin was found on $kadmin_exists" | sed "s,$kadmin_exists,${SED_RED},"; fi
  if [ "$kinit_exists" ]; then echo "kadmin was found on $kinit_exists" | sed "s,$kinit_exists,${SED_RED},"; fi
  if [ "$klist_exists" ] && [ -x "$klist_exists" ]; then echo "klist execution"; klist; fi
  ptrace_scope="$(cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null)"
  if [ "$ptrace_scope" ] && [ "$ptrace_scope" -eq 0 ]; then echo "ptrace protection is disabled (0), you might find tickets inside processes memory" | sed "s,is disabled,${SED_RED},g";
  else echo "ptrace protection is enabled ($ptrace_scope), you need to disable it to search for tickets inside processes memory" | sed "s,is enabled,${SED_GREEN},g";
  fi
  
  (env || printenv) 2>/dev/null | grep -E "^KRB5" | sed -${E} "s,KRB5,${SED_RED},g"

  printf "%s\n" "$PSTORAGE_KERBEROS" | while read f; do
    if [ -r "$f" ]; then
      if echo "$f" | grep -q .k5login; then
        echo ".k5login file (users with access to the user who has this file in his home)"
        cat "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
      elif echo "$f" | grep -q keytab; then
        echo ""
        echo "keytab file found, you may be able to impersonate some kerberos principals and add users or modify passwords"
        klist -k "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
        printf "$(klist -k $f 2>/dev/null)\n" | awk '{print $2}' | while read l; do
          if [ "$l" ] && echo "$l" | grep -q "@"; then
            printf "$ITALIC  --- Impersonation command: ${NC}kadmin -k -t /etc/krb5.keytab -p \"$l\"\n" | sed -${E} "s,$l,${SED_RED},g"
            #kadmin -k -t /etc/krb5.keytab -p "$l" -q getprivs 2>/dev/null #This should show the permissions of each impersoanted user, the thing is that in a test it showed that every user had the same permissions (even if they didn't). So this test isn't valid
            #We could also try to create a new user or modify a password, but I'm not user if linpeas should do that
          fi
        done
      elif echo "$f" | grep -q krb5.conf; then
        ls -l "$f"
        cat "$f" 2>/dev/null | sed -${E} "s,default_ccache_name,${SED_RED},";
      elif echo "$f" | grep -q kadm5.acl; then
        ls -l "$f" 
        cat "$f" 2>/dev/null
      elif echo "$f" | grep -q sssd.conf; then
        ls -l "$f"
        cat "$f" 2>/dev/null | sed -${E} "s,cache_credentials ?= ?[tT][rR][uU][eE],${SED_RED},";
      elif echo "$f" | grep -q secrets.ldb; then
        echo "You could use SSSDKCMExtractor to extract the tickets stored here" | sed -${E} "s,SSSDKCMExtractor,${SED_RED},";
        ls -l "$f"
      elif echo "$f" | grep -q .secrets.mkey; then
        echo "This is the secrets file to use with SSSDKCMExtractor" | sed -${E} "s,SSSDKCMExtractor,${SED_RED},";
        ls -l "$f"
      fi
    fi
  done
  ls -l "/tmp/krb5cc*" "/var/lib/sss/db/ccache_*" "/etc/opt/quest/vas/host.keytab" 2>/dev/null || echo_not_found "tickets kerberos"
  klist 2>/dev/null || echo_not_found "klist"
  echo ""

fi