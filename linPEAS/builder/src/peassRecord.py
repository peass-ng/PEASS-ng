class PEASRecord:
    def __init__(self, name, auto_check: bool, exec: list, filerecords: list):
        self.name = name
        self.bash_name = name.upper().replace(" ","_").replace("-","_")
        self.auto_check = auto_check
        self.exec = exec
        self.filerecords = filerecords