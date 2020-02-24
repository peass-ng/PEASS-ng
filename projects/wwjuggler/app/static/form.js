file:///root/projects/wwjuggler/app/static/form_options.js {"mtime":1578310037779,"ctime":1578075190140,"size":9552,"etag":"34ebpirra9t4","orphaned":false}
$("#option").on("change", function(){
    hide_fields()
});


function hide_fields() {
    $('#option_msg').html(options_msgs[$("#option").val()]);

    switch($("#option").val()) {
        case "deauth_aireplay":
            show_essid()
            show_bssid()
            show_client()
            hide_fake_essids()
            hide_stealth()
            hide_channel()
            break;

        case "deauth_mdk4":
            show_essid()
            show_bssid()
            show_client()
            show_stealth()
            show_channel()
            hide_fake_essids()
            break;

        case "WIDS_confusion":
            show_essid()
            show_bssid()
            show_stealth()
            hide_client()
            hide_fake_essids()
            hide_channel()
            break;

        case "fake_aps":
            show_channel()
            show_stealth()
            show_fake_essids()
            hide_bssid()
            hide_client()
            hide_essid()
            break;

        case "reinject_data":
            show_bssid()
            show_stealth()
            hide_essid()
            hide_channel()
            hide_client()
            hide_fake_essids()
            break;

        case "EAPOL_DoS":
            show_bssid()
            show_stealth()
            hide_essid()
            hide_channel()
            hide_client()
            hide_fake_essids()
            break;

        case "TKIP_DoS":
            show_bssid()
            show_stealth()
            hide_essid()
            hide_channel()
            hide_client()
            hide_fake_essids()
            break;

        case "evil_twin":
            hide_known_beacons()
            hide_mac_whitelist()
            hide_mac_blacklist()
            hide_essid_whitelist()
            hide_essid_blacklist()
            hide_loud()
            show_essid()
            show_bssid()
            show_channel()
            break;

        case "mana":
            show_known_beacons()
            show_mac_whitelist()
            show_mac_blacklist()
            show_essid_whitelist()
            show_essid_blacklist()
            show_loud()
            hide_essid()
            hide_bssid()
            break;

        default:
            show_essid()
            show_bssid()
            show_client()
            show_fake_essids()
            show_stealth()
            show_channel()
            show_known_beacons()
            show_mac_whitelist()
            show_mac_blacklist()
            show_essid_whitelist()
            show_essid_blacklist()
            show_loud()
            break;
    }
}

function hide_essid(){
    $("#essid1").hide()
    $("label[for='essid1']").hide()
    $("#essid2").hide()
    $("label[for='essid2']").hide()
}

function show_essid(){
    $("#essid1").show()
    $("label[for='essid1']").show()
    $("#essid2").show()
    $("label[for='essid2']").show()
}

function hide_bssid(){
    $("#bssid").hide()
    $("label[for='bssid']").hide()
    $("#bssid1").hide()
    $("label[for='bssid1']").hide()
    $("#bssid2").hide()
    $("label[for='bssid2']").hide()
}

function show_bssid(){
    $("#bssid1").show()
    $("label[for='bssid1']").show()
    $("#bssid2").show()
    $("label[for='bssid2']").show()
    $("#bssid").show()
    $("label[for='bssid']").show()
}

function hide_client(){
    $("#client1").hide()
    $("label[for='client1']").hide()
    $("#client2").hide()
    $("label[for='client2']").hide()
}

function show_client(){
    $("#client1").show()
    $("label[for='client1']").show()
    $("#client2").show()
    $("label[for='client2']").show()
}

function hide_channel(){
    $("#channel").hide()
    $("label[for='channel']").hide()
}

function show_channel(){
    $("#channel").show()
    $("label[for='channel']").show()
}

function show_fake_essids(){
    $("#fake_essids").show()
    $("label[for='fake_essids']").show()
}

function hide_fake_essids(){
    $("#fake_essids").hide()
    $("label[for='fake_essids']").hide()
}

function show_stealth(){
    $("#stealth").show()
}

function hide_stealth(){
    $("#stealth").hide()
}

function show_loud(){
    $("#loud").show()
}

function hide_loud(){
    $("#loud").hide()
}

function hide_known_beacons(){
    $("#known_beacons").hide()
    $("label[for='known_beacons']").hide()
}

function show_known_beacons(){
    $("#known_beacons").show()
    $("label[for='known_beacons']").show()
}

function hide_mac_whitelist(){
    $("#mac_whitelist").hide()
    $("label[for='mac_whitelist']").hide()
}

function show_mac_whitelist(){
    $("#mac_whitelist").show()
    $("label[for='mac_whitelist']").show()
}

function hide_mac_blacklist(){
    $("#mac_blacklist").hide()
    $("label[for='mac_blacklist']").hide()
}

function show_mac_blacklist(){
    $("#mac_blacklist").show()
    $("label[for='mac_blacklist']").show()
}

function hide_essid_whitelist(){
    $("#essid_whitelist").hide()
    $("label[for='essid_whitelist']").hide()
}

function show_essid_whitelist(){
    $("#essid_whitelist").show()
    $("label[for='essid_whitelist']").show()
}

function hide_essid_blacklist(){
    $("#essid_blacklist").hide()
    $("label[for='essid_blacklist']").hide()
}

function show_essid_blacklist(){
    $("#essid_blacklist").show()
    $("label[for='essid_blacklist']").show()
}

var options_msgs = {
    "deauth_aireplay": "Deauthenticate a single client (sending a packet specifically for the client), clients inside an AP or clients of a ESSID (sending broadcast deuthentication packets). More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#deauthentication-packets'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#deauthentication-packets </a>",
    "deauth_mdk4": "Deauthenticate a single client, clients inside an AP or clients of a ESSID by discovering clients connected and sending deauthentication/disassociation packets to them. Stealth mode make match all Sequence Numbersand not send broadcast deauthentication packets. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#disassociation-packets'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#disassociation-packets </a>",
    "WIDS_confusion": "Confuse/Abuse Intrusion Detection and Prevention Systems by cross-connecting clients to multiple WDS nodes or fake rogue APs. If no stealth then it launch Zero_Chaos' WIDS exploit (authenticates clients from a WDS to foreign APs to make WIDS go nuts). More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4 </a>",
    "fake_aps": "Sends beacon frames to show fake APs at clients. This can sometimes crash network scanners and even drivers. If no stealth, then it uses also non-printable caracters in generated SSIDs and create SSIDs that break the 32-byte limit. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4 </a>",
    "reinject_data": "Sends authentication frames to all APs found in range. Too many clients can freeze or reset several APs. If stealth, then this test connects clients to the AP and reinjects sniffed data to keep them alive. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4 </a>",
    "EAPOL_DoS": "Floods an AP with EAPOL Start frames to keep it busy with fake sessions and thus disables it to handle any legitimate clients. Or logs off clients by injecting fake EAPOL Logoff messages. If stealth, use Logoff messages to kick clients. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4 </a>",
    "TKIP_DoS": "Sends random packets or re-injects duplicates on another QoS queue to provoke Michael Countermeasures on TKIP APs. AP will then shutdown for a whole minute, making this an effective DoS. If stealth, Use the new QoS exploit which only needs to reinject a few packets instead of the random packet injection, which is unreliable but works without QoS. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#more-dos-attacks-by-mdk4 </a>",
    "evil_twin": "Create a fake access point. You decide the authentication method, the name and the BSSID. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#evil-twin'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#evil-twin </a>",
    "mana": "Find the PNL of the devices and create fake APs with that ESSIDS. Select the authentication method. More info in <a href='https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#karma-mana-loud-mana-and-known-beacons-attack'> https://book.hacktricks.xyz/pentesting/pentesting-network/wifi-attacks#karma-mana-loud-mana-and-known-beacons-attack </a>",
    "airodump": "Capture handshakes in the indicated channel using airodump-ng.",
    "nullpin": "Some really bad implementations allowed the Null PIN to connect (very weird also). Reaver can test this (Bully cannot).",
    "pixiedust": "Try to find if the randomization is weak"
}


window.onload = hide_fields
