# Title: Software Information - ssh files
# ID: SI_Ssh
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching ssl/ssh files
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.004,T1021.004
# Functions Used: print_2title, print_3title
# Global Variables: $HOME, $HOMESEARCH, $ROOT_FOLDER, $SEARCH_IN_FOLDER, $TIMEOUT, $USER, $wgroups
# Initial Functions:
# Generated Global Variables: $certsb4_grep, $hostsallow, $hostsdenied, $sshconfig, $writable_agents, $agent_sockets, $privatekeyfilesetc, $privatekeyfileshome, $privatekeyfilesroot, $privatekeyfilesmnt, $sc_number, $sc_depth, $sc_pub, $ssh_ca_files, $sc_file, $sc_line, $sc_meta, $sc_candidate, $ssh_ca_root_login_seen, $sc_mapped, $sc_key, $ssh_ca_context, $sc_header, $sc_value, $sc_pattern, $ssh_ca_principals_seen, $sc_keyword, $sc_config, $sc_output, $sc_root, $sc_count, $sc_size, $mux_home, $mux_path, $mux_count
# Fat linpeas: 0
# Small linpeas: 1

# A multiplexed SSH master can leave a Unix socket below the current home.
# Inspect only .ssh and one direct child level; do not connect to candidates.
ssh_control_socket_candidates() {
  mux_home=$1
  mux_count=0
  [ "$SEARCH_IN_FOLDER" ] && return 0
  [ -n "$mux_home" ] && [ -d "$mux_home/.ssh" ] || return 0
  for mux_path in "$mux_home"/.ssh/* "$mux_home"/.ssh/*/*; do
    [ -S "$mux_path" ] && [ ! -L "$mux_path" ] || continue
    if [ "$mux_count" -eq 0 ]; then
      print_3title "Unix sockets under current SSH home (ControlMaster candidates):" "T1021.004"
    fi
    LC_ALL=C ls -ld "$mux_path" 2>/dev/null
    mux_count=$((mux_count + 1))
    if [ "$mux_count" -ge 20 ]; then
      printf '%s\n' '  [*] Output capped at 20 sockets; others may be unseen.'
      break
    fi
  done
  if [ "$mux_count" -gt 0 ]; then
    printf '%s\n' '  [?] Path, owner and mode only. Config, access and live SSH master remain unverified; an agent socket is different.'
    echo ""
  fi
}

# SSH user CA correlation. This is a bounded config read, not sshd's effective
# configuration: Match criteria, command-line options and unvisited Includes may
# change the values for a particular connection.
ssh_ca_scan_config() {
  local sc_file="$1" sc_depth="$2" sc_line sc_number=0 sc_keyword sc_value
  local sc_pattern sc_mapped sc_candidate sc_pub sc_key sc_header sc_meta
  [ "$sc_depth" -le 3 ] && [ "$ssh_ca_files" -lt 32 ] || return 0
  [ -f "$sc_file" ] && [ -r "$sc_file" ] || return 0
  ssh_ca_files=$((ssh_ca_files + 1))
  while IFS= read -r sc_line || [ -n "$sc_line" ]; do
    sc_number=$((sc_number + 1))
    [ "$sc_number" -le 256 ] || break
    # Only simple, unquoted directives are interpreted. Never evaluate config text.
    sc_line=${sc_line%%#*}
    case "$sc_line" in *\"*|*\'* ) continue ;; esac
    set -f
    # Intentional whitespace tokenization with globbing disabled.
    # shellcheck disable=SC2086
    set -- $sc_line
    set +f
    [ "$#" -gt 0 ] || continue
    sc_keyword=$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')
    shift
    case "$sc_keyword" in
      match)
        ssh_ca_context='Match (conditional)'
        ;;
      include)
        [ "$#" -le 8 ] || continue
        for sc_pattern do
          # Restrict to plain paths and a final-component '*' glob. This avoids
          # eval, broad directory walks, and ambiguous quoted/token paths.
          case "$sc_pattern" in
            *[!A-Za-z0-9_./*-]*|*/*/*/*/*/*/*/*|*'**'*) continue ;;
          esac
          case "$sc_pattern" in
            /*) ;;
            *) sc_pattern="/etc/ssh/$sc_pattern" ;;
          esac
          case "${sc_pattern%/*}" in *'*'*) continue ;; esac
          sc_mapped="$sc_pattern"
          [ "$SEARCH_IN_FOLDER" ] && sc_mapped="${ROOT_FOLDER%/}$sc_pattern"
          for sc_candidate in $sc_mapped; do
            [ "$ssh_ca_files" -lt 32 ] || break
            case "$sc_candidate" in *[!A-Za-z0-9_./-]* ) continue ;; esac
            ssh_ca_scan_config "$sc_candidate" $((sc_depth + 1))
          done
        done
        ;;
      trustedusercakeys|authorizedprincipalsfile|authorizedprincipalscommand|permitrootlogin)
        [ "$#" -gt 0 ] || continue
        sc_value="$1"
        case "$sc_keyword" in
          trustedusercakeys)
            [ "$#" -eq 1 ] || continue
            case "$sc_value" in /*) ;; *) continue ;; esac
            case "$sc_value" in *[!A-Za-z0-9_./-]* ) continue ;; esac
            printf 'TrustedUserCAKeys %s (%s:%s; %s)\n' "$sc_value" "$sc_file" "$sc_number" "$ssh_ca_context"
            case "$sc_value" in *.pub) ;; *) continue ;; esac
            sc_pub="$sc_value"
            [ "$SEARCH_IN_FOLDER" ] && sc_pub="${ROOT_FOLDER%/}$sc_value"
            [ -f "$sc_pub" ] && [ -r "$sc_pub" ] || continue
            sc_key=${sc_pub%.pub}
            [ -f "$sc_key" ] && [ -r "$sc_key" ] && [ ! -L "$sc_key" ] || continue
            sc_header=$(dd if="$sc_key" bs=64 count=1 2>/dev/null) || continue
            case "$sc_header" in
              '-----BEGIN OPENSSH PRIVATE KEY-----'*|'-----BEGIN RSA PRIVATE KEY-----'*|\
              '-----BEGIN EC PRIVATE KEY-----'*|'-----BEGIN DSA PRIVATE KEY-----'*|\
              '-----BEGIN PRIVATE KEY-----'*|'-----BEGIN ENCRYPTED PRIVATE KEY-----'*) ;;
              *) continue ;;
            esac
            sc_meta=$(stat -c '%U %a' "$sc_key" 2>/dev/null) ||
              sc_meta=$(stat -f '%Su %Lp' "$sc_key" 2>/dev/null) || continue
            printf 'Readable CA private-key sibling: %s (owner mode: %s; header only checked)\n' "$sc_key" "$sc_meta"
            ;;
          authorizedprincipalsfile|authorizedprincipalscommand)
            ssh_ca_principals_seen=1
            printf '%s %s (%s:%s; %s)\n' "$sc_keyword" "$sc_value" "$sc_file" "$sc_number" "$ssh_ca_context"
            ;;
          permitrootlogin)
            ssh_ca_root_login_seen=1
            printf 'PermitRootLogin %s (%s:%s; %s)\n' "$sc_value" "$sc_file" "$sc_number" "$ssh_ca_context"
            ;;
        esac
        ;;
    esac
  done < "$sc_file"
}

ssh_ca_trust_correlation() {
  local sc_config='/etc/ssh/sshd_config' sc_output
  [ "$SEARCH_IN_FOLDER" ] && sc_config="${ROOT_FOLDER%/}$sc_config"
  [ -f "$sc_config" ] && [ -r "$sc_config" ] || return 0
  sc_output=$(
    ssh_ca_files=0
    ssh_ca_context='before Match in inspected files'
    ssh_ca_principals_seen=0
    ssh_ca_root_login_seen=0
    ssh_ca_scan_config "$sc_config" 0
    [ "$ssh_ca_principals_seen" -eq 0 ] &&
      printf 'No AuthorizedPrincipalsFile/Command observed in inspected files.\n'
    [ "$ssh_ca_root_login_seen" -eq 0 ] &&
      printf 'No PermitRootLogin observed in inspected files.\n'
  )
  case "$sc_output" in *'TrustedUserCAKeys '*) ;; *) return 0 ;; esac
  print_3title 'SSH user CA trust (partial config inspection)' 'T1552.004,T1021.004'
  printf '%s\n' "$sc_output"
  printf '%s\n' 'If both principal sources are absent from the effective config, certificate principals must match account names.'
  printf '%s\n' 'Earlier directives, Match, unvisited Includes, and runtime options may change these values; key headers do not prove a CA match or root login.'
  echo ''
}

# Show only a bounded portion of the readable server config. Match headings
# retain the scope of subsequent directives; this text is not sshd's policy.
ssh_forwarding_config_review() {
  print_3title 'SSH server directives (review effective SSH policy)' 'T1021.004'
  if [ ! -f "$1" ] || [ ! -r "$1" ]; then
    printf '%s\n' 'SSH server config unreadable; forwarding policy unknown.'
    return 0
  fi
  awk '
    NR > 512 { exit }
    {
      line = substr($0, 1, 1024)
      sub(/[[:space:]]*#.*/, "", line)
      sub(/^[[:space:]]*/, "", line)
      sub(/[[:space:]]*$/, "", line)
      if (line == "") next
      split(line, fields, /[[:space:]]+/)
      key = tolower(fields[1])
      if (key == "match") {
        context = line
        printf "%d: %s\n", NR, line
      } else if (key == "include" || key == "permitrootlogin" ||
                 key == "challengeresponseauthentication" ||
                 key == "passwordauthentication" || key == "usepam" ||
                 key == "port" || key == "permitemptypasswords" ||
                 key == "pubkeyauthentication" || key == "listenaddress" ||
                 key == "forwardagent" || key == "allowagentforwarding" ||
                 key == "authorizedkeysfile" || key == "authorizedkeyscommand" ||
                 key == "authorizedkeyscommanduser" || key == "allowtcpforwarding" ||
                 key == "disableforwarding" || key == "permitopen" ||
                 key == "forcecommand" || key == "chrootdirectory") {
        printf "%d [%s]: %s\n", NR, context, line
      }
    }
    BEGIN { context = "global" }
  ' "$1"
  printf '%s\n' 'Text review only (first 512 lines, 1024 characters per line). Include targets, Match applicability, command-line overrides, and authorized-key restrictions are unknown here; key-helper directives do not prove execution or vulnerability.'
  echo ''
}

# Without timeout, inspect only direct files in /root. Cap the number and size
# before reading a header; report metadata, never key material.
ssh_root_key_headers() {
  local sc_root="$1" sc_file sc_count=0 sc_size sc_header sc_meta
  [ "$(id -u 2>/dev/null)" = 0 ] || return 0
  [ -d "$sc_root" ] && [ -r "$sc_root" ] && [ -x "$sc_root" ] || return 0
  for sc_file in "$sc_root"/* "$sc_root"/.[!.]* "$sc_root"/..?*; do
    [ -f "$sc_file" ] && [ -r "$sc_file" ] && [ ! -L "$sc_file" ] || continue
    sc_count=$((sc_count + 1))
    [ "$sc_count" -le 64 ] || break
    sc_size=$(stat -c '%s' "$sc_file" 2>/dev/null) ||
      sc_size=$(stat -f '%z' "$sc_file" 2>/dev/null) || continue
    case "$sc_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sc_size" -le 1048576 ] || continue
    sc_header=$(dd if="$sc_file" bs=64 count=1 2>/dev/null) || continue
    case "$sc_header" in
      '-----BEGIN OPENSSH PRIVATE KEY-----'*|'-----BEGIN RSA PRIVATE KEY-----'*|\
      '-----BEGIN EC PRIVATE KEY-----'*|'-----BEGIN DSA PRIVATE KEY-----'*|\
      '-----BEGIN PRIVATE KEY-----'*|'-----BEGIN ENCRYPTED PRIVATE KEY-----'*) ;;
      *) continue ;;
    esac
    sc_meta=$(stat -c '%U %a' "$sc_file" 2>/dev/null) ||
      sc_meta=$(stat -f '%Su %Lp' "$sc_file" 2>/dev/null) || continue
    printf '%s (owner mode: %s; header only checked)\n' "$sc_file" "$sc_meta"
  done
}


print_2title "Searching ssl/ssh files" "T1552.004,T1021.004"
if [ "$PSTORAGE_CERTSB4" ]; then certsb4_grep=$(grep -L "\"\|'\|(" $PSTORAGE_CERTSB4 2>/dev/null); fi
if ! [ "$SEARCH_IN_FOLDER" ]; then
  sshconfig="$(ls /etc/ssh/ssh_config 2>/dev/null)"
  hostsdenied="$(ls /etc/hosts.denied 2>/dev/null)"
  hostsallow="$(ls /etc/hosts.allow 2>/dev/null)"
  agent_sockets=$(find /run/user /tmp -type s \( -path "/run/user/*/ssh-*/agent.*" -o -name "ssh-agent.sock" -o -path "/tmp/ssh-*" \) 2>/dev/null)
  writable_agents=$(find /tmp /etc /home /run/user \
    \( -type s -a \( -name "agent.*" -o -name "ssh-agent.sock" -o -path "*/ssh-*/agent.*" -o -name "*gpg-agent*" \) \
    -a \( \( -user "$USER" \) -o \( -perm -o=w \) -o \( -perm -g=w -a \( $wgroups \) \) \) \) 2>/dev/null)
else
  sshconfig="$(ls ${ROOT_FOLDER}etc/ssh/ssh_config 2>/dev/null)"
  hostsdenied="$(ls ${ROOT_FOLDER}etc/hosts.denied 2>/dev/null)"
  hostsallow="$(ls ${ROOT_FOLDER}etc/hosts.allow 2>/dev/null)"
  agent_sockets=$(find "${ROOT_FOLDER}"tmp "${ROOT_FOLDER}"run -type s \( -name "agent.*" -o -name "ssh-agent.sock" \) 2>/dev/null)
  writable_agents=$(find "${ROOT_FOLDER}" \
    \( -type s -a \( -name "agent.*" -o -name "ssh-agent.sock" -o -path "*/ssh-*/agent.*" -o -name "*gpg-agent*" \) \
    -a \( \( -user "$USER" \) -o \( -perm -o=w \) -o \( -perm -g=w -a \( $wgroups \) \) \) \) 2>/dev/null)
fi

peass{SSH}

if [ "$SEARCH_IN_FOLDER" ]; then
  ssh_forwarding_config_review "${ROOT_FOLDER%/}/etc/ssh/sshd_config"
else
  ssh_forwarding_config_review '/etc/ssh/sshd_config'
fi
ssh_ca_trust_correlation

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$TIMEOUT" ]; then
    privatekeyfilesetc=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' /etc 2>/dev/null)
    privatekeyfileshome=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' $HOMESEARCH 2>/dev/null)
    privatekeyfilesroot=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' /root 2>/dev/null)
    privatekeyfilesmnt=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' /mnt 2>/dev/null)
  else
    privatekeyfilesetc=$(grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' /etc 2>/dev/null) #If there is tons of files linpeas gets frozen here without a timeout
    privatekeyfileshome=$(grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' $HOME/.ssh 2>/dev/null)
    privatekeyfilesroot=$(ssh_root_key_headers /root)
  fi
else
  # If $SEARCH_IN_FOLDER lets just search for private keys in the whole firmware
  privatekeyfilesetc=$(timeout 120 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY\-\-\-\-\-' "$ROOT_FOLDER" 2>/dev/null)
fi

if [ "$privatekeyfilesetc" ] || [ "$privatekeyfileshome" ] || [ "$privatekeyfilesroot" ] || [ "$privatekeyfilesmnt" ] ; then
  echo ""
  print_3title "Possible private SSH keys were found!" | sed -${E} "s,private SSH keys,${SED_RED},"
  if [ "$privatekeyfilesetc" ]; then printf "$privatekeyfilesetc\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfileshome" ]; then printf "$privatekeyfileshome\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfilesroot" ]; then printf "$privatekeyfilesroot\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfilesmnt" ]; then printf "$privatekeyfilesmnt\n" | sed -${E} "s,.*,${SED_RED},"; fi
  echo ""
fi
if [ "$certsb4_grep" ] || [ "$PSTORAGE_CERTSBIN" ]; then
  print_3title "Some certificates were found (out limited):" "T1552.004,T1021.004"
  printf "$certsb4_grep\n" | head -n 20
  printf "$PSTORAGE_CERTSBIN\n" | head -n 20
    echo ""
fi
if [ "$PSTORAGE_CERTSCLIENT" ]; then
  print_3title "Some client certificates were found:" "T1552.004,T1021.004"
  printf "$PSTORAGE_CERTSCLIENT\n"
  echo ""
fi
if [ "$PSTORAGE_SSH_AGENTS" ]; then
  print_3title "Some SSH Agent files were found:" "T1552.004,T1021.004"
  printf "$PSTORAGE_SSH_AGENTS\n"
  echo ""
fi
if [ "$agent_sockets" ]; then
  print_3title "Potential SSH agent sockets were found:" "T1552.004,T1021.004"
  printf "%s\n" "$agent_sockets" | sed -${E} "s,.*,${SED_RED},"
  echo ""
fi
ssh_control_socket_candidates "$HOME"
if ssh-add -l 2>/dev/null | grep -qv 'no identities'; then
  print_3title "Listing SSH Agents" "T1552.004,T1021.004"
  ssh-add -l
  echo ""
fi
if gpg-connect-agent "keyinfo --list" /bye 2>/dev/null | grep "D - - 1"; then
  print_3title "Listing gpg keys cached in gpg-agent" "T1552.004,T1021.004"
  gpg-connect-agent "keyinfo --list" /bye
  echo ""
fi
if [ "$writable_agents" ]; then
  print_3title "Writable ssh and gpg agents" "T1552.004,T1021.004"
  printf "%s\n" "$writable_agents"
fi
if [ "$PSTORAGE_SSH_CONFIG" ]; then
  print_3title "Some home ssh config file was found" "T1552.004,T1021.004"
  printf "%s\n" "$PSTORAGE_SSH_CONFIG" | while read f; do ls "$f" | sed -${E} "s,$f,${SED_RED},"; cat "$f" 2>/dev/null | grep -Iv "^$" | grep -v "^#" | sed -${E} "s,User|ProxyCommand,${SED_RED},"; done
  echo ""
fi
if [ "$hostsdenied" ]; then
  print_3title "/etc/hosts.denied file found, read the rules:" "T1552.004,T1021.004"
  printf "$hostsdenied\n"
  cat " ${ROOT_FOLDER}etc/hosts.denied" 2>/dev/null | grep -v "#" | grep -Iv "^$" | sed -${E} "s,.*,${SED_GREEN},"
  echo ""
fi
if [ "$hostsallow" ]; then
  print_3title "/etc/hosts.allow file found, trying to read the rules:" "T1552.004,T1021.004"
  printf "$hostsallow\n"
  cat " ${ROOT_FOLDER}etc/hosts.allow" 2>/dev/null | grep -v "#" | grep -Iv "^$" | sed -${E} "s,.*,${SED_RED},"
  echo ""
fi
if [ "$sshconfig" ]; then
  echo ""
  echo "Searching inside /etc/ssh/ssh_config for interesting info"
  grep -v "^#"  ${ROOT_FOLDER}etc/ssh/ssh_config 2>/dev/null | grep -Ev "\W+\#|^#" 2>/dev/null | grep -Iv "^$" | sed -${E} "s,Host|ForwardAgent|User|ProxyCommand,${SED_RED},"
fi
echo ""
