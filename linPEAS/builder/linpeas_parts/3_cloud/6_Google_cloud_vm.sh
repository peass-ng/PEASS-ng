# Title: Cloud - Google Cloud VM
# ID: CL_Google_cloud_vm
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Google Cloud VM Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_gcp, print_2title, print_3title, print_info
# Global Variables: $is_gcp_vm, $GCP_GOOD_SCOPES, $GCP_BAD_SCOPES
# Initial Functions: check_gcp
# Generated Global Variables: $gcp_req, $p_id, $p_num, $pssh_k, $p_attrs, $osl_u, $osl_g, $osl_sk, $osl_au, $inst_d, $inst_hostn, $inst_id, $inst_img, $inst_mt, $inst_n, $inst_tag, $inst_zone, $inst_k8s_loc, $inst_k8s_name, $inst_k8s_osl_e, $inst_k8s_klab, $inst_k8s_kubec, $inst_k8s_kubenv, $iface
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_gcp_vm" = "Yes" ]; then
    gcp_req=""
    if [ "$(command -v curl || echo -n '')" ]; then
        gcp_req='curl -s -f -L -H "Metadata-Flavor: Google"'
    elif [ "$(command -v wget || echo -n '')" ]; then
        gcp_req='wget -q -O - --header "Metadata-Flavor: Google"'
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi


    if [ "$gcp_req" ]; then
        print_2title "Google Cloud Platform Enumeration"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/gcp-security/index.html"

        ## GC Project Info
        p_id=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/project-id')
        [ "$p_id" ] && echo "Project-ID: $p_id"
        p_num=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/numeric-project-id')
        [ "$p_num" ] && echo "Project Number: $p_num"
        pssh_k=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/attributes/ssh-keys')
        [ "$pssh_k" ] && echo "Project SSH-Keys: $pssh_k"
        p_attrs=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/attributes/?recursive=true')
        [ "$p_attrs" ] && echo "All Project Attributes: $p_attrs"

        # OSLogin Info
        osl_u=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/users)
        [ "$osl_u" ] && echo "OSLogin users: $osl_u"
        osl_g=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/groups)
        [ "$osl_g" ] && echo "OSLogin Groups: $osl_g"
        osl_sk=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/security-keys)
        [ "$osl_sk" ] && echo "OSLogin Security Keys: $osl_sk"
        osl_au=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/authorize)
        [ "$osl_au" ] && echo "OSLogin Authorize: $osl_au"

        # Instance Info
        inst_d=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/description)
        [ "$inst_d" ] && echo "Instance Description: "
        inst_hostn=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/hostname)
        [ "$inst_hostn" ] && echo "Hostname: $inst_hostn"
        inst_id=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/id)
        [ "$inst_id" ] && echo "Instance ID: $inst_id"
        inst_img=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/image)
        [ "$inst_img" ] && echo "Instance Image: $inst_img"
        inst_mt=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/machine-type)
        [ "$inst_mt" ] && echo "Machine Type: $inst_mt"
        inst_n=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/name)
        [ "$inst_n" ] && echo "Instance Name: $inst_n"
        inst_tag=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/scheduling/tags)
        [ "$inst_tag" ] && echo "Instance tags: $inst_tag"
        inst_zone=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/zone)
        [ "$inst_zone" ] && echo "Zone: $inst_zone"

        inst_k8s_loc=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/cluster-location")
        [ "$inst_k8s_loc" ] && echo "K8s Cluster Location: $inst_k8s_loc"
        inst_k8s_name=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/cluster-name")
        [ "$inst_k8s_name" ] && echo "K8s Cluster name: $inst_k8s_name"
        inst_k8s_osl_e=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/enable-oslogin")
        [ "$inst_k8s_osl_e" ] && echo "K8s OSLoging enabled: $inst_k8s_osl_e"
        inst_k8s_klab=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kube-labels")
        [ "$inst_k8s_klab" ] && echo "K8s Kube-labels: $inst_k8s_klab"
        inst_k8s_kubec=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kubeconfig")
        [ "$inst_k8s_kubec" ] && echo "K8s Kubeconfig: $inst_k8s_kubec"
        inst_k8s_kubenv=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kube-env")
        [ "$inst_k8s_kubenv" ] && echo "K8s Kube-env: $inst_k8s_kubenv"

        echo ""
        print_3title "Interfaces"
        for iface in $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/"); do 
            echo "  IP: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/ip")
            echo "  Subnetmask: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/subnetmask")
            echo "  Gateway: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/gateway")
            echo "  DNS: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/dns-servers")
            echo "  Network: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/network")
            echo "  ==============  "
        done
        
        echo ""
        print_3title "User Data"
        echo $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/startup-script")
        echo ""

        echo ""
        print_3title "Service Accounts"
        for sa in $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/"); do 
            echo "  Name: $sa"
            echo "  Email: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/email")
            echo "  Aliases: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/aliases")
            echo "  Identity: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/identity")
            echo "  Scopes: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/scopes") | sed -${E} "s,${GCP_GOOD_SCOPES},${SED_GREEN},g" | sed -${E} "s,${GCP_BAD_SCOPES},${SED_RED},g"
            echo "  Token: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/token")
            echo "  ==============  "
        done
    fi
    echo ""
fi