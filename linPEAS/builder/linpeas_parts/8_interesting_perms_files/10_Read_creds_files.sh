# Title: Interesting Permissions Files - Read creds files
# ID: IP_Read_creds_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Hashes in passwd file, shadow files and other files
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_no, print_list
# Global Variables: $IAMROOT, $SEARCH_IN_FOLDER, $USER, $wgroups
# Initial Functions:
# Generated Global Variables: $possible_check
# Fat linpeas: 0
# Small linpeas: 1


##-- IPF) Hashes in passwd file
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_list "Hashes inside passwd file? ........... "
  if grep -qv '^[^:]*:[x\*\!]\|^#\|^$' /etc/passwd /etc/master.passwd /etc/group 2>/dev/null; then grep -v '^[^:]*:[x\*]\|^#\|^$' /etc/passwd /etc/pwd.db /etc/master.passwd /etc/group 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IPF) Writable in passwd file
  print_list "Writable passwd file? ................ "
  if [ -w "/etc/passwd" ]; then echo "/etc/passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/pwd.db" ]; then echo "/etc/pwd.db is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/master.passwd" ]; then echo "/etc/master.passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IPF) Credentials in fstab
  print_list "Credentials in fstab/mtab? ........... "
  if grep -qE "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null; then grep -E "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IPF) Read shadow files
  print_list "Can I read shadow files? ............. "
  if [ "$(cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/gshadow /etc/gshadow- /etc/master.passwd /etc/spwd.db 2>/dev/null)" ]; then cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/gshadow /etc/gshadow- /etc/master.passwd /etc/spwd.db 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "Can I read shadow plists? ............ "
  possible_check=""
  (for l in /var/db/dslocal/nodes/Default/users/*; do if [ -r "$l" ];then echo "$l"; defaults read "$l"; possible_check="1"; fi; done; if ! [ "$possible_check" ]; then echo_no; fi) 2>/dev/null || echo_no

  print_list "Can I write shadow plists? ........... "
  possible_check=""
  (for l in /var/db/dslocal/nodes/Default/users/*; do if [ -w "$l" ];then echo "$l"; possible_check="1"; fi; done; if ! [ "$possible_check" ]; then echo_no; fi) 2>/dev/null || echo_no

  ##-- IPF) Read opasswd file
  print_list "Can I read opasswd file? ............. "
  if [ -r "/etc/security/opasswd" ]; then cat /etc/security/opasswd 2>/dev/null || echo ""
  else echo_no
  fi

  ##-- IPF) network-scripts
  print_list "Can I write in network-scripts? ...... "
  if ! [ "$IAMROOT" ] && [ -w "/etc/sysconfig/network-scripts/" ]; then echo "You have write privileges on /etc/sysconfig/network-scripts/" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ "$(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges on $(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IPF) Read root dir
  print_list "Can I read root folder? .............. "
  (ls -al /root/ 2>/dev/null | grep -vi "total 0") || echo_no
  echo ""
fi