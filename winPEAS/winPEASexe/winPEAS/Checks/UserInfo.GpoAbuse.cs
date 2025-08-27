using System; using winPEAS.Helpers; namespace winPEAS.Checks { internal partial class UserInfo { 
void PrintPotentialGpoAbuseIndicators() { 
    try {
        Beaprint.MainPrint("Potential GPO abuse (Active Directory)");
        Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/gpo-abuse.html", "Check if you can abuse GPO permissions to run code as SYSTEM");
        if (!Checks.IsPartOfDomain || Checks.IsCurrentUserLocal) { Beaprint.NotFoundPrint(); return; }
        bool isGpcOwner = false;
        foreach (var kv in Checks.CurrentUserSiDs) {
            if (!string.IsNullOrEmpty(kv.Value) && kv.Value.Equals("Group Policy Creator Owners", StringComparison.OrdinalIgnoreCase)) { isGpcOwner = true; break; }
        }
        if (isGpcOwner) { Beaprint.BadPrint("  [!] Current user token contains 'Group Policy Creator Owners' – you may be able to create/modify GPOs."); }
        else { Beaprint.NoColorPrint("  [-] Not a member of 'Group Policy Creator Owners' (based on current token)."); }
        try {
            var applied = winPEAS.Info.SystemInfo.GroupPolicy.GroupPolicy.GetLocalGroupPolicyInfos();
            var anyPrinted = false;
            foreach (var info in applied) {
                if ($"{info.GPOType}".Equals("machine", StringComparison.OrdinalIgnoreCase)) {
                    var fileSysPath = $"{info.FileSysPath}";
                    if (string.IsNullOrEmpty(fileSysPath)) continue;
                    if (fileSysPath.StartsWith("\\\\")) {
                        var rights = PermissionsHelper.GetPermissionsFolder(fileSysPath, Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);
                        if (rights.Count > 0) {
                            if (!anyPrinted) { Beaprint.BadPrint("  [!] Writable applied GPO folders in SYSVOL (abusable):"); anyPrinted = true; }
                            Beaprint.BadPrint($"      -> {fileSysPath} | Rights: {string.Join(", ", rights)}");
                        }
                    }
                }
            }
            if (!isGpcOwner && !anyPrinted) { Beaprint.NotFoundPrint(); }
        } catch (Exception ex2) {
            Beaprint.GrayPrint("    [i] Error while checking applied GPO folders: " + ex2.Message);
            if (!isGpcOwner) { Beaprint.NotFoundPrint(); }
        }
        Beaprint.GrayPrint("    Tip: If you can edit a GPO linked to this computer, tools like SharpGPOAbuse can add an immediate scheduled task to execute a command as SYSTEM.");
    } catch (Exception ex) { Beaprint.PrintException(ex.Message); }
}
} }
