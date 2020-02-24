#!/bin/bash


echo "Executing: echo -e \"$1\n$2\n$3\n$4\n$5\n$6\n$7\" | eaphammer --cert-wizard interactive"
echo -e "$1\n$2\n$3\n$4\n$5\n$6\n$7" | eaphammer --cert-wizard interactive