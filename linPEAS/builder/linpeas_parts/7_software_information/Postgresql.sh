# Title: Software Information - PostgreSQL
# ID: SI_Postgresql
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: PostgreSQL brute
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_no, print_list, warn_exec
# Global Variables: $DEBUG, $TIMEOUT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


peass{PostgreSQL}

if [ "$TIMEOUT" ] && [ "$(command -v psql || echo -n '')" ] || [ "$DEBUG" ]; then  # In some OS (like OpenBSD) it will expect the password from console and will pause the script. Also, this OS doesn't have the "timeout" command so lets only use this checks in OS that has it.
#checks to see if any postgres password exists and connects to DB 'template0' - following commands are a variant on this
  print_list "PostgreSQL connection to template0 using postgres/NOPASS ........ "
  if [ "$(timeout 1 psql -U postgres -d template0 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template1 using postgres/NOPASS ........ "
  if [ "$(timeout 1 psql -U postgres -d template1 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template0 using pgsql/NOPASS ........... "
  if [ "$(timeout 1 psql -U pgsql -d template0 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template1 using pgsql/NOPASS ........... "
  if [ "$(timeout 1 psql -U pgsql -d template1 -c 'select version()' 2> /dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi
  echo ""
fi