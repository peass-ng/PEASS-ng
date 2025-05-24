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
# Functions Used: print_2title
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Container related tools present (if any):"

# Container runtimes
command -v docker
command -v lxc
command -v rkt
command -v podman
command -v runc
command -v ctr
command -v containerd
command -v crio
command -v nerdctl

# Container management
command -v kubectl
command -v crictl
command -v docker-compose
command -v docker-machine
command -v minikube
command -v kind

# Container networking
command -v docker-proxy
command -v cni
command -v flanneld
command -v calicoctl

# Container security
command -v apparmor_parser
command -v seccomp
command -v gvisor
command -v kata-runtime

# Container debugging
command -v nsenter
command -v unshare
command -v chroot
command -v capsh
command -v setcap
command -v getcap

echo ""