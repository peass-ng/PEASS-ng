using System;
using System.ComponentModel;

namespace winPEAS.Native.Enums
{
    [Flags]
    enum SessionSecurity : uint
    {
        [Description("None checked")]
        None = 0x00000000,

        [Description("Require message integrity")]
        Integrity = 0x00000010, // Message integrity

        [Description("Require message confidentiality")]
        Confidentiality = 0x00000020, // Message confidentiality

        [Description("Require NTLMv2 session security")]
        NTLMv2 = 0x00080000,

        [Description("Require 128-bit encryption")]
        Require128BitKey = 0x20000000,

        [Description("Require 56-bit encryption")]
        Require56BitKey = 0x80000000
    }
}
