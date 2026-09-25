# Title: Container - List mounted tokens
# ID: CT_List_mounted_tokens
# Author: Carlos Polop, HT Bot
# Last Update: 25-09-2026
# Description: Report mounted Kubernetes service-account credentials without disclosing bearer-token contents.
# License: GNU GPL
# Version: 1.0
# Mitre: T1528,T1552.007
# Functions Used: print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables: $k8s_mounted_credential_found, $k8s_mounted_sa_dir
# Fat linpeas: 0
# Small linpeas: 1

k8s_mounted_credential_found=""
for k8s_mounted_sa_dir in \
  /var/run/secrets/kubernetes.io/serviceaccount \
  /run/secrets/kubernetes.io/serviceaccount \
  /secrets/kubernetes.io/serviceaccount; do
  if [ -d "$k8s_mounted_sa_dir" ] && { [ -e "$k8s_mounted_sa_dir/token" ] || [ -e "$k8s_mounted_sa_dir/namespace" ]; }; then
    if [ -z "$k8s_mounted_credential_found" ]; then
      print_2title "Mounted Kubernetes service-account credentials" "T1528,T1552.007"
      print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/attacking-kubernetes-from-inside-a-pod.html"
      k8s_mounted_credential_found="1"
    fi

    echo "Directory: $k8s_mounted_sa_dir"
    if [ -r "$k8s_mounted_sa_dir/namespace" ]; then
      echo "Namespace: $(cat "$k8s_mounted_sa_dir/namespace" 2>/dev/null)"
    fi
    if [ -r "$k8s_mounted_sa_dir/token" ]; then
      echo "Token: present and readable (contents suppressed)" | sed -${E} "s,present and readable,${SED_RED_YELLOW},"
    elif [ -e "$k8s_mounted_sa_dir/token" ]; then
      echo "Token: present but not readable"
    fi
    if [ -r "$k8s_mounted_sa_dir/ca.crt" ]; then
      echo "Cluster CA: present and readable"
    fi
    echo ""
  fi
done
