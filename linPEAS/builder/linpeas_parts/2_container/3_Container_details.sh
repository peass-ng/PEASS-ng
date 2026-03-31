# Title: Container - Container details
# ID: CT_Container_details
# Author: Carlos Polop
# Last Update: 21-03-2026
# Description: Gather general container runtime context, local runtime CLIs, and visible container-management surfaces relevant to privilege escalation.
# License: GNU GPL
# Version: 1.0
# Mitre: T1613,T1611
# Functions Used: containerCheck, echo_no, enumerateDockerSockets, print_2title, print_list, warn_exec
# Global Variables: $containerType
# Initial Functions: containerCheck
# Generated Global Variables: $containerCounts, $crictlcontainers, $ctrcontainers, $dockercontainers, $lxccontainers, $nerdctlcontainers, $podmancontainers, $rktcontainers
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Container details" "T1613,T1611"
print_list "Is this a container? ...........$NC $containerType"

has_runtime_cli() {
    command -v "$1" >/dev/null 2>&1
}

print_runtime_info() {
    if has_runtime_cli "$1"; then
        print_list "$2$NC "
        shift 2
        warn_exec "$@"
    fi
}

get_runtime_container_count() {
    if has_runtime_cli "$1"; then
        shift
        "$@" 2>/dev/null | wc -l | tr -d ' '
    else
        echo "0"
    fi
}

print_running_containers() {
    if [ "$1" -ne "0" ]; then
        echo "$2" | sed -${E} "s,.*,${SED_RED},"
        shift
        shift
        "$@" 2>/dev/null
        echo ""
    fi
}

if [ -e "/proc/vz" ] && ! [ -e "/proc/bc" ]; then
    print_list "Container Runtime ..............$NC OpenVZ"
fi

if [ -f "/run/systemd/container" ]; then
     print_list "Systemd Container ..............$NC $(cat /run/systemd/container)"
fi

if [ -f "/run/.containerenv" ]; then
    print_list "Podman/OCI marker ..............$NC /run/.containerenv"
fi

if [ -f "/.dockerenv" ]; then
    print_list "Docker marker ..................$NC /.dockerenv"
fi

# Get container runtime info
print_runtime_info docker "Docker version ..............." docker version
print_runtime_info docker "Docker info ................." docker info
print_runtime_info podman "Podman version .............." podman version
print_runtime_info podman "Podman info ................" podman info
print_runtime_info lxc "LXC version ................" lxc version
print_runtime_info lxc "LXC info ..................." lxc info
print_runtime_info crio "CRI-O version ..............." crio --version
print_runtime_info runc "runc version ..............." runc --version
print_runtime_info crun "crun version ..............." crun --version
print_runtime_info nerdctl "nerdctl version ............" nerdctl version
print_runtime_info crictl "crictl version ............." crictl version
print_runtime_info ctr "ctr version ................" ctr version

print_list "Interesting runtime sockets ... "$NC
enumerateDockerSockets

print_list "Any running containers? ........ "$NC
# Get counts of running containers for each platform
dockercontainers=0
podmancontainers=0
lxccontainers=0
rktcontainers=0
nerdctlcontainers=0
crictlcontainers=0
ctrcontainers=0

dockercontainers=$(get_runtime_container_count docker docker ps --format "{{.Names}}")
podmancontainers=$(get_runtime_container_count podman podman ps --format "{{.Names}}")
lxccontainers=$(get_runtime_container_count lxc lxc list -c n --format csv)
rktcontainers=$(get_runtime_container_count rkt sh -c 'rkt list 2>/dev/null | tail -n +2')
nerdctlcontainers=$(get_runtime_container_count nerdctl nerdctl ps --format "{{.Names}}")
crictlcontainers=$(get_runtime_container_count crictl crictl ps -q)
ctrcontainers=$(get_runtime_container_count ctr ctr -n k8s.io containers list -q)

if [ "$dockercontainers" -eq "0" ] && [ "$lxccontainers" -eq "0" ] && [ "$rktcontainers" -eq "0" ] && [ "$podmancontainers" -eq "0" ] && [ "$nerdctlcontainers" -eq "0" ] && [ "$crictlcontainers" -eq "0" ] && [ "$ctrcontainers" -eq "0" ]; then
    echo_no
else
    containerCounts=""
    if [ "$dockercontainers" -ne "0" ]; then containerCounts="${containerCounts}docker($dockercontainers) "; fi
    if [ "$podmancontainers" -ne "0" ]; then containerCounts="${containerCounts}podman($podmancontainers) "; fi
    if [ "$lxccontainers" -ne "0" ]; then containerCounts="${containerCounts}lxc($lxccontainers) "; fi
    if [ "$rktcontainers" -ne "0" ]; then containerCounts="${containerCounts}rkt($rktcontainers) "; fi
    if [ "$nerdctlcontainers" -ne "0" ]; then containerCounts="${containerCounts}nerdctl($nerdctlcontainers) "; fi
    if [ "$crictlcontainers" -ne "0" ]; then containerCounts="${containerCounts}crictl($crictlcontainers) "; fi
    if [ "$ctrcontainers" -ne "0" ]; then containerCounts="${containerCounts}ctr($ctrcontainers) "; fi
    echo "Yes $containerCounts" | sed -${E} "s,.*,${SED_RED},"
    
    # List any running containers with more details
    print_running_containers "$dockercontainers" "Running Docker Containers" docker ps -a
    print_running_containers "$podmancontainers" "Running Podman Containers" podman ps -a
    print_running_containers "$lxccontainers" "Running LXC Containers" lxc list
    print_running_containers "$rktcontainers" "Running RKT Containers" rkt list
    print_running_containers "$nerdctlcontainers" "Running nerdctl Containers" nerdctl ps -a
    print_running_containers "$crictlcontainers" "Running CRI Containers" crictl ps -a
    print_running_containers "$ctrcontainers" "Running ctr Containers (k8s.io namespace)" ctr -n k8s.io containers list
fi


echo ""
