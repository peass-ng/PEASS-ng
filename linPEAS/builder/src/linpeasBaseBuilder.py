import os
from typing import List

from .linpeasModule import LinpeasModule, LinpeasModuleList
from .yamlGlobals import (
    LINPEAS_PARTS,
    TEMPORARY_LINPEAS_BASE_PATH,
    PEAS_CHECKS_MARKUP
)

class LinpeasBaseBuilder:
    def __init__(self, all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules):
        # Everything relevant
        self.all_modules = self.get_modules(all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules)
        # Only base
        self.base = self.get_base()
        # Only checks
        self.checks = self.get_checks()
        print(f"[+] {len(self.checks)} checks located")
        # Only functions sorted
        self.functions = self.get_functions()
        # Only variables sorted
        self.variables = self.get_variables()

        self.linpeas_base = ""
        


    def build(self):
        print("[+] Building temporary linpeas_base.sh with the indicated modules...")
        
        # Add base code
        for base in self.base:
            self.linpeas_base += base.sh_code.strip() + "\n\n"
        
        # Add variables
        self.linpeas_base += "\n\n\n# Variables\n\n"
        for variable in self.variables:
            if "Checks pre-everything" in variable.sh_code:
                a=1
            self.linpeas_base += variable.sh_code.strip() + "\n\n"
        
        self.linpeas_base += "\n\n\n# Functions\n\n"
        # Add functions
        for function in self.functions:
            self.linpeas_base += function.sh_code.strip() + "\n\n"

        self.linpeas_base += "\n\n\n# Checks\n\n"

        section_checks = {}
        check_names = []
        for check in self.checks:
            # Get the section of the check
            for part_mod in LINPEAS_PARTS["modules"]:
                if part_mod["folder_path"] in check.path:
                    if part_mod["name"] not in section_checks:
                        section_checks[part_mod["name"]] = part_mod
                        section_checks[part_mod["name"]]["checks"] = []
                    section_checks[part_mod["name"]]["checks"].append(check)
                    break
        
        initial_functions = set()
        for section_name, section_info in section_checks.items():
            # Add 1 time the big section name to check_names to then put it inside linpeas in PEAS_CHECKS_MARKUP
            if not section_info['name_check'] in check_names: check_names.append(section_info['name_check'])
            self.linpeas_base += f"\nif echo $CHECKS | grep -q {section_info['name_check']}; then\n"
            self.linpeas_base += f'print_title "{section_name}"\n'

            # Sort checks alphabetically to get them in the same order as they are in the folder
            section_info["checks"] = sorted(section_info["checks"], key=lambda x: int(os.path.basename(x.path).split('_')[0]) if os.path.basename(x.path).split('_')[0].isdigit() else 99)
            for check in section_info["checks"]:
                for func in check.initial_functions:
                    if not func in initial_functions:
                        self.linpeas_base += func + "\n"
                        initial_functions.add(func)
                
                self.linpeas_base += check.sh_code.strip() + "\n\n"

            self.linpeas_base += f"\nfi\necho ''\necho ''\n"
            self.linpeas_base += 'if [ "$WAIT" ]; then echo "Press enter to continue"; read "asd"; fi\n'

        self.linpeas_base = self.linpeas_base.replace(PEAS_CHECKS_MARKUP, ",".join(check_names))

        with open(TEMPORARY_LINPEAS_BASE_PATH, "w") as f:
            f.write(self.linpeas_base)
    
    def find_func_module(self, func_name:str):
        """Given a function name and the list of modules return the module that contains the function"""
        
        modules = []
        for module in self.all_modules:
            if func_name in module.defined_funcs:
                modules.append(module)
        
        if len(modules) == 0:
            raise Exception(f"Function {func_name} not found in any module")
        elif len(modules) > 1:
            raise Exception(f"Function {func_name} found in more than 1 module: {modules}")
        
        return modules[0]

    def find_variable_module(self, var_name:str, orig_module:LinpeasModule):
        """Given a variable name and the list of modules return the module that contains the variable"""
        
        modules = []
        for module in self.all_modules:
            if var_name in module.generated_global_variables:
                modules.append(module)
        
        if len(modules) == 0:
            raise Exception(f"Variable '{var_name}' from {orig_module.path} not found in any module")
        elif len(modules) > 1:
            raise Exception(f"Variable {var_name} found in more than 1 module: {', '.join([m.path for m in modules])}")
        
        return modules[0]
    
    def sort_funcs(self, functions:List[LinpeasModule]):
        """Given a list of functions, return the list sorted by dependencies"""
        
        sorted_funcs = functions.copy()
        retry = False

        for i,func in enumerate(functions):
            for d_func in func.functions_used:
                is_base = False
                # If the dependant variable is defined in a module that is in the base, remove it from the list
                if any (d_func in m.defined_funcs for m in self.base):
                    try:
                        sorted_funcs.index(d_func) # Check if it's there
                        sorted_funcs.remove(d_func) # Remove if it's
                        retry = True # After a failure, start again
                    except:
                        pass
                    
                    is_base = True
                
                if is_base:
                    continue
                
                # If a dependant variable is after the current one, move it to the current position
                try:
                    dp_index = functions.index(d_func)
                except:
                    raise Exception(f"Variable {d_func} not found in {func.path}")
                
                if dp_index > i:
                    sorted_funcs.remove(d_func)
                    sorted_funcs.insert(i, functions[dp_index])
                    retry = True
                    
        if retry:
            return self.sort_funcs(sorted_funcs)
        return sorted_funcs
    

    def sort_variables(self, variables:List[LinpeasModule]):
        """Given a list of variables, return the list sorted by dependencies"""
        
        sorted_vars = variables.copy()
        retry = False

        for i,var in enumerate(variables):
            for d_var in var.global_variables:
                is_base = False
                # If the dependant variable is defined in a module that is in the base, remove it from the list
                if any (d_var in m.generated_global_variables for m in self.base):
                    try:
                        sorted_vars.index(d_var) # Check if it's there
                        sorted_vars.remove(d_var) # Remove if it's
                        retry = True # After a failure, start again
                    except:
                        pass
                    
                    is_base = True
                
                if is_base:
                    continue
                
                # If a dependant variable is after the current one, move it to the current position
                try:
                    dp_index = variables.index(d_var)
                except:
                    raise Exception(f"Variable {d_var} not found in {var.path}")
                
                if dp_index > i:
                    sorted_vars.remove(d_var)
                    sorted_vars.insert(i, variables[dp_index])
                    retry = True
                    
        if retry:
            return self.sort_variables(sorted_vars)
        return sorted_vars
    
    def get_funcs_deps(self, module, all_funcs):
        """Given 1 module and the list of modules return the functions recursively it depends on"""
        
        module_funcs = list(set(module.initial_functions + module.functions_used))
        for func in module_funcs:
            func_module = self.find_func_module(func)
            #print(f"{module.id} has found {func} in {func_module.id}") #To find circular dependencies
            if not func_module.is_function:
                continue
            if func_module in all_funcs:
                all_funcs.remove(func_module)
            all_funcs.append(func_module)
            all_funcs = self.get_funcs_deps(func_module, all_funcs)
        
        return all_funcs


    def get_vars_deps(self, module, all_vars):
        """Given 1 module and the list of modules return the variables recursively it depends on"""
        
        for var in module.global_variables:
            var_module = self.find_variable_module(var, module)
            #print(f"{module.id} has found {var} in {var_module.id}") #To find circular dependencies
            if not var_module.is_variable:
                continue
            if var_module in all_vars:
                all_vars.remove(var_module)
            all_vars.append(var_module)
            all_vars = self.get_vars_deps(var_module, all_vars)
        
        return all_vars
    
    
    def get_functions(self):
        """Get all the functions used sorted, first the ones that don't depend on any other, then the ones that depend on the previous ones, etc."""

        all_funcs = LinpeasModuleList()

        for module in self.checks:
            all_funcs = self.get_funcs_deps(module, all_funcs)
        
        return self.sort_funcs(all_funcs)


    def get_variables(self):
        """Get all the variables used sorted, first the ones that don't depend on any other, then the ones that depend on the previous ones, etc."""

        all_variables = LinpeasModuleList()

        for module in self.checks + self.functions:
            all_variables = self.get_vars_deps(module, all_variables)

        return self.sort_variables(all_variables)
            
    
    def get_checks(self):
        """Given all the modules get only the checks"""

        checks = LinpeasModuleList()
        for module in self.all_modules:
            if not module.is_check:
                continue
            
            checks.append(module)
        
        return checks


    def get_base(self):
        """Given all the modules get only the base"""

        checks = LinpeasModuleList()
        for module in self.all_modules:
            if not module.is_base:
                continue
            
            checks.append(module)
        
        return checks

    
    def enumerate_directory(self, path):
        """Given a directory get the paths to all the files inside it"""
        return sorted([os.path.join(path, f) for f in os.listdir(path) if os.path.isfile(os.path.join(path, f))])
    
    def get_modules(self, all_modules, all_no_fat_modules, no_network_scanning, small, include_modules, exclude_modules) -> LinpeasModuleList:
        """Get all the base, variable, function and specified modules to create the new linpeas"""

        print("[+] Checking the syntax of the modules...")
        parsed_modules = LinpeasModuleList()
        all_module_paths = []
        # Base modules
        all_module_paths += self.enumerate_directory(LINPEAS_PARTS["base"])

        # Function modules
        all_module_paths += self.enumerate_directory(LINPEAS_PARTS["functions"])

        # Variable modules
        all_module_paths += self.enumerate_directory(LINPEAS_PARTS["variables"])
        
        for module in LINPEAS_PARTS["modules"]:
            for ex_module in exclude_modules:
                if ex_module in module["folder_path"] or ex_module in [module["name"], module["name_check"]]:
                    continue
            all_module_paths += self.enumerate_directory(module["folder_path"])
        
        for module in all_module_paths:
            m = LinpeasModule(module)

            # If base, function or variable, add it as it will only be used if needed
            if m.is_function or m.is_variable:
                parsed_modules.append(m)
                continue
            
            # If base but no interested in network scanning, skip, else, add
            if m.is_base:
                if "check_network_jobs" in m.path and no_network_scanning:
                    continue
                parsed_modules.append(m)
                continue
            
            # If explicitely excluded, skip
            if m.id in exclude_modules:
                continue
            if all_no_fat_modules and m.is_fat:
                continue
            if small and not m.is_small:
                continue
            
            # If implicitly included, add
            if all_modules or all_no_fat_modules or m.id in include_modules:
                parsed_modules.append(m)
            for in_module in include_modules:
                if in_module.lower() in os.path.basename(m.path).lower() or in_module.lower() == m.id.lower() or in_module in [m.section_info["name"], m.section_info["name_check"]]:
                    parsed_modules.append(m)
                    break
            
        return parsed_modules



