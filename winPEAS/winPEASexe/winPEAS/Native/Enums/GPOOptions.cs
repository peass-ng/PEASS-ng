using System.ComponentModel;

namespace winPEAS.Native.Enums
{
    enum GPOOptions
    {
        [Description("All Sections Enabled")]
        ALL_SECTIONS_ENABLED = 0,

        [Description("User Section Disbled")]
        USER_SECTION_DISABLED = 1,

        [Description("Computer Section Disable")]
        COMPUTER_SECTION_DISABLE = 2,

        [Description("All Sections Disabled")]
        ALL_SECTIONS_DISABLED = 3
    }
}
