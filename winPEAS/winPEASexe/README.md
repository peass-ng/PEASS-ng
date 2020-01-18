# Windows Privilege Escalation Awesome Script (.exe)

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/winPEAS/winPEASexe/images/winpeas.png)

**WinPEAS is a script that searh for possible paths to escalate privileges on Windows hosts. The checks are explained on [book.hacktricks.xyz](https://book.hacktricks.xyz/windows/windows-local-privilege-escalation)**

Check also the **Local Windows Privilege Escalation checklist** from **[book.hacktricks.xyz](https://book.hacktricks.xyz/windows/checklist-windows-privilege-escalation)**

[![youtube](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/winPEAS/winPEASexe/images/screen.png)](https://youtu.be/66gOwXMnxRI)

## Quick Start

Download the **[latest version from here](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/tree/master/winPEAS/winPEASexe/winPEAS/bin)** or **compile it yourself**.
```bash
winpeas.exe cmd searchfast #cmd commands and avoid sleepig (noisy - CTFs)
winpeas.exe #Will execute all checks except the ones that use a CMD
winpeas.exe cmd #All checks
winpeas.exe cmd fast #All except the one that search for files
winpeas.exe systeminfo userinfo #Only systeminfo and userinfo checks executed
winpeas.exe notcolor #Do not color the output
```

## Basic information

The goal of this project is to search for possible **Privilege Escalation Paths** in Windows environments.

It should take only a **few seconds** to execute almost all the checks and **some minutes during the last check searching in the whole main drive** for known files that could contain passwords (the time depened on the number of files in your drive). Get rid of that time consuming check using the parameter `fast`.

By default, the progam **sleeps 150ms** before start searching files in each directory. This is made to consume less resources (**stealthier**). You can **avoid this sleep using `searchfast` parameter**.

The **ouput will be colored** using **ansi** colors. If you are executing `winpeas.exe` **from a Windows console**, you need to set a registry value to see the colors:
```
REG ADD HKCU\Console /v VirtualTerminalLevel /t REG_DWORD /d 1
```

Below you have some indications about what does each color means exacty, but keep in mind that **Red** is for something interesting (from a pentester perspective) and **Green** is something well configured (from a defender perspective).

The tool is based in **[SeatBelt](https://github.com/GhostPack/Seatbelt)**.


## Help

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/winPEAS/winPEASexe/images/help.png)

## Colors

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/winPEAS/winPEASexe/images/colors.png)

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
  - [x] Writable service registry binpath
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
  - [x] Saved RDP connections
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
  - [x] SSH Keys inside registry
  - [x] Cloud credentials
  - [x] Possible registries with credentials
  - [x] Possible credentials files in users homes
  - [x] Possible password files inside the Recycle bin
  - [x] Possible files containing credentials (this take some minutes)
  - [x] User documents (limit 100)

</details>

## Let's improve PEASS together

If you want to **add something** and have **any cool idea** related to this project, please let me know it in the **telegram group https://t.me/peass** or using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues)** and we will update the master version.

## Please, if this tool has been useful for you consider to donate

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=DED2HWDYLFT2C&source=url)

## Looking for a useful Privilege Escalation Course?

Contact me and ask about the **Privilege Escalation Course** I am preparing for attackers and defenders (**100% technical**).

## TODO

- Add more checks
- Mantain updated Watson
- List wifi networks without using CMD
- List credentials inside the Credential Manager without using CMD

If you want to help with any of this, you can do it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues)** or you can submit a pull request.

If you find any issue, please report it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues)**.

**WinPEAS** is being **updated** every time I find something that could be useful to escalate privileges.

## Advisory

All the scripts/binaries of the PEAS Suite should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.

## License

MIT License

By Polop<sup>(TM)</sup>
