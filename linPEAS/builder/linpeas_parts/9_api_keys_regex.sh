
if [ "$REGEXES" ] && [ "$TIMEOUT" ]; then
    peass{REGEXES}
else
    echo "Regexes to search for API keys aren't activated, use param '-r' "
fi