from .src.peasLoaded import PEASLoaded
from .src.linpeasBuilder import LinpeasBuilder
from .src.linpeasBaseBuilder import LinpeasBaseBuilder
from .src.yamlGlobals import FINAL_FAT_LINPEAS_PATH, FINAL_LINPEAS_PATH, TEMPORARY_LINPEAS_BASE_PATH

import os
import stat

#python3 -m builder.linpeas_builder
def main():
    # Load configuration
    ploaded = PEASLoaded()

    # Build temporary  linpeas_base.sh file
    lbasebuilder = LinpeasBaseBuilder()
    lbasebuilder.build()

    # Build final linpeas.sh
    lbuilder = LinpeasBuilder(ploaded)
    lbuilder.build()
    lbuilder.write_linpeas(FINAL_FAT_LINPEAS_PATH)
    lbuilder.write_linpeas(FINAL_LINPEAS_PATH, rm_startswith="FAT_LINPEAS")
    os.remove(TEMPORARY_LINPEAS_BASE_PATH) #Remove the built linpeas_base.sh file
    
    st = os.stat(FINAL_FAT_LINPEAS_PATH)
    os.chmod(FINAL_FAT_LINPEAS_PATH, st.st_mode | stat.S_IEXEC)
    
    st = os.stat(FINAL_LINPEAS_PATH)
    os.chmod(FINAL_LINPEAS_PATH, st.st_mode | stat.S_IEXEC)


if __name__ == "__main__":
    main()