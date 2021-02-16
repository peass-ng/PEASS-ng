namespace winPEAS.KnownFileCreds.Browsers.Firefox
{
    class LoginData
    {
        public long id { get; set; }
        public string hostname { get; set; }
        public string url { get; set; }
        public string httprealm { get; set; }
        public string formSubmitURL { get; set; }
        public string usernameField { get; set; }
        public string passwordField { get; set; }
        public string encryptedUsername { get; set; }
        public string encryptedPassword { get; set; }
        public string guid { get; set; }
        public int encType { get; set; }
        public long timeCreated { get; set; }
        public long timeLastUsed { get; set; }
        public long timePasswordChanged { get; set; }
        public long timesUsed { get; set; }
    }
}
