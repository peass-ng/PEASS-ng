#!/bin/bash


echo "Executing: airodump-ng $1 -c \"$2\" --bssid \"$3\" -w \"$4\""
airodump-ng "$1" -c "$2" --bssid "$3" -w "$4" --output-format pcap
