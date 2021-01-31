namespace winPEAS.Info.SystemInfo.Printers
{
    public class PrinterInfo
    {
        public string Name { get; }
        public string Status { get; }
        public string Sddl { get; }
        public bool IsDefault { get; }
        public bool IsNetworkPrinter { get; }

        public PrinterInfo(string name, string status, string sddl, bool isDefault, bool isNetworkPrinter)
        {
            Name = name;
            Status = status;
            Sddl = sddl;
            IsDefault = isDefault;
            IsNetworkPrinter = isNetworkPrinter;
        }
    }
}
