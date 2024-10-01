import os
import re

from .yamlGlobals import (
    LINPEAS_PARTS
)

class LinpeasModule:
    def __init__(self, path):
        self.path = path
        with open(path, 'r') as file:
            self.module_text = file.read()
        
        self.sh_code = ""
        self.is_check = False
        self.is_function = False
        self.is_variable = False
        self.is_base = False

        if "/functions/" in path:
            self.is_function = True
        
        elif "/variables/" in path:
            self.is_variable = True
        
        elif "/linpeas_base/" in path:
            self.is_base = True
         
        self.section_info = {}
        if not (self.is_base or self.is_function or self.is_variable):
            for module in LINPEAS_PARTS["modules"]:
                if module["folder_path"] in path:
                    self.section_info = module
                    self.is_check = True
                    break
        
        if not (self.is_base or self.is_function or self.is_variable or self.is_check):
            raise Exception(f"Module {path} doesn't belong to any section")

        for i,line in enumerate(self.module_text.splitlines()):
            if i == 0:
                if not line.startswith("# Title:"):
                    raise Exception(f"Wrong title in module {path}. It should start with '# Title: '")
                self.title = line[8:].strip()
            
            elif i == 1:
                if not line.startswith("# ID:"):
                    raise Exception(f"Wrong ID in module {path}. It should start with '# ID: '")
                
                self.id = line[5:].strip()

                if re.sub('^[0-9]+_', '', os.path.basename(path).replace(".sh", "")) not in [self.id, self.id[3:]]:
                    raise Exception(f"Wrong ID in module {path}. It should be the same as the filename")
            
            elif i == 2:
                if not line.startswith("# Author:"):
                    raise Exception(f"Wrong author in module {path}. It should start with '# Author: '")
                self.author = line[10:].strip()
            
            elif i == 3:
                if not line.startswith("# Last Update:"):
                    raise Exception(f"Wrong last update in module {path}. It should start with '# Last Update: '")
                self.last_update = line[15:].strip()
            
            elif i == 4:
                if not line.startswith("# Description:"):
                    raise Exception(f"Wrong description in module {path}. It should start with '# Description: '")
                self.description = line[15:].strip()
            
            elif i == 5:
                if not line.startswith("# License:"):
                    raise Exception(f"Wrong license in module {path}. It should start with '# License: '")
            
            elif i == 6:
                if not line.startswith("# Version:"):
                    raise Exception(f"Wrong version in module {path}. It should start with '# Version: '")
                self.version = line[11:].strip()
            
            elif i == 7:
                if not line.startswith("# Functions Used:"):
                    raise Exception(f"Wrong functions used in module {path}. It should start with '# Functions Used: '")
                self.functions_used = line[17:].split(",")
                self.functions_used = [f.strip() for f in self.functions_used if f.strip()]

                if "/variables/" in path and self.functions_used:
                    raise Exception(f"Variables shouldn't user functions, so functions in module {path} should be empty")
            
            elif i == 8:
                if not line.startswith("# Global Variables:"):
                    raise Exception(f"Wrong global variables in module {path}. It should start with '# Global Variables: '")
                self.global_variables = line[19:].split(",")
                self.global_variables = [f.strip().replace("$", "") for f in self.global_variables if f.strip()]
            
            elif i == 9:
                if not line.startswith("# Initial Functions:"):
                    raise Exception(f"Wrong initial functions in module {path}. It should start with '# Initial Functions: '")
                self.initial_functions = line[20:].split(",")
                self.initial_functions = [f.strip() for f in self.initial_functions if f.strip()]
            
            elif i == 10:
                if not line.startswith("# Generated Global Variables:"):
                    raise Exception(f"Wrong generated global variables in module {path}. It should start with '# Generated Global Variables: '")
                self.generated_global_variables = line[29:].split(",")
                self.generated_global_variables = [f.strip().replace("$", "") for f in self.generated_global_variables if f.strip()]
            
            elif i == 11:
                if not line.startswith("# Fat linpeas:"):
                    raise Exception(f"Wrong generated global variables in module {path}. It should start with '# Generated Global Variables: '")
                self.is_fat = bool(int(line[15]))
            
            elif i == 12:
                if not line.startswith("# Small linpeas:"):
                    raise Exception(f"Wrong generated global variables in module {path}. It should start with '# Generated Global Variables: '")
                self.is_small = bool(int(line[17]))
            
            elif i == 13:
                if line != "":
                    raise Exception(f"Wrong module {path}. Line 12 should be a new line")
            
            else:
                self.sh_code += line + "\n"
        
        self.sh_code = self.sh_code.strip()
        self.defined_funcs = self.extract_function_names()

        # Check if the indicated dependencies are actually being used
        for func in self.functions_used:
            if func not in self.sh_code and func not in self.initial_functions and not "peass{" in self.sh_code:
                raise Exception(f"Used function '{func}' in module {path} doesn't exist in the final code")
        
        for var in self.global_variables:
            if var not in self.sh_code and not "peass{" in self.sh_code:
                raise Exception(f"Used variable '{var}' in module {path} doesn't exist in the final code")
        
        for var in self.generated_global_variables:
            if var not in self.sh_code:
                raise Exception(f"Generated variable '{var}' in module {path} doesn't exist in the final code")
        
        # Check for funcs and vars imported from itself
        for func in self.defined_funcs:
            if func in self.functions_used:
                raise Exception(f"Function '{func}' in module {path} is imported from itself")
        
        for var in self.global_variables:
            if var in self.generated_global_variables:
                raise Exception(f"Variable '{var}' in module {path} is imported from itself")
        
        # Check if all variables are correctly defined
        linux_global_vars = [
            "OPTARG",
            "PID",
            "PPID",
            "AWS_CONTAINER_CREDENTIALS_RELATIVE_URI",
            "AWS_LAMBDA_RUNTIME_API",
            "ECS_CONTAINER_METADATA_URI",
            "ECS_CONTAINER_METADATA_URI_v4",
            "IDENTITY_ENDPOINT",
            "IDENTITY_HEADER",
            "KUBERNETES_SERVICE_PORT_HTTPS",
            "KUBERNETES_SERVICE_HOST"
        ]
        main_base = None
        
        # Base global variables don't need to be defined
        if self.id != "BS_variables_base":
            main_base = LinpeasModule(os.path.join(os.path.dirname(__file__), "..", "linpeas_parts", "linpeas_base", "0_variables_base.sh"))
        
        for var in self.extract_variables(self.sh_code):
            if len(var) > 2 and not var in linux_global_vars and var not in self.global_variables and var not in self.generated_global_variables:
                if not var.startswith("PSTORAGE_"):
                    if not main_base or var not in main_base.generated_global_variables:
                        raise Exception(f"Variable '{var}' in module {path} is not defined")
            

    def __eq__(self, other):
        # Check if other object is an instance of LinpeasModule
        if isinstance(other, LinpeasModule):
            return self.id == other.id
        return NotImplemented  # Return NotImplemented for unsupported comparisons

    def extract_function_names(self):
        # This regular expression pattern matches function definitions in sh code
        pattern = r'\b(\w+)\s*\(\s*\)\s*{'
        return re.findall(pattern, self.sh_code)

    def extract_variables(self, sh_code):
        # This regex pattern matches variables in the form $VAR_NAME or ${VAR_NAME}
        pattern = r'\$({?([a-zA-Z_][a-zA-Z0-9_]*)}?)'
        matches = re.findall(pattern, sh_code)
        # Extract the variable name from each match
        variables = [match[1] for match in matches]
        return list(set(variables))  # Using set to remove duplicates


class LinpeasModuleList(list):
    def __contains__(self, item):
        # Check if item is already a LinpeasModule object.
        if isinstance(item, LinpeasModule):
            return super().__contains__(item)
        
        # Otherwise, treat the item as the id of a LinpeasModule.
        for module in self:
            if module.id == item:
                return True
        return False

    def index(self, item_id):
        for index, module in enumerate(self):
            if module.id == item_id:
                return index
        raise ValueError(f"{item_id} is not in the list")

    def remove(self, item):
        # If item is an id, find the corresponding object first.
        if not isinstance(item, LinpeasModule):
            index = self.index(item)
            super().pop(index)
        else:
            super().remove(item)

    def insert(self, index, item):
        # Ensure that item is a LinpeasModule object before inserting.
        if not isinstance(item, LinpeasModule):
            raise ValueError("Item should be an instance of LinpeasModule")
        super().insert(index, item)
    
    def copy(self):
        return LinpeasModuleList(super().copy())
    