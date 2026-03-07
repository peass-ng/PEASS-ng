#!/usr/bin/env python3
"""
Adds # Mitre: metadata and annotates print_2title/print_3title calls
in every LinPEAS check module with the appropriate MITRE ATT&CK technique IDs.
"""

import os, re, sys

BASE = os.path.join(os.path.dirname(__file__), "..", "linPEAS", "builder", "linpeas_parts")

# Mapping: relative path from linpeas_parts → comma-separated MITRE technique IDs
MITRE_MAP = {
    # ─── Section 1: System Information ────────────────────────────────────────
    "1_system_information/1_Operative_system.sh":        "T1082",
    "1_system_information/2_Sudo_version.sh":            "T1548.003,T1068",
    "1_system_information/3_USBCreator.sh":              "T1548",
    "1_system_information/4_Path.sh":                    "T1574.007",
    "1_system_information/5_Date.sh":                    "T1082",
    "1_system_information/6_CPU_info.sh":                "T1082",
    "1_system_information/7_Mounts.sh":                  "T1082,T1120",
    "1_system_information/8_Disks.sh":                   "T1082",
    "1_system_information/9_Disks_extra.sh":             "T1082",
    "1_system_information/10_Environment.sh":            "T1082,T1552.007",
    "1_system_information/11_Dmesg.sh":                  "T1082",
    "1_system_information/12_Macos_os_checks.sh":        "T1082",
    "1_system_information/16_Protections.sh":            "T1518.001",
    "1_system_information/17_Kernel_Modules.sh":         "T1547.006",
    "1_system_information/19_Kernel_Exploit_Registry.sh":"T1068",
    # ─── Section 2: Container ─────────────────────────────────────────────────
    "2_container/1_Container_tools.sh":                  "T1613",
    "2_container/2_List_mounted_tokens.sh":              "T1528,T1552.007",
    "2_container/3_Container_details.sh":                "T1613,T1611",
    "2_container/4_Docker_container_details.sh":         "T1613",
    "2_container/5_Container_breakout.sh":               "T1611",
    "2_container/7_RW_bind_mounts_nosuid.sh":            "T1611",
    # ─── Section 3: Cloud ─────────────────────────────────────────────────────
    "3_cloud/1_Check_if_in_cloud.sh":                    "T1580",
    "3_cloud/2_AWS_EC2.sh":                              "T1552.005,T1580",
    "3_cloud/3_AWS_ECS.sh":                              "T1552.005,T1580",
    "3_cloud/4_AWS_Lambda.sh":                           "T1552.005,T1580",
    "3_cloud/5_AWS_Codebuild.sh":                        "T1552.005,T1580",
    "3_cloud/6_Google_cloud_function.sh":                "T1552.005,T1580",
    "3_cloud/7_Google_cloud_vm.sh":                      "T1552.005,T1580",
    "3_cloud/8_Azure_VM.sh":                             "T1552.005,T1580",
    "3_cloud/9_Azure_app_service.sh":                    "T1552.005,T1580",
    "3_cloud/10_Azure_automation_account.sh":            "T1552.005,T1580",
    "3_cloud/11_DO_Droplet.sh":                          "T1552.005,T1580",
    "3_cloud/13_Ali_Cloud.sh":                           "T1552.005,T1580",
    "3_cloud/14_IBM_Cloud.sh":                           "T1552.005,T1580",
    "3_cloud/15_Tencent_Cloud.sh":                       "T1552.005,T1580",
    # ─── Section 4: Processes / Crons / Timers / Services / Sockets ───────────
    "4_procs_crons_timers_srvcs_sockets/1_List_processes.sh":              "T1057",
    "4_procs_crons_timers_srvcs_sockets/2_Process_cred_in_memory.sh":      "T1003,T1055",
    "4_procs_crons_timers_srvcs_sockets/3_Process_binaries_perms.sh":      "T1574,T1554",
    "4_procs_crons_timers_srvcs_sockets/4_Processes_PPID_different_user.sh":"T1134.004",
    "4_procs_crons_timers_srvcs_sockets/5_Files_open_process_other_user.sh":"T1083",
    "4_procs_crons_timers_srvcs_sockets/6_Different_procs_1min.sh":        "T1057",
    "4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh":                   "T1053.003",
    "4_procs_crons_timers_srvcs_sockets/8_Macos_launch_agents_daemons.sh": "T1543.001",
    "4_procs_crons_timers_srvcs_sockets/9_System_timers.sh":               "T1053.003",
    "4_procs_crons_timers_srvcs_sockets/10_Services.sh":                   "T1543.002,T1007",
    "4_procs_crons_timers_srvcs_sockets/11_Systemd.sh":                    "T1543.002",
    "4_procs_crons_timers_srvcs_sockets/12_Socket_files.sh":               "T1559",
    "4_procs_crons_timers_srvcs_sockets/13_Unix_sockets_listening.sh":     "T1571,T1049",
    "4_procs_crons_timers_srvcs_sockets/14_DBus_analysis.sh":              "T1559.001",
    "4_procs_crons_timers_srvcs_sockets/15_Rcommands_trust.sh":            "T1021.004",
    "4_procs_crons_timers_srvcs_sockets/16_Crontab_UI_misconfig.sh":       "T1053.003",
    "4_procs_crons_timers_srvcs_sockets/17_Deleted_open_files.sh":         "T1083",
    # ─── Section 5: Network Information ───────────────────────────────────────
    "5_network_information/1_Network_interfaces.sh":     "T1016",
    "5_network_information/2_Hostname_hosts_dns.sh":     "T1016,T1018",
    "5_network_information/3_Network_neighbours.sh":     "T1018,T1040",
    "5_network_information/4_Open_ports.sh":             "T1049",
    "5_network_information/5_Macos_network_capabilities.sh":"T1016",
    "5_network_information/6_Macos_network_services.sh": "T1016",
    "5_network_information/7_Tcpdump.sh":                "T1040",
    "5_network_information/8_Iptables.sh":               "T1016",
    "5_network_information/9_Inetdconf.sh":              "T1049",
    "5_network_information/10_Macos_hardware_ports.sh":  "T1016",
    "5_network_information/11_Internet_access.sh":       "T1016,T1590",
    # ─── Section 6: Users Information ─────────────────────────────────────────
    "6_users_information/1_My_user.sh":                  "T1033",
    "6_users_information/1_Macos_my_user_hooks.sh":      "T1033,T1543.001",
    "6_users_information/2_Macos_user_hooks.sh":         "T1543.001",
    "6_users_information/3_Macos_keychains.sh":          "T1555.001",
    "6_users_information/4_Macos_systemkey.sh":          "T1555.001",
    "6_users_information/5_Pgp_keys.sh":                 "T1552.004",
    "6_users_information/6_Clipboard_highlighted_text.sh":"T1115",
    "6_users_information/7_Sudo_l.sh":                   "T1548.003",
    "6_users_information/8_Sudo_tokens.sh":              "T1548.003",
    "6_users_information/9_Doas.sh":                     "T1548",
    "6_users_information/10_Pkexec.sh":                  "T1548",
    "6_users_information/11_Superusers.sh":              "T1087.001,T1548",
    "6_users_information/12_Users_with_console.sh":      "T1087.001",
    "6_users_information/13_Users_groups.sh":            "T1087.001,T1069.001",
    "6_users_information/14_Login_now.sh":               "T1033",
    "6_users_information/15_Last_logons.sh":             "T1033",
    "6_users_information/17_Password_policy.sh":         "T1201",
    "6_users_information/18_Brute_su.sh":                "T1110.001",
    # ─── Section 7: Software Information ──────────────────────────────────────
    "7_software_information/1_Useful_software.sh":       "T1082",
    "7_software_information/2_Compilers.sh":             "T1587.001",
    "7_software_information/3_Macos_writable_installed_apps.sh":"T1574",
    "7_software_information/Apache_nginx.sh":            "T1552.001",
    "7_software_information/Awsvault.sh":                "T1552.005",
    "7_software_information/Browser_profiles.sh":        "T1539,T1217",
    "7_software_information/Cached_AD_hashes.sh":        "T1003.003",
    "7_software_information/Containerd.sh":              "T1613",
    "7_software_information/Docker.sh":                  "T1613",
    "7_software_information/Dovecot.sh":                 "T1552.001",
    "7_software_information/Extra_software.sh":          "T1082",
    "7_software_information/FreeIPA.sh":                 "T1552.001",
    "7_software_information/Gitlab.sh":                  "T1552.001",
    "7_software_information/Kcpassword.sh":              "T1555.001",
    "7_software_information/Kerberos.sh":                "T1558.003",
    "7_software_information/Log4shell.sh":               "T1190",
    "7_software_information/Logstash.sh":                "T1552.001",
    "7_software_information/Mysql.sh":                   "T1552.001",
    "7_software_information/PGP_GPG.sh":                 "T1552.004",
    "7_software_information/PHP_Sessions.sh":            "T1552.001",
    "7_software_information/Pamd.sh":                    "T1556.003",
    "7_software_information/Postgresql.sh":              "T1552.001",
    "7_software_information/Postgresql_Event_Triggers.sh":"T1505.001",
    "7_software_information/Runc.sh":                    "T1613,T1611",
    "7_software_information/SKey.sh":                    "T1556",
    "7_software_information/Screen_sessions.sh":         "T1563",
    "7_software_information/Splunk.sh":                  "T1552.001",
    "7_software_information/Ssh.sh":                     "T1552.004,T1021.004",
    "7_software_information/Tmux.sh":                    "T1563",
    "7_software_information/Vault_ssh.sh":               "T1552.004",
    "7_software_information/YubiKey.sh":                 "T1556",
    # ─── Section 8: Interesting Permissions / Files ────────────────────────────
    "8_interesting_perms_files/1_SUID.sh":               "T1548.001",
    "8_interesting_perms_files/2_SGID.sh":               "T1548.001",
    "8_interesting_perms_files/3_Files_ACLs.sh":         "T1222",
    "8_interesting_perms_files/4_Capabilities.sh":       "T1548.001",
    "8_interesting_perms_files/5_Users_with_capabilities.sh":"T1548.001",
    "8_interesting_perms_files/6_Misconfigured_ldso.sh": "T1574.006",
    "8_interesting_perms_files/7_Files_etc_profile_d.sh":"T1546.004",
    "8_interesting_perms_files/8_Files_etc_init_d.sh":   "T1543.002",
    "8_interesting_perms_files/9_App_armour_profiles.sh":"T1518.001",
    "8_interesting_perms_files/10_Read_creds_files.sh":  "T1552.001",
    "8_interesting_perms_files/11_Root_files_home_dir.sh":"T1083",
    "8_interesting_perms_files/12_Others_files_in_my_dirs.sh":"T1083",
    "8_interesting_perms_files/13_Root_readable_files_notworld_readeble.sh":"T1083",
    "8_interesting_perms_files/14_Writable_files_owner_all.sh":"T1574",
    "8_interesting_perms_files/15_Writable_files_group.sh":"T1574",
    "8_interesting_perms_files/16_IGEL_OS_SUID.sh":      "T1548.001",
    "8_interesting_perms_files/16_Writable_root_execs.sh":"T1574",
    # ─── Section 9: Interesting Files ─────────────────────────────────────────
    "9_interesting_files/1_Sh_files_in_PATH.sh":         "T1574.007",
    "9_interesting_files/2_Date_in_firmware.sh":         "T1082",
    "9_interesting_files/3_Executable_files_by_user.sh": "T1083",
    "9_interesting_files/4_Macos_unsigned_apps.sh":      "T1204.002",
    "9_interesting_files/5_Unexpected_in_opt.sh":        "T1083",
    "9_interesting_files/6_Unexpected_in_root.sh":       "T1083",
    "9_interesting_files/7_Modified_last_5mins.sh":      "T1083",
    "9_interesting_files/8_Writable_log_files.sh":       "T1070.002",
    "9_interesting_files/9_My_home.sh":                  "T1083",
    "9_interesting_files/10_Others_homes.sh":            "T1552.001",
    "9_interesting_files/11_Mail_apps.sh":               "T1114.001",
    "9_interesting_files/12_Mails.sh":                   "T1114.001",
    "9_interesting_files/13_Backup_folders.sh":          "T1552.001",
    "9_interesting_files/14_Backup_files.sh":            "T1552.001",
    "9_interesting_files/15_Db_files.sh":                "T1005",
    "9_interesting_files/16_Macos_downloaded_files.sh":  "T1005",
    "9_interesting_files/17_Web_files.sh":               "T1005",
    "9_interesting_files/18_Hidden_files.sh":            "T1564.001",
    "9_interesting_files/19_Readable_files_tmp_backups.sh":"T1552.001",
    "9_interesting_files/20_Passwords_history_cmd.sh":   "T1552.001",
    "9_interesting_files/21_Passwords_history_files.sh": "T1552.001",
    "9_interesting_files/22_Passwords_php_files.sh":     "T1552.001",
    "9_interesting_files/23_Passwords_files_home.sh":    "T1552.001",
    "9_interesting_files/24_Passwords_TTY.sh":           "T1552.001",
    "9_interesting_files/25_IPs_logs.sh":                "T1083",
    "9_interesting_files/26_Mails_addr_inside_logs.sh":  "T1114.001",
    "9_interesting_files/27_Passwords_in_logs.sh":       "T1552.001",
    "9_interesting_files/28_Files_with_passwords.sh":    "T1552.001",
    # ─── Section 10: API Keys Regex ───────────────────────────────────────────
    "10_api_keys_regex/regexes.sh":                      "T1552.001,T1528",
}

VERSION_RE = re.compile(r'^(# Version:.*)', re.MULTILINE)
PRINT2_RE  = re.compile(r'''(print_2title\s+"[^"]*")(\s*)$''', re.MULTILINE)
PRINT3_RE  = re.compile(r'''(print_3title\s+"[^"]*")(\s*)$''', re.MULTILINE)

changed = 0
skipped = 0

for rel_path, mitre_ids in MITRE_MAP.items():
    abs_path = os.path.normpath(os.path.join(BASE, rel_path))
    if not os.path.isfile(abs_path):
        print(f"  SKIP (not found): {rel_path}")
        skipped += 1
        continue

    with open(abs_path, "r") as f:
        text = f.read()

    # 1. Insert # Mitre: after # Version: (only if not already present)
    if "# Mitre:" not in text:
        text = VERSION_RE.sub(rf'\1\n# Mitre: {mitre_ids}', text, count=1)

    # 2. Annotate print_2title calls that don't already have a 2nd argument
    def add_mitre_to_title2(m):
        call = m.group(1)
        # Skip if already has a 2nd quoted arg after the first
        full_line = m.group(0)
        if re.search(r'print_2title\s+"[^"]*"\s+"', full_line):
            return full_line
        return call + f' "{mitre_ids}"'

    text = PRINT2_RE.sub(add_mitre_to_title2, text)

    # 3. Annotate print_3title calls similarly
    def add_mitre_to_title3(m):
        call = m.group(1)
        full_line = m.group(0)
        if re.search(r'print_3title\s+"[^"]*"\s+"', full_line):
            return full_line
        return call + f' "{mitre_ids}"'

    text = PRINT3_RE.sub(add_mitre_to_title3, text)

    with open(abs_path, "w") as f:
        f.write(text)
    changed += 1

print(f"\nDone: {changed} files updated, {skipped} skipped.")
