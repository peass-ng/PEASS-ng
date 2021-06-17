from .src.peasLoaded import PEASLoaded
from .src.linpeasBuilder import LinpeasBuilder


#python3 -m builder.linpeas_builder
def main():
    ploaded = PEASLoaded()
    lbuilder = LinpeasBuilder(ploaded)
    lbuilder.build()


if __name__ == "__main__":
    main()