# Title: Software Information - Mysql
# ID: SI_Mysql
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Mysql credentials
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $knw_usrs, $nosh_usrs, $sh_usrs, $DEBUG, $USER, $STRINGS
# Initial Functions:
# Generated Global Variables: $mysqluser, $mysqlexec, $mysqlconnect, $mysqlconnectnopass, $mysqluser, $version_output, $major_version, $version, $process_info
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_MYSQL" ] || [ "$DEBUG" ]; then
  print_2title "Searching mysql credentials and exec"
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
  print_2title "MySQL version"
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
  else echo_no
  fi
  echo ""
fi

### This section checks if MySQL (mysqld) is running as root and if its version is 4.x or 5.x to refer a known local privilege escalation exploit! ###

# Find the mysqld process
process_info=$(ps aux | grep '[m]ysqld' | head -n1)

if [ -z "$process_info" ]; then
  echo "MySQL process not found." | sed -${E} "s,.*,${SED_GREEN},"
else

  # Extract the process user
  mysqluser=$(echo "$process_info" | awk '{print $1}')

  # Get the MySQL version string
  version_output=$(mysqld --version 2>&1)

  # Extract the version number (expects format like X.Y.Z)
  version=$(echo "$version_output" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -n1)

  if [ -z "$version" ]; then
    echo "Unable to determine MySQL version." | sed -${E} "s,.*,${SED_GREEN},"
  else

    # Extract the major version number (X from X.Y.Z)
    major_version=$(echo "$version" | cut -d. -f1)

    # Check if MySQL is running as root and if the version is either 4.x or 5.x
    if [ "$mysqluser" = "root" ] && { [ "$major_version" -eq 4 ] || [ "$major_version" -eq 5 ]; }; then
      echo "MySQL is running as root with version $version. This is a potential local privilege escalation vulnerability!" | sed -${E} "s,.*,${SED_RED},"
      echo "\tRefer to: https://www.exploit-db.com/exploits/1518" | sed -${E} "s,.*,${SED_YELLOW},"
      echo "\tRefer to: https://medium.com/r3d-buck3t/privilege-escalation-with-mysql-user-defined-functions-996ef7d5ceaf" | sed -${E} "s,.*,${SED_YELLOW},"
    else
      echo "MySQL is running as user '$mysqluser' with version $version." | sed -${E} "s,.*,${SED_GREEN},"
    fi
    ### ------------------------------------------------------------------------------------------------------------------------------------------------ ###
  
  fi
fi