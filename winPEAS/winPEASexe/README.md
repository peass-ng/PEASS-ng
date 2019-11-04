# Windows Privilege Escalation Awesome Script (.exe)

![](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/raw/master/winPEAS/winPEASexe/images/winpeas.png)

**WinPEAS is a script that searh for possible paths to escalate privileges on Windows hosts. The checks are explained on [book.hacktricks.xyz](https://book.hacktricks.xyz/windows/windows-local-privilege-escalation)**

Check also the **Local Windows Privilege Escalation checklist** from **[book.hacktricks.xyz](https://book.hacktricks.xyz/windows/checklist-windows-privilege-escalation)**

## Quick Start

Download the **[latest version from here](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/tree/master/winPEAS/winPEASexe/winPEAS/bin)** or **compile it yourself**.
```bash
winpeas.exe ansii #ANSII color for linux consoles (reverse shell)
winpeas.exe #Will execute all checks except the ones that execute MD commands
winpeas.exe cmd #All checks
winpeas.exe cmd fast #All except the one that search for files
winpeas.exe systeminfo userinfo #Only systeminfo and userinfo checks executed 
```

## Basic information

The goal of this project is to search for possible **Privilege Escalation Paths** in Windows environments.

It should take only a **few seconds** to execute almost all the checks and **some minutes searching in the whole main drive** for known files that could contain passwords (the time depened on the number of files in your drive). Get rif of that time consuming check using the parameter `fast`.

The **ouput will be colored**. Below you have some indications about what does each color means exacty, but keep in mind that **Red** is for something interesting (from a pentester perspective) and **Green** is something good (from a defender perspective).

The tool is heavily based in **[SeatBelt](https://github.com/GhostPack/Seatbelt)**.

**IMPORTANT TO NOTICE:** By default WinPEAS will use colord for Windows terminals (without ANSII characters). If execute winpeas.exe from a reverse shell without any option **no color will be printed**. To see colors in a linux terminal you need to use the **ansii** parameter.

## Help

![](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/raw/master/winPEAS/winPEASexe/images/help.png)

## Colors

![](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/raw/master/winPEAS/winPEASexe/images/colors.png)

## Checks

<details>
  <summary>Details</summary>
    
- **System Information**
  - [x] Basic System info information
  - [x] Use Watson to search for vulnerabilities
  - [x] PS, Audit, WEF and LAPS Settings
  - [x] Environment Variables
  - [x] Internet Settings
  - [x] Current drives information
  - [x] AV?
  - [x] UAC configuration

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

- **Processes Information**
  - [x] Interesting processes (non Microsoft)

- **Services Information**
  - [x] Interesting services (non Microsoft) information
  - [x] Writable service registry
  - [x] PATH Dll Hijacking

- **Applications Information**
  - [x] Current Active Window
  - [x] Installed software
  - [x] AutoRuns
  - [x] Scheduled tasks

- **Network Information**
  - [x] Current net shares
  - [x] hosts file
  - [x] Network Interfaces
  - [x] Listening ports
  - [x] Firewall rules
  - [x] DNS Cache (limit 70)

- **Windows Credentials**
  - [x] Windows Vault
  - [x] Credential Manager
  - [x] Saved RDO connections
  - [x] Recently run commands
  - [x] DPAPI Masterkeys
  - [x] DPAPI Credential files
  - [x] Remote Desktop Connection Manager credentials
  - [x] Kerberos Tickets
  - [x] Wifi
  - [x] AppCmd.exe
  - [x] SSClient.exe
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

- **Interesting Files and registry**
  - [x] Putty sessions
  - [x] Putty SSH host keys
  - [x] Cloud credentials
  - [x] Possible registries with credentials
  - [x] Possible credentials files in users homes
  - [x] Possible password files inside the Recycle bin
  - [x] Possible files containing credentials (this take some minutes)
  - [x] User documents (limit 100)

</details>

## Do not fork it!!

If you want to **add something** and have **any cool idea** related to this project, please let me know it using the **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/issues)** and we will update the master version.

## TODO

- Add more checks
- Mantain updated Watson
- List wifi networks without using CMD
- List credentials inside the Credential Manager without using CMD

If you want to help with any of this, you can do it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/issues)** or you can submit a pull request.

If you find any issue, please report it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-script-suite/issues)**.

**WinPEAS** is being **updated** every time I find something that could be useful to escalate privileges.

## License

MIT License

By Polop<sup>(TM)</sup>
