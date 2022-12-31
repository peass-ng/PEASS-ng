namespace winPEAS.KnownFileCreds.Browsers.Firefox
{
    class FFLogins
    {
        public long nextId { get; set; }
        public LoginData[] logins { get; set; }
        public string[] disabledHosts { get; set; }
        public int version { get; set; }
    }
}
