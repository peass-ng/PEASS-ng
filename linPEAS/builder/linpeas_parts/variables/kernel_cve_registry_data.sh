# Title: Variables - kernel_cve_registry_data
# ID: kernel_cve_registry_data
# Author: Carlos Polop
# Last Update: 25-02-2026
# Description: Embedded kernel exploit matching datasets extracted from linux-exploit-suggester and linux-exploit-suggester-2 examples. Data is split across KERNEL_CVE_DATA_1..X with a maximum of 25 rows per env variable. This file also stores reference-only CVE tokens found in example repos when no explicit suggester matching rule exists.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $KERNEL_CVE_DATA_1, $KERNEL_CVE_DATA_2, $KERNEL_CVE_DATA_3, $KERNEL_CVE_DATA_4, $KERNEL_CVE_DATA_5, $KERNEL_CVE_DATA_6, $KERNEL_CVE_DATA_7, $KERNEL_CVE_DATA_8, $KERNEL_CVE_DATA_9, $KERNEL_CVE_DATA_10, $KERNEL_CVE_DATA_11, $KERNEL_CVE_DATA_12, $KERNEL_CVE_DATA_13, $KERNEL_CVE_DATA_14, $KERNEL_CVE_DATA_15, $KERNEL_CVE_DATA_16, $KERNEL_CVE_DATA_17, $KERNEL_CVE_DATA_18, $KERNEL_CVE_DATA_19, $KERNEL_CVE_DATA_20, $KERNEL_CVE_DATA_21
# Fat linpeas: 0
# Small linpeas: 1


# Max 25 rows per env variable to avoid hitting env variable size limits.
KERNEL_CVE_DATA_1="$(cat <<'EOF_DATA_1'
CVE-2004-1235	elflbl	pkg=linux-kernel,ver=2.4.29		1	
CVE-2004-1235	uselib()	pkg=linux-kernel,ver=2.4.29		1	Known to work only for 2.4 series (even though 2.6 is also vulnerable)
CVE-2004-1235	krad3	pkg=linux-kernel,ver>=2.6.5,ver<=2.6.11		1	
CVE-2004-0077	mremap_pte	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.2		1	
CVE-2006-2451	raptor_prctl	pkg=linux-kernel,ver>=2.6.13,ver<=2.6.17		1	
CVE-2006-2451	prctl	pkg=linux-kernel,ver>=2.6.13,ver<=2.6.17		1	
CVE-2006-2451	prctl2	pkg=linux-kernel,ver>=2.6.13,ver<=2.6.17		1	
CVE-2006-2451	prctl3	pkg=linux-kernel,ver>=2.6.13,ver<=2.6.17		1	
CVE-2006-2451	prctl4	pkg=linux-kernel,ver>=2.6.13,ver<=2.6.17		1	
CVE-2006-3626	h00lyshit	pkg=linux-kernel,ver>=2.6.8,ver<=2.6.16		1	
CVE-2008-0600	vmsplice1	pkg=linux-kernel,ver>=2.6.17,ver<=2.6.24		1	
CVE-2008-0600	vmsplice2	pkg=linux-kernel,ver>=2.6.23,ver<=2.6.24		1	
CVE-2008-4210	ftrex	pkg=linux-kernel,ver>=2.6.11,ver<=2.6.22		1	world-writable sgid directory and shell that does not drop sgid privs upon exec (ash/sash) are required
CVE-2008-4210	exit_notify	pkg=linux-kernel,ver>=2.6.25,ver<=2.6.29		1	
CVE-2009-2692	sock_sendpage (simple version)	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.30	ubuntu=7.10,RHEL=4,fedora=4|5|6|7|8|9|10|11	1	Works for systems with /proc/sys/vm/mmap_min_addr equal to 0
CVE-2009-2692,CVE-2009-1895	sock_sendpage	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.30	ubuntu=9.04	1	/proc/sys/vm/mmap_min_addr needs to equal 0 OR pulseaudio needs to be installed
CVE-2009-2692,CVE-2009-1895	sock_sendpage2	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.30		1	Works for systems with /proc/sys/vm/mmap_min_addr equal to 0
CVE-2009-2692,CVE-2009-1895	sock_sendpage3	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.30		1	/proc/sys/vm/mmap_min_addr needs to equal 0 OR pulseaudio needs to be installed
CVE-2009-2692,CVE-2009-1895	sock_sendpage (ppc)	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.30	ubuntu=8.10,RHEL=4|5	1	/proc/sys/vm/mmap_min_addr needs to equal 0
CVE-2009-2698	the rebel (udp_sendmsg)	pkg=linux-kernel,ver>=2.6.1,ver<=2.6.19	debian=4	1	/proc/sys/vm/mmap_min_addr needs to equal 0 OR pulseaudio needs to be installed
CVE-2009-2698	hoagie_udp_sendmsg	pkg=linux-kernel,ver>=2.6.1,ver<=2.6.19,x86	debian=4	1	Works for systems with /proc/sys/vm/mmap_min_addr equal to 0
CVE-2009-2698	katon (udp_sendmsg)	pkg=linux-kernel,ver>=2.6.1,ver<=2.6.19,x86	debian=4	1	Works for systems with /proc/sys/vm/mmap_min_addr equal to 0
CVE-2009-2698	ip_append_data	pkg=linux-kernel,ver>=2.6.1,ver<=2.6.19,x86	fedora=4|5|6,RHEL=4	1	Works for systems with /proc/sys/vm/mmap_min_addr equal to 0
CVE-2009-3547	pipe.c 1	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.31		1	
CVE-2009-3547	pipe.c 2	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.31		1	
EOF_DATA_1
)"

KERNEL_CVE_DATA_2="$(cat <<'EOF_DATA_2'
CVE-2009-3547	pipe.c 3	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.31		1	
CVE-2010-3301	ptrace_kmod2	pkg=linux-kernel,ver>=2.6.26,ver<=2.6.34	debian=6.0{kernel:2.6.(32|33|34|35)-(1|2|trunk)-amd64},ubuntu=(10.04|10.10){kernel:2.6.(32|35)-(19|21|24)-server}	1	
CVE-2010-1146	reiserfs	pkg=linux-kernel,ver>=2.6.18,ver<=2.6.34	ubuntu=9.10	1	
CVE-2010-2959	can_bcm	pkg=linux-kernel,ver>=2.6.18,ver<=2.6.36	ubuntu=10.04{kernel:2.6.32-24-generic}	1	
CVE-2010-3904	rds	pkg=linux-kernel,ver>=2.6.30,ver<2.6.37	debian=6.0{kernel:2.6.(31|32|34|35)-(1|trunk)-amd64},ubuntu=10.10|9.10,fedora=13{kernel:2.6.33.3-85.fc13.i686.PAE},ubuntu=10.04{kernel:2.6.32-(21|24)-generic}	1	
CVE-2010-3848,CVE-2010-3850,CVE-2010-4073	half_nelson	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.36	ubuntu=(10.04|9.10){kernel:2.6.(31|32)-(14|21)-server}	1	
N/A	caps_to_root	pkg=linux-kernel,ver>=2.6.34,ver<=2.6.36,x86	ubuntu=10.10	1	
N/A	caps_to_root 2	pkg=linux-kernel,ver>=2.6.34,ver<=2.6.36	ubuntu=10.10	1	
CVE-2010-4347	american-sign-language	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.36		1	
CVE-2010-3437	pktcdvd	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.36	ubuntu=10.04	1	
CVE-2010-3081	video4linux	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.33	RHEL=5	1	
CVE-2012-0056	memodipper	pkg=linux-kernel,ver>=3.0.0,ver<=3.1.0	ubuntu=(10.04|11.10){kernel:3.0.0-12-(generic|server)}	1	
CVE-2012-0056,CVE-2010-3849,CVE-2010-3850	full-nelson	pkg=linux-kernel,ver>=2.6.0,ver<=2.6.36	ubuntu=(9.10|10.10){kernel:2.6.(31|35)-(14|19)-(server|generic)},ubuntu=10.04{kernel:2.6.32-(21|24)-server}	1	
CVE-2013-1858	CLONE_NEWUSER|CLONE_FS	pkg=linux-kernel,ver=3.8,CONFIG_USER_NS=y		1	CONFIG_USER_NS needs to be enabled 
CVE-2013-2094	perf_swevent	pkg=linux-kernel,ver>=2.6.32,ver<3.8.9,x86_64	RHEL=6,ubuntu=12.04{kernel:3.2.0-(23|29)-generic},fedora=16{kernel:3.1.0-7.fc16.x86_64},fedora=17{kernel:3.3.4-5.fc17.x86_64},debian=7{kernel:3.2.0-4-amd64}	1	No SMEP/SMAP bypass
CVE-2013-2094	perf_swevent 2	pkg=linux-kernel,ver>=2.6.32,ver<3.8.9,x86_64	ubuntu=12.04{kernel:3.(2|5).0-(23|29)-generic}	1	No SMEP/SMAP bypass
CVE-2013-0268	msr	pkg=linux-kernel,ver>=2.6.18,ver<3.7.6		1	
CVE-2013-1959	userns_root_sploit	pkg=linux-kernel,ver>=3.0.1,ver<3.8.9		1	
CVE-2013-2094	semtex	pkg=linux-kernel,ver>=2.6.32,ver<3.8.9	RHEL=6	1	
CVE-2014-0038	timeoutpwn	pkg=linux-kernel,ver>=3.4.0,ver<=3.13.1,CONFIG_X86_X32=y	ubuntu=13.10	1	CONFIG_X86_X32 needs to be enabled
CVE-2014-0038	timeoutpwn 2	pkg=linux-kernel,ver>=3.4.0,ver<=3.13.1,CONFIG_X86_X32=y	ubuntu=(13.04|13.10){kernel:3.(8|11).0-(12|15|19)-generic}	1	CONFIG_X86_X32 needs to be enabled
CVE-2014-0196	rawmodePTY	pkg=linux-kernel,ver>=2.6.31,ver<=3.14.3		1	
CVE-2014-2851	use-after-free in ping_init_sock() (DoS)	pkg=linux-kernel,ver>=3.0.1,ver<=3.14		0	
CVE-2014-4014	inode_capable	pkg=linux-kernel,ver>=3.0.1,ver<=3.13	ubuntu=12.04	1	
CVE-2014-4699	ptrace/sysret	pkg=linux-kernel,ver>=3.0.1,ver<=3.8	ubuntu=12.04	1	
EOF_DATA_2
)"

KERNEL_CVE_DATA_3="$(cat <<'EOF_DATA_3'
CVE-2014-4943	PPPoL2TP (DoS)	pkg=linux-kernel,ver>=3.2,ver<=3.15.6		1	
CVE-2014-5207	fuse_suid	pkg=linux-kernel,ver>=3.0.1,ver<=3.16.1		1	
CVE-2015-9322	BadIRET	pkg=linux-kernel,ver>=3.0.1,ver<3.17.5,x86_64	RHEL<=7,fedora=20	1	
CVE-2015-3290	espfix64_NMI	pkg=linux-kernel,ver>=3.13,ver<4.1.6,x86_64		1	
N/A	bluetooth	pkg=linux-kernel,ver<=2.6.11		1	
CVE-2015-1328	overlayfs	pkg=linux-kernel,ver>=3.13.0,ver<=3.19.0	ubuntu=(12.04|14.04){kernel:3.13.0-(2|3|4|5)*-generic},ubuntu=(14.10|15.04){kernel:3.(13|16).0-*-generic}	1	
CVE-2015-8660	overlayfs (ovl_setattr)	pkg=linux-kernel,ver>=3.0.0,ver<=4.3.3		1	
CVE-2015-8660	overlayfs (ovl_setattr)	pkg=linux-kernel,ver>=3.0.0,ver<=4.3.3	ubuntu=(14.04|15.10){kernel:4.2.0-(18|19|20|21|22)-generic}	1	
CVE-2016-0728	keyring	pkg=linux-kernel,ver>=3.10,ver<4.4.1		0	Exploit takes about ~30 minutes to run. Exploit is not reliable, see: https://cyseclabs.com/blog/cve-2016-0728-poc-not-working
CVE-2016-2384	usb-midi	pkg=linux-kernel,ver>=3.0.0,ver<=4.4.8	ubuntu=14.04,fedora=22	1	Requires ability to plug in a malicious USB device and to execute a malicious binary as a non-privileged user
CVE-2016-4997	target_offset	pkg=linux-kernel,ver>=4.4.0,ver<=4.4.0,cmd:grep -qi ip_tables /proc/modules	ubuntu=16.04{kernel:4.4.0-21-generic}	1	ip_tables.ko needs to be loaded
CVE-2016-4557	double-fdput()	pkg=linux-kernel,ver>=4.4,ver<4.5.5,CONFIG_BPF_SYSCALL=y,sysctl:kernel.unprivileged_bpf_disabled!=1	ubuntu=16.04{kernel:4.4.0-21-generic}	1	CONFIG_BPF_SYSCALL needs to be set && kernel.unprivileged_bpf_disabled != 1
CVE-2016-5195	dirtycow	pkg=linux-kernel,ver>=2.6.22,ver<=4.8.3	debian=7|8,RHEL=5{kernel:2.6.(18|24|33)-*},RHEL=6{kernel:2.6.32-*|3.(0|2|6|8|10).*|2.6.33.9-rt31},RHEL=7{kernel:3.10.0-*|4.2.0-0.21.el7},ubuntu=16.04|14.04|12.04	4	For RHEL/CentOS see exact vulnerable versions here: https://access.redhat.com/sites/default/files/rh-cve-2016-5195_5.sh
CVE-2016-5195	dirtycow 2	pkg=linux-kernel,ver>=2.6.22,ver<=4.8.3	debian=7|8,RHEL=5|6|7,ubuntu=14.04|12.04,ubuntu=10.04{kernel:2.6.32-21-generic},ubuntu=16.04{kernel:4.4.0-21-generic}	4	For RHEL/CentOS see exact vulnerable versions here: https://access.redhat.com/sites/default/files/rh-cve-2016-5195_5.sh
CVE-2016-8655	chocobo_root	pkg=linux-kernel,ver>=4.4.0,ver<4.9,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=(14.04|16.04){kernel:4.4.0-(21|22|24|28|31|34|36|38|42|43|45|47|51)-generic}	1	CAP_NET_RAW capability is needed OR CONFIG_USER_NS=y needs to be enabled
CVE-2016-9793	SO_{SND|RCV}BUFFORCE	pkg=linux-kernel,ver>=3.11,ver<4.8.14,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1		1	CAP_NET_ADMIN caps OR CONFIG_USER_NS=y needed. No SMEP/SMAP/KASLR bypass included. Tested in QEMU only
CVE-2017-6074	dccp	pkg=linux-kernel,ver>=2.6.18,ver<=4.9.11,CONFIG_IP_DCCP=[my]	ubuntu=(14.04|16.04){kernel:4.4.0-62-generic}	1	Requires Kernel be built with CONFIG_IP_DCCP enabled. Includes partial SMEP/SMAP bypass
CVE-2017-7308	af_packet	pkg=linux-kernel,ver>=3.2,ver<=4.10.6,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=16.04{kernel:4.8.0-(34|36|39|41|42|44|45)-generic}	1	CAP_NET_RAW cap or CONFIG_USER_NS=y needed. Modified version at 'ext-url' adds support for additional kernels
CVE-2017-16995	eBPF_verifier	pkg=linux-kernel,ver>=4.4,ver<=4.14.8,CONFIG_BPF_SYSCALL=y,sysctl:kernel.unprivileged_bpf_disabled!=1	debian=9.0{kernel:4.9.0-3-amd64},fedora=25|26|27,ubuntu=14.04{kernel:4.4.0-89-generic},ubuntu=(16.04|17.04){kernel:4.(8|10).0-(19|28|45)-generic}	5	CONFIG_BPF_SYSCALL needs to be set && kernel.unprivileged_bpf_disabled != 1
CVE-2017-1000112	NETIF_F_UFO	pkg=linux-kernel,ver>=4.4,ver<=4.13,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=14.04{kernel:4.4.0-*},ubuntu=16.04{kernel:4.8.0-*}	1	CAP_NET_ADMIN cap or CONFIG_USER_NS=y needed. SMEP/KASLR bypass included. Modified version at 'ext-url' adds support for additional distros/kernels
CVE-2017-1000253	PIE_stack_corruption	pkg=linux-kernel,ver>=3.2,ver<=4.13,x86_64	RHEL=6,RHEL=7{kernel:3.10.0-514.21.2|3.10.0-514.26.1}	1	
CVE-2018-5333	rds_atomic_free_op NULL pointer dereference	pkg=linux-kernel,ver>=4.4,ver<=4.14.13,cmd:grep -qi rds /proc/modules,x86_64	ubuntu=16.04{kernel:4.4.0|4.8.0}	1	rds.ko kernel module needs to be loaded. Modified version at 'ext-url' adds support for additional targets and bypassing KASLR.
CVE-2018-14634	Mutagen Astronomy	pkg=linux-kernel,x86_64,ver>=4.14.1,ver<=4.14.54	debian=8,RHEL=6|7	1	systems with less than 32GB of RAM are unlikely to be affected by this issue
CVE-2018-18955	subuid_shell	pkg=linux-kernel,ver>=4.15,ver<=4.19.2,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1,cmd:[ -u /usr/bin/newuidmap ],cmd:[ -u /usr/bin/newgidmap ]	ubuntu=18.04{kernel:4.15.0-20-generic},fedora=28{kernel:4.16.3-301.fc28}	1	CONFIG_USER_NS needs to be enabled
CVE-2019-13272	PTRACE_TRACEME	pkg=linux-kernel,ver>=4,ver<5.1.17,sysctl:kernel.yama.ptrace_scope==0,x86_64	ubuntu=16.04{kernel:4.15.0-*},ubuntu=18.04{kernel:4.15.0-*},debian=9{kernel:4.9.0-*},debian=10{kernel:4.19.0-*},fedora=30{kernel:5.0.9-*}	1	Requires an active PolKit agent.
EOF_DATA_3
)"

KERNEL_CVE_DATA_4="$(cat <<'EOF_DATA_4'
CVE-2019-15666	XFRM_UAF	pkg=linux-kernel,ver>=3,ver<5.0.19,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1,CONFIG_XFRM=y		1	CONFIG_USER_NS needs to be enabled; CONFIG_XFRM needs to be enabled
CVE-2021-27365	linux-iscsi	pkg=linux-kernel,ver<=5.11.3,CONFIG_SLAB_FREELIST_HARDENED!=y	RHEL=8	1	CONFIG_SLAB_FREELIST_HARDENED must not be enabled
CVE-2021-3490	eBPF ALU32 bounds tracking for bitwise ops	pkg=linux-kernel,ver>=5.7,ver<5.12,CONFIG_BPF_SYSCALL=y,sysctl:kernel.unprivileged_bpf_disabled!=1	ubuntu=20.04{kernel:5.8.0-(25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52)-*},ubuntu=21.04{kernel:5.11.0-16-*}	5	CONFIG_BPF_SYSCALL needs to be set && kernel.unprivileged_bpf_disabled != 1
CVE-2021-3493	Ubuntu OverlayFS	pkg=linux-kernel,ver>=3.13,ver<5.14,x86_64	ubuntu=(14.04|16.04|18.04|20.04|20.10)	1	Only Ubuntu is affected.
CVE-2021-22555	Netfilter heap out-of-bounds write	pkg=linux-kernel,ver>=2.6.19,ver<=5.12-rc6	ubuntu=20.04{kernel:5.8.0-*}	1	ip_tables kernel module must be loaded
CVE-2022-0847	DirtyPipe	pkg=linux-kernel,ver>=5.8,ver<=5.16.11	ubuntu=(20.04|21.04),debian=11	1	
CVE-2022-0995	watch_queue	pkg=linux-kernel,ver>=5.8,ver<5.16.5,x86_64	ubuntu=21.10{kernel:5.13.0.37-generic}	1	Not 100% reliable, may need to be run a couple of times. It rare cases it may panic the kernel.
CVE-2022-2586	nft_object UAF	pkg=linux-kernel,ver>=5.12,ver<5.19,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=(20.04){kernel:5.12.13}	1	kernel.unprivileged_userns_clone=1 required (to obtain CAP_NET_ADMIN)
CVE-2022-32250	nft_object UAF (NFT_MSG_NEWSET)	pkg=linux-kernel,ver<5.18.1,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=(22.04){kernel:5.15.0-27-generic}	1	kernel.unprivileged_userns_clone=1 required (to obtain CAP_NET_ADMIN)
CVE-2023-0386	OverlayFS suid smuggle	pkg=linux-kernel,ver>=5.11,ver<=6.2,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	ubuntu=22.04.1{kernel:5.15.0-57-generic}	1	CONFIG_USER_NS needs to be enabled && kernel.unprivileged_userns_clone=1 required
CVE-2024-1086	double-free in nf_tables	pkg=linux-kernel,x86_64,ver>=5.14,ver<=6.6,CONFIG_NF_TABLES=y,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1	debian=12,ubuntu=22.04	1	CONFIG_USER_NS and CONFIG_NF_TABLES need to be enabled && kernel.unprivileged_userns_clone=1 required
CVE-2021-3560	Polkit race authentication bypass	cmd:sh -c "apt list --installed 2>/dev/null | grep -E 'polkit.*0\\.105-26' | grep -qEv 'ubuntu1\\.[1-9]' || yum list installed 2>/dev/null | grep -qE 'polkit.*\\(0\\.117-2\\|0\\.115-6\\|0\\.11[3-9]\\)' || rpm -qa 2>/dev/null | grep -qE 'polkit.*\\(0\\.117-2\\|0\\.115-6\\|0\\.11[3-9]\\)'"		1	Migrated from former standalone 1_system_information check
CVE-2025-38236	AF_UNIX MSG_OOB UAF	pkg=linux-kernel,ver>=6.9.0		1	Migrated from former standalone 1_system_information check
CVE-2025-38352	POSIX CPU timers race	pkg=linux-kernel,ver>=6.12,ver<6.12.34,CONFIG_POSIX_CPU_TIMERS_TASK_WORK!=y		1	Migrated from former standalone 1_system_information check
af_packet	2016-8655	4.4.0		http://www.exploit-db.com/exploits/40871
american-sign-language	2010-4347	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36		http://www.securityfocus.com/bid/45408
ave		2.4.19,2.4.20		
brk		2.4.10,2.4.18,2.4.19,2.4.20,2.4.21,2.4.22		
can_bcm	2010-2959	2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36		http://www.exploit-db.com/exploits/14814
caps_to_root	n/a	2.6.34,2.6.35,2.6.36		http://www.exploit-db.com/exploits/15916
clone_newuser	N\A	3.3.5,3.3.4,3.3.2,3.2.13,3.2.9,3.2.1,3.1.8,3.0.5,3.0.4,3.0.2,3.0.1,3.2,3.0.1,3.0		http://www.exploit-db.com/exploits/38390
dirty_cow	2016-5195	2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36,2.6.37,2.6.38,2.6.39,3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0,3.2.0,3.3.0,3.4.0,3.5.0,3.6.0,3.7.0,3.7.6,3.8.0,3.9.0		http://www.exploit-db.com/exploits/40616
CVE-2010-0415	do_pages_move	2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31	sieve	1	Spenders Enlightenment
elfcd		2.6.12		
elfdump		2.4.27		
EOF_DATA_4
)"

KERNEL_CVE_DATA_5="$(cat <<'EOF_DATA_5'
elflbl		2.4.29		http://www.exploit-db.com/exploits/744
exit_notify		2.6.25,2.6.26,2.6.27,2.6.28,2.6.29		http://www.exploit-db.com/exploits/8369
exp.sh		2.6.9,2.6.10,2.6.16,2.6.13		
expand_stack		2.4.29		
CVE-2018-14665	exploit_x	2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36,2.6.37,2.6.38,2.6.39,3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0,3.2.0,3.3.0,3.4.0,3.5.0,3.6.0,3.7.0,3.7.6,3.8.0,3.9.0,3.10.0,3.11.0,3.12.0,3.13.0,3.14.0,3.15.0,3.16.0,3.17.0,3.18.0,3.19.0,4.0.0,4.1.0,4.2.0,4.3.0,4.4.0,4.5.0,4.6.0,4.7.0		1	http://www.exploit-db.com/exploits/45697
ftrex	2008-4210	2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22		http://www.exploit-db.com/exploits/6851
CVE-2017-16695	get_rekt	4.4.0,4.8.0,4.10.0,4.13.0		1	http://www.exploit-db.com/exploits/45010
h00lyshit	2006-3626	2.6.8,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16		http://www.exploit-db.com/exploits/2013
half_nelson1	2010-3848	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36	econet	http://www.exploit-db.com/exploits/17787
half_nelson2	2010-3850	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36	econet	http://www.exploit-db.com/exploits/17787
half_nelson3	2010-4073	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36	econet	http://www.exploit-db.com/exploits/17787
kdump		2.6.13		
km2		2.4.18,2.4.22		
krad		2.6.5,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11		
krad3		2.6.5,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11		http://exploit-db.com/exploits/1397
local26		2.6.13		
loginx		2.4.22		
loko		2.4.22,2.4.23,2.4.24		
memodipper	2012-0056	2.6.39,3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0		http://www.exploit-db.com/exploits/18411
mremap_pte		2.4.20,2.2.24,2.4.25,2.4.26,2.4.27		http://www.exploit-db.com/exploits/160
msr	2013-0268	2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36,2.6.37,2.6.38,2.6.39,3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0,3.2.0,3.3.0,3.4.0,3.5.0,3.6.0,3.7.0,3.7.6		http://www.exploit-db.com/exploits/27297
newlocal		2.4.17,2.4.19		
newsmp		2.6		
ong_bak		2.6.5		
overlayfs	2015-8660	3.13.0,3.16.0,3.19.0		http://www.exploit-db.com/exploits/39230
EOF_DATA_5
)"

KERNEL_CVE_DATA_6="$(cat <<'EOF_DATA_6'
packet_set_ring	2017-7308	4.8.0		http://www.exploit-db.com/exploits/41994
perf_swevent	2013-2094	3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0,3.2.0,3.3.0,3.4.0,3.4.1,3.4.2,3.4.3,3.4.4,3.4.5,3.4.6,3.4.8,3.4.9,3.5.0,3.6.0,3.7.0,3.8.0,3.8.1,3.8.2,3.8.3,3.8.4,3.8.5,3.8.6,3.8.7,3.8.8,3.8.9		http://www.exploit-db.com/exploits/26131
pipe.c_32bit	2009-3547	2.4.4,2.4.5,2.4.6,2.4.7,2.4.8,2.4.9,2.4.10,2.4.11,2.4.12,2.4.13,2.4.14,2.4.15,2.4.16,2.4.17,2.4.18,2.4.19,2.4.20,2.4.21,2.4.22,2.4.23,2.4.24,2.4.25,2.4.26,2.4.27,2.4.28,2.4.29,2.4.30,2.4.31,2.4.32,2.4.33,2.4.34,2.4.35,2.4.36,2.4.37,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31		http://www.securityfocus.com/data/vulnerabilities/exploits/36901-1.c
pktcdvd	2010-3437	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36		http://www.exploit-db.com/exploits/15150
pp_key	2016-0728	3.4.0,3.5.0,3.6.0,3.7.0,3.8.0,3.8.1,3.8.2,3.8.3,3.8.4,3.8.5,3.8.6,3.8.7,3.8.8,3.8.9,3.9.0,3.9.6,3.10.0,3.10.6,3.11.0,3.12.0,3.13.0,3.13.1		http://www.exploit-db.com/exploits/39277
prctl		2.6.13,2.6.14,2.6.15,2.6.16,2.6.17		http://www.exploit-db.com/exploits/2004
prctl2		2.6.13,2.6.14,2.6.15,2.6.16,2.6.17		http://www.exploit-db.com/exploits/2005
prctl3		2.6.13,2.6.14,2.6.15,2.6.16,2.6.17		http://www.exploit-db.com/exploits/2006
prctl4		2.6.13,2.6.14,2.6.15,2.6.16,2.6.17		http://www.exploit-db.com/exploits/2011
ptrace		2.4.18,2.4.19,2.4.20,2.4.21,2.4.22		
ptrace24		2.4.9		
CVE-2007-4573	ptrace_kmod	2.4.18,2.4.19,2.4.20,2.4.21,2.4.22		1	
ptrace_kmod2	2010-3301	2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34	ia32syscall,robert_you_suck	http://www.exploit-db.com/exploits/15023
pwned		2.6.11		
py2		2.6.9,2.6.17,2.6.15,2.6.13		
raptor_prctl	2006-2451	2.6.13,2.6.14,2.6.15,2.6.16,2.6.17		http://www.exploit-db.com/exploits/2031
rawmodePTY	2014-0196	2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36,2.6.37,2.6.38,2.6.39,3.14.0,3.15.0		http://packetstormsecurity.com/files/download/126603/cve-2014-0196-md.c
rds	2010-3904	2.6.30,2.6.31,2.6.32,2.6.33,2.6.34,2.6.35,2.6.36		http://www.exploit-db.com/exploits/15285
reiserfs	2010-1146	2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33,2.6.34		http://www.exploit-db.com/exploits/12130
remap		2.4		
rip		2.2		
CVE-2008-4113	sctp	2.6.26		1	
semtex	2013-2094	2.6.37,2.6.38,2.6.39,3.0.0,3.0.1,3.0.2,3.0.3,3.0.4,3.0.5,3.0.6,3.1.0		http://www.exploit-db.com/exploits/25444
smpracer		2.4.29		
sock_sendpage	2009-2692	2.4.4,2.4.5,2.4.6,2.4.7,2.4.8,2.4.9,2.4.10,2.4.11,2.4.12,2.4.13,2.4.14,2.4.15,2.4.16,2.4.17,2.4.18,2.4.19,2.4.20,2.4.21,2.4.22,2.4.23,2.4.24,2.4.25,2.4.26,2.4.27,2.4.28,2.4.29,2.4.30,2.4.31,2.4.32,2.4.33,2.4.34,2.4.35,2.4.36,2.4.37,2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30	wunderbar_emporium	http://www.exploit-db.com/exploits/9435
EOF_DATA_6
)"

KERNEL_CVE_DATA_7="$(cat <<'EOF_DATA_7'
sock_sendpage2	2009-2692	2.4.4,2.4.5,2.4.6,2.4.7,2.4.8,2.4.9,2.4.10,2.4.11,2.4.12,2.4.13,2.4.14,2.4.15,2.4.16,2.4.17,2.4.18,2.4.19,2.4.20,2.4.21,2.4.22,2.4.23,2.4.24,2.4.25,2.4.26,2.4.27,2.4.28,2.4.29,2.4.30,2.4.31,2.4.32,2.4.33,2.4.34,2.4.35,2.4.36,2.4.37,2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30	proto_ops	http://www.exploit-db.com/exploits/9436
stackgrow2		2.4.29,2.6.10		
timeoutpwn	2014-0038	3.4.0,3.5.0,3.6.0,3.7.0,3.8.0,3.8.9,3.9.0,3.10.0,3.11.0,3.12.0,3.13.0,3.4.0,3.5.0,3.6.0,3.7.0,3.8.0,3.8.5,3.8.6,3.8.9,3.9.0,3.9.6,3.10.0,3.10.6,3.11.0,3.12.0,3.13.0,3.13.1		http://www.exploit-db.com/exploits/31346
CVE-2009-1185	udev	2.6.25,2.6.26,2.6.27,2.6.28,2.6.29	udev <1.4.1	1	http://www.exploit-db.com/exploits/8478
udp_sendmsg_32bit	2009-2698	2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19		http://downloads.securityfocus.com/vulnerabilities/exploits/36108.c
uselib24		2.6.10,2.4.17,2.4.22,2.4.25,2.4.27,2.4.29		
CVE-2009-1046	vconsole	2.6		1	
video4linux	2010-3081	2.6.0,2.6.1,2.6.2,2.6.3,2.6.4,2.6.5,2.6.6,2.6.7,2.6.8,2.6.9,2.6.10,2.6.11,2.6.12,2.6.13,2.6.14,2.6.15,2.6.16,2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.25,2.6.26,2.6.27,2.6.28,2.6.29,2.6.30,2.6.31,2.6.32,2.6.33		http://www.exploit-db.com/exploits/15024
vmsplice1	2008-0600	2.6.17,2.6.18,2.6.19,2.6.20,2.6.21,2.6.22,2.6.23,2.6.24,2.6.24.1	jessica biel	http://www.exploit-db.com/exploits/5092
vmsplice2	2008-0600	2.6.23,2.6.24	diane_lane	http://www.exploit-db.com/exploits/5093
w00t		2.4.10,2.4.16,2.4.17,2.4.18,2.4.19,2.4.20,2.4.21		
CVE-2004-0186	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2007-4573	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2008-0009	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2008-0010	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-0065	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-1046	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-1185	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-1897	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-2910	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2009-3001	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2010-0832	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2010-2240	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2010-2963	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2010-4170	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_7
)"

KERNEL_CVE_DATA_8="$(cat <<'EOF_DATA_8'
CVE-2010-4258	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2011-1485	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2011-1493	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2011-2921	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2012-0809	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2013-1763	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2014-0476	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2014-3153	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2014-4322	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2014-5119	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2014-9322	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-0568	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-0570	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-1318	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-1805	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-1815	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-1862	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-3202	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-3246	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-3315	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-3636	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-5287	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-6565	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2015-8612	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-0819	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_8
)"

KERNEL_CVE_DATA_9="$(cat <<'EOF_DATA_9'
CVE-2016-0820	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-10277	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-1240	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-1247	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-1531	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-1583	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2059	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2411	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2434	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2435	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2475	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-2503	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-3857	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-3873	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-4989	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-5340	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-5425	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-6187	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-6662	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-6663	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-6664	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-6787	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-7117	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-8453	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2016-8633	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_9
)"

KERNEL_CVE_DATA_10="$(cat <<'EOF_DATA_10'
CVE-2016-9566	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-0358	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-0403	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-0437	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-0569	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000251	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000363	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000366	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000367	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000370	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000371	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000379	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000380	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-1000405	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-10661	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-11176	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-16695	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-18344	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-2636	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-5123	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-5618	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-5899	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-7184	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2017-7616	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-1000001	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_10
)"

KERNEL_CVE_DATA_11="$(cat <<'EOF_DATA_11'
CVE-2018-10900	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-14665	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-17182	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-18281	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-3639	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-6554	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-6555	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-8781	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2018-9568	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-10149	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-10567	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-11190	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-12181	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-14040	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-14041	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-16508	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-18634	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-18675	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-18683	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-18862	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-19377	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-2000	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-2025	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-2181	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-2214	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_11
)"

KERNEL_CVE_DATA_12="$(cat <<'EOF_DATA_12'
CVE-2019-2215	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-7304	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-7308	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-9213	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-9500	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2019-9503	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-0041	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-0423	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-11179	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-12351	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-12352	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-14356	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-14381	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-14386	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-16119	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-24490	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-25220	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-27194	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-27786	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-28343	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-28588	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-3680	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-8835	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2020-9470	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-0399	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_12
)"

KERNEL_CVE_DATA_13="$(cat <<'EOF_DATA_13'
CVE-2021-0920	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1048	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1905	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1940	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1961	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1968	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-1969	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-20226	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-23134	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-25369	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-25370	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-26341	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-26708	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-27363	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-27364	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-28663	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-28664	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-29657	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-3156	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-32606	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-33909	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-34866	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-3492	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-3573	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-3609	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_13
)"

KERNEL_CVE_DATA_14="$(cat <<'EOF_DATA_14'
CVE-2021-3715	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-39793	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-39815	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-4034	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-41073	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-42008	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-4204	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-42327	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-43267	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-4440	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-44733	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2021-45608	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-0185	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-0435	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-1015	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-1016	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-1786	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-1972	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-20122	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-20186	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-20409	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-20421	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-2078	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-22057	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-22071	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_14
)"

KERNEL_CVE_DATA_15="$(cat <<'EOF_DATA_15'
CVE-2022-22265	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-22706	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-23222	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-24354	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-25636	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-25664	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-2590	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-2602	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-27666	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-29582	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-34918	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-38181	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-3910	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-41218	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-42703	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-42895	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-42896	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-4543	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-46395	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-47943	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2022-49080	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-0179	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-0266	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-0461	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-0590	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_15
)"

KERNEL_CVE_DATA_16="$(cat <<'EOF_DATA_16'
CVE-2023-1206	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-1829	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2008	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-20938	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-21400	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2156	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2163	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-23586	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2593	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2598	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-26083	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2612	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-2640	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-31248	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32233	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32629	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-3269	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32832	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32837	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32878	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-32882	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-33063	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-33106	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-33107	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-3338	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_16
)"

KERNEL_CVE_DATA_17="$(cat <<'EOF_DATA_17'
CVE-2023-3389	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-3390	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-35001	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-3865	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-3866	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-4130	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-4211	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-42483	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-4273	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-45864	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-4611	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-48409	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-50809	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-5178	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-52440	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-52447	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-52922	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-52926	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-5717	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-6200	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-6241	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-6546	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-6931	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2023-6932	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-0582	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_17
)"

KERNEL_CVE_DATA_18="$(cat <<'EOF_DATA_18'
CVE-2024-20018	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-21455	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-23372	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-23373	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-23380	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-26809	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-26921	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-26925	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-26926	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-31333	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-33060	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-35880	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-36016	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-36886	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-36904	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-36974	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-36978	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-38399	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-38402	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-41003	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-41009	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-41010	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-43047	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-43882	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-44068	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_18
)"

KERNEL_CVE_DATA_19="$(cat <<'EOF_DATA_19'
CVE-2024-46713	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-46740	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-49739	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-49848	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-49882	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-50066	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-50264	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-50302	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-53104	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-53141	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-53197	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-56614	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-56615	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-56626	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-56627	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2024-56770	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-0072	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-0927	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21479	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21666	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21669	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21670	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21692	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21700	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21703	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_19
)"

KERNEL_CVE_DATA_20="$(cat <<'EOF_DATA_20'
CVE-2025-21756	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-21836	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-22056	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-23280	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-23330	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-32463	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-37752	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-37756	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-37899	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-37947	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-38001	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-38003	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-38004	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-38617	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-39946	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-39965	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-40040	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-6349	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-8045	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2025-8109	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
CVE-2106-2504	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; no matching rule defined in source suggesters
EOF_DATA_20
)"

KERNEL_CVE_DATA_21="$(cat <<'EOF_DATA_21'
CVE-2015-8550	double-fetch	pkg=linux-kernel,ver=4.19.65		1	From kernel-exploit-factory detail section (test version Linux-4.19.65)
CVE-2017-8890	inet_csk_clone_lock double-free	pkg=linux-kernel,ver=4.10.15		1	From kernel-exploit-factory detail section (test version Linux-4.10.15)
CVE-2019-8956	sctp_sendmsg null pointer dereference	pkg=linux-kernel,ver=4.20.0,x86		1	From kernel-exploit-factory detail section; exploit chain is documented for 32-bit with CVE-2019-9213
CVE-2021-31440	eBPF verifier __reg_combine_64_into_32	pkg=linux-kernel,ver>=5.11,ver<5.12,CONFIG_BPF_SYSCALL=y,sysctl:kernel.unprivileged_bpf_disabled!=1		1	From kernel-exploit-factory detail section and exploit prerequisites
CVE-2021-4154	cgroup fsconfig type confusion	pkg=linux-kernel,ver=5.13.3		1	From kernel-exploit-factory detail section (test version Linux-5.13.3)
CVE-2022-2588	route4_filter double-free	pkg=linux-kernel,ver=5.19.1,CONFIG_USER_NS=y,sysctl:kernel.unprivileged_userns_clone==1		1	From kernel-exploit-factory detail section and exploit prerequisites
CVE-2022-2639	openvswitch reserve_sfa_size integer overflow	pkg=linux-kernel,ver=5.17.4,cmd:grep -qi openvswitch /proc/modules		1	From kernel-exploit-factory detail section; openvswitch module required
CVE-2025-21702	net/sched qdisc UAF	pkg=linux-kernel,ver=6.6.75,CONFIG_NET_SCHED=y		1	From kernel-exploit-factory detail section (test version Linux-6.6.75)
CVE-2017-16994	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; appears as related bypass mention
CVE-2020-27171	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; appears as related comment in exploit source
CVE-2024-0193	catalog_reference_only	9999.9999.9999		0	Reference-only CVE token from example repos; appears as upstream source reference
EOF_DATA_21
)"
