namespace winPEAS.Info.SystemInfo.AuditPolicies
{
    internal class AuditEntryInfo
    {
        public string Target { get; }
        public string Subcategory { get; }
        public string SubcategoryGuid { get; }
        public AuditType AuditType { get; }

        public AuditEntryInfo(
            string target,
            string subcategory,
            string subcategoryGuid,
            AuditType auditType)
        {
            Target = target;
            Subcategory = subcategory;
            SubcategoryGuid = subcategoryGuid;
            AuditType = auditType;
        }
    }
}
