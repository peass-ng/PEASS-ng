# TODO

### Generate Nice Reports
- [x] Create a parser from linpeas and winpeas.exe output to JSON. You can fin it [here](https://github.com/peass-ng/PEASS-ng/tree/master/parser).
- [ ] Create a python script that generates a nice HTML/PDF from the JSON output

### Generate a DB of Known Vulnerable Binaries
- [ ] Create a DB of the md5/sha1 of binaries known to be vulnerable to command execution/Privilege Escalation

### Maintain Updated LinPEAS's known SUID exploits 
- [ ] Maintain updated LinPEAS's known SUID exploits 

### Network Capabilities for WinPEAS
- [ ] Give to WinPEAS network host discover capabilities and port scanner capabilities (like LinPEAS has)

### Add More checks to LinPEAS and WinPEAS
- [ ] Add more checks in LinPEAS
- [ ] Add more checks in WinPEAS

### Find a way to minify and/or obfuscate LinPEAS automatically
- [ ] Find a way to minify and/or obfuscate linpeas.sh automatically. If you know a way contact me in Telegram or via github issues

### Create a PEASS-ng Web Page were the project is properly presented
- [ ] Let me know in Telegram or github issues if you are interested in helping with this

### Relate LinPEAS and WinPEAS with the Att&ck matrix
- [ ] In the title of each check of LinPEAS and WinPEAS indicate between parenthesis and in grey the Tactic used. Example: **Enumerating something** (*T1234*)
- [ ] Once the previous task is done, modify LinPEAS and WinPEAS to be able to indicate just the Tactic(s) that want to be executed so the scripts only execute the checks related to those tactics. Example: `linpeas.sh -T T1590,T1591`
