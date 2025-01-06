# Title: Cloud - Google Cloud Function
# ID: CL_Google_cloud_function
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Google Cloud Function Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_gcp, print_2title, print_3title, print_info
# Global Variables: $is_gcp_function, $GCP_GOOD_SCOPES, $GCP_BAD_SCOPES
# Initial Functions: check_gcp
# Generated Global Variables: $gcp_req, $p_id, $p_num, $inst_id, $inst_zone, $mtls_info
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_gcp_function" = "Yes" ]; then
    gcp_req=""
    if [ "$(command -v curl)" ]; then
        gcp_req='curl -s -f -L -H "Metadata-Flavor: Google"'
    elif [ "$(command -v wget)" ]; then
        gcp_req='wget -q -O - --header "Metadata-Flavor: Google"'
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi

    # GCP Enumeration
    if [ "$gcp_req" ]; then
        print_2title "Google Cloud Platform Enumeration"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/gcp-security/index.html"

        ## GC Project Info
        p_id=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/project-id')
        [ "$p_id" ] && echo "Project-ID: $p_id"
        p_num=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/numeric-project-id')
        [ "$p_num" ] && echo "Project Number: $p_num"

        # Instance Info
        inst_id=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/id)
        [ "$inst_id" ] && echo "Instance ID: $inst_id"
        mtls_info=$(eval $gcp_req http://metadata/computeMetadata/v1/instance/platform-security/auto-mtls-configuration)
        [ "$mtls_info" ] && echo "MTLS info: $mtls_info"
        inst_zone=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/zone)
        [ "$inst_zone" ] && echo "Zone: $inst_zone"

        echo ""
        print_3title "Service Accounts"
        for sa in $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/"); do 
            echo "  Name: $sa"
            echo "  Email: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/${sa}email")
            echo "  Aliases: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/${sa}aliases")
            echo "  Identity: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/${sa}identity")
            echo "  Scopes: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/${sa}scopes") | sed -${E} "s,${GCP_GOOD_SCOPES},${SED_GREEN},g" | sed -${E} "s,${GCP_BAD_SCOPES},${SED_RED},g"
            echo "  Token: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/${sa}token")
            echo "  ==============  "
        done
    fi
fi