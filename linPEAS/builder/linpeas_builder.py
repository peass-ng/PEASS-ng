from .src.peasLoaded import PEASLoaded
from .src.linpeasBuilder import LinpeasBuilder
from .src.yamlGlobals import FINAL_LINPEAS_PATH

import os
import stat

#python3 -m builder.linpeas_builder
def main():
    ploaded = PEASLoaded()
    lbuilder = LinpeasBuilder(ploaded)
    lbuilder.build()
    lbuilder.write_linpeas(FINAL_LINPEAS_PATH)
    st = os.stat(FINAL_LINPEAS_PATH)
    os.chmod(FINAL_LINPEAS_PATH, st.st_mode | stat.S_IEXEC)


if __name__ == "__main__":
    main()