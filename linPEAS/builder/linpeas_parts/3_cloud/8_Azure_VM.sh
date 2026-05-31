# Title: Cloud - Azure VM
# ID: CL_Azure_VM
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Azure VM Enumeration
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.005,T1580
# Functions Used: check_az_vm, exec_with_jq, print_2title, print_3title
# Global Variables: $is_az_vm
# Initial Functions: check_az_vm
# Generated Global Variables: $API_VERSION, $HEADER, $az_req, $URL, $_az_vm_token_url, $_az_vm_instance_json, $_az_vm_resource_id, $_az_vm_mgmt_token_json, $_az_vm_mgmt_token, $_az_vm_arm_json, $_az_vm_uai_id, $_az_vm_uai_client_id, $_az_vm_uai_principal_id, $_az_vm_wire_data, $_az_vm_wire_client_id, $_az_vm_wire_res_id, $_az_vm_wire_header, $_az_vm_wire_url
# Fat linpeas: 0
# Small linpeas: 1

az_vm_json_value() {
  if [ "$(command -v jq || echo -n '')" ]; then
    jq -r "$1 // empty" 2>/dev/null
  elif [ "$(command -v python3 || echo -n '')" ]; then
    python3 -c 'import json,sys
obj=json.load(sys.stdin)
cur=obj
for p in sys.argv[1].strip(".").split("."):
    if not p:
        continue
    cur = cur.get(p, {}) if isinstance(cur, dict) else {}
print(cur if isinstance(cur, str) else "")' "$1" 2>/dev/null
  else
    sed -n "s/.*\"$2\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p" | head -n 1
  fi
}

az_vm_request() {
  if [ "$(command -v curl || echo -n '')" ]; then
    curl -s -f -L -H "$HEADER" "$1" 2>/dev/null
  elif [ "$(command -v wget || echo -n '')" ]; then
    wget -q -O - --header "$HEADER" "$1" 2>/dev/null
  fi
}

az_vm_request_arm() {
  if [ "$(command -v curl || echo -n '')" ]; then
    curl -s -f -L -H "Authorization: Bearer $1" "$2" 2>/dev/null
  elif [ "$(command -v wget || echo -n '')" ]; then
    wget -q -O - --header "Authorization: Bearer $1" "$2" 2>/dev/null
  fi
}

az_vm_print_token() {
  _az_vm_token_url="$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=$2"
  if [ "$3" ]; then
    _az_vm_token_url="${_az_vm_token_url}\&$3"
  fi
  print_3title "$1" "T1552.005,T1580"
  exec_with_jq eval $az_req "$_az_vm_token_url"
  echo ""
}

az_vm_print_standard_tokens() {
  az_vm_print_token "Management token$1" "https://management.azure.com/" "$2"
  az_vm_print_token "Graph token$1" "https://graph.microsoft.com/" "$2"
  az_vm_print_token "Vault token$1" "https://vault.azure.net/" "$2"
  az_vm_print_token "Storage token$1" "https://storage.azure.com/" "$2"
}

az_vm_request_wireserver() {
  _az_vm_wire_header="$1"
  _az_vm_wire_url="$2"
  if [ "$(command -v curl || echo -n '')" ]; then
    if [ "$_az_vm_wire_header" ]; then
      curl -s -f -L --connect-timeout 2 --max-time 5 -H "$_az_vm_wire_header" "$_az_vm_wire_url" 2>/dev/null
    else
      curl -s -f -L --connect-timeout 2 --max-time 5 "$_az_vm_wire_url" 2>/dev/null
    fi
  elif [ "$(command -v wget || echo -n '')" ]; then
    if [ "$_az_vm_wire_header" ]; then
      wget -q -O - --timeout 5 --tries 1 --header "$_az_vm_wire_header" "$_az_vm_wire_url" 2>/dev/null
    else
      wget -q -O - --timeout 5 --tries 1 "$_az_vm_wire_url" 2>/dev/null
    fi
  fi
}

az_vm_try_wire_identity_tokens() {
  print_3title "WireServer/HostGAPlugin managed identity fallback" "T1552.005,T1580"
  print_info "ARM identity discovery failed. Trying WireServer GoalState, ExtensionsConfig and HostGAPlugin /vmSettings for identity-looking selectors. These endpoints are environment-dependent and may expose no managed identity data."

  _az_vm_wire_data="$(
    az_vm_request_wireserver "x-ms-version: 2012-11-30" "http://168.63.129.16/machine?comp=goalstate"
    az_vm_request_wireserver "x-ms-version: 2012-11-30" "http://168.63.129.16/machine/?comp=goalstate"
    az_vm_request_wireserver "" "http://168.63.129.16:32526/vmSettings"
  )"

  if [ "$_az_vm_wire_data" ]; then
    printf "%s\n" "$_az_vm_wire_data" | grep -Eio '([A-Za-z0-9_./:-]*Identity[A-Za-z0-9_./:-]*|Microsoft\.ManagedIdentity/userAssignedIdentities/[^"<>[:space:]]+|clientId["[:space:]:=]+[0-9a-fA-F-]{36}|IdentityClientId[^0-9a-fA-F]*[0-9a-fA-F-]{36})' | sort -u | head -n 80

    if [ "$(command -v jq || echo -n '')" ]; then
      printf "%s" "$_az_vm_wire_data" | jq -r '.. | objects | to_entries[]? | select((.key|test("(?i)(clientId|IdentityClientId)$")) and (.value|type=="string")) | .value' 2>/dev/null | sort -u | while read -r _az_vm_wire_client_id; do
        if printf "%s" "$_az_vm_wire_client_id" | grep -Eq '^[0-9a-fA-F-]{36}$'; then
          print_info "Trying IMDS tokens for WireServer-discovered client_id=$_az_vm_wire_client_id"
          az_vm_print_standard_tokens " for WireServer client_id $_az_vm_wire_client_id" "client_id=$_az_vm_wire_client_id"
        fi
      done
    fi

    printf "%s\n" "$_az_vm_wire_data" | grep -Eio '/subscriptions/[^"<>[:space:]]+/resourceGroups/[^"<>[:space:]]+/providers/Microsoft\.ManagedIdentity/userAssignedIdentities/[^"<>[:space:]]+' | sort -u | while read -r _az_vm_wire_res_id; do
      print_info "Trying IMDS tokens for WireServer-discovered msi_res_id=$_az_vm_wire_res_id"
      az_vm_print_standard_tokens " for WireServer msi_res_id" "msi_res_id=$_az_vm_wire_res_id"
    done
  else
    echo "WireServer/HostGAPlugin did not return data from this context."
  fi
  echo ""
}

if [ "$is_az_vm" = "Yes" ]; then
  print_2title "Azure VM Enumeration" "T1552.005,T1580"
  HEADER="Metadata:true"
  URL="http://169.254.169.254/metadata"
  API_VERSION="2021-12-13" #https://learn.microsoft.com/en-us/azure/virtual-machines/instance-metadata-service?tabs=linux#supported-api-versions
  
  az_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      az_req="curl -s -f -L -H '$HEADER'"
  elif [ "$(command -v wget || echo -n '')" ]; then
      az_req="wget -q -O - --header '$HEADER'"
  else 
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  if [ "$az_req" ]; then
    print_3title "Instance details" "T1552.005,T1580"
    exec_with_jq eval $az_req "$URL/instance?api-version=$API_VERSION"
    echo ""

    print_3title "Load Balancer details" "T1552.005,T1580"
    exec_with_jq eval $az_req "$URL/loadbalancer?api-version=$API_VERSION"
    echo ""

    print_3title "User Data" "T1552.005,T1580"
    exec_with_jq eval $az_req "$URL/instance/compute/userData?api-version=$API_VERSION\&format=text" | base64 -d 2>/dev/null
    echo ""

    print_3title "Custom Data and other configs (root needed)" "T1552.005,T1580"
    (cat /var/lib/waagent/ovf-env.xml || cat /var/lib/waagent/CustomData/ovf-env.xml) 2>/dev/null | sed "s,CustomData.*,${SED_RED},"
    echo ""

    print_3title "Management token" "T1552.005,T1580"
    print_info "This is the default VM managed identity token. If several user-assigned identities exist and no system identity is present, Azure may require client_id/object_id/msi_res_id."
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://management.azure.com/"
    echo ""

    print_3title "Graph token" "T1552.005,T1580"
    print_info "This is the default VM managed identity token."
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://graph.microsoft.com/"
    echo ""
    
    print_3title "Vault token" "T1552.005,T1580"
    print_info "This is the default VM managed identity token."
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://vault.azure.net/"
    echo ""

    print_3title "Storage token" "T1552.005,T1580"
    print_info "This is the default VM managed identity token."
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://storage.azure.com/"
    echo ""

    print_3title "Attached user-assigned managed identities and tokens" "T1552.005,T1580"
    print_info "LinPEAS tries to discover all attached UAIs by using the default Management token to read the VM ARM identity block. If that token cannot read Microsoft.Compute/virtualMachines/read, IMDS can still issue tokens for known client_id/object_id/msi_res_id values, but the full attached identity list cannot be discovered from IMDS alone."

    _az_vm_instance_json="$(az_vm_request "$URL/instance?api-version=$API_VERSION")"
    _az_vm_resource_id="$(printf "%s" "$_az_vm_instance_json" | az_vm_json_value ".compute.resourceId" "resourceId")"
    _az_vm_mgmt_token_json="$(az_vm_request "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://management.azure.com/")"
    _az_vm_mgmt_token="$(printf "%s" "$_az_vm_mgmt_token_json" | az_vm_json_value ".access_token" "access_token")"

    if [ "$_az_vm_resource_id" ] && [ "$_az_vm_mgmt_token" ]; then
      _az_vm_arm_json="$(az_vm_request_arm "$_az_vm_mgmt_token" "https://management.azure.com${_az_vm_resource_id}?api-version=2024-07-01")"
      if printf "%s" "$_az_vm_arm_json" | grep -q '"userAssignedIdentities"'; then
        if [ "$(command -v jq || echo -n '')" ]; then
          printf "%s" "$_az_vm_arm_json" | jq '.identity'
          printf "%s" "$_az_vm_arm_json" | jq -r '.identity.userAssignedIdentities // {} | to_entries[] | [.key, .value.clientId, .value.principalId] | @tsv' 2>/dev/null | while IFS="$(printf '\t')" read -r _az_vm_uai_id _az_vm_uai_client_id _az_vm_uai_principal_id; do
            if [ "$_az_vm_uai_client_id" ]; then
              print_info "Requesting tokens for UAI client_id=$_az_vm_uai_client_id principal_id=$_az_vm_uai_principal_id resource_id=$_az_vm_uai_id"
              az_vm_print_standard_tokens " for UAI $_az_vm_uai_client_id" "client_id=$_az_vm_uai_client_id"
            fi
          done
        else
          echo "$_az_vm_arm_json" | sed "s,access_token,${SED_RED},g"
          print_info "Install jq to parse all attached user-assigned identities and request tokens for each one automatically."
          az_vm_try_wire_identity_tokens
        fi
      else
        echo "Could not read attached user-assigned identities from ARM with the default managed identity token."
        az_vm_try_wire_identity_tokens
      fi
    else
      echo "Could not obtain the VM resource ID or default Management token needed for ARM identity discovery."
      az_vm_try_wire_identity_tokens
    fi
    echo ""
  fi
  echo ""
fi
