# Title: Cloud - AWS Lambda
# ID: CL_AWS_Lambda
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: AWS Lambda Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_lambda, print_2title
# Global Variables: $is_aws_lambda
# Initial Functions: check_aws_lambda
# Generated Global Variables: 
# Fat linpeas: 0
# Small linpeas: 0


if [ "$is_aws_lambda" = "Yes" ]; then
  print_2title "AWS Lambda Enumeration"
  printf "Function name: "; env | grep AWS_LAMBDA_FUNCTION_NAME
  printf "Region: "; env | grep AWS_REGION
  printf "Secret Access Key: "; env | grep AWS_SECRET_ACCESS_KEY
  printf "Access Key ID: "; env | grep AWS_ACCESS_KEY_ID
  printf "Session token: "; env | grep AWS_SESSION_TOKEN
  printf "Security token: "; env | grep AWS_SECURITY_TOKEN
  printf "Runtime API: "; env | grep AWS_LAMBDA_RUNTIME_API
  printf "Event data: "; (curl -s "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/next" 2>/dev/null || wget -q -O - "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/next")
  echo ""
fi