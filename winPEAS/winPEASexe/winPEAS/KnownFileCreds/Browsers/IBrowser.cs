using System.Collections.Generic;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal interface IBrowser
    {
        string Name { get; }
        void PrintInfo();
        IEnumerable<CredentialModel> GetSavedCredentials();
    }
}
