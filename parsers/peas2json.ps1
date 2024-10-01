# Based on https://github.com/peass-ng/PEASS-ng/blob/master/parsers/peas2json.py

# Pattern to identify main section titles
$CHAR_1 = [String][char]0x2550 # ═
$CHAR_2 = [String][char]0x2554 # ╔
$CHAR_3 = [String][char]0x2563 # ╣
$CHAR_4 = [String][char]0x255a # ╚
$TITLE_CHARS = [String][char]0x2550, [String][char]0x2554, [String][char]0x2563, [String][char]0x255a  # ═, ╔, ╣, ╚
$TITLE1_PATTERN = $CHAR_1*14 + $CHAR_3 #══════════════╣#
#The size of the first pattern varies, but at least should be that large
$TITLE2_PATTERN = $CHAR_2 + $CHAR_1*10 + $CHAR_3 #╔══════════╣#
$TITLE3_PATTERN = $CHAR_1*2 + $CHAR_3 #══╣#
$INFO_PATTERN = $CHAR_4 #╚ #

$encoding = [System.Text.Encoding]::UTF8

# Patterns from color
## The order is important, the first string colored with a color will be the one selected (the same string cannot be colored with different colors)
$global:COLORS = @{
    "REDYELLOW" = "\x1b\[1;31;103m";
    "RED" = "\x1b\[1;31m";
    "GREEN" = "\x1b\[1;32m";
    "YELLOW" = "\x1b\[1;33m";
    "BLUE" = "\x1b\[1;34m";
    "MAGENTA" = "\x1b\[1;95m", "\x1b\[1;35m";
    "CYAN" = "\x1b\[1;36m", "\x1b\[1;96m";
    "LIGHT_GREY" = "\x1b\[1;37m";
    "DARKGREY" = "\x1b\[1;90m";
}

$global:FINAL_JSON = @{}

$global:C_SECTION = $FINAL_JSON
$global:C_MAIN_SECTION = $FINAL_JSON
$global:C_2_SECTION = $FINAL_JSON
$global:C_3_SECTION = $FINAL_JSON

function is_section {
    param (
        [string] $line,
        [string] $pattern
    )

    # Checks ifa  line matches the pattern
    return $line.contains($pattern)
}

function clean_colors {
    param (
        [string] $line
    )

    # Given a line, clean the colors inside of it

    $line = $line -replace '\x1b\[[0-9;]*m',''
    $line = $line.Trim()
    return $line
    
}

function clean_title {
    param (
        [string] $line
    )
    # Given a title, clean it
    foreach($c in $TITLE_CHARS){
        $line = $line.Replace($c, "")
    }

    $line = [System.Text.Encoding]::ASCII.GetString($encoding.GetBytes($line))
    $line = $line.Trim()
    return $line

}

function get_colors {
    param (
        [string] $line
    )

    [hashtable]$colors = @{}

    $global:COLORS.GetEnumerator() | ForEach-Object {
        $colors[$_.Key] = ''
        foreach($reg in $_.Value){ # eq reg in regexs in py
            $split_color = $line -split $reg 
            # Start from index 1 as the index 0 isn't colored
            if($split_color -And $split_color.Length -gt 1){
                $split_color = $split_color | Select-Object -Skip 1

                # For each potential color, find the string before any possible color termination
                foreach($potential_color_str in $split_color){
                    $color_str1 = ($potential_color_str -split "\x1b")[0]
                    $color_str2 = ($potential_color_str -split "\[0m")[0]
                    $color_str = $color_str2
                    if($color_str1.Length -lt $color_str2.Length){
                        $color_str = $color_str1
                    }
                    
                    if($color_str){
                        $color_str = clean_colors $color_str.trim()
                        # Avoid having the same color for the same string
                        if($color_str){
                            $colors[$_.Key] += $color_str
                        }
                    }
                }
            }


        }
        if(-not $colors[$_.Key]){
            $colors.Remove($_.Key)
        }
    }

    return $colors
    
}

function parse_title {
    param (
        [string] $line
    )
    # Given a title, clean it

    $cleaned_title_pt = clean_title($line)
    return clean_colors $cleaned_title_pt
    
}

function parse_line {
    param (
        [string] $line
    )
    #Parse the given line, adding it to the FINAL_JSON structure

    if( $line.Contains("Cron jobs") ){
        $a = 1
    }

    # for debug
    #$line
    #Start-Sleep -Milliseconds 500

    if(is_section $line $TITLE1_PATTERN){
        $title = parse_title $line
        #New-Object System.Collections.Generic.List[System.Object]
        $FINAL_JSON.add($title, @{ "sections" = @{}; "lines" = @(); "infos" = @() })
        $global:C_MAIN_SECTION = $global:FINAL_JSON.$title
        $global:C_SECTION = $global:C_MAIN_SECTION
    }
    elseif(is_section $line $TITLE2_PATTERN){
        $title = parse_title $line
        $global:C_MAIN_SECTION.'sections'.Add($title, @{ "sections" = @{}; "lines" = @(); "infos" = @() })
        $global:C_2_SECTION = $global:C_MAIN_SECTION.'sections'.$title
        $global:C_SECTION = $global:C_2_SECTION
    }
    elseif(is_section $line $TITLE3_PATTERN){
        $title = parse_title $line
        $global:C_2_SECTION.'sections'.add($title, @{ "sections" = @{}; "lines" = @(); "infos" = @() })
        $global:C_3_SECTION = $global:C_2_SECTION.'sections'.$title
        $global:C_SECTION = $global:C_3_SECTION
    }
    elseif(is_Section $line $INFO_PATTERN){
        $title = parse_title $line
        $global:C_SECTION["infos"] += $title
    }

    #If here, then it's text
    else{
        #If no main section parsed yet, pass
        if($global:C_SECTION -eq @{}){
            return
        }
        $global:C_SECTION['lines'] += @{"raw_text" = $line; "colors" = get_colors $line;"clean_text" = clean_title(clean_colors $line)}
    }
}

function main {
    foreach($line in Get-Content -LiteralPath $OUTPUT_PATH){
        $line = $line.Trim()
        #Write-Host $line
        if(-not $line -or -not (clean_colors $line)){ #Remove empty lines or lines just with colors hex
            continue
        }

        parse_line $line
    }

    $FINAL_JSON | ConvertTo-Json -depth 100 | Out-File $JSON_PATH
    
}


try {
    $OUTPUT_PATH = $(Read-Host "Output Path")
    $JSON_PATH = $(Read-Host "JSON Path")
}
catch {
    Write-Host "Error: Please pass the peas.out file and the path to save the json"
    exit
}

main
