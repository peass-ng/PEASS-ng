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
if [ "$(command -v docker || echo -n '')" ]; then
    print_list "Docker version ...............$NC "
    warn_exec docker version
    print_list "Docker info .................$NC "
    warn_exec docker info
fi

if [ "$(command -v podman || echo -n '')" ]; then
    print_list "Podman version ..............$NC "
    warn_exec podman version
    print_list "Podman info ................$NC "
    warn_exec podman info
fi

if [ "$(command -v lxc || echo -n '')" ]; then
    print_list "LXC version ................$NC "
    warn_exec lxc version
    print_list "LXC info ...................$NC "
    warn_exec lxc info
fi

if [ "$(command -v crio || echo -n '')" ]; then
    print_list "CRI-O version ..............$NC "
    warn_exec crio --version
fi

if [ "$(command -v runc || echo -n '')" ]; then
    print_list "runc version ...............$NC "
    warn_exec runc --version
fi

if [ "$(command -v crun || echo -n '')" ]; then
    print_list "crun version ...............$NC "
    warn_exec crun --version
fi

if [ "$(command -v nerdctl || echo -n '')" ]; then
    print_list "nerdctl version ............$NC "
    warn_exec nerdctl version
fi

if [ "$(command -v crictl || echo -n '')" ]; then
    print_list "crictl version .............$NC "
    warn_exec crictl version
fi

if [ "$(command -v ctr || echo -n '')" ]; then
    print_list "ctr version ................$NC "
    warn_exec ctr version
fi

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

if [ "$(command -v docker || echo -n '')" ]; then dockercontainers=$(docker ps --format "{{.Names}}" 2>/dev/null | wc -l | tr -d ' '); fi
if [ "$(command -v podman || echo -n '')" ]; then podmancontainers=$(podman ps --format "{{.Names}}" 2>/dev/null | wc -l | tr -d ' '); fi
if [ "$(command -v lxc || echo -n '')" ]; then lxccontainers=$(lxc list -c n --format csv 2>/dev/null | wc -l | tr -d ' '); fi
if [ "$(command -v rkt || echo -n '')" ]; then rktcontainers=$(rkt list 2>/dev/null | tail -n +2  | wc -l | tr -d ' '); fi
if [ "$(command -v nerdctl || echo -n '')" ]; then nerdctlcontainers=$(nerdctl ps --format "{{.Names}}" 2>/dev/null | wc -l | tr -d ' '); fi
if [ "$(command -v crictl || echo -n '')" ]; then crictlcontainers=$(crictl ps -q 2>/dev/null | wc -l | tr -d ' '); fi
if [ "$(command -v ctr || echo -n '')" ]; then ctrcontainers=$(ctr -n k8s.io containers list -q 2>/dev/null | wc -l | tr -d ' '); fi

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
    if [ "$dockercontainers" -ne "0" ]; then 
        echo "Running Docker Containers" | sed -${E} "s,.*,${SED_RED},"
        docker ps -a 2>/dev/null
        #echo "Docker Container Details" | sed -${E} "s,.*,${SED_RED},"
        #docker inspect $(docker ps -q) 2>/dev/null | grep -E "Privileged|CapAdd|CapDrop|SecurityOpt|HostConfig" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$podmancontainers" -ne "0" ]; then 
        echo "Running Podman Containers" | sed -${E} "s,.*,${SED_RED},"
        podman ps -a 2>/dev/null
        #echo "Podman Container Details" | sed -${E} "s,.*,${SED_RED},"
        #podman inspect $(podman ps -q) 2>/dev/null | grep -E "Privileged|CapAdd|CapDrop|SecurityOpt|HostConfig" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$lxccontainers" -ne "0" ]; then 
        echo "Running LXC Containers" | sed -${E} "s,.*,${SED_RED},"
        lxc list 2>/dev/null
        #echo "LXC Container Details" | sed -${E} "s,.*,${SED_RED},"
        #lxc config show $(lxc list -c n --format csv) 2>/dev/null | grep -E "security.privileged|security.capabilities|security.syscalls" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$rktcontainers" -ne "0" ]; then 
        echo "Running RKT Containers" | sed -${E} "s,.*,${SED_RED},"
        rkt list 2>/dev/null
        #echo "RKT Container Details" | sed -${E} "s,.*,${SED_RED},"
        #rkt status $(rkt list --format=json 2>/dev/null | jq -r '.[].id') 2>/dev/null | grep -E "privileged|capabilities|security" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$nerdctlcontainers" -ne "0" ]; then
        echo "Running nerdctl Containers" | sed -${E} "s,.*,${SED_RED},"
        nerdctl ps -a 2>/dev/null
        echo ""
    fi
    if [ "$crictlcontainers" -ne "0" ]; then
        echo "Running CRI Containers" | sed -${E} "s,.*,${SED_RED},"
        crictl ps -a 2>/dev/null
        echo ""
    fi
    if [ "$ctrcontainers" -ne "0" ]; then
        echo "Running ctr Containers (k8s.io namespace)" | sed -${E} "s,.*,${SED_RED},"
        ctr -n k8s.io containers list 2>/dev/null
        echo ""
    fi
fi


echo ""
