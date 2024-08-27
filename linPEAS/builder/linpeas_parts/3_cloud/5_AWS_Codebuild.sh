# Title: Cloud - AWS Codebuild
# ID: CL_AWS_Codebuild
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: AWS Codebuild Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_codebuild, exec_with_jq, print_2title, print_3title
# Global Variables: $is_aws_codebuild
# Initial Functions: check_aws_codebuild
# Generated Global Variables: $aws_req, $METADATA_URL, $CREDS_PATH, $URL_CREDS
# Fat linpeas: 0
# Small linpeas: 0


if [ "$is_aws_codebuild" = "Yes" ]; then
  print_2title "AWS Codebuild Enumeration"

  aws_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      aws_req="curl -s -f"
  elif [ "$(command -v wget || echo -n '')" ]; then
      aws_req="wget -q -O -"
  else 
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
      echo "The addresses are in /codebuild/output/tmp/env.sh"
  fi

  if [ "$aws_req" ]; then
    print_3title "Credentials"
    CREDS_PATH=$(cat /codebuild/output/tmp/env.sh | grep "AWS_CONTAINER_CREDENTIALS_RELATIVE_URI" | cut -d "'" -f 2)
    URL_CREDS="http://169.254.170.2$CREDS_PATH" # Already has a / at the begginig
    exec_with_jq eval $aws_req "$URL_CREDS"; echo ""

    print_3title "Container Info"
    METADATA_URL=$(cat /codebuild/output/tmp/env.sh | grep "ECS_CONTAINER_METADATA_URI" | cut -d "'" -f 2)
    exec_with_jq eval $aws_req "$METADATA_URL"; echo ""
  fi
  echo ""
fi