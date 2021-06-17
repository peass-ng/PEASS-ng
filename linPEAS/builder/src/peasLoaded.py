from .fileRecord import FileRecord
from .peassRecord import PEASRecord
from .yamlGlobals import YAML_LOADED, DEFAULTS

class PEASLoaded:
    def __init__(self):
        to_search = YAML_LOADED["search"]
        self.peasrecords = []
        for name,peasrecord_json in to_search.items():
            filerecords = []
            for regex,fr in peasrecord_json["files"].items():
                filerecords.append(
                    FileRecord(
                        regex=regex,
                        **fr
                    )
                )
                
            self.peasrecords.append(
                PEASRecord(
                    name=name,
                    auto_check=peasrecord_json["config"]["auto_check"],
                    exec=peasrecord_json["config"].get("exec", DEFAULTS["exec"]),
                    filerecords=filerecords
                )
            )