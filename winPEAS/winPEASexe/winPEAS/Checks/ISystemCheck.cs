namespace winPEAS.Checks
{
    internal interface ISystemCheck
    {
        void PrintInfo(bool isDebug);

        /// <summary>
        /// MITRE ATT&amp;CK technique IDs associated with this check category
        /// (e.g. new[] { "T1082", "T1548.002" }).
        /// </summary>
        string[] MitreAttackIds { get; }
    }
}
