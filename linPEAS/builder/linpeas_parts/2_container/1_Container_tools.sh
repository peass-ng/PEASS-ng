# Title: Container - Container Tools
# ID: CT_Container_tools
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Find container related tools in the PATH of the system that could be used for container escape:
#   - Container runtime tools
#   - Container management tools
#   - Container networking tools
#   - Common vulnerable scenarios:
#     * Misconfigured container tools
#     * Privileged container tools
#     * Container escape tools
#   - Exploitation methods:
#     * Tool abuse: Exploit container tool misconfigurations
#     * Common attack vectors:
#       - Runtime escape
#       - Privilege escalation
#       - Container breakout
#     * Exploit techniques:
#       - Tool misconfiguration abuse
#       - Privileged tool exploitation
#       - Container escape tool usage
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Container related tools present (if any):"

# Container runtimes
warn_exec command -v docker
warn_exec command -v lxc
warn_exec command -v rkt
warn_exec command -v podman
warn_exec command -v runc
warn_exec command -v ctr
warn_exec command -v containerd
warn_exec command -v crio
warn_exec command -v nerdctl

# Container management
warn_exec command -v kubectl
warn_exec command -v crictl
warn_exec command -v docker-compose
warn_exec command -v docker-machine
warn_exec command -v minikube
warn_exec command -v kind

# Container networking
warn_exec command -v docker-proxy
warn_exec command -v cni
warn_exec command -v flanneld
warn_exec command -v calicoctl

# Container security
warn_exec command -v apparmor_parser
warn_exec command -v seccomp
warn_exec command -v gvisor
warn_exec command -v kata-runtime

# Container debugging
warn_exec command -v nsenter
warn_exec command -v unshare
warn_exec command -v chroot
warn_exec command -v capsh
warn_exec command -v setcap
warn_exec command -v getcap

echo ""