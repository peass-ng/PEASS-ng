from flask_wtf import FlaskForm
from wtforms import StringField, SelectField, IntegerField, BooleanField, SubmitField, PasswordField, TextAreaField, validators, SelectMultipleField
from wtforms.validators import DataRequired


class DoSForm(FlaskForm):
    option = SelectField(u'DoS Attacks', choices=[('deauth_mdk4', 'MDK4 Deauth'), ('deauth_aireplay', 'Aireplay Deauth'), ('WIDS_confusion', 'WIDS Confusion'), ('fake_aps', 'Fake APs'), ('reinject_data', 'DoS AP reinjecting data'), ('EAPOL_DoS', 'EAPOL_DoS'), ('TKIP_DoS', 'TKIP_DoS')], validators=[DataRequired()])
    interface = SelectField(u'Interface', choices=[], validators=[DataRequired()])
    essid1 = SelectField(u'ESSID1', choices=[], validators=(validators.Optional(),))
    essid2 = StringField(u'ESSID2', validators=(validators.Optional(),))
    bssid1 = SelectField(u'BSSID1', choices=[], validators=(validators.Optional(),))
    bssid2 = StringField(u'BSSID2', [
        validators.Optional(),
        validators.Regexp(r'^[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}$', message="Username must contain only letters numbers or underscore"),
        validators.Length(min=17, max=17, message="BSSID must be 17 chars length: 45:D2:28:33:B7:2D")
    ])
    client1 = SelectField(u'Client1', choices=[], validators=(validators.Optional(),))
    client2 = StringField(u'Client2', [
        validators.Optional(),
        validators.Regexp(r'^[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}$', message="Username must contain only letters numbers or underscore"),
        validators.Length(min=17, max=17, message="Client MAC must be 17 chars length: 45:D2:28:33:B7:2D")
    ])
    fake_essids = StringField(u'Fake essids', validators=(validators.Optional(),))
    time = IntegerField(u'Time', validators=(validators.Optional(),))
    channel = IntegerField(u'Channel', validators=(validators.Optional(),))
    stealth = BooleanField(u'Stealth')
    submit = SubmitField(u'Attack')

class ClientForm(FlaskForm):
    option = SelectField(u'Client Attack', choices=[('evil_twin', 'Evil Twin'), ('mana', 'Mana')], validators=[DataRequired()])
    interface = SelectField(u'Interface', choices=[], validators=[DataRequired()])
    auth = SelectField(u'Auth method', choices=[("open","open"),("wpa-psk","wpa-psk"),("wpa-eap","wpa-eap"),("owe","owe"),("owe-transition","owe-transition"),("owe-psk","owe-psk")], validators=[DataRequired()])
    essid1 = SelectField(u'ESSID1', choices=[], validators=(validators.Optional(),))
    essid2 = StringField(u'ESSID2', validators=(validators.Optional(),))
    bssid = StringField(u'BSSID', [
        validators.Optional(),
        validators.Regexp(r'^[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}:[\da-fA-F]{2}$', message="Username must contain only letters numbers or underscore"),
        validators.Length(min=17, max=17, message="BSSID must be 17 chars length: 45:D2:28:33:B7:2D")
    ], default="74:D0:2B:90:56:F2")
    wpa_version = SelectField(u'WPA version', choices=[("2","2"),("1","1")], validators=(validators.Optional(),))
    channel = IntegerField(u'Channel', validators=(validators.Optional(),))
    loud = BooleanField(u'Loud')
    known_beacons = StringField(u'Known Beacons', validators=(validators.Optional(),))
    mac_whitelist = SelectMultipleField(u'Mac Whitelist', choices=[], validators=(validators.Optional(),))
    mac_blacklist = SelectMultipleField(u'Mac Blacklist', choices=[], validators=(validators.Optional(),))
    essid_whitelist = SelectMultipleField(u'ESSID Whitelist', choices=[], validators=(validators.Optional(),))
    essid_blacklist = SelectMultipleField(u'ESSID Blacklist', choices=[], validators=(validators.Optional(),))
    submit = SubmitField(u'Attack')

class CreateCertForm(FlaskForm):
    cc = StringField(u'Please enter two letter country code for certs (i.e. US, FR)', default="US", validators=[DataRequired()])
    state = StringField(u'Please enter state or province for certs (i.e. Ontario, New Jersey)', default="California", validators=[DataRequired()])
    city = StringField(u'Please enter locale for certs (i.e. London, Hong Kong)', default="Los Angeles", validators=[DataRequired()])
    organization = StringField(u'Please enter organization for certs (i.e. Evil Corp)', default="Microsoft", validators=[DataRequired()])
    department = StringField(u'Please enter org unit for certs (i.e. Hooman Resource Says)', default="Human Resources", validators=[DataRequired()])
    email = StringField(u'Please enter email for certs (i.e. cyberz@h4x0r.lulz)', default="humanresources@microsoft.com", validators=[DataRequired()])
    cn = StringField(u'Please enter common name (CN) for certs.', default="microsoft.com", validators=[DataRequired()])
    submit = SubmitField(u'Create')

class HandshakeCapture(FlaskForm):
    option = SelectField(u'', choices=[("airodump","Airodump-ng")], validators=[DataRequired()])
    interface = SelectField(u'Interface', choices=[], validators=[DataRequired()])
    bssid = SelectField(u'BSSID', choices=[], validators=(validators.Optional(),))
    channel = IntegerField(u'Channel', validators=[DataRequired()])
    submit = SubmitField(u'Capture')

class ExecForm(FlaskForm):
    cmd = StringField(u'Command line to execute', default="whoami", validators=[DataRequired()])
    submit = SubmitField(u'Execute')

class WPSCracking(FlaskForm):
    option = SelectField(u'Option', choices=[("custompin", "Custom PIN"), ("nullpin", "Null PIN"), ("pixiedust", "Pixiedust"), ("bruteforce_wps", "Brute-Force")], validators=[DataRequired()])
    tool = SelectField(u'Tool', choices=[("reaver", "reaver"), ("bully", "bully")], validators=[DataRequired()])
    interface = SelectField(u'Interface', choices=[], validators=[DataRequired()])
    bssid = SelectField(u'BSSID', choices=[], validators=(validators.Optional(),))
    channel = IntegerField(u'Channel', validators=[DataRequired()])
    pin = IntegerField(u'Pin', validators=(validators.Optional(),))
    ignore_locks = BooleanField(u'Ignore Locks', default=True)
    submit = SubmitField(u'Crack')