namespace winPEAS.Checks
{
    internal static class Globals
    {
        public static string StrTrue = "True";
        public static string StrFalse = "False";
        public static readonly string PrintCredStringsLimited = "[pP][aA][sS][sS][wW][a-zA-Z0-9_-]*|[pP][wW][dD][a-zA-Z0-9_-]*|[nN][aA][mM][eE]|[lL][oO][gG][iI][nN]|[cC][oO][nN][tT][rR][aA][sS][eE][a-zA-Z0-9_-]*|[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL][a-zA-Z0-9_-]*|[aA][pP][iI]|[tT][oO][kK][eE][nN]|[sS][eE][sS][sS][a-zA-Z0-9_-]*";
        public static readonly string PrintCredStrings = PrintCredStringsLimited + "|[uU][sS][eE][rR][a-zA-Z0-9_-]*";
    }
}
