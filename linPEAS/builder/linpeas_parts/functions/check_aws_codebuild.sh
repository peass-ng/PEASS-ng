# Title: Cloud - check_aws_codebuild
# ID: check_aws_codebuild
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in AWS CodeBuild
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_aws_codebuild
# Fat linpeas: 0
# Small linpeas: 1


check_aws_codebuild(){
  is_aws_codebuild="No"

  if [ -f "/codebuild/output/tmp/env.sh" ] && grep -q "AWS_CONTAINER_CREDENTIALS_RELATIVE_URI" "/codebuild/output/tmp/env.sh" ; then
    is_aws_codebuild="Yes"
  fi
}