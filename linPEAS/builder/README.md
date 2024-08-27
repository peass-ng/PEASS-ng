# Build you own linpeas!

You can **build you own linpeas which will contain only the checks you want**. This is useful to reduce the time it takes to run linpeas and to make linpeas more stealth and modular.

## Quick start building linpeas.sh

It's possible to indicate the params `--all`, `--all-no-fat` and `--small` to build the classic `linpeas_fat.sh`, `linpeas.sh` and `linpeas_small.sh`:

- **linpeas_fat.sh**: Contains all checks, even third party applications in base64 embedded.
- **linpeas.sh**: Contains all checks, but only the third party application `linux exploit suggester` is embedded. This is the default `linpeas.sh`.
- **linpeas_small.sh**: Contains only the most *important* checks making its size smaller.

However, in order to indicate only some specific checks, you can use the `--include` and `--exclude` params. These arguments supports a comma separated list of modules to add or remove from the final linpeas. Note that the matchs are done by checking **if the module path string contains any of the words** indicated in those params. Therefore, if you want to inde all the tests from the `linpeas_parts/3_cloud` it's enough to indicate `--include "cloud"`. Or if you want to include only the check `linpeas_parts/3_cloud/1_Check_if_in_Cloud` you can indicate `--include "Check_if_in_Cloud"`.

```bash
# Run this commands from 1 level above the builder folder. From here: cd ..
# Build linpeas_fat (linpeas with all checks, even third party applications in base64 embedded)
python3 -m builder.linpeas_builder --all --output /tmp/linpeas_fat.sh

# Build regular linpeas
python3 -m builder.linpeas_builder --all-no-fat --output /tmp/linpeas.sh

# Build small linpeas
python3 -m builder.linpeas_builder --small --output /tmp/linpeas_small.sh

# Build linpeas only with container and cloud checks
python3 -m builder.linpeas_builder --include "container,cloud" --output /tmp/linpeas_custom.sh

# Build linpeas only with regexes
python3 -m builder.linpeas_builder --include "api_keys_regex" --output /tmp/linpeas_custom.sh

# Build linpeas only with some specific modules
## You can customize it as much as you want
python3 -m builder.linpeas_builder --include "CPU_info,Sudo_version,Clipboard_highlighted_text" --output /tmp/linpeas_custom.sh

# Build linpeas excluding some specific modules
python3 -m builder.linpeas_builder --exclude "CPU_info,Sudo_version,Clipboard_highlighted_text" --output /tmp/linpeas_custom.sh
```

## How to add new modules

Adding new modules is very easy. You just need to create a new file in the `linpeas_parts/<corresponding section>` folder with the following structure with the bash code to run. Note that every new module should have some specific metadata at the beggining of the file. This metadata is used by the builder to generate the final linpeas.

Metadata example:

```bash
# Title: Cloud - Check if in cloud
# ID: CL_Check_if_in_cloud
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the current system is inside a cloud environment
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_codebuild, check_aws_ec2, check_aws_ecs, check_aws_lambda, check_az_app, check_az_vm, check_do, check_gcp, check_ibm_vm, check_tencent_cvm, print_list
# Global Variables: $is_aws_codebuild, $is_aws_ecs, $is_aws_ec2, , $is_aws_lambda, $is_az_app, $is_az_vm, $is_do, $is_gcp_vm, $is_gcp_function, $is_ibm_vm, $is_aws_ec2_beanstalk, $is_aliyun_ecs, $is_tencent_cvm
# Initial Functions: check_gcp, check_aws_ecs, check_aws_ec2, check_aws_lambda, check_aws_codebuild, check_do, check_ibm_vm, check_az_vm, check_az_app, check_aliyun_ecs, check_tencent_cvm
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

<code>
```

### Metadata parts explained

- **Title**: Title of the module
- **ID**: Unique identifier of the module. It has to be the same as the filename without the extension and with the section identifier as prefix (in this case `CL`)
- **Author**: Author of the module
- **Last Update**: Last update of the module
- **Description**: Description of the module
- **License**: License of the module
- **Version**: Version of the module
- **Functions Used**: Functions used by the module inside the bash code. If your module is using a function not defined here, linpeas won't be built.
- **Global Variables**: Global variables used by the module inside the bash code. If your module is using a global variable not defined here, linpeas won't be built.
- **Initial Functions**: Functions that are called at the beggining of the module. If your module is using a function not defined here, linpeas won't be built.
- **Generated Global Variables**: Global variables generated (given a relevant value) by the module. If your module is generating a global variable not defined here, linpeas won't be built.
- **Fat linpeas**: Set only as 1 if the module is loading a third party app, if not 0.
- **Small linpeas**: Set as 1 if it's a quick check, if not 0.