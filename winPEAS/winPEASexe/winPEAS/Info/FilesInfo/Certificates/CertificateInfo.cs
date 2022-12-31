using System;
using System.Collections.Generic;

namespace winPEAS.Info.FilesInfo.Certificates
{
    internal class CertificateInfo
    {
        public string StoreLocation { get; set; }
        public string Issuer { get; set; }
        public string Subject { get; set; }
        public DateTime ValidDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool HasPrivateKey { get; set; }
        public bool? KeyExportable { get; set; }
        public string Thumbprint { get; set; }
        public string Template { get; set; }
        public List<string> EnhancedKeyUsages { get; set; } = new List<string>();
    }
}
