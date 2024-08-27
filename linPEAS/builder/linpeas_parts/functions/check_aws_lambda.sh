# Title: Cloud - check_aws_lambda
# ID: check_aws_lambda
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in AWS Lambda
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_aws_lambda
# Fat linpeas: 0
# Small linpeas: 1


check_aws_lambda(){
  is_aws_lambda="No"

  if (env | grep -q AWS_LAMBDA_); then
    is_aws_lambda="Yes"
  fi
}