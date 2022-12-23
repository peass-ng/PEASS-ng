namespace winPEAS.Info.NetworkInfo.InternetSettings
{
    internal class InternetSettingsKey
    {
        public string ValueName { get; }
        public string Value { get; }
        public string Hive { get; }
        public string Path { get; }
        public string Interpretation { get; }

        public InternetSettingsKey(
            string hive,
            string path,
            string valueName,
            string value,
            string interpretation)
        {
            ValueName = valueName;
            Value = value;
            Interpretation = interpretation;
            Hive = hive;
            Path = path;
        }
    }
}
