# Title: Cloud - check_aws_ec2
# ID: check_aws_ec2
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in AWS EC2
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: 
# Initial Functions:
# Generated Global Variables: $is_aws_ec2, $is_aws_ec2_beanstalk, $EC2_TOKEN
# Fat linpeas: 0
# Small linpeas: 1


check_aws_ec2(){
  is_aws_ec2="No"
  is_aws_ec2_beanstalk="No"

  if [ -d "/var/log/amazon/" ]; then
    is_aws_ec2="Yes"
    EC2_TOKEN=$(curl --connect-timeout 2 -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null || wget --timeout 2 --tries 1 -q -O - --method PUT "http://169.254.169.254/latest/api/token" --header "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null)

  else
    EC2_TOKEN=$(curl --connect-timeout 2 -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null || wget --timeout 2 --tries 1 -q -O - --method PUT "http://169.254.169.254/latest/api/token" --header "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null)
    if [ "$(echo $EC2_TOKEN | cut -c1-2)" = "AQ" ]; then
      is_aws_ec2="Yes"
    fi
  fi
  
  if [ "$is_aws_ec2" = "Yes" ] && grep -iq "Beanstalk" "/etc/motd"; then
    is_aws_ec2_beanstalk="Yes"
  fi
}
