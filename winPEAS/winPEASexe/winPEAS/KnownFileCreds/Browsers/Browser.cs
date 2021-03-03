using System.Collections.Generic;

namespace winPEAS.KnownFileCreds.Browsers
{
    static class Browser
    {
        public static readonly List<string> CredStringsRegex = new List<string>
        {
            "PASSW[a-zA-Z0-9_-]*=",
            "PWD[a-zA-Z0-9_-]*=",
            "USER[a-zA-Z0-9_-]*=",
            "NAME=",
            "&LOGIN",
            "=LOGIN",
            "CONTRASEÑA[a-zA-Z0-9_-]*=",
            "CREDENTIAL[a-zA-Z0-9_-]*=",
            "API_KEY",
            "TOKEN"
        };
    }
}
