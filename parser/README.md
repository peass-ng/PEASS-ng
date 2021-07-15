# Privilege Escalation Awesome Scripts JSON exporter

This script allows you to transform the output of linpeas/macpeas/winpeas to JSON.

```python3
python3 peass-parser.py </path/to/executed_peass> </path/to/output_peass.json>
```

This script is still in beta version and has been tested only with linpeas output.

## Format
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
          "https://book.hacktricks.xyz/linux-unix/privilege-escalation#kernel-exploits"
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
          "https://book.hacktricks.xyz/linux-unix/privilege-escalation#kernel-exploits"
        ]
      },
      "infos": []
```


There can also be a `<Third level Section Name>`

# TODO:

I'm looking for **someone that could create HTML and PDF reports** from this JSON.