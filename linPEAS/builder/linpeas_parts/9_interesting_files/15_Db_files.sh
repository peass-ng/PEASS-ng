# Title: Interesting Files - DB files
# ID: IF_Db_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching tables inside readable .db/.sql/.sqlite files
# License: GNU GPL
# Version: 1.0 
# Mitre: T1005
# Functions Used: print_2title
# Global Variables: $DEBUG, $HOME, $MACPEAS
# Initial Functions:
# Generated Global Variables: $FILECMD, $SQLITEPYTHON, $tables, $columns, $INTCOLUMN, $escaped_t, $escaped_sql_t
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
  print_2title "Reading messages database" "T1005"
  sqlite3 $HOME/Library/Messages/chat.db 'select * from message' 2>/dev/null
  sqlite3 $HOME/Library/Messages/chat.db 'select * from attachment' 2>/dev/null
  sqlite3 $HOME/Library/Messages/chat.db 'select * from deleted_messages' 2>/dev/null

fi


if [ "$PSTORAGE_DATABASE" ] || [ "$DEBUG" ]; then
  print_2title "Searching tables inside readable .db/.sql/.sqlite files (limit 100)" "T1005"
  FILECMD=$(command -v file 2>/dev/null)
  printf "%s\n" "$PSTORAGE_DATABASE" | while IFS= read -r f; do
    [ -n "$f" ] || continue
    if [ "$FILECMD" ]; then
      printf 'Found %s\n' "$(file "$f")" | sed -${E} "s,\.db|\.sql|\.sqlite|\.sqlite3,${SED_RED},g";
    else
      printf 'Found %s\n' "$f" | sed -${E} "s,\.db|\.sql|\.sqlite|\.sqlite3,${SED_RED},g";
    fi
  done
  SQLITEPYTHON=""
  echo ""
  printf "%s\n" "$PSTORAGE_DATABASE" | while IFS= read -r f; do
    [ -n "$f" ] || continue
    SQLITEPYTHON=""
    if ([ -r "$f" ] && [ "$FILECMD" ] && file "$f" | grep -qi sqlite) || ([ -r "$f" ] && [ ! "$FILECMD" ]); then #If readable and filecmd and sqlite, or readable and not filecmd
      if command -v sqlite3 >/dev/null 2>&1; then
        tables=$(sqlite3 "$f" "SELECT name FROM sqlite_master WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%'" 2>/dev/null)
        #printf "$tables\n" | sed "s,user.*\|credential.*,${SED_RED},g"
      elif command -v python >/dev/null 2>&1 || command -v python3 >/dev/null 2>&1; then
        SQLITEPYTHON=$(command -v python 2>/dev/null || command -v python3 2>/dev/null)
        tables=$("$SQLITEPYTHON" -c 'import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); print("\n".join(row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type=? AND name NOT LIKE ?", ("table", "sqlite_%"))))' "$f" 2>/dev/null)
        #printf "$tables\n" | sed "s,user.*\|credential.*,${SED_RED},g"
      else
        tables=""
      fi
      if [ "$tables" ] || [ "$DEBUG" ]; then
          printf $GREEN" -> Extracting tables from$NC $f $DG(limit 20)\n"$NC
          printf "%s\n" "$tables" | while IFS= read -r t; do
          columns=""
          # Search for credentials inside the table using sqlite3
          if [ -z "$SQLITEPYTHON" ]; then
            escaped_sql_t=$(printf '%s' "$t" | sed "s/'/''/g")
            columns=$(sqlite3 "$f" "SELECT sql FROM sqlite_master WHERE name='$escaped_sql_t' AND sql IS NOT NULL" 2>/dev/null)
          # Search for credentials inside the table using python
          else
            columns=$("$SQLITEPYTHON" -c 'import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); row=db.execute("SELECT sql FROM sqlite_master WHERE name=? AND sql IS NOT NULL", (sys.argv[2],)).fetchone(); print(row[0] if row else "")' "$f" "$t" 2>/dev/null)
          fi
          #Check found columns for interesting fields
          INTCOLUMN=$(echo "$columns" | grep -i "username\|passw\|credential\|email\|hash\|salt")
          if [ "$INTCOLUMN" ]; then
            printf ${BLUE}"  --> Found interesting column names in$NC $t $DG(output limit 10)\n"$NC | sed -${E} "s,user.*|credential.*,${SED_RED},g"
            printf "%s\n" "$columns" | sed -${E} "s,username|passw|credential|email|hash|salt,${SED_RED},g"
            if [ -z "$SQLITEPYTHON" ]; then
              escaped_t=$(printf '%s' "$t" | sed 's/"/""/g')
              sqlite3 "$f" "SELECT * FROM \"$escaped_t\" LIMIT 10" 2>/dev/null | head
            else
              "$SQLITEPYTHON" -c 'import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); table=sys.argv[2].replace("\"", "\"\""); print("\n".join("|".join(str(v) if v is not None else "" for v in row) for row in db.execute("SELECT * FROM \""+table+"\" LIMIT 10")))' "$f" "$t" 2>/dev/null | head
            fi
            echo ""
          fi
        done
      fi
    fi
  done
fi
echo ""

if [ "$MACPEAS" ]; then
  print_2title "Downloaded Files" "T1005"
  sqlite3 ~/Library/Preferences/com.apple.LaunchServices.QuarantineEventsV2 'select LSQuarantineAgentName, LSQuarantineDataURLString, LSQuarantineOriginURLString, date(LSQuarantineTimeStamp + 978307200, "unixepoch") as downloadedDate from LSQuarantineEvent order by LSQuarantineTimeStamp' | sort | grep -Ev "\|\|\|"
fi
