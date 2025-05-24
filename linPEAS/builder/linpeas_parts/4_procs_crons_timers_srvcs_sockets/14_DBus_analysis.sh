# Title: Processes & Cron & Services & Timers - D-Bus Analysis
# ID: PR_DBus_analysis
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Comprehensive D-Bus analysis for privilege escalation vectors:
#   - D-Bus Service Objects enumeration
#   - D-Bus Service Object permissions and ownership
#   - D-Bus Configuration files analysis
#   - D-Bus Policy analysis
#   - D-Bus Method and Interface analysis
#   - D-Bus Privilege Escalation Vectors
# License: GNU GPL
# Version: 1.3
# Functions Used: print_2title, print_3title, print_info, echo_not_found
# Global Variables: $IAMROOT, $mygroups, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $dbuslistG, $knw_usrs, $rootcommon, $SED_RED, $SED_GREEN, $SED_BLUE, $SED_LIGHT_CYAN, $SED_LIGHT_MAGENTA, $NC
# Initial Functions:
# Generated Global Variables: $dbuslist, $srvc_object, $genpol, $userpol, $grppol, $dangerous_service, $pattern, $dir, $weak_policies, $dangerous_services, $dangerous, $dbussrvc_object, $patterns, $methods, $file, $dbusservice, $session_services, $prop, $dangerous_session_services, $interface, $dangerous_methods, $dbus_file, $dbus_service, $method, $dangerous_patterns, $properties, $interfaces, $dangerous_props, $service, $info, $allow_rules
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
    print_2title "D-Bus Analysis"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#d-bus"


    # Function to check for dangerous methods
    check_dangerous_methods() {
        service="$1"
        interface="$2"
        dangerous=0
        dangerous_methods=""
        
        # Common dangerous method patterns - using space-separated string instead of array
        patterns="StartUnit StopUnit RestartUnit EnableUnit DisableUnit SetProperty SetUser SetPassword CreateUser DeleteUser ModifyUser Execute Run Spawn Shell Command Exec Authenticate Login Logout Reboot Shutdown PowerOff Suspend Hibernate Update Install Uninstall Configure Modify Change Delete Remove Add Create Write Read Access Grant Revoke Allow Deny"
        
        # Get methods for the interface
        methods=$(busctl introspect "$service" "$interface" 2>/dev/null | grep "method" | awk '{print $2}')
        
        # Check each method against dangerous patterns
        for method in $methods; do
            for pattern in $patterns; do
                if echo "$method" | grep -qi "$pattern"; then
                    dangerous=1
                    dangerous_methods="${dangerous_methods}${method} "
                fi
            done
        done
        
        if [ "$dangerous" -eq 1 ]; then
            echo "  └─(${RED}Potentially dangerous methods found${NC})"
            echo "     └─ $dangerous_methods" | sed 's/^/        /'
        fi
        
        return $dangerous
    }

    # Function to check for dangerous properties
    check_dangerous_properties() {
        service="$1"
        interface="$2"
        dangerous=0
        dangerous_props=""
        
        # Common dangerous property patterns - using space-separated string instead of array
        patterns="Executable Command Path User Group Permission Access Auth Password Secret Key Token Credential Config Setting Policy Rule Allow Deny Write Read Execute"
        
        # Get properties for the interface
        properties=$(busctl introspect "$service" "$interface" 2>/dev/null | grep "property" | awk '{print $2}')
        
        # Check each property against dangerous patterns
        for prop in $properties; do
            for pattern in $patterns; do
                if echo "$prop" | grep -qi "$pattern"; then
                    dangerous=1
                    dangerous_props="${dangerous_props}${prop} "
                fi
            done
        done
        
        if [ "$dangerous" -eq 1 ]; then
            echo "  └─(${RED}Potentially dangerous properties found${NC})"
            echo "     └─ $dangerous_props" | sed 's/^/        /'
        fi
        
        return $dangerous
    }

    # Function to analyze service object
    analyze_service_object() {
        dbusservice="$1"
        info=""
        dangerous=0
        
        # Get service status
        info=$(busctl status "$dbusservice" 2>/dev/null)
        
        # Check for root ownership
        if echo "$info" | grep -qE "^(UID|EUID|OwnerUID)=0"; then
            echo "  └─(${RED}Running as root${NC})"
            dangerous=1
        fi
        
        # Get service interfaces
        interfaces=$(busctl tree "$dbusservice" 2>/dev/null)
        if [ -n "$interfaces" ]; then
            echo "  └─ Interfaces:"
            echo "$interfaces" | sed 's/^/     /'
            
            # Check each interface for dangerous methods and properties
            echo "$interfaces" | while read -r interface; do
                if [ -n "$interface" ]; then
                    if check_dangerous_methods "$dbusservice" "$interface"; then
                        dangerous=1
                    fi
                    if check_dangerous_properties "$dbusservice" "$interface"; then
                        dangerous=1
                    fi
                fi
            done
        fi
        
        # Check for known dangerous services - using space-separated string instead of array
        dangerous_services="org.freedesktop.systemd1 org.freedesktop.PolicyKit1 org.freedesktop.Accounts org.freedesktop.login1 org.freedesktop.hostname1 org.freedesktop.timedate1 org.freedesktop.locale1 org.freedesktop.machine1 org.freedesktop.portable1 org.freedesktop.resolve1 org.freedesktop.timesync1 org.freedesktop.import1 org.freedesktop.export1 org.gnome.SettingsDaemon org.gnome.Shell org.gnome.SessionManager org.gnome.DisplayManager org.gnome.ScreenSaver"
        
        for dangerous_service in $dangerous_services; do
            if echo "$dbusservice" | grep -qi "$dangerous_service"; then
                echo "  └─(${RED}Known dangerous service: $dangerous_service${NC})"
                dangerous=1
            fi
        done
        
        # If service is dangerous, provide exploitation hints
        if [ "$dangerous" -eq 1 ]; then
            echo "  └─(${RED}Potential privilege escalation vector${NC})"
            echo "     └─ Try: busctl call $dbusservice / [Interface] [Method] [Arguments]"
            echo "     └─ Or: dbus-send --session --dest=$dbusservice / [Interface] [Method] [Arguments]"
        fi
    }

    # Function to analyze policy file
    analyze_policy_file() {
        file="$1"
        weak_policies=0
        
        # Check file permissions
        if ! [ "$IAMROOT" ] && [ -w "$file" ]; then
            echo "  └─(${RED}Writable policy file${NC})"
            weak_policies=$((weak_policies + 1))
        fi
        
        # Check general policy
        genpol=$(grep "<policy>" "$file" 2>/dev/null)
        if [ -n "$genpol" ]; then
            echo "  └─(${RED}Weak general policy found${NC})"
            echo "     └─ $genpol" | sed 's/^/        /'
            weak_policies=$((weak_policies + 1))
        fi
        
        # Check user policies
        userpol=$(grep "<policy user=" "$file" 2>/dev/null | grep -v "root")
        if [ -n "$userpol" ]; then
            echo "  └─(${RED}Weak user policy found${NC})"
            echo "     └─ $userpol" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g"
            weak_policies=$((weak_policies + 1))
        fi
        
        # Check group policies
        grppol=$(grep "<policy group=" "$file" 2>/dev/null | grep -v "root")
        if [ -n "$grppol" ]; then
            echo "  └─(${RED}Weak group policy found${NC})"
            echo "     └─ $grppol" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"
            weak_policies=$((weak_policies + 1))
        fi
        
        # Check for allow rules in default context
        allow_rules=$(grep -A 5 "context=\"default\"" "$file" 2>/dev/null | grep "allow")
        if [ -n "$allow_rules" ]; then
            echo "  └─(${RED}Allow rules in default context${NC})"
            echo "     └─ $allow_rules" | sed 's/^/        /'
            weak_policies=$((weak_policies + 1))
        fi
        
        # Check for specific dangerous policy patterns - using space-separated string instead of array
        dangerous_patterns="allow_any allow_all allow_root allow_user allow_group allow_anonymous allow_any_user allow_any_group allow_any_uid allow_any_gid allow_any_pid allow_any_connection allow_any_method allow_any_property allow_any_signal allow_any_interface allow_any_path allow_any_destination allow_any_sender allow_any_receiver"
        
        for pattern in $dangerous_patterns; do
            if grep -qi "$pattern" "$file" 2>/dev/null; then
                echo "  └─(${RED}Dangerous policy pattern found: $pattern${NC})"
                weak_policies=$((weak_policies + 1))
            fi
        done
        
        return $weak_policies
    }

    # Analyze D-Bus Service Objects
    dbuslist=$(busctl list 2>/dev/null)
    if [ -n "$dbuslist" ]; then
        echo "$dbuslist" | while read -r dbus_service; do
            # Print service name with highlighting
            echo "$dbus_service" | sed -${E} "s,$dbuslistG,${SED_GREEN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
            
            # Analyze service if it's not in the known list
            if ! echo "$dbus_service" | grep -qE "$dbuslistG"; then
                dbussrvc_object=$(echo "$dbus_service" | cut -d " " -f1)
                analyze_service_object "$dbussrvc_object"
            fi
        done
    else
        echo_not_found "busctl"
    fi

    # Analyze D-Bus Configuration Files
    if [ "$PSTORAGE_DBUS" ]; then
        echo ""
        print_2title "D-Bus Configuration Files"
        echo "$PSTORAGE_DBUS" | while read -r dir; do
            for dbus_file in "$dir"/*; do
                if [ -f "$dbus_file" ]; then
                    echo "Analyzing $dbus_file:"
                    if analyze_policy_file "$dbus_file"; then
                        echo "  └─(${RED}Multiple weak policies found${NC})"
                    fi
                fi
            done
        done
    fi

    # Check for D-Bus session bus
    if command -v dbus-send >/dev/null 2>&1; then
        echo ""
        print_3title "D-Bus Session Bus Analysis"
        if dbus-send --session --dest=org.freedesktop.DBus --type=method_call --print-reply /org/freedesktop/DBus org.freedesktop.DBus.ListNames 2>/dev/null | grep -q "Error"; then
            echo "(${RED}No access to session bus${NC})"
        else
            echo "(${GREEN}Access to session bus available${NC})"
            # List available services on session bus
            session_services=$(dbus-send --session --dest=org.freedesktop.DBus --type=method_call --print-reply /org/freedesktop/DBus org.freedesktop.DBus.ListNames 2>/dev/null | grep "string" | sed 's/^/     /')
            echo "$session_services"
            
            # Check for known dangerous session services - using space-separated string instead of array
            dangerous_session_services="org.gnome.SettingsDaemon org.gnome.Shell org.gnome.SessionManager org.gnome.DisplayManager org.gnome.ScreenSaver org.freedesktop.Notifications org.freedesktop.ScreenSaver org.freedesktop.PowerManagement org.freedesktop.UPower org.freedesktop.NetworkManager org.freedesktop.Avahi org.freedesktop.UDisks2 org.freedesktop.ModemManager1 org.freedesktop.PackageKit org.freedesktop.PolicyKit1 org.freedesktop.systemd1 org.freedesktop.Accounts org.freedesktop.login1"
            
            for dangerous_service in $dangerous_session_services; do
                if echo "$session_services" | grep -qi "$dangerous_service"; then
                    echo "  └─(${RED}Known dangerous session service: $dangerous_service${NC})"
                    echo "     └─ Try: dbus-send --session --dest=$dangerous_service / [Interface] [Method] [Arguments]"
                fi
            done
        fi
    fi
fi
echo ""