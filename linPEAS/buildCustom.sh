#!/bin/bash

python3 -m builder.linpeas_builder \
  --include "system_information,users_information,interesting_perms_files,interesting_files,procs_crons_timers_srvcs_sockets,software_information,network_information,IF_CTF_flags,IF_CTF_flags_advanced" \
  --exclude "container,cloud,api_keys_regex,different_procs_1min,dbus_analysis" \
  --output /tmp/linpeas_ctf.sh
