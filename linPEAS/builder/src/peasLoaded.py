from .fileRecord import FileRecord
from .peassRecord import PEASRecord
from .yamlGlobals import YAML_LOADED, DEFAULTS

class PEASLoaded:
    def __init__(self):
        to_search = YAML_LOADED["search"]
        self.peasrecords = []
        for record in to_search:
            record_value = record["value"]
            # Shared YAML uses value.disable; accept the older config.disable too.
            if any("linpeas" in str(disabled).lower() for disabled in (
                    record_value.get("disable"), record_value["config"].get("disable"))):
                continue

            filerecords = []
            for filerecord in record_value["files"]:
                filerecords.append(
                    FileRecord(
                        regex=filerecord["name"],
                        **filerecord["value"]
                    )
                )
            
            name = record["name"]
            self.peasrecords.append(
                PEASRecord(
                    name=name,
                    auto_check=record_value["config"]["auto_check"],
                    exec=record_value["config"].get("exec", DEFAULTS["exec"]),
                    filerecords=filerecords
                )
            )
