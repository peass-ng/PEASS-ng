# PEASS Post Exploitation Module for Metasploit

You can use this module to **automatically execute a PEASS script from a meterpreter or shell session obtained in metasploit**.

## Manual Installation
Copy the `peass.rb` file to the path `modules/post/multi/gather/` inside the metasploit installation.

In Kali: `sudo cp ./peass.rb /usr/share/metasploit-framework/modules/post/multi/gather/`

Now you can do `reload_all` inside a running msfconsole or the next time you launch a new msfconsole the peass module will be **automatically loaded**.

## How to use it
```
msf6 exploit(multi/handler) > use post/multi/gather/peass
msf6 post(multi/gather/peass) > show info

       Name: Multi PEASS launcher
     Module: post/multi/gather/peass
   Platform: BSD, Linux, OSX, Unix, Windows
       Arch: 
       Rank: Normal

Provided by:
  Carlos Polop <@carlospolopm>

Compatible session types:
  Meterpreter
  Shell

Basic options:
  Name        Current Setting                                                           Required  Description
  ----        ---------------                                                           --------  -----------
  PARAMETERS                                                                            no        Parameters to use in the execution of the script
  PASSWORD    qzke5he7u5n6ijcxhlnj2bc2o556xool                                          no        Password to encrypt and obfuscate the script (randomly generated). The length must be 32B. If no password is set, only base64 will be used.
  SESSION                                                                               yes       The session to run this module on.
  TEMP_DIR                                                                              no        Path to upload the obfuscated PEASS script. By default "C:\Windows\System32\spool\drivers\color" is used in Windows and "/tmp" in unix.
  TIMEOUT     900                                                                       no        Timeout of the execution of the PEASS script (15min by default)
  URL         https://raw.githubusercontent.com/carlospolop/PEASS-ng/master/winPEAS/wi  yes       Path to the PEASS script. Accepted: http(s):// URL or absolute local path. Linpeas: https://raw.githubusercontent.com/carlospolop/PEASS-ng
              nPEASexe/binaries/Obfuscated%20Releases/winPEASany.exe                              /master/linPEAS/linpeas.sh

Description:
  This module will launch the indicated PEASS (Privilege Escalation 
  Awesome Script Suite) script to enumerate the system. You need to 
  indicate the URL or local path to LinPEAS if you are in some Unix or 
  to WinPEAS if you are in Windows.

References:
  https://github.com/carlospolop/PEASS-ng
  https://www.youtube.com/watch?v=9_fJv_weLU0
```

The options are pretty self-explanatory. Just notice that you can set parametes like "-h" in `PARAMETERS` and then linpeas/winpeas will just show the help (*just like when you execute them from a console*).

**IMPORTANT**: You won't see any result until the execution of the script is completed.