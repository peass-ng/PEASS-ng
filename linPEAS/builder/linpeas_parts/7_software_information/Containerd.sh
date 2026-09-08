# Title: Software Information - containerd installed
# ID: SI_Containerd
# Author: Carlos Polop
# Last Update: 21-03-2026
# Description: containerd and related CRI/containerd client tooling installed
# License: GNU GPL
# Version: 1.0
# Mitre: T1613
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $containerd, $containerd_cli, $crictl_cli, $nerdctl_cli
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  containerd=$(command -v containerd || echo -n '')
  containerd_cli=$(command -v ctr || echo -n '')
  nerdctl_cli=$(command -v nerdctl || echo -n '')
  crictl_cli=$(command -v crictl || echo -n '')
  if [ "$containerd" ] || [ "$containerd_cli" ] || [ "$nerdctl_cli" ] || [ "$crictl_cli" ] || [ "$DEBUG" ]; then
    print_2title "Checking if containerd/CRI tooling is available" "T1613"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/container-security/runtime-api-and-daemon-exposure.html"
    if [ "$containerd" ]; then
      echo "containerd was found in $containerd" | sed -${E} "s,.*,${SED_RED},"
    fi
    if [ "$containerd_cli" ]; then
      echo "ctr was found in $containerd_cli, you may be able to inspect or manage containerd content with it" | sed -${E} "s,.*,${SED_RED},"
      ctr image list 2>&1
    fi
    if [ "$nerdctl_cli" ]; then
      echo "nerdctl was found in $nerdctl_cli, you may be able to interact with containerd namespaces and containers with it" | sed -${E} "s,.*,${SED_RED},"
      nerdctl images 2>&1
    fi
    if [ "$crictl_cli" ]; then
      echo "crictl was found in $crictl_cli, you may be able to inspect CRI-managed containers with it" | sed -${E} "s,.*,${SED_RED},"
      crictl images 2>&1
    fi
    echo ""
  fi
fi
