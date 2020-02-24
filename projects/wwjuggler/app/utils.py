from subprocess import Popen, PIPE
from time import sleep
import os, signal, glob, datetime

count_ps = 0
executing_procs = {}
wlans_being_used = []
main_iface = ""
current_path = os.path.dirname(os.path.abspath(__file__))
scripts_path = current_path + "/scripts/"
executing_path = current_path + "/executing/"
store_path = os.path.expanduser("~/.wwjuggler/")
current_store_path = store_path + datetime.datetime.now().strftime("%d-%m-%Y_%H:%M")
store_airodump = current_store_path + "/airodump_scan"

#############################
### Wlan ifaces functions ###
#############################
def get_wlan_interfaces():
    global main_iface
    wlans = {}
    up_wlans_interfaces()
    procs = get_procs()
    
    for w in os.popen('ip link show | grep -oP "wlan[\da-zA-Z]*"').read().split():
        wlans[w] = "Managed" if "Managed" in os.popen("iwconfig "+w+" | grep Mode").read() else "Monitor"
        if w == main_iface:
            wlans[w] = wlans[w] + "- Main in use"
            continue
        being_used = True if any( [ p["terminated"] == "Running" for p in procs if (w != main_iface and w in p["name"]) ] ) else False # True is a process using the interface is running
        if being_used:
            wlans[w] = wlans[w] + "- in use"
    return wlans

def up_wlans_interfaces():
    for iface in os.popen('ip link show | grep "DOWN" | grep -oP "wlan[\da-zA-Z]*"').read().split():
        os.system("ifconfig "+iface+" up")
    for iface in os.popen('ip link show | grep "DOWN" | grep -oP "wlan[\da-zA-Z]*"').read().split():
        os.system("nmcli device set "+iface+" managed yes")
        os.system("ifconfig "+iface+" up")



#############################
###### Clean functions ######
#############################
#def clean_exec_procs_dir():
#    fileList = glob.glob(executing_path+'/*.out')
#    fileList += glob.glob(executing_path+'/*.err')
#    for filePath in fileList:
#        try:
#            os.remove(filePath)
#        except:
#            print("Error while deleting file : ", filePath)

def clean_data(data):
    not_interesting = ["First time seen", "Last time seen", "Speed", "LAN IP", "ID-length"]
    for ni in not_interesting:
        data.pop(ni, None)
    return data



#############################
## Wifi scanning functions ##
#############################
def get_scan_results():
    if not os.path.isfile(store_airodump+'/wwjuggler-airo-01.csv'):
        return ([],[])

    with open(store_airodump+'/wwjuggler-airo-01.csv','r') as f:
        csv_content = f.read().splitlines()
    stations = []
    stations_header = [val.lstrip() for val in csv_content[1].strip().split(",")]
    clients = []
    clients_header = []
    actual = stations
    actual_header = stations_header
    is_client = False
    for line in csv_content[2:]:
        line = line.strip()
        if ("Probed ESSIDs") in line:
            actual = clients
            clients_header = [val.lstrip() for val in line.split(",")]
            actual_header = clients_header
            is_client = True
            continue
        actual.append({})
        if not is_client:
            line_splitted = line.split(",")
        else:
            line_splitted = line.split(",")[:6]+[",".join(line.split(",")[6:])]
        for i, value in enumerate(line_splitted):
            actual[-1][actual_header[i]] = value.replace("  ","").lstrip()
        actual[-1] = clean_data(actual[-1])
    
    return (stations, clients)


def get_macs_aps_clients():
    stations,clients = get_scan_results()
    stations_macs = list(set([ b["BSSID"] for b in stations if b["BSSID"]]))
    stations_macs.sort()
    essids = list(set([ b["ESSID"] for b in stations if "ESSID" in b.keys() and b["ESSID"]]))
    essids.sort()
    clients_macs = list(set([ c["Station MAC"] for c in clients if c["Station MAC"]]))
    clients_macs.sort()
    stations_macs.insert(0,"")
    essids.insert(0,"")
    clients_macs.insert(0,"")
    return stations_macs, essids, clients_macs


def restart_airo():
    global main_iface
    #Stop airodump
    for pid in os.popen("pgrep -f '/wwjuggler-airo'").read().splitlines():
        print("Kill airodump pid "+str(pid))
        os.kill(int(pid), signal.SIGTERM)

    #Delete previous airodumps
    fileList = glob.glob(store_airodump+'/wwjuggler-airo*')
    for filePath in fileList:
        try:
            os.remove(filePath)
        except:
            print("Error while deleting file : ", filePath)

    #Start airodump
    wlans = get_wlan_interfaces()
    if len(wlans) > 0:
        iface = list(wlans.keys())[0]
        main_iface = iface
        cmd = "airodump-ng --wps -w "+store_airodump+"/wwjuggler-airo --output-format csv --background 1 " + main_iface
        print("Executing airodump: "+cmd)
        Popen(cmd.split(" "))
        sleep(5)
    else:
        print("NO WLAN INTERFACE DETECTED!!!")



#############################
#Process management functions
#############################
def get_procs():
    global executing_procs
    
    files_executing = list(filter(os.path.isfile, glob.glob(current_store_path + "/*")))
    files_executing.sort(key=lambda x: os.path.getmtime(x))
    files_executing.reverse()

    procs = []
    for f in files_executing:
        f_err = f.replace(".out",".err")
        if ".out" in f:
            if int(os.path.getsize(f)) >= int(os.path.getsize(f_err)):
                procs.append({"name": f.split("/")[-1], "terminated": "Stopped" if not (executing_procs[f.split("/")[-1]].poll() is None) else "Running", "tail": os.popen('tail -n 20 ' + f).read()})
            else:
                procs.append({"name": f_err.split("/")[-1], "terminated": "Stopped" if not (executing_procs[f.split("/")[-1]].poll() is None) else "Running", "tail": os.popen('tail -n 20 ' + f_err).read()})
    
    return procs



#############################
###### Other functions ######
#############################
def my_execute(cmd, outfile, errfile, shell=False):
    global executing_procs, count_ps

    print("Executing: "+str(cmd))
    with open(outfile, "wb") as out, open(errfile, "wb") as err:
            proc = Popen(cmd, stdout=out, stderr=err, stdin=PIPE, shell=shell, preexec_fn=os.setsid)
        
    executing_procs[outfile.split("/")[-1]] = proc
    count_ps += 1