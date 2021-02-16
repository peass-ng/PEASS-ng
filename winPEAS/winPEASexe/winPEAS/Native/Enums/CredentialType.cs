namespace winPEAS.Native.Enums
{
    /// <summary>
    ///     Enum CredentialType
    /// 
    ///     The type of the credential. This member cannot be changed after the credential is created.
    /// </summary>
    public enum CredentialType : uint
    {
        /// <summary>
        ///     The lack of credential type
        /// </summary>
        None = 0,

        /// <summary>
        ///     Generic credential type
        /// 
        ///     The credential is a generic credential. The credential will not be used by any particular authentication package. 
        ///     The credential will be stored securely but has no other significant characteristics.
        /// </summary>
        Generic = 1,

        /// <summary>
        ///     Domain password credential type
        /// 
        ///     The credential is a password credential and is specific to Microsoft's authentication packages. 
        ///     The NTLM, Kerberos, and Negotiate authentication packages will automatically use this credential when connecting to the named target.
        /// </summary>
        DomainPassword = 2,

        /// <summary>
        ///     Domain certificate credential type
        /// 
        ///     The credential is a certificate credential and is specific to Microsoft's authentication packages. 
        ///     The Kerberos, Negotiate, and Schannel authentication packages automatically use this credential when connecting to the named target.
        /// </summary>
        DomainCertificate = 3
    }
}
