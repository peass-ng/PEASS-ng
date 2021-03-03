using System;

namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    internal class AsrRule
    {
        public Guid Rule { get; private set; }
        public int State { get; private set; }

        public AsrRule(Guid rule, int state)
        {
            Rule = rule;
            State = state;
        }
    }
}
