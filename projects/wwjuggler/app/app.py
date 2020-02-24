from flask import Flask, render_template, flash, redirect, url_for, request
app = Flask(__name__)

from flask_httpauth import HTTPBasicAuth
from flask_bootstrap import Bootstrap
bootstrap = Bootstrap(app)
from werkzeug.security import generate_password_hash, check_password_hash
#from app import app
from app.forms import DoSForm, ClientForm, CreateCertForm, HandshakeCapture, ExecForm, WPSCracking
from app.utils import *

import os, glob, signal
from subprocess import Popen, PIPE
from time import sleep

app.config['SECRET_KEY'] = os.urandom(32)

auth = HTTPBasicAuth()
users = {
    "hacker": generate_password_hash("wwjuggler"),
}


@auth.verify_password
def verify_password(username, password):
    if username in users:
        return check_password_hash(users.get(username), password)
    return False


@auth.login_required
@app.route('/change_wlan_mode/<string:wiface>', methods=['GET'])
def change_wlan_mode(wiface):
    wlans = get_wlan_interfaces()
    print(wiface)
    if wiface in wlans:
        if wlans[wiface] == "Managed":
            #p = Popen(("iwconfig '"+wiface+"' mode monitor").split(" "), stdout=PIPE, stderr=PIPE)
            p = Popen(("airmon-ng start '"+wiface).split(" "), stdout=PIPE, stderr=PIPE)
        else:
            #p = Popen(("iwconfig '"+wiface+"' mode managed").split(" "), stdout=PIPE, stderr=PIPE)
            p = Popen(("airmon-ng stop '"+wiface).split(" "), stdout=PIPE, stderr=PIPE)
        stdout, stderr = p.communicate()

        if stdout:
            flash(stdout.decode("utf-8"))
        if stderr:
            flash("STDERR: "+stderr.decode("utf-8"))
    return render_template('index.html', wlans=get_wlan_interfaces())



@app.route('/')
@app.route('/index')
@auth.login_required
def index():
    wlans = get_wlan_interfaces()
    return render_template('index.html', wlans=wlans)



@auth.login_required
@app.route('/scan_results')
def scan_results():
    stations,clients = get_scan_results()
    return render_template('scan_results.html', aps=stations, clients=clients)


@auth.login_required
@app.route('/dos', methods=['GET', 'POST'])
def dos():
    station_macs, essids, clients_macs = get_macs_aps_clients()
    form = DoSForm(request.form)
    form.interface.choices = [(wlan, "{} ({})".format(wlan, mode)) for wlan,mode in get_wlan_interfaces().items()]
    form.essid1.choices = [(e, e) for e in essids]
    form.bssid1.choices = [(b, b) for b in station_macs]
    form.client1.choices = [(c, c) for c in clients_macs]

    if form.validate_on_submit():
        essid = form.essid2.data if form.essid2.data else ( form.essid1.data if form.essid1.data else "")
        bssid = form.bssid2.data if form.bssid2.data else ( form.bssid1.data if form.bssid1.data else "")
        client = form.client2.data if form.client2.data else ( form.client1.data if form.client1.data else "")
        
        exec_msg = "Executing " + form.option.data + " in interface " + form.interface.data
        cmd = scripts_path+"/DoS.sh -o "+ form.option.data + " -i " + form.interface.data
        if essid:
            exec_msg += ", against ESSID " + essid
            cmd += " -e \"" + essid + "\""
        if bssid:
            exec_msg += ", against BSSID " + bssid
            cmd += " -b " + bssid
        if client:
            exec_msg += ", against Client " + client
            cmd += " -m " + client
        if form.channel.data:
            exec_msg += " in Channel " + str(form.channel.data)
            cmd += " -c " + str(form.channel.data)
        if form.fake_essids.data:
            exec_msg += " anouncing fake ESSIDS ("+form.fake_essids.data+")"
            cmd += " -f \"" + form.fake_essids.data + "\""
        if form.time.data:
            exec_msg += " during " + str(form.time.data) + "s"
            cmd += " -t " + str(form.time.data)
        else:
            exec_msg += " indefinitely"
        if form.stealth.data:
            exec_msg += " and stealthy"
            cmd += " -s"
        flash(exec_msg)

        outfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".out"
        errfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".err"
        my_execute(cmd, outfile, errfile, True)

        return redirect(url_for('console'))

    return render_template('form.html', formtype="DoS Attack", form=form)


@auth.login_required
@app.route('/client', methods=['GET', 'POST'])
def client():
    station_macs, essids, clients_macs = get_macs_aps_clients()
    form = ClientForm(request.form)
    form.interface.choices = [(wlan, "{} ({})".format(wlan, mode)) for wlan,mode in get_wlan_interfaces().items()]
    form.essid_whitelist.choices = [(e, e) for e in essids]
    form.essid_blacklist.choices = [(e, e) for e in essids]
    form.mac_whitelist.choices = [(c, c) for c in clients_macs]
    form.mac_blacklist.choices = [(c, c) for c in clients_macs]

    if form.validate_on_submit():
        essid = form.essid2.data if form.essid2.data else ( form.essid1.data if form.essid1.data else "")
        
        exec_msg = "Executing " + form.option.data + " in interface " + form.interface.data + " using as authentication "+form.auth.data
        cmd = scripts_path+"/Client.sh -o "+ form.option.data + " -i " + form.interface.data + " -a " + form.auth.data
        if form.wpa_version.data:
            exec_msg += "(" + form.wpa_version.data +")"
            cmd += " -w " + form.wpa_version.data
        if essid:
            exec_msg += ", as ESSID " + essid
            cmd += " -e " + essid
        if form.bssid.data:
            exec_msg += ", as BSSID " + form.bssid.data
            cmd += " -e " + essid
        if form.channel.data:
            exec_msg += " in Channel " + str(form.channel.data)
            cmd += " -c " + str(form.channel.data)
        if form.loud.data:
            exec_msg += " (Loud mode)"
            cmd += " -l"
        if form.known_beacons.data:
            exec_msg += " - Known Beacons declared"
            cmd += " -k " + ",".join(form.known_beacons.data)
        if form.mac_whitelist.data:
            exec_msg += " - Mac Whitelist declared"
            cmd += " -p " + ",".join(form.mac_whitelist.data)
        if form.mac_blacklist.data:
            exec_msg += " - Mac Blacklist declared"
            cmd += " -v " + ",".join(form.mac_blacklist.data)
        if form.essid_whitelist.data:
            exec_msg += " - ESSID Whitelist declared"
            cmd += " -n " + ",".join(form.essid_whitelist.data)
        if form.essid_blacklist.data:
            exec_msg += " - ESSID Blacklist declared"
            cmd += " -m " + ",".join(form.essid_blacklist.data)
        flash(exec_msg)

        outfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".out"
        errfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".err"
        my_execute(cmd.split(" "), outfile, errfile)

        return redirect(url_for('console'))

    return render_template('form.html', formtype="Client Attack", form=form)


@auth.login_required
@app.route('/create_cert', methods=['GET', 'POST'])
def create_cert():
    form = CreateCertForm(request.form)

    if form.validate_on_submit():        
        exec_msg = "Creating certificate with CC: "+form.cc.data+", State:"+form.state.data+", City: "+form.city.data+", Organization: "+form.organization.data+", Department: "+form.department.data+", Email"+form.email.data+", CN:"+form.cn.data
        cmd = scripts_path+"/Create_cert.sh '{}' '{}' '{}' '{}' '{}' '{}' '{}'".format(form.cc.data, form.state.data, form.city.data, form.organization.data, form.department.data, form.email.data, form.cn.data)
        flash(exec_msg)

        outfile = current_store_path+"/create_cert"+str(count_ps)+".out"
        errfile = current_store_path+"/create_cert"+str(count_ps)+".err"
        my_execute(cmd, outfile, errfile, True)
        return redirect(url_for('console'))

    return render_template('form.html', formtype="Create EAP Certificate", form=form)


@auth.login_required
@app.route('/wps_cracking', methods=['GET', 'POST'])
def wps_cracking():
    station_macs, essids, clients_macs = get_macs_aps_clients()
    form = WPSCracking(request.form)
    form.interface.choices = [(wlan, "{} ({})".format(wlan, mode)) for wlan,mode in get_wlan_interfaces().items()]
    form.bssid.choices = [(b, b) for b in station_macs]

    if form.validate_on_submit():        
        exec_msg = "WPS cracking with option "+form.option.data+" against BSSID "+form.bssid.data+" in channel "+str(form.channel.data)
        cmd = scripts_path+"/WPS.sh -t {} -i '{}' -b '{}' -c '{}' -o '{}'".format(form.tool.data, form.interface.data, form.bssid.data, form.channel.data, form.option.data)
        
        if form.ignore_locks.data:
            exec_msg += "(ignore LOCK)"
            cmd += " -l"
        
        if form.pin.data:
            exec_msg += ", PIN: "+str(form.pin.data)
            cmd += " -p "+str(form.pin.data)

        if form.option.data == "nullpin":
            cmd.replace("bully", "reaver")

        flash(exec_msg)

        outfile = current_store_path+"/"+form.interface.data+"-"+str(count_ps)+".out"
        errfile = current_store_path+"/"+form.interface.data+"-"+str(count_ps)+".err"
        my_execute(cmd, outfile, errfile, True)
        return redirect(url_for('console'))

    return render_template('form.html', formtype="WPS Cracking", form=form)


@auth.login_required
@app.route('/capture_handshake', methods=['GET', 'POST'])
def capture_handshake():
    station_macs, essids, clients_macs = get_macs_aps_clients()
    form = HandshakeCapture(request.form)
    form.interface.choices = [(wlan, "{} ({})".format(wlan, mode)) for wlan,mode in get_wlan_interfaces().items()]
    form.bssid.choices = [(b, b) for b in station_macs]

    if form.validate_on_submit():        
        exec_msg = "Capturing handshakes using interface " + form.interface.data + " of bssid " + form.bssid.data + " in channel "+str(form.channel.data)
        cmd = scripts_path+"/Capture_handshakes.sh " + form.interface.data + " " +str(form.channel.data) + " " + form.bssid.data + " " + current_store_path + "/psk"
        flash(exec_msg)

        outfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".out"
        errfile = current_store_path+"/"+form.interface.data+"-"+form.option.data+str(count_ps)+".err"
        my_execute(cmd.split(" "), outfile, errfile)
        return redirect(url_for('console'))

    return render_template('form.html', formtype="Capture Handshakes", form=form)


@auth.login_required
@app.route('/execute', methods=['GET', 'POST'])
def execute():
    form = ExecForm()

    if form.validate_on_submit():        
        exec_msg = "Going to execute: " + form.cmd.data 
        flash(exec_msg)

        outfile = current_store_path+"/execute"+str(count_ps)+".out"
        errfile = current_store_path+"/execute"+str(count_ps)+".err"
        my_execute(form.cmd.data , outfile, errfile, True)
        sleep(1)#So when you gets to console the command is probably executed already
        return redirect(url_for('console'))

    return render_template('form.html', formtype="Execute", form=form)


@auth.login_required
@app.route('/console')
def console():
    procs = get_procs()
    return render_template('console.html', procs=procs)


@auth.login_required
@app.route('/kill/<string:file_name>', methods=['GET'])
def kill(file_name):
    global executing_procs
    procs = get_procs()

    if ".err" in file_name:
        file_name=file_name.replace(".err",".out")
        
    if file_name in executing_procs and any([p["terminated"] == "Running" for p in procs if p["name"] == file_name]): #Check that the process is still running
        pid = executing_procs[file_name].pid
        try:
            if not "mana" in file_name and not "evil_twin" in file_name:
                os.killpg(pid, signal.SIGTERM)
            else:
                executing_procs[file_name].communicate(input=b"\n")
                sleep(4)
            flash("Process terminated ("+file_name.split(".")[0]+") with PID "+str(pid)+".")
        except Exception as e:
            print(e)
    
    #if os.path.exists(current_store_path+"/"+file_name.split(".")[0]+".out"):
    #    os.remove(current_store_path+"/"+file_name.split(".")[0]+".out")
    #if os.path.exists(current_store_path+"/"+file_name.split(".")[0]+".err"):
    #    os.remove(current_store_path+"/"+file_name.split(".")[0]+".err")

    return redirect(url_for('console'))


@auth.login_required
@app.route('/killall', methods=['GET'])
def killall():
    global executing_procs
    procs = get_procs()

    for file_name in executing_procs:
        if any([p["terminated"] == "Running" for p in procs if p["name"] == file_name]): #Check that the process is still running
            pid = executing_procs[file_name].pid
            try:
                if not "mana" in file_name and not "evil_twin" in file_name:
                    os.killpg(pid, signal.SIGTERM)
                else:
                    executing_procs[file_name].communicate(input=b"\n")
                    sleep(4)
                flash("Process terminated ("+file_name.split(".")[0]+") with PID "+str(pid)+".")
            except:
                pass

            #if os.path.exists(current_store_path+"/"+file_name.split(".")[0]+".out"):
            #    os.remove(current_store_path+"/"+file_name.split(".")[0]+".out")
            #if os.path.exists(current_store_path+"/"+file_name.split(".")[0]+".err"):
            #    os.remove(current_store_path+"/"+file_name.split(".")[0]+".err")
    
    executing_procs = {}

    return redirect(url_for('console'))


@auth.login_required
@app.route('/restart_airodump', methods=['POST'])
def restart_airodump():
    restart_airo()
    return redirect(url_for('index'))


@auth.login_required
@app.route('/reboot', methods=['POST'])
def reboot():
    os.system("reboot")



##################################################
################ INITIAL ACTIONS #################
##################################################
#clean_exec_procs_dir()              #Start cleaning other executions
if not os.path.exists(store_path):  #Create main dir if it doesn't exit (1st run)
    os.mkdir(store_path)
if not os.path.exists(current_store_path):  #Create main dir if it doesn't exit (1st run)
    os.mkdir(current_store_path)
if not os.path.exists(store_airodump):  #Create main dir if it doesn't exit (1st run)
    os.mkdir(store_airodump)
    sleep(0.5)

restart_airo()                      #"Restart" any airodump to capture packets