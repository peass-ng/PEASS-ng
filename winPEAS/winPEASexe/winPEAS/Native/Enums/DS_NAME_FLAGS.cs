using System;

namespace winPEAS.Native.Enums
{
    /// <summary>
    /// Used to define how the name syntax will be cracked. These flags are used by the DsCrackNames function.
    /// </summary>
    [Flags]
    public enum DS_NAME_FLAGS
    {
        /// <summary>Indicate that there are no associated flags.</summary>
        DS_NAME_NO_FLAGS = 0x0,

        ///<summary>Perform a syntactical mapping at the client without transferring over the network. The only syntactic mapping supported is from DS_FQDN_1779_NAME to DS_CANONICAL_NAME or DS_CANONICAL_NAME_EX.</summary>
        DS_NAME_FLAG_SYNTACTICAL_ONLY = 0x1,

        ///<summary>Force a trip to the DC for evaluation, even if this could be locally cracked syntactically.</summary>
        DS_NAME_FLAG_EVAL_AT_DC = 0x2,

        ///<summary>The call fails if the domain controller is not a global catalog server.</summary>
        DS_NAME_FLAG_GCVERIFY = 0x4,

        ///<summary>Enable cross forest trust referral.</summary>
        DS_NAME_FLAG_TRUST_REFERRAL = 0x8
    }
}
