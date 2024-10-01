using System.Collections.Generic;

namespace winPEAS.Info.ProcessInfo
{
    static class DefensiveProcesses
    {
        private static Dictionary<string, HashSet<string>> Definitions = new Dictionary<string, HashSet<string>>()
        {
            { "ALYac", new HashSet<string>() {  "alyac.exe",  "aylaunch.exe",  "asmsetup.exe",  } },
            { "AVG Antivirus", new HashSet<string>() {  "avgui.exe",  } },
            { "AVG", new HashSet<string>() {  "avgemc.exe",  "afwserv.exe",  "avgsvc.exe",  "aswidsagent.exe",  } },
            { "Ad-Aware Total Security by Lavasoft", new HashSet<string>() {  "ffcachetool.exe",  "avktray.exe",  "gdsc.exe",  "bootcdwizard.exe",  "avkservice.exe",  "ask.exe",  "avkwctlx64.exe",  "gdfwadmin.exe",  "avktuner.exe",  "initinst.exe",  "gdfwsvc.exe",  "avk.exe",  "avkwscpe.exe",  "avkwctl.exe",  "avktunerservice.exe",  "mkisofs.exe",  "gdfirewalltray.exe",  "initinstx64.exe",  "gdgadgetinst32.exe",  "gdfwsvcx64.exe",  "aawtray.exe",  } },
            { "AhnLab-V3", new HashSet<string>() {  "aup80if.ex",  "v3ui.exe",  "v3medic.exe",  "v3lite.exe",  "v3l4cli.exe",  } },
            { "Antiy-AVL", new HashSet<string>() {  "avl.exe",  } },
            { "Arcabit", new HashSet<string>() {  "arcavir.exe",  "arcaconfsv.exe",  "arcabit.core.loggingservice.exe",  "arcabit.core.configurator2.exe",  "arcabit.exe",  } },
            { "Avast Antivirus", new HashSet<string>() {  "avastui.exe",  } },
            { "Avast", new HashSet<string>() {  "avast-antivirus.exe",  "avastsvc.exe",  "ashserv.exe",  } },
            { "Avira", new HashSet<string>() {  "avira.webapphost.exe",  } },
            { "Baidu", new HashSet<string>() {  "bav.exe",  "bavcloud.exe",  "bavhm.exe",  "bavsvc.exe",  "bavtray.exe",  "bavupdater.exe",  "bavbsreport.exe",  } },
            { "BitDefender", new HashSet<string>() {  "epprotectedservice.exe",  "epsecurityservice.exe",  "epupdateservice.exe",  "epupdateserver.exe",  "bdagent.exe",  } },
            { "Bkav Pro", new HashSet<string>() {  "bkavutil.exe",  "bkav.exe",  "bkavpro.exe",  "bkavservice.exe",  } },
            { "CMC", new HashSet<string>() {  "cmcpanel.exe",  "cmccore.exe",  "cmctrayicon.exe",  } },
            { "Cisco", new HashSet<string>() {  "sfc.exe",  } },
            { "ClamAV", new HashSet<string>() {  "clamscan.exe",  "freshclam.exe",  } },
            { "Comodo", new HashSet<string>() {  "cavwp.exe",  "cfp.exe",  } },
            { "CrowdStrike Falcon", new HashSet<string>() {  "falconsensorwinos.exe",  } },
            { "Cybereason", new HashSet<string>() {  "cybereasonransomfreeservicehost.exe",  } },
            { "Cylance", new HashSet<string>() {  "cylancesvc.exe",  } },
            { "Cynet", new HashSet<string>() {  "cynet.exe",  "cexplore.exe",  "cynet.zerologondetector.exe",  } },
            { "Cyradar", new HashSet<string>() {  "cyradarexecutorservices.exe",  "cyradaredr.exe",  "cyradares.exe",  } },
            { "DrWeb", new HashSet<string>() {  "dwscancl.exe",  "drwebsettingprocess.exe",  "dwsysinfo.exe",  "drwupsrv.exe",  "dwnetfilter.exe",  "dwscanner.exe",  "dwservice.exe",  "frwl_notify.exe",  "frwl_svc.exe",  "spideragent.exe",  "spideragent_adm.exe",  } },
            { "ESET-NOD32", new HashSet<string>() {  "eraagent.exe",  "shouldiremoveit.com",  "ecmd.exe",  "egui.exe",  } },
            { "F-Secure", new HashSet<string>() {  "fsav32.exe",  "fsdfwd.exe",  "fsguiexe.exe",  "fsav.exe",  } },
            { "G Data AntiVirus", new HashSet<string>() {  "bootcdwizard.exe",  "avkservice.exe",  "avktray.exe",  "gdgadgetinst32.exe",  "ransomwareremovalhelper.exe",  "gdlog.exe",  "sec.exe",  "avkwctlx64.exe",  "updategui.exe",  "avk.exe",  "autorundelayloader.exe",  "avkcmd.exe",  "avkwscpe.exe",  "iupdateavk.exe",  } },
            { "GridinSoft Anti-Malware", new HashSet<string>() {  "uninst.exe",  "gtkmgmtc.exe",  "tkcon.exe",  "unpacker.exe",  } },
            { "IObit Malware Fighter 3", new HashSet<string>() {  "imfantivirususb.exe",  "actioncenterdownloader.exe",  "adsremovalsetup.exe",  "feedback.exe",  "iobituninstal.exe",  "sendbugreport.exe",  "imf_iobitdel.exe",  "imfantivirustips.exe",  "promote.exe",  "imfupdater.exe",  "imf_actioncenterdownloader.exe",  "imfregister.exe",  "reprocess.exe",  "imfsrv_iobitdel.exe",  "liveupdate.exe",  "xmaspromote.exe",  "spsetup.exe",  "imf_downconfig.exe",  "uninstallpromote.exe",  "bluebirdinit.exe",  "imftips.exe",  "locallang.exe",  "imfinstaller.exe",  "aupdate.exe",  "startmenu.exe",  "iwsimfxp.exe",  "ppuninstaller.exe",  "taskschedule.exe",  "fixplugin.exe",  "imfantivirusfix.exe",  "imfbigupgrade.exe",  "imftips_iobitdel.exe",  "imfsrv.exe",  "iobitcommunities.exe",  "autoupdate.exe",  "unins000.exe",  "homepage.exe",  } },
            { "IObit Malware Fighter 6", new HashSet<string>() {  "iwsimf_av.exe",  "imfantivirususb.exe",  "feedback.exe",  "sendbugreportnew.exe",  "ransomware.exe",  "imfantivirustips.exe",  "imfdbupdatestat.exe",  "imf_actioncenterdownloader.exe",  "iwsimf.exe",  "browserprotect.exe",  "driverscan.exe",  "imfregister.exe",  "reprocess.exe",  "liveupdate.exe",  "christmas.exe",  "bf.exe",  "imf_downconfig.exe",  "browsercleaner.exe",  "antitracking.exe",  "bluebirdinit.exe",  "imftips.exe",  "imfinstaller.exe",  "locallang.exe",  "carescan.exe",  "imfsrvwsc.exe",  "safebox.exe",  "aupdate.exe",  "iobitliveupdate.exe",  "imfchecker.exe",  "iwsimfxp.exe",  "ppuninstaller.exe",  "imfantivirusfix.exe",  "imfbigupgrade.exe",  "exclusivepsimf.exe",  "imfanalyzer.exe",  "bfimf.exe",  "imfsrv.exe",  "autoupdate.exe",  "spinit.exe",  "homepage.exe",  "dugtrio.exe",  } },
            { "IObit Security 360", new HashSet<string>() {  "is360tray.exe",  "is360init.exe",  "is360srv.exe",  "e_privacysweeper.exe",  "a_hijackscan.exe",  "g_portable.exe",  "d_powerfuldelete.exe",  "b_securityholes.exe",  "is360updater.exe",  "unins000.exe",  "f_pctuneup.exe",  "imf_freesoftwaredownloader.exe",  "c_passivedefense.exe",  } },
            { "K7AntiVirus Plus by K7 Computing Pvt Ltd", new HashSet<string>() {  "healthmon.exe",  "k7avqrnt.exe",  "k7tliehistory.exe",  "k7tlusbvaccine.exe",  "k7tsalrt.exe",  "k7tlwintemp.exe",  "k7tlinettemp.exe",  "k7tshlpr.exe",  "k7disinfectorgui.exe",  "k7tlvirtkey.exe",  "k7tlmtry.exe",  "k7fwsrvc.exe",  "k7tsecurity.exe",  "k7avmscn.exe",  "k7ctscan.exe",  "k7tsecurityuninstall.exe",  "k7rtscan.exe",  "k7avscan.exe",  "k7crvsvc.exe",  "k7tsdbg.exe",  "k7emlpxy.exe",  } },
            { "K7AntiVirus Premium by K7 Computing Pvt Ltd", new HashSet<string>() {  "k7quervarcleaningtool.exe",  "k7ndfhlpr.exe",  "healthmon.exe",  "k7avqrnt.exe",  "k7tliehistory.exe",  "k7tlusbvaccine.exe",  "k7tsstart.exe",  "k7tsalrt.exe",  "k7tlwintemp.exe",  "k7mebezatencremovaltool.exe",  "k7tlinettemp.exe",  "k7tsmain.exe",  "k7tshlpr.exe",  "k7tssplh.exe",  "k7disinfectorgui.exe",  "k7tlvirtkey.exe",  "k7tlmtry.exe",  "k7fwsrvc.exe",  "k7tsreminder.exe",  "k7tsecurity.exe",  "k7avmscn.exe",  "k7ctscan.exe",  "k7rtscan.exe",  "k7tsnews.exe",  "k7avscan.exe",  "k7crvsvc.exe",  "k7emlpxy.exe",  "k7tsupdt.exe",  } },
            { "Kaspersky Anti-Ransomware Tool for Business", new HashSet<string>() {  "anti_ransom_gui.exe",  "dump_writer_agent.exe",  "anti_ransom.exe",  } },
            { "Kaspersky Anti-Virus 2011", new HashSet<string>() {  "kldw.exe",  } },
            { "Kaspersky Anti-Virus 2013", new HashSet<string>() {  "ffcert.exe",  } },
            { "Kaspersky Anti-Virus Personal", new HashSet<string>() {  "kavsend.exe",  "kavsvc.exe",  "getsysteminfo.exe",  "uninstall.exe",  } },
            { "Kaspersky Antivirus", new HashSet<string>() {  "avp.exe",  } },
            { "Kaspersky", new HashSet<string>() {  "klnagent.exe",  } },
            { "Malwarebytes", new HashSet<string>() {  "mbam.exe",  "mbar.exe",  "mbae.exe",  } },
            { "McAfee All Access – AntiVirus Plus", new HashSet<string>() {  "compatibilitytester.exe",  "mispreg.exe",  "mcods.exe",  "mcvsmap.exe",  "mcocrollback.exe",  "mpfalert.exe",  "mcvulalert.exe",  "mvsinst.exe",  "mcupdmgr.exe",  "mcpvtray.exe",  "mcvuladmagnt.exe",  "mcvulunpk.exe",  "qcshm.exe",  "mcoemmgr.exe",  "qcconsol.exe",  "mcuihost.exe",  "mcvsshld.exe",  "mcinstru.exe",  "mcvulcon.exe",  "mcsync.exe",  "firesvc.exe",  "qccons32.exe",  "mcsvrcnt.exe",  "mcvulusragnt.exe",  "shrcl.exe",  "mcodsscan.exe",  "mcapexe.exe",  "mcautoreg.exe",  "mcinfo.exe",  "mcvulctr.exe",  "svcdrv.exe",  } },
            { "McAfee AntiSpyware", new HashSet<string>() {  "msssrv.exe",  "mcspy.exe",  "msscli.exe",  } },
            { "McAfee AntiVirus Plus", new HashSet<string>() {  "mispreg.exe",  "mcvsmap.exe",  "mcods.exe",  "mcactinst.exe",  "mcocrollback.exe",  "mpfalert.exe",  "mcinsupd.exe",  "langsel.exe",  "mvsinst.exe",  "mcshell.exe",  "mfehidin.exe",  "mchlp32.exe",  "mcupdmgr.exe",  "saupd.exe",  "uninstall.exe",  "mcawfwk.exe",  "qcshm.exe",  "mcsacore.exe",  "mcoemmgr.exe",  "qcconsol.exe",  "mcuihost.exe",  "mcinstru.exe",  "mcvsshld.exe",  "mcoobeof.exe",  "mcsync.exe",  "firesvc.exe",  "qccons32.exe",  "saui.exe",  "mcsvrcnt.exe",  "shrcl.exe",  "mcsmtfwk.exe",  "mcautoreg.exe",  "mcuninst.exe",  "mcinfo.exe",  "actutil.exe",  } },
            { "McAfee Antivirus", new HashSet<string>() {  "mcafee.exe",  } },
            { "NANO Antivirus beta by Nano Security Ltd", new HashSet<string>() {  "nanoreportc64.exe",  "nanorst.exe",  "uninstall.exe",  "nanoreport.exe",  "nanosvc.exe",  "nanoav64.exe",  "nanoreportc.exe",  } },
            { "NANO-Antivirus", new HashSet<string>() {  "nanoav.exe",  } },
            { "Norton Antivirus", new HashSet<string>() {  "nortonsecurity.exe",  } },
            { "PCMatic", new HashSet<string>() {  "pcmaticpushcontroller.exe",  "pcmaticrt.exe",  } },
            { "Panda Security", new HashSet<string>() {  "psanhost.exe",  } },
            { "Panda", new HashSet<string>() {  "avengine.exe",  } },
            { "Quick Heal AntiVirus Pro", new HashSet<string>() {  "delnboot.exe",  "0000007c_afupdfny.exe",  "asmain.exe",  "asclsrvc.exe",  "acappaa.exe",  "activate.exe",  } },
            { "Quick Heal Total Security", new HashSet<string>() {  "delnboot.exe",  "contact.exe",  "activate.exe",  "acappaa.exe",  } },
            { "Sophos Anti-Rootkit 1.5.0", new HashSet<string>() {  "helper.exe",  "svrtcli.exe",  "sctcleanupservice.exe",  "native.exe",  "svrtservice.exe",  "svrtgui.exe",  "sarcli.exe",  "sctboottasks.exe",  } },
            { "Sophos Anti-Virus", new HashSet<string>() {  "sav32cli.exe",  "savprogress.exe",  "savservice.exe",  "native.exe",  "swi_di.exe",  "backgroundscanclient.exe",  "savmain.exe",  "forceupdatealongsidesgn.exe",  "swc_service.exe",  "savproxy.exe",  "savcleanupservice.exe",  "savadminservice.exe",  } },
            { "Symantec Endpoint Protection", new HashSet<string>() {  "ccsvchst.exe",  } },
            { "Symantec", new HashSet<string>() {  "sepwscsvc64.exe",  } },
            { "Total Defense Anti-Virus", new HashSet<string>() {  "caoscheck.exe",  "ccprovsp.exe",  "caschelp.exe",  "caisstutorial.exe",  "ccwatcher.exe",  "cawsc.exe",  "ccevtmgr.exe",  "ccprovep.exe",  "casc.exe",  "cclogconfig.exe",  "ccschedulersvc.exe",  "cckasubmit.exe",  "ccproxysrvc.exe",  "caunst.exe",  } },
            { "Trend micro", new HashSet<string>() {  "uiwinmgr.exe",  "ntrtscan.exe",  "tmntsrv.exe",  "pccpfw.exe",  } },
            { "VIPRE Advanced Security by ThreatTrack Security", new HashSet<string>() {  "sbamtray.exe",  "sbamwsc.exe",  "sbamcommandlinescanner.exe",  "sbamcreaterestore.exe",  "sbamsvc.exe",  "avcproxy.exe",  "sbbd.exe",  } },
            { "VIPRE Antivirus by GFI Software", new HashSet<string>() {  "sbamtray.exe",  "sbsetupdrivers.exe",  "sbamsafemodeui.exe",  "sbpimsvc.exe",  "sbamwsc.exe",  "sbrc.exe",  "sfe.exe",  "sbagentdiagnostictool.exe",  "sbamcommandlinescanner.exe",  "sbamsvc.exe",  "sbamcreaterestore.exe",  "sbamui.exe",  } },
            { "ViRobot Anti-Ransomware by HAURI", new HashSet<string>() {  "vrbbdsvc.exe",  "uninstall.exe",  "vrbbdlogviewer.exe",  "vrbbdbackup.exe",  "vrpuller.exe",  } },
            { "ViRobot Internet Security 2011 by HAURI", new HashSet<string>() {  "hvrpcuselock.exe",  "hvrlogview.exe",  "hvreasyrobot.exe",  "hvrsetup.exe",  "hvrfilewipe.exe",  "hvrmalsvc.exe",  "hvrtrafficviewer.exe",  "hvrscan.exe",  "hvrcontain.exe",  "hvrquarantview.exe",  "hvrtray.exe",  } },
            { "Webroot", new HashSet<string>() {  "wrsa.exe",  } },
            { "Windows defender", new HashSet<string>() {  "msmpeng.exe",  "mpcmdrun.exe",  "msascuil.exe",  "windefend.exe",  "msascui.exe",  "msmpsvc.exe",  } },
            { "Zillya Internet Security by ALLIT Service", new HashSet<string>() {  "drvcmd.exe",  "ziscore.exe",  "keyboard.exe",  "systemresearchtool.exe",  "zis.exe",  "zisnet.exe",  "conscan.exe",  "zisupdater.exe",  "zisaux.exe",  "ziships.exe",  } },
            { "Zillya! Antivirus by ALLIT Service", new HashSet<string>() {  "wscmgr.exe",  "drvcmd.exe",  "zillya.exe",  "zavaux.exe",  "reporter.exe",  "autoruntool.exe",  "taskmanagertool.exe",  } },
            { "Zillya! Internet Security by ALLIT Service", new HashSet<string>() {  "restoretool.exe",  "drvcmd.exe",  "wscmgr.exe",  "zefcore.exe",  "zefsvc.exe",  "fwdisabler.exe",  "zefaux.exe",  "backuphostfile.exe",  "conscanner.exe",  "reporter.exe",  "autoruntool.exe",  "zef.exe",  "taskmanagertool.exe",  } },
            { "ZoneAlarm Anti-Ransomware by Check Point Software", new HashSet<string>() {  "zup.exe",  "consrvhost.exe",  "zaarupdateservice.exe",  "zaar.exe",  "sbacipollasrvhost.exe",  "uninst.exe",  } },
            { "ZoneAlarm Antivirus by Check Point, Inc", new HashSet<string>() {  "threatemulation.exe",  "multiscan.exe",  "restoreutility.exe",  "vsmon.exe",  "zatray.exe",  "multifix.exe",  } },
            { "ZoneAlarm by Check Point, Inc", new HashSet<string>() {  "instmtdr.exe",  "zatutor.exe",  "cpes_clean.exe",  "multiscan.exe",  "zauninst.exe",  "zlclient.exe",  "multifix.exe",  } }
        };

        // reverse lookup list
        public static Dictionary<string, HashSet<string>> AVVendorsByProcess = new Dictionary<string, HashSet<string>>();

        static DefensiveProcesses()
        {
            // initialize the structure here
            foreach (var kvp in Definitions)
            {
                var vendor = kvp.Key;

                foreach (var executable in kvp.Value)
                {
                    var sanitizedExecutable = executable.Trim().ToLower();

                    if (!AVVendorsByProcess.ContainsKey(sanitizedExecutable))
                    {
                        AVVendorsByProcess.Add(sanitizedExecutable, new HashSet<string>() { vendor });
                    }
                    else
                    {
                        AVVendorsByProcess[sanitizedExecutable].Add(vendor);
                    }
                }
            }
        }
    }
}
