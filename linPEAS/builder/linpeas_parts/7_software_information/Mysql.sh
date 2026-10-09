# Title: Software Information - Mysql
# ID: SI_Mysql
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Mysql credentials
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $knw_usrs, $nosh_usrs, $sh_usrs, $DEBUG, $USER, $STRINGS
# Initial Functions:
# Generated Global Variables: $mysqluser, $mysqlexec, $mysqlconnect, $mysqlconnectnopass, $mysqluser, $version_output, $version, $process_info, $mysql_unit_dir, $mysql_unit_parent, $mysql_unit_file, $mysql_unit_count, $mysql_unit_size, $mysql_unit_signal
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_MYSQL" ] || [ "$DEBUG" ]; then
  print_2title "Searching mysql credentials and exec" "T1552.001"
  printf "%s\n" "$PSTORAGE_MYSQL" | while read d; do
    if [ -f "$d" ] && ! [ "$(basename $d)" = "mysql" ]; then # Only interested in "mysql" that are folders (filesaren't the ones with creds)
      echo "Potential file containing credentials:"
      ls -l "$d"
      if [ "$STRINGS" ]; then
        strings "$d"
      else
        echo "Strings not found, cat the file and check it to get the creds"
      fi

    else
      for f in $(find $d -name debian.cnf 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "We can read the mysql debian.cnf. You can use this username/password to log in MySQL" | sed -${E} "s,.*,${SED_RED},"
          cat "$f"
        fi
      done
      
      for f in $(find $d -name user.MYD 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "We can read the Mysql Hashes from $f" | sed -${E} "s,.*,${SED_RED},"
          grep -oaE "[-_\.\*a-zA-Z0-9]{3,}" "$f" | grep -v "mysql_native_password"
        fi
      done
      
      for f in $(grep -lr "user\s*=" $d 2>/dev/null | grep -v "debian.cnf"); do
        if [ -r "$f" ]; then
          u=$(cat "$f" | grep -v "#" | grep "user" | grep "=" 2>/dev/null)
          echo "From '$f' Mysql user: $u" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
        fi
      done
      
      for f in $(find $d -name my.cnf 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "Found readable $f"
          grep -v "^#" "$f" | grep -Ev "\W+\#|^#" 2>/dev/null | grep -Iv "^$" | sed "s,password.*,${SED_RED},"
        fi
      done
    fi
    
    mysqlexec=$(whereis lib_mysqludf_sys.so 2>/dev/null | grep -Ev '^lib_mysqludf_sys.so:$' | grep "lib_mysqludf_sys\.so")
    if [ "$mysqlexec" ]; then
      echo "Found $mysqlexec. $(whereis lib_mysqludf_sys.so)"
      echo "If you can login in MySQL you can execute commands doing: SELECT sys_eval('id');" | sed -${E} "s,.*,${SED_RED},"
    fi
  done
fi
echo ""

#-- SI) Mysql version
if [ "$(command -v mysql || echo -n '')" ] || [ "$(command -v mysqladmin || echo -n '')" ] || [ "$DEBUG" ]; then
  print_2title "MySQL version" "T1552.001"
  mysql --version 2>/dev/null || echo_not_found "mysql"
  mysqluser=$(systemctl status mysql 2>/dev/null | grep -o ".\{0,0\}user.\{0,50\}" | cut -d '=' -f2 | cut -d ' ' -f1)
  if [ "$mysqluser" ]; then
    echo "MySQL user: $mysqluser" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
  fi
  echo ""
  echo ""

  #-- SI) Mysql connection root/root
  print_list "MySQL connection using default root/root ........... "
  mysqlconnect=$(mysqladmin -uroot -proot version 2>/dev/null)
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root --password=root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  #-- SI) Mysql connection root/toor
  print_list "MySQL connection using root/toor ................... "
  mysqlconnect=$(mysqladmin -uroot -ptoor version 2>/dev/null)
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root --password=toor -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  #-- SI) Mysql connection root/NOPASS
  mysqlconnectnopass=$(mysqladmin -uroot version 2>/dev/null)
  print_list "MySQL connection using root/NOPASS ................. "
  if [ "$mysqlconnectnopass" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
    mysql -u root -e "SELECT User,Host,plugin FROM mysql.user;" 2>/dev/null | sed -${E} "s,auth_socket|unix_socket|plugin,${SED_RED},g"
    mysql -u root -e "SHOW VARIABLES LIKE 'secure_file_priv'; SHOW VARIABLES LIKE 'local_infile';" 2>/dev/null | sed -${E} "s,secure_file_priv|local_infile,${SED_RED},g"
  else echo_no
  fi
  echo ""
fi

### Review the MySQL/MariaDB service identity without assuming a version is exploitable. ###

# Find the mysqld process
process_info=$(ps aux 2>/dev/null | grep -E '[m]ysqld|[m]ariadbd' | head -n1)

if [ -z "$process_info" ]; then
  echo "MySQL/MariaDB process not visible to this user (it may be stopped or /proc may hide other users)."

  # A fixed, small unit-file review can still expose an explicitly configured
  # root context when hidepid prevents process enumeration. Never run the unit.
  if [ -d /run/systemd/system ]; then
    mysql_unit_count=0
    for mysql_unit_dir in /etc/systemd/system /usr/lib/systemd/system /lib/systemd/system; do
      [ -d "$mysql_unit_dir" ] || continue
      mysql_unit_parent=$mysql_unit_dir
      while [ "$mysql_unit_parent" != / ] && [ ! -L "$mysql_unit_parent" ]; do
        mysql_unit_parent=${mysql_unit_parent%/*}
        [ "$mysql_unit_parent" ] || mysql_unit_parent=/
      done
      [ "$mysql_unit_parent" = / ] || continue
      for mysql_unit_file in "$mysql_unit_dir"/*mysql*.service "$mysql_unit_dir"/*mariadb*.service; do
        [ -f "$mysql_unit_file" ] && [ -r "$mysql_unit_file" ] && [ ! -L "$mysql_unit_file" ] || continue
        mysql_unit_count=$((mysql_unit_count + 1))
        if [ "$mysql_unit_count" -gt 12 ]; then
          echo "MySQL/MariaDB unit review incomplete (12-file limit)."
          break 2
        fi
        mysql_unit_size=$(stat -c %s "$mysql_unit_file" 2>/dev/null) ||
          mysql_unit_size=$(stat -f %z "$mysql_unit_file" 2>/dev/null) || continue
        case "$mysql_unit_size" in ''|*[!0-9]*) continue ;; esac
        [ "${#mysql_unit_size}" -le 6 ] && [ "$mysql_unit_size" -le 65536 ] || continue
        mysql_unit_signal=$(head -c 65537 "$mysql_unit_file" 2>/dev/null | awk '
          /^\[Service\][[:space:]]*$/ { service = 1; next }
          /^\[/ { service = 0; next }
          !service || /^[[:space:]]*[#;]/ { next }
          {
            line = $0
            sub(/^[[:space:]]*/, "", line)
            if (line ~ /^User[[:space:]]*=/) {
              sub(/^User[[:space:]]*=[[:space:]]*/, "", line)
              sub(/[[:space:]]*$/, "", line)
              user = line
            } else if (line ~ /^DynamicUser[[:space:]]*=/) {
              sub(/^DynamicUser[[:space:]]*=[[:space:]]*/, "", line)
              sub(/[[:space:]]*$/, "", line)
              dynamic = tolower(line)
            } else if (line ~ /^ExecStart[[:space:]]*=/) {
              sub(/^ExecStart[[:space:]]*=[[:space:]]*[-+!@]*/, "", line)
              direct = (line ~ /^\/[^[:space:]]+\/(mysqld|mariadbd)([[:space:]]|$)/ &&
                line !~ /(^|[[:space:]])--user([=[:space:]]|$)/)
            }
          }
          END {
            if (direct && (user == "" || user == "root") && dynamic !~ /^(yes|true|1)$/)
              print (user == "root" ? "explicit root" : "default root")
          }
        ' 2>/dev/null)
        if [ "$mysql_unit_signal" ]; then
          printf 'MySQL/MariaDB unit root-context review candidate: %s (%s; process state, drop-ins, SQL FILE/UDF rights and plugin directory unverified)\n' "$mysql_unit_file" "$mysql_unit_signal"
        fi
      done
    done
  fi
else

  # Extract the process user
  mysqluser=$(echo "$process_info" | awk '{print $1}')

  # Get the MySQL version string
  version_output=$(mysqld --version 2>&1)

  # Extract the version number (expects format like X.Y.Z)
  version=$(echo "$version_output" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -n1)

  if [ "$mysqluser" = "root" ]; then
    printf 'MySQL/MariaDB process runs as root%s; review SQL access, FILE/UDF rights, plugin directory, and effective service policy.\n' "${version:+ (binary version $version)}" | sed -${E} "s,.*,${SED_RED},"
  else
    printf "MySQL/MariaDB process runs as user '%s'%s.\n" "$mysqluser" "${version:+ (binary version $version)}" | sed -${E} "s,.*,${SED_GREEN},"
  fi
fi
