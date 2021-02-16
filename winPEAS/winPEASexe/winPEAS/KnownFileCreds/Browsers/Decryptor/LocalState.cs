namespace winPEAS.KnownFileCreds.Browsers.Decryptor
{
    public class LocalState
    {
        public class OsCrypt
        {
            public string encrypted_key { get; set; }
        }

        public OsCrypt os_crypt { get; set; }
    }
}
