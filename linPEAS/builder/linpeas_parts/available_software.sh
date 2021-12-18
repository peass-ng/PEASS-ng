###########################################
#---------) Available Software (----------#
###########################################

#-- 1AS) Useful software
print_2title "Useful software"
command -v "$CONTAINER_CMDS" nmap aws nc ncat netcat nc.traditional wget curl ping gcc g++ make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo fetch ctr authbind 2>/dev/null
echo ""

#-- 2AS) Search for compilers
print_2title "Installed Compiler"
(dpkg --list 2>/dev/null | grep "compiler" | grep -v "decompiler\|lib" 2>/dev/null || yum list installed 'gcc*' 2>/dev/null | grep gcc 2>/dev/null; command -v gcc g++ 2>/dev/null || locate -r "/gcc[0-9\.-]\+$" 2>/dev/null | grep -v "/doc/");
echo ""

if [ "$(command -v pkg 2>/dev/null)" ]; then
    print_2title "Vulnerable Packages"
    pkg audit -F | sed -${E} "s,vulnerable,${SED_RED},g"
    echo ""
fi

if [ "$(command -v brew 2>/dev/null)" ]; then
    print_2title "Brew Installed Packages"
    brew list
    echo ""
fi

if [ "$MACPEAS" ]; then
print_2title "Writable Installed Applications"
system_profiler SPApplicationsDataType | grep "Location:" | cut -d ":" -f 2 | cut -c2- | while read f; do
    if [ -w "$f" ]; then
    echo "$f is writable" | sed -${E} "s,.*,${SED_RED},g"
    fi
done

system_profiler SPFrameworksDataType | grep "Location:" | cut -d ":" -f 2 | cut -c2- | while read f; do
    if [ -w "$f" ]; then
    echo "$f is writable" | sed -${E} "s,.*,${SED_RED},g"
    fi
done
fi