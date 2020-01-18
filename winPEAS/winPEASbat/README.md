# Windows Privilege Escalation Awesome Script (.bat)

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/winPEAS/winPEASexe/images/winpeas.png)

**WinPEAS is a script that searh for possible paths to escalate privileges on Windows hosts. The checks are explained on [book.hacktricks.xyz](https://book.hacktricks.xyz/windows/windows-local-privilege-escalation)**

Check also the **Local Windows Privilege Escalation checklist** from [book.hacktricks.xyz](https://book.hacktricks.xyz/windows/checklist-windows-privilege-escalation)

### WinPEAS.bat is a batch script made for Windows systems which don't support WinPEAS.exe (Net.4 required)

Unfortunately this script **does not support colors** so you will need to know what are you looking for in each test and, also, you will have to know how to learn the icacls output, see below.

## Windows PE using CMD (.bat)

If you want to search for files and registry that could contain passwords, set to *yes* the *long* variable at the beginning of the script.

The script will use acceschk.exe if it is available (with that name). But it is not necessary, it also uses wmic + icacls.

Some of the tests in this script were extracted from **[here](https://github.com/enjoiz/Privesc/blob/master/privesc.bat)** and from **[here](https://github.com/codingo/OSCP-2/blob/master/Windows/WinPrivCheck.bat)**


### Main checks

<details>
  <summary>Details</summary>

- [x] Systeminfo --SO version and patches-- (windows suggester)
- [x] Common known exploits (2K, XP, 2K3, 2K8, Vista, 7)
- [x] Audit Settings
- [x] WEF Settings
- [x] LAPS installed?
- [x] UAC Settings
- [x] AV?
- [x] PS Settings
- [x] Mounted disks
- [x] SCCM installed?
- [x] Remote Desktop Credentials Manager?
- [x] WSUS Settings
- [x] Processes list
- [x] Interesting file permissions of binaries being executed 
- [x] Interesting file permissions of binaries run at startup
- [x] AlwaysInstallElevated?
- [x] Network info (see below)
- [x] Users info (see below)
- [x] Current user privileges 
- [x] Service binary permissions 
- [x] Check if permissions to modify any service registy
- [x] Unquoted Service paths  
- [x] DLL Hijacking in PATH
- [x] Windows Vault
- [x] DPAPI Master Keys
- [x] AppCmd.exe?
- [x] Search for known registry to have passwords and keys inside
- [x] Search for known files to have passwords inside (can take some minutes)
- [x] If *long*, search files with passwords inside 
- [x] If *long*, search registry with passwords inside 

### More enumeration

- [x] Date & Time
- [x] Env
- [x] Installed Software
- [x] Running Processes 
- [x] Current Shares 
- [x] Network Interfaces
- [x] Used Ports
- [x] Firewall
- [x] ARP
- [x] Routes
- [x] Hosts
- [x] Cached DNS
- [x] Info about current user (PRIVILEGES)
- [x] List groups (info about administrators)
- [x] Current logon users 

</details>

### Understanding icacls permissions

Icacls is the program used to check the rights that groups and users have in a file or folder.

Iclals is the main binary used here to check permissions.

Its output is not intuitive so if you are not familiar with the command, continue reading. Take into account that in XP you need administrators rights to use icacls (for this OS is very recommended to upload sysinternals accesschk.exe to enumerate rights).

**Interesting permissions**

```
D - Delete access
F - Full access (Edit_Permissions+Create+Delete+Read+Write)
N - No access
M - Modify access (Create+Delete+Read+Write)
RX - Read and eXecute access
R - Read-only access
W - Write-only access
```

We will focus in **F** (full), **M** (Modify access) and **W** (write).

**Use of Icacls by WinPEAS**

When checking rights of a file or a folder the script search for the strings: *(F)* or *(M)* or *(W)* and the string ":\" (so the path of the file being checked will appear inside the output).

It also checks that the found right (F, M or W) can be exploited by the current user.

A typical output where you dont have any nice access is:
```
C:\Windows\Explorer.EXE NT SERVICE\TrustedInstaller:(F)
```

An output where you have some interesting privilege will be like:
```
C:\Users\john\Desktop\desktop.ini NT AUTHORITY\SYSTEM:(I)(F)
                                MYDOMAIN\john:(I)(F)
```

Here you can see that the privileges of user *NT AUTHORITY\SYSTEM* appears in the output because it is in the same line as the path of the binary. However, in the next line, you can see that our user (john) has full privileges in that file. 

This is the kind of outpuf that you have to look for when usnig the winPEAS.bat script.

[More info about icacls here](https://ss64.com/nt/icacls.html)

## Let's improve PEASS together

If you want to **add something** and have **any cool idea** related to this project, please let me know it in the **telegram group https://t.me/peass** or using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues)** and we will update the master version.

## Please, if this tool has been useful for you consider to donate

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=DED2HWDYLFT2C&source=url)

## Looking for a useful Privilege Escalation Course?

Contact me and ask about the **Privilege Escalation Course** I am preparing for attackers and defenders (**100% technical**).

## Advisory

All the scripts/binaries of the PEAS Suite should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.

## License

MIT License

By Polop<sup>(TM)</sup>
