namespace winPEAS.Native.Enums
{
    /// <summary>
    /// Provides formats to use for input and output names for the DsCrackNames function.
    /// </summary>
    public enum DS_NAME_FORMAT
    {
        ///<summary>Indicates the name is using an unknown name type. This format can impact performance because it forces the server to attempt to match all possible formats. Only use this value if the input format is unknown.</summary>
        DS_UNKNOWN_NAME = 0,

        ///<summary>Indicates that the fully qualified distinguished name is used. For example: "CN = someone, OU = Users, DC = Engineering, DC = Fabrikam, DC = Com"</summary>
        DS_FQDN_1779_NAME = 1,

        ///<summary>Indicates a Windows NT 4.0 account name. For example: "Engineering\someone" The domain-only version includes two trailing backslashes (\\).</summary>
        DS_NT4_ACCOUNT_NAME = 2,

        ///<summary>Indicates a user-friendly display name, for example, Jeff Smith. The display name is not necessarily the same as relative distinguished name (RDN).</summary>
        DS_DISPLAY_NAME = 3,

        ///<summary>Indicates a GUID string that the IIDFromString function returns. For example: "{4fa050f0-f561-11cf-bdd9-00aa003a77b6}"</summary>
        DS_UNIQUE_ID_NAME = 6,

        ///<summary>Indicates a complete canonical name. For example: "engineering.fabrikam.com/software/someone" The domain-only version includes a trailing forward slash (/).</summary>
        DS_CANONICAL_NAME = 7,

        ///<summary>Indicates that it is using the user principal name (UPN). For example: "someone@engineering.fabrikam.com"</summary>
        DS_USER_PRINCIPAL_NAME = 8,

        ///<summary>This element is the same as DS_CANONICAL_NAME except that the rightmost forward slash (/) is replaced with a newline character (\n), even in a domain-only case. For example: "engineering.fabrikam.com/software\nsomeone"</summary>
        DS_CANONICAL_NAME_EX = 9,

        ///<summary>Indicates it is using a generalized service principal name. For example: "www/www.fabrikam.com@fabrikam.com"</summary>
        DS_SERVICE_PRINCIPAL_NAME = 10,

        ///<summary>Indicates a Security Identifier (SID) for the object. This can be either the current SID or a SID from the object SID history. The SID string can use either the standard string representation of a SID, or one of the string constants defined in Sddl.h. For more information about converting a binary SID into a SID string, see SID Strings. The following is an example of a SID string: "S-1-5-21-397955417-626881126-188441444-501"</summary>
        DS_SID_OR_SID_HISTORY_NAME = 11,
    }
}
