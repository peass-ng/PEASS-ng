namespace winPEAS.Native.Enums
{
    public enum SECURITY_LOGON_TYPE : uint
    {
        Interactive = 2,        // logging on interactively.
        Network,                // logging using a network.
        Batch,                  // logon for a batch process.
        Service,                // logon for a service account.
        Proxy,                  // Not supported.
        Unlock,                 // Tattempt to unlock a workstation.
        NetworkCleartext,       // network logon with cleartext credentials
        NewCredentials,         // caller can clone its current token and specify new credentials for outbound connections
        RemoteInteractive,      // terminal server session that is both remote and interactive
        CachedInteractive,      // attempt to use the cached credentials without going out across the network
        CachedRemoteInteractive,// same as RemoteInteractive, except used internally for auditing purposes
        CachedUnlock            // attempt to unlock a workstation
    }
}
