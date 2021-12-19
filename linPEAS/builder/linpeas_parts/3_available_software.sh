###########################################
#---------) Available Software (----------#
###########################################

#-- 1AS) Useful software
print_2title "Useful software"
for tool in $USEFUL_SOFTWARE; do command -v "$tool"; done
echo ""

#-- 2AS) Search for compilers
print_2title "Installed Compilers"
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