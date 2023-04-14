$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$filePath = Join-Path $scriptDir "regexes.yaml"
$url = "https://raw.githubusercontent.com/JaimePolop/RExpository/main/regex.yaml"

Invoke-WebRequest $url -OutFile $filePath