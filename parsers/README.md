# Privilege Escalation Awesome Scripts Parsers

These scripts allows you to transform the output of linpeas/macpeas/winpeas to JSON and then to PDF and HTML.

```python3
python3 peas2json.py </path/to/executed_peass.out> </path/to/peass.json>
python3 json2pdf.py </path/to/peass.json> </path/to/peass.pdf>
python3 json2html.py </path/to/peass.json> </path/to/peass.html>
```


## JSON Format
Basically, **each section has**:
 - Infos (URLs or info about the section)
 - Text lines (the real text info found in the section, colors included)
 - More sections

There is a **maximun of 3 levels of sections**.

```json
{
  "<Main Section Name>": {
    "sections": {
      "<Secondary Section Name>": {
        "sections": {},
        "lines": [
          {
            "raw_text": "\u001b[0m\u001b[1;33m[+] \u001b[1;32mnmap\u001b[1;34m is available for network discover & port scanning, you should use it yourself",
            "clean_text": "[+]  is available for network discover & port scanning, you should use it yourself",
            "colors": {
                "GREEN": [
                    "nmap"
                ],
                "YELLOW": [
                    "[+]"
                ]
            }
          }
        ],
        "infos": [
          "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#kernel-exploits"
        ]
      },
      "infos": []
```

```json
{
  "System Information": {
    "sections": {
      "Operative system": {
        "sections": {},
        "lines": [
          {
            "raw_text": "\u001b[0m\u001b[1;33m[+] \u001b[1;32mnmap\u001b[1;34m is available for network discover & port scanning, you should use it yourself",
            "clean_text": "[+]  is available for network discover & port scanning, you should use it yourself",
            "colors": {
                "GREEN": [
                    "nmap"
                ],
                "YELLOW": [
                    "[+]"
                ]
            }
          }
        ],
        "infos": [
          "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#kernel-exploits"
        ]
      },
      "infos": []
```


There can also be a `<Third level Section Name>`

If you need to transform several outputs check out https://github.com/mnemonic-re/parsePEASS

# TODO:

- **PRs improving the code and the aspect of the final PDFs and HTMLs are always welcome!**
