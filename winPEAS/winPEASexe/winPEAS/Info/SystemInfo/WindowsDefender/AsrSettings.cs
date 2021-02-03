using System.Collections.Generic;

namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    internal class AsrSettings
    {
        public bool Enabled { get; }
        public IList<AsrRule> Rules { get; } = new List<AsrRule>();
        public IList<string> Exclusions { get; } = new List<string>();

        public AsrSettings(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
