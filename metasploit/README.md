# PEASS Post Exploitation Module for Metasploit

You can use this module to **automatically execute a PEASS script from a meterpreter or shell session obtained in metasploit**.

## Manual Installation
Copy the `peass.rb` file to the path `modules/post/multi/gather/` inside the metasploit installation.

In Kali: 
```bash
sudo cp ./peass.rb /usr/share/metasploit-framework/modules/post/multi/gather/
# or
sudo wget https://raw.githubusercontent.com/peass-ng/PEASS-ng/master/metasploit/peass.rb -O /usr/share/metasploit-framework/modules/post/multi/gather/peass.rb
```

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
  Carlos Polop <@hacktricks_live>

Compatible session types:
  Meterpreter
  Shell

Basic options:
  Name        Current Setting                                                           Required  Description
  ----        ---------------                                                           --------  -----------
  PARAMETERS                                                                            no        Parameters to pass to the script
  PASSWORD    um1xipfws17nkw1bi1ma3bh7tzt4mo3e                                          no        Password to encrypt and obfuscate the script (randomly generated). The length must be 32B. If no password is set, only base64 will be used

  WINPEASS    true                                                                      yes       Use PEASS for Windows or PEASS for linux. Default is windows change to false for linux.
  CUSTOM_URL                                                                            no        Path to the PEASS script. Accepted: http(s):// URL or absolute local path.
                                            
  SESSION                                                                               yes       The session to run this module on.
  SRVHOST                                                                               no        Set your metasploit instance IP if you want to download the PEASS script from here via http(s) instead of uploading it.
  SRVPORT     443                                                                       no        Port to download the PEASS script from using http(s) (only used if SRVHOST)
  SSL         true                                                                      no        Indicate if you want to communicate with https (only used if SRVHOST)
  SSLCert                                                                               no        Path to a custom SSL certificate (default is randomly generated)
  TEMP_DIR                                                                              no        Path to upload the obfuscated PEASS script inside the compromised machine. By default "C:\Windows\System32\spool\drivers\color" is used in
                                                                                                   Windows and "/tmp" in Unix.
  TIMEOUT     900                                                                       no        Timeout of the execution of the PEASS script (15min by default)
  URIPATH     /mvpo.txt                                                                 no        URI path to download the script from there (only used if SRVHOST)

Description:
  This module will launch the indicated PEASS (Privilege Escalation 
  Awesome Script Suite) script to enumerate the system. You need to 
  indicate the URL or local path to LinPEAS if you are in some Unix or 
  to WinPEAS if you are in Windows. By default this script will upload 
  the PEASS script to the host (encrypted and/or encoded) and will 
  load it and execute it. You can configure this module to download 
  the encrypted/encoded PEASS script from this metasploit instance via 
  HTTP instead of uploading it.

References:
  https://github.com/peass-ng/PEASS-ng
  https://www.youtube.com/watch?v=9_fJv_weLU0
```

The options are pretty self-explanatory.

Notice that **by default** the obfuscated PEASS script if going to be **uploaded** but if you **set SRVHOST it will be downloaded** via http(s) from the metasploit instance (**so nothing will be written in the disk of the compromised host**).

Notice that you can **set parametes** like `-h` in `PARAMETERS` and then linpeas/winpeas will just show the help (*just like when you execute them from a console*).

**IMPORTANT**: You won't see any output until the execution of the script is completed.
