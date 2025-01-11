# Title: Cloud - Check if in cloud
# ID: CL_Check_if_in_cloud
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the current system is inside a cloud environment
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_codebuild, check_aws_ec2, check_aws_ecs, check_aws_lambda, check_az_app, check_az_vm, check_az_automation_acc, check_do, check_gcp, check_ibm_vm, check_tencent_cvm, print_list
# Global Variables: $is_aws_codebuild, $is_aws_ecs, $is_aws_ec2, , $is_aws_lambda, $is_az_app, $is_az_automation_acc, $is_az_vm, $is_do, $is_gcp_vm, $is_gcp_function, $is_ibm_vm, $is_aws_ec2_beanstalk, $is_aliyun_ecs, $is_tencent_cvm
# Initial Functions: check_gcp, check_aws_ecs, check_aws_ec2, check_aws_lambda, check_aws_codebuild, check_do, check_ibm_vm, check_az_vm, check_az_app, check_az_automation_acc, check_aliyun_ecs, check_tencent_cvm
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


printf "${YELLOW}Learn and practice cloud hacking techniques in ${BLUE}training.hacktricks.wiki\n"$NC
echo ""

print_list "GCP Virtual Machine? ................. $is_gcp_vm\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "GCP Cloud Funtion? ................... $is_gcp_function\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "AWS ECS? ............................. $is_aws_ecs\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "AWS EC2? ............................. $is_aws_ec2\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "AWS EC2 Beanstalk? ................... $is_aws_ec2_beanstalk\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "AWS Lambda? .......................... $is_aws_lambda\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "AWS Codebuild? ....................... $is_aws_codebuild\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "DO Droplet? .......................... $is_do\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "IBM Cloud VM? ........................ $is_ibm_vm\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "Azure VM or Az metadata? ............. $is_az_vm\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "Azure APP? ........................... $is_az_app\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "Azure Automation Account? ............ $is_az_automation_acc\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "Aliyun ECS? .......................... $is_aliyun_ecs\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
print_list "Tencent CVM? ......................... $is_tencent_cvm\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"

echo ""