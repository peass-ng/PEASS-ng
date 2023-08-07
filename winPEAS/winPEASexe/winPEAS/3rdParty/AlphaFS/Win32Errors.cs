/*  Copyright (C) 2008-2018 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

namespace Alphaleonis.Win32
{
   internal static class Win32Errors
   {
      /// <summary>Use this to translate error codes into HRESULTs like 0x80070006 for ERROR_INVALID_HANDLE.</summary>
      public static int GetHrFromWin32Error(uint errorCode)
      {
         return (int) unchecked((int) 0x80070000 | errorCode);
      }

      // System Error Codes.
      // http://msdn.microsoft.com/en-us/library/windows/desktop/ms681381%28v=vs.85%29.aspx

      // Win32 Error Codes.
      // https://infosys.beckhoff.com/content/1033/tcdiagnostics/html/tcdiagnostics_win32_errorcodes.htm


      public const uint ERROR_INVALID_FILE_SIZE = 0xFFFFFFFF;

      /// <summary>(0) The operation completed successfully.</summary>
      public const uint ERROR_SUCCESS = 0;

      /// <summary>(0) The operation completed successfully.</summary>
      public const uint NO_ERROR = 0;

      /// <summary>(1) Incorrect function.</summary>
      public const uint ERROR_INVALID_FUNCTION = 1;

      /// <summary>(2) The system cannot find the file specified.</summary>
      public const uint ERROR_FILE_NOT_FOUND = 2;

      /// <summary>(3) The system cannot find the path specified.</summary>
      public const uint ERROR_PATH_NOT_FOUND = 3;
      //public const uint ERROR_TOO_MANY_OPEN_FILES = 4;

      /// <summary>(5) Access is denied.</summary>
      public const uint ERROR_ACCESS_DENIED = 5;

      //public const uint ERROR_INVALID_HANDLE = 6;
      //public const uint ERROR_ARENA_TRASHED = 7;
      //public const uint ERROR_NOT_ENOUGH_MEMORY = 8;   
      //public const uint ERROR_INVALID_BLOCK = 9;
      //public const uint ERROR_BAD_ENVIRONMENT = 10;
      //public const uint ERROR_BAD_FORMAT = 11;
      //public const uint ERROR_INVALID_ACCESS = 12;

      /// <summary>(13) The data is invalid.</summary>
      public const uint ERROR_INVALID_DATA = 13;

      //public const uint ERROR_OUTOFMEMORY = 14;

      /// <summary>(15) The system cannot find the drive specified.</summary>
      public const uint ERROR_INVALID_DRIVE = 15;

      //public const uint ERROR_CURRENT_DIRECTORY = 16;

      /// <summary>(17) The system cannot move the file to a different disk drive.</summary>
      public const uint ERROR_NOT_SAME_DEVICE = 17;

      /// <summary>(18) There are no more files.</summary>
      public const uint ERROR_NO_MORE_FILES = 18;
      //public const uint ERROR_WRITE_PROTECT = 19;
      //public const uint ERROR_BAD_UNIT = 20;

      /// <summary>(21) The device is not ready.</summary>
      public const uint ERROR_NOT_READY = 21;

      //public const uint ERROR_BAD_COMMAND = 22;
      //public const uint ERROR_CRC = 23;
      //public const uint ERROR_BAD_LENGTH = 24;

      /// <summary>(25) The drive cannot locate a specific area or track on the disk.</summary>
      public const uint ERROR_SEEK = 25;

      //public const uint ERROR_NOT_DOS_DISK = 26;
      //public const uint ERROR_SECTOR_NOT_FOUND = 27;
      //public const uint ERROR_OUT_OF_PAPER = 28;
      //public const uint ERROR_WRITE_FAULT = 29;
      //public const uint ERROR_READ_FAULT = 30;
      //public const uint ERROR_GEN_FAILURE = 31;

      /// <summary>(32) The process cannot access the file because it is being used by another process.</summary>
      public const uint ERROR_SHARING_VIOLATION = 32;

      /// <summary>(33) The process cannot access the file because another process has locked a portion of the file.</summary>
      public const uint ERROR_LOCK_VIOLATION = 33;

      //public const uint ERROR_WRONG_DISK = 34;
      //public const uint ERROR_SHARING_BUFFER_EXCEEDED = 36;

      /// <summary>(38) Reached the end of the file.</summary>
      public const uint ERROR_HANDLE_EOF = 38;

      //public const uint ERROR_HANDLE_DISK_FULL = 39;
      public const uint ERROR_NOT_SUPPORTED = 50;
      //public const uint ERROR_REM_NOT_LIST = 51;
      //public const uint ERROR_DUP_NAME = 52;

      /// <summary>(53) The network path was not found.</summary>
      public const uint ERROR_BAD_NETPATH = 53;

      //public const uint ERROR_NETWORK_BUSY = 54;
      //public const uint ERROR_DEV_NOT_EXIST = 55;   
      //public const uint ERROR_TOO_MANY_CMDS = 56;
      //public const uint ERROR_ADAP_HDW_ERR = 57;
      //public const uint ERROR_BAD_NET_RESP = 58;
      //public const uint ERROR_UNEXP_NET_ERR = 59;
      //public const uint ERROR_BAD_REM_ADAP = 60;
      //public const uint ERROR_PRINTQ_FULL = 61;
      //public const uint ERROR_NO_SPOOL_SPACE = 62;
      //public const uint ERROR_PRINT_CANCELLED = 63;
      //public const uint ERROR_NETNAME_DELETED = 64;

      /// <summary>(65) Network access is denied.</summary>
      public const uint ERROR_NETWORK_ACCESS_DENIED = 65;

      //public const uint ERROR_BAD_DEV_TYPE = 66;

      /// <summary>(67) The network name cannot be found.</summary>
      public const uint ERROR_BAD_NET_NAME = 67;

      //public const uint ERROR_TOO_MANY_NAMES = 68;
      //public const uint ERROR_TOO_MANY_SESS = 69;
      //public const uint ERROR_SHARING_PAUSED = 70;
      //public const uint ERROR_REQ_NOT_ACCEP = 71;
      //public const uint ERROR_REDIR_PAUSED = 72;

      /// <summary>(80) The file exists.</summary>
      public const uint ERROR_FILE_EXISTS = 80;

      //public const uint ERROR_CANNOT_MAKE = 82;
      //public const uint ERROR_FAIL_I24 = 83;
      //public const uint ERROR_OUT_OF_STRUCTURES = 84;
      //public const uint ERROR_ALREADY_ASSIGNED = 85;
      //public const uint ERROR_INVALID_PASSWORD = 86;

      /// <summary>(87) The parameter is incorrect.</summary>
      public const uint ERROR_INVALID_PARAMETER = 87;

      //public const uint ERROR_NET_WRITE_FAULT = 88;
      //public const uint ERROR_NO_PROC_SLOTS = 89;
      //public const uint ERROR_TOO_MANY_SEMAPHORES = 100;
      //public const uint ERROR_EXCL_SEM_ALREADY_OWNED = 101;
      //public const uint ERROR_SEM_IS_SET = 102;
      //public const uint ERROR_TOO_MANY_SEM_REQUESTS = 103;
      //public const uint ERROR_INVALID_AT_INTERRUPT_TIME = 104;
      //public const uint ERROR_SEM_OWNER_DIED = 105;
      //public const uint ERROR_SEM_USER_LIMIT = 106;
      //public const uint ERROR_DISK_CHANGE = 107;
      //public const uint ERROR_DRIVE_LOCKED = 108;
      //public const uint ERROR_BROKEN_PIPE = 109;
      //public const uint ERROR_OPEN_FAILED = 110;
      //public const uint ERROR_BUFFER_OVERFLOW = 111;
      //public const uint ERROR_DISK_FULL = 112;
      //public const uint ERROR_NO_MORE_SEARCH_HANDLES = 113;
      //public const uint ERROR_INVALID_TARGET_HANDLE = 114;
      //public const uint ERROR_INVALID_CATEGORY = 117;
      //public const uint ERROR_INVALID_VERIFY_SWITCH = 118;
      //public const uint ERROR_BAD_DRIVER_LEVEL = 119;
      //public const uint ERROR_CALL_NOT_IMPLEMENTED = 120;
      //public const uint ERROR_SEM_TIMEOUT = 121;

      /// <summary>(122) The data area passed to a system call is too small.</summary>
      public const uint ERROR_INSUFFICIENT_BUFFER = 122;

      /// <summary>(123) The filename, directory name, or volume label syntax is incorrect.</summary>
      public const uint ERROR_INVALID_NAME = 123;

      //public const uint ERROR_INVALID_LEVEL = 124;
      //public const uint ERROR_NO_VOLUME_LABEL = 125;
      //public const uint ERROR_INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;
      //public const uint ERROR_MOD_NOT_FOUND = 126;
      //public const uint ERROR_PROC_NOT_FOUND = 127;
      //public const uint ERROR_WAIT_NO_CHILDREN = 128;
      //public const uint ERROR_CHILD_NOT_COMPLETE = 129;
      //public const uint ERROR_DIRECT_ACCESS_HANDLE = 130;

      /// <summary>(131) An attempt was made to move the file pointer before the beginning of the file.</summary>
      public const uint ERROR_NEGATIVE_SEEK = 131;

      //public const uint ERROR_SEEK_ON_DEVICE = 132;
      //public const uint ERROR_IS_JOIN_TARGET = 133;
      //public const uint ERROR_IS_JOINED = 134;
      //public const uint ERROR_IS_SUBSTED = 135;
      //public const uint ERROR_NOT_JOINED = 136;
      //public const uint ERROR_NOT_SUBSTED = 137;
      //public const uint ERROR_JOIN_TO_JOIN = 138;
      //public const uint ERROR_SUBST_TO_SUBST = 139;
      //public const uint ERROR_JOIN_TO_SUBST = 140;
      //public const uint ERROR_SUBST_TO_JOIN = 141;
      //public const uint ERROR_BUSY_DRIVE = 142;

      /// <summary>(143) The system cannot join or substitute a drive to or for a directory on the same drive.</summary>
      public const uint ERROR_SAME_DRIVE = 143;

      //public const uint ERROR_DIR_NOT_ROOT = 144;

      /// <summary>(145) The directory is not empty.</summary>
      public const uint ERROR_DIR_NOT_EMPTY = 145;

      //public const uint ERROR_IS_SUBST_PATH = 146;
      //public const uint ERROR_IS_JOIN_PATH = 147;
      //public const uint ERROR_PATH_BUSY = 148;
      //public const uint ERROR_IS_SUBST_TARGET = 149;
      //public const uint ERROR_SYSTEM_TRACE = 150;
      //public const uint ERROR_INVALID_EVENT_COUNT = 151;
      //public const uint ERROR_TOO_MANY_MUXWAITERS = 152;
      //public const uint ERROR_INVALID_LIST_FORMAT = 153;
      //public const uint ERROR_LABEL_TOO_LONG = 154;
      //public const uint ERROR_TOO_MANY_TCBS = 155;
      //public const uint ERROR_SIGNAL_REFUSED = 156;
      //public const uint ERROR_DISCARDED = 157;

      //// <summary>(158) The segment is already unlocked.</summary>
      //public const uint ERROR_NOT_LOCKED = 158;

      //public const uint ERROR_BAD_THREADID_ADDR = 159;
      //public const uint ERROR_BAD_ARGUMENTS = 160;
      //public const uint ERROR_BAD_PATHNAME = 161;
      //public const uint ERROR_SIGNAL_PENDING = 162;
      //public const uint ERROR_MAX_THRDS_REACHED = 164;
      //public const uint ERROR_LOCK_FAILED = 167;
      //public const uint ERROR_BUSY = 170;   
      //public const uint ERROR_CANCEL_VIOLATION = 173;
      //public const uint ERROR_ATOMIC_LOCKS_NOT_SUPPORTED = 174;
      //public const uint ERROR_INVALID_SEGMENT_NUMBER = 180;
      //public const uint ERROR_INVALID_ORDINAL = 182;

      /// <summary>(183) Cannot create a file when that file already exists.</summary>
      public const uint ERROR_ALREADY_EXISTS = 183;

      //public const uint ERROR_INVALID_FLAG_NUMBER = 186;
      //public const uint ERROR_SEM_NOT_FOUND = 187;
      //public const uint ERROR_INVALID_STARTING_CODESEG = 188;
      //public const uint ERROR_INVALID_STACKSEG = 189;
      //public const uint ERROR_INVALID_MODULETYPE = 190;
      //public const uint ERROR_INVALID_EXE_SIGNATURE = 191;
      //public const uint ERROR_EXE_MARKED_INVALID = 192;
      //public const uint ERROR_BAD_EXE_FORMAT = 193;
      //public const uint ERROR_ITERATED_DATA_EXCEEDS_64k = 194;
      //public const uint ERROR_INVALID_MINALLOCSIZE = 195;
      //public const uint ERROR_DYNLINK_FROM_INVALID_RING = 196;
      //public const uint ERROR_IOPL_NOT_ENABLED = 197;
      //public const uint ERROR_INVALID_SEGDPL = 198;
      //public const uint ERROR_AUTODATASEG_EXCEEDS_64k = 199;
      //public const uint ERROR_RING2SEG_MUST_BE_MOVABLE = 200;
      //public const uint ERROR_RELOC_CHAIN_XEEDS_SEGLIM = 201;
      //public const uint ERROR_INFLOOP_IN_RELOC_CHAIN = 202;

      //// <summary>(203) The system could not find the environment option that was entered.</summary>
      //public const uint ERROR_ENVVAR_NOT_FOUND = 203;

      //public const uint ERROR_NO_SIGNAL_SENT = 205;
      //public const uint ERROR_FILENAME_EXCED_RANGE = 206;
      //public const uint ERROR_RING2_STACK_IN_USE = 207;
      //public const uint ERROR_META_EXPANSION_TOO_LONG = 208;
      //public const uint ERROR_INVALID_SIGNAL_NUMBER = 209;
      //public const uint ERROR_THREAD_1_INACTIVE = 210;
      //public const uint ERROR_LOCKED = 212;
      //public const uint ERROR_TOO_MANY_MODULES = 214;
      //public const uint ERROR_NESTING_NOT_ALLOWED = 215;
      //public const uint ERROR_EXE_MACHINE_TYPE_MISMATCH = 216;
      //public const uint ERROR_EXE_CANNOT_MODIFY_SIGNED_BINARY = 217;
      //public const uint ERROR_EXE_CANNOT_MODIFY_STRONG_SIGNED_BINARY = 218;
      //public const uint ERROR_BAD_PIPE = 230;
      //public const uint ERROR_PIPE_BUSY = 231;
      //public const uint ERROR_NO_DATA = 232;
      //public const uint ERROR_PIPE_NOT_CONNECTED = 233;

      /// <summary>(234) More data is available.</summary>
      public const uint ERROR_MORE_DATA = 234;

      //public const uint ERROR_VC_DISCONNECTED = 240;
      //public const uint ERROR_INVALID_EA_NAME = 254;
      //public const uint ERROR_EA_LIST_INCONSISTENT = 255;
      //public const uint WAIT_TIMEOUT = 258;   

      ///// <summary>(259) No more data is available.</summary>
      public const uint ERROR_NO_MORE_ITEMS = 259;

      //public const uint ERROR_CANNOT_COPY = 266;

      /// <summary>(267) The directory name is invalid.</summary>
      public const uint ERROR_DIRECTORY = 267;

      //public const uint ERROR_EAS_DIDNT_FIT = 275;
      //public const uint ERROR_EA_FILE_CORRUPT = 276;
      //public const uint ERROR_EA_TABLE_FULL = 277;
      //public const uint ERROR_INVALID_EA_HANDLE = 278;
      //public const uint ERROR_EAS_NOT_SUPPORTED = 282;
      //public const uint ERROR_NOT_OWNER = 288;
      //public const uint ERROR_TOO_MANY_POSTS = 298;
      //public const uint ERROR_PARTIAL_COPY = 299;
      //public const uint ERROR_OPLOCK_NOT_GRANTED = 300;
      //public const uint ERROR_INVALID_OPLOCK_PROTOCOL = 301;
      //public const uint ERROR_DISK_TOO_FRAGMENTED = 302;
      //public const uint ERROR_DELETE_PENDING = 303;
      //public const uint ERROR_MR_MID_NOT_FOUND = 317;
      //public const uint ERROR_SCOPE_NOT_FOUND = 318;
      //public const uint ERROR_INVALID_ADDRESS = 487;
      //public const uint ERROR_ARITHMETIC_OVERFLOW = 534;
      //public const uint ERROR_PIPE_CONNECTED = 535;
      //public const uint ERROR_PIPE_LISTENING = 536;
      //public const uint ERROR_EA_ACCESS_DENIED = 994;

      /// <summary>(995) The I/O operation has been aborted because of either a thread exit or an application request.</summary>
      public const uint ERROR_OPERATION_ABORTED = 995;

      //public const uint ERROR_IO_INCOMPLETE = 996;

      //// <summary>(997) Overlapped I/O operation is in progress.</summary>
      //public const uint ERROR_IO_PENDING = 997;

      //public const uint ERROR_NOACCESS = 998;
      //public const uint ERROR_SWAPERROR = 999;
      //public const uint ERROR_STACK_OVERFLOW = 1001;
      //public const uint ERROR_INVALID_MESSAGE = 1002;
      //public const uint ERROR_CAN_NOT_COMPLETE = 1003;
      //public const uint ERROR_INVALID_FLAGS = 1004;
      //public const uint ERROR_UNRECOGNIZED_VOLUME = 1005;
      //public const uint ERROR_FILE_INVALID = 1006;
      //public const uint ERROR_FULLSCREEN_MODE = 1007;
      //public const uint ERROR_NO_TOKEN = 1008;
      //public const uint ERROR_BADDB = 1009;
      //public const uint ERROR_BADKEY = 1010;
      //public const uint ERROR_CANTOPEN = 1011;
      //public const uint ERROR_CANTREAD = 1012;
      //public const uint ERROR_CANTWRITE = 1013;
      //public const uint ERROR_REGISTRY_RECOVERED = 1014;
      //public const uint ERROR_REGISTRY_CORRUPT = 1015;
      //public const uint ERROR_REGISTRY_IO_FAILED = 1016;
      //public const uint ERROR_NOT_REGISTRY_FILE = 1017;
      //public const uint ERROR_KEY_DELETED = 1018;
      //public const uint ERROR_NO_LOG_SPACE = 1019;
      //public const uint ERROR_KEY_HAS_CHILDREN = 1020;
      //public const uint ERROR_CHILD_MUST_BE_VOLATILE = 1021;
      //public const uint ERROR_NOTIFY_ENUM_DIR = 1022;
      //public const uint ERROR_DEPENDENT_SERVICES_RUNNING = 1051;
      //public const uint ERROR_INVALID_SERVICE_CONTROL = 1052;
      //public const uint ERROR_SERVICE_REQUEST_TIMEOUT = 1053;
      //public const uint ERROR_SERVICE_NO_THREAD = 1054;
      //public const uint ERROR_SERVICE_DATABASE_LOCKED = 1055;
      //public const uint ERROR_SERVICE_ALREADY_RUNNING = 1056;
      //public const uint ERROR_INVALID_SERVICE_ACCOUNT = 1057;
      //public const uint ERROR_SERVICE_DISABLED = 1058;
      //public const uint ERROR_CIRCULAR_DEPENDENCY = 1059;
      //public const uint ERROR_SERVICE_DOES_NOT_EXIST = 1060;
      //public const uint ERROR_SERVICE_CANNOT_ACCEPT_CTRL = 1061;
      //public const uint ERROR_SERVICE_NOT_ACTIVE = 1062;
      //public const uint ERROR_FAILED_SERVICE_CONTROLLER_CONNECT = 1063;
      //public const uint ERROR_EXCEPTION_IN_SERVICE = 1064;
      //public const uint ERROR_DATABASE_DOES_NOT_EXIST = 1065;
      //public const uint ERROR_SERVICE_SPECIFIC_ERROR = 1066;
      //public const uint ERROR_PROCESS_ABORTED = 1067;
      //public const uint ERROR_SERVICE_DEPENDENCY_FAIL = 1068;
      //public const uint ERROR_SERVICE_LOGON_FAILED = 1069;
      //public const uint ERROR_SERVICE_START_HANG = 1070;
      //public const uint ERROR_INVALID_SERVICE_LOCK = 1071;
      //public const uint ERROR_SERVICE_MARKED_FOR_DELETE = 1072;
      //public const uint ERROR_SERVICE_EXISTS = 1073;
      //public const uint ERROR_ALREADY_RUNNING_LKG = 1074;
      //public const uint ERROR_SERVICE_DEPENDENCY_DELETED = 1075;
      //public const uint ERROR_BOOT_ALREADY_ACCEPTED = 1076;
      //public const uint ERROR_SERVICE_NEVER_STARTED = 1077;
      //public const uint ERROR_DUPLICATE_SERVICE_NAME = 1078;
      //public const uint ERROR_DIFFERENT_SERVICE_ACCOUNT = 1079;
      //public const uint ERROR_CANNOT_DETECT_DRIVER_FAILURE = 1080;
      //public const uint ERROR_CANNOT_DETECT_PROCESS_ABORT = 1081;
      //public const uint ERROR_NO_RECOVERY_PROGRAM = 1082;
      //public const uint ERROR_SERVICE_NOT_IN_EXE = 1083;
      //public const uint ERROR_NOT_SAFEBOOT_SERVICE = 1084;
      //public const uint ERROR_END_OF_MEDIA = 1100;
      //public const uint ERROR_FILEMARK_DETECTED = 1101;
      //public const uint ERROR_BEGINNING_OF_MEDIA = 1102;
      //public const uint ERROR_SETMARK_DETECTED = 1103;
      //public const uint ERROR_NO_DATA_DETECTED = 1104;
      //public const uint ERROR_PARTITION_FAILURE = 1105;
      //public const uint ERROR_INVALID_BLOCK_LENGTH = 1106;
      //public const uint ERROR_DEVICE_NOT_PARTITIONED = 1107;
      //public const uint ERROR_UNABLE_TO_LOCK_MEDIA = 1108;
      //public const uint ERROR_UNABLE_TO_UNLOAD_MEDIA = 1109;
      //public const uint ERROR_MEDIA_CHANGED = 1110;
      //public const uint ERROR_BUS_RESET = 1111;
      //public const uint ERROR_NO_MEDIA_IN_DRIVE = 1112;
      //public const uint ERROR_NO_UNICODE_TRANSLATION = 1113;
      //public const uint ERROR_DLL_INIT_FAILED = 1114;
      //public const uint ERROR_SHUTDOWN_IN_PROGRESS = 1115;
      //public const uint ERROR_NO_SHUTDOWN_IN_PROGRESS = 1116;
      //public const uint ERROR_IO_DEVICE = 1117;
      //public const uint ERROR_SERIAL_NO_DEVICE = 1118;
      //public const uint ERROR_IRQ_BUSY = 1119;
      //public const uint ERROR_MORE_WRITES = 1120;
      //public const uint ERROR_COUNTER_TIMEOUT = 1121;
      //public const uint ERROR_FLOPPY_ID_MARK_NOT_FOUND = 1122;
      //public const uint ERROR_FLOPPY_WRONG_CYLINDER = 1123;
      //public const uint ERROR_FLOPPY_UNKNOWN_ERROR = 1124;
      //public const uint ERROR_FLOPPY_BAD_REGISTERS = 1125;
      //public const uint ERROR_DISK_RECALIBRATE_FAILED = 1126;
      //public const uint ERROR_DISK_OPERATION_FAILED = 1127;
      //public const uint ERROR_DISK_RESET_FAILED = 1128;
      //public const uint ERROR_EOM_OVERFLOW = 1129;
      //public const uint ERROR_NOT_ENOUGH_SERVER_MEMORY = 1130;
      //public const uint ERROR_POSSIBLE_DEADLOCK = 1131;
      //public const uint ERROR_MAPPED_ALIGNMENT = 1132;
      //public const uint ERROR_SET_POWER_STATE_VETOED = 1140;
      //public const uint ERROR_SET_POWER_STATE_FAILED = 1141;
      //public const uint ERROR_TOO_MANY_LINKS = 1142;

      /// <summary>(1150) The specified program requires a newer version of Windows.</summary>
      public const uint ERROR_OLD_WIN_VERSION = 1150;

      //public const uint ERROR_APP_WRONG_OS = 1151;
      //public const uint ERROR_SINGLE_INSTANCE_APP = 1152;
      //public const uint ERROR_RMODE_APP = 1153;
      //public const uint ERROR_INVALID_DLL = 1154;
      //public const uint ERROR_NO_ASSOCIATION = 1155;
      //public const uint ERROR_DDE_FAIL = 1156;
      //public const uint ERROR_DLL_NOT_FOUND = 1157;
      //public const uint ERROR_NO_MORE_USER_HANDLES = 1158;
      //public const uint ERROR_MESSAGE_SYNC_ONLY = 1159;
      //public const uint ERROR_SOURCE_ELEMENT_EMPTY = 1160;
      //public const uint ERROR_DESTINATION_ELEMENT_FULL = 1161;
      //public const uint ERROR_ILLEGAL_ELEMENT_ADDRESS = 1162;
      //public const uint ERROR_MAGAZINE_NOT_PRESENT = 1163;
      //public const uint ERROR_DEVICE_REINITIALIZATION_NEEDED = 1164;   
      //public const uint ERROR_DEVICE_REQUIRES_CLEANING = 1165;
      //public const uint ERROR_DEVICE_DOOR_OPEN = 1166;
      //public const uint ERROR_DEVICE_NOT_CONNECTED = 1167;
      //public const uint ERROR_NOT_FOUND = 1168;
      //public const uint ERROR_NO_MATCH = 1169;
      //public const uint ERROR_SET_NOT_FOUND = 1170;
      //public const uint ERROR_POINT_NOT_FOUND = 1171;
      //public const uint ERROR_NO_TRACKING_SERVICE = 1172;
      //public const uint ERROR_NO_VOLUME_ID = 1173;
      //public const uint ERROR_UNABLE_TO_REMOVE_REPLACED = 1175;
      //public const uint ERROR_UNABLE_TO_MOVE_REPLACEMENT = 1176;
      //public const uint ERROR_UNABLE_TO_MOVE_REPLACEMENT_2 = 1177;
      //public const uint ERROR_JOURNAL_DELETE_IN_PROGRESS = 1178;
      //public const uint ERROR_JOURNAL_NOT_ACTIVE = 1179;
      //public const uint ERROR_POTENTIAL_FILE_FOUND = 1180;
      //public const uint ERROR_JOURNAL_ENTRY_DELETED = 1181;

      //// <summary>(1200) The specified device name is invalid.</summary>
      //public const uint ERROR_BAD_DEVICE = 1200;

      //public const uint ERROR_CONNECTION_UNAVAIL = 1201;
      //public const uint ERROR_DEVICE_ALREADY_REMEMBERED = 1202;
      //public const uint ERROR_NO_NET_OR_BAD_PATH = 1203;
      //public const uint ERROR_BAD_PROVIDER = 1204;
      //public const uint ERROR_CANNOT_OPEN_PROFILE = 1205;
      //public const uint ERROR_BAD_PROFILE = 1206;
      //public const uint ERROR_NOT_CONTAINER = 1207;

      //// <summary>(1208) An extended error has occurred.</summary>
      //public const uint ERROR_EXTENDED_ERROR = 1208;

      //public const uint ERROR_INVALID_GROUPNAME = 1209;
      //public const uint ERROR_INVALID_COMPUTERNAME = 1210;
      //public const uint ERROR_INVALID_EVENTNAME = 1211;
      //public const uint ERROR_INVALID_DOMAINNAME = 1212;
      //public const uint ERROR_INVALID_SERVICENAME = 1213;
      //public const uint ERROR_INVALID_NETNAME = 1214;
      //public const uint ERROR_INVALID_SHARENAME = 1215;
      //public const uint ERROR_INVALID_PASSWORDNAME = 1216;
      //public const uint ERROR_INVALID_MESSAGENAME = 1217;
      //public const uint ERROR_INVALID_MESSAGEDEST = 1218;
      //public const uint ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;
      //public const uint ERROR_REMOTE_SESSION_LIMIT_EXCEEDED = 1220;
      //public const uint ERROR_DUP_DOMAINNAME = 1221;

      //// <summary>(1222) The network is not present or not started.</summary>
      //public const uint ERROR_NO_NETWORK = 1222;

      //public const uint ERROR_CANCELLED = 1223;
      //public const uint ERROR_USER_MAPPED_FILE = 1224;
      //public const uint ERROR_CONNECTION_REFUSED = 1225;
      //public const uint ERROR_GRACEFUL_DISCONNECT = 1226;
      //public const uint ERROR_ADDRESS_ALREADY_ASSOCIATED = 1227;
      //public const uint ERROR_ADDRESS_NOT_ASSOCIATED = 1228;
      //public const uint ERROR_CONNECTION_INVALID = 1229;
      //public const uint ERROR_CONNECTION_ACTIVE = 1230;
      //public const uint ERROR_NETWORK_UNREACHABLE = 1231;
      //public const uint ERROR_HOST_UNREACHABLE = 1232;
      //public const uint ERROR_PROTOCOL_UNREACHABLE = 1233;
      //public const uint ERROR_PORT_UNREACHABLE = 1234;

      /// <summary>(1235) The request was aborted.</summary>
      public const uint ERROR_REQUEST_ABORTED = 1235;

      //public const uint ERROR_CONNECTION_ABORTED = 1236;
      //public const uint ERROR_RETRY = 1237;
      //public const uint ERROR_CONNECTION_COUNT_LIMIT = 1238;
      //public const uint ERROR_LOGIN_TIME_RESTRICTION = 1239;
      //public const uint ERROR_LOGIN_WKSTA_RESTRICTION = 1240;
      //public const uint ERROR_INCORRECT_ADDRESS = 1241;
      //public const uint ERROR_ALREADY_REGISTERED = 1242;
      //public const uint ERROR_SERVICE_NOT_FOUND = 1243;
      //public const uint ERROR_NOT_AUTHENTICATED = 1244;
      //public const uint ERROR_NOT_LOGGED_ON = 1245;
      //public const uint ERROR_CONTINUE = 1246;   
      //public const uint ERROR_ALREADY_INITIALIZED = 1247;
      //public const uint ERROR_NO_MORE_DEVICES = 1248;   
      //public const uint ERROR_NO_SUCH_SITE = 1249;
      //public const uint ERROR_DOMAIN_CONTROLLER_EXISTS = 1250;
      //public const uint ERROR_ONLY_IF_CONNECTED = 1251;
      //public const uint ERROR_OVERRIDE_NOCHANGES = 1252;
      //public const uint ERROR_BAD_USER_PROFILE = 1253;
      //public const uint ERROR_NOT_SUPPORTED_ON_SBS = 1254;
      //public const uint ERROR_SERVER_SHUTDOWN_IN_PROGRESS = 1255;
      //public const uint ERROR_HOST_DOWN = 1256;
      //public const uint ERROR_NON_ACCOUNT_SID = 1257;
      //public const uint ERROR_NON_DOMAIN_SID = 1258;
      //public const uint ERROR_APPHELP_BLOCK = 1259;
      //public const uint ERROR_ACCESS_DISABLED_BY_POLICY = 1260;
      //public const uint ERROR_REG_NAT_CONSUMPTION = 1261;
      //public const uint ERROR_CSCSHARE_OFFLINE = 1262;
      //public const uint ERROR_PKINIT_FAILURE = 1263;
      //public const uint ERROR_SMARTCARD_SUBSYSTEM_FAILURE = 1264;
      //public const uint ERROR_DOWNGRADE_DETECTED = 1265;
      //public const uint ERROR_MACHINE_LOCKED = 1271;
      //public const uint ERROR_CALLBACK_SUPPLIED_INVALID_DATA = 1273;
      //public const uint ERROR_SYNC_FOREGROUND_REFRESH_REQUIRED = 1274;
      //public const uint ERROR_DRIVER_BLOCKED = 1275;
      //public const uint ERROR_INVALID_IMPORT_OF_NON_DLL = 1276;
      //public const uint ERROR_ACCESS_DISABLED_WEBBLADE = 1277;
      //public const uint ERROR_ACCESS_DISABLED_WEBBLADE_TAMPER = 1278;
      //public const uint ERROR_RECOVERY_FAILURE = 1279;
      //public const uint ERROR_ALREADY_FIBER = 1280;
      //public const uint ERROR_ALREADY_THREAD = 1281;
      //public const uint ERROR_STACK_BUFFER_OVERRUN = 1282;
      //public const uint ERROR_PARAMETER_QUOTA_EXCEEDED = 1283;
      //public const uint ERROR_DEBUGGER_INACTIVE = 1284;
      //public const uint ERROR_DELAY_LOAD_FAILED = 1285;
      //public const uint ERROR_VDM_DISALLOWED = 1286;
      //public const uint ERROR_UNIDENTIFIED_ERROR = 1287;
      //public const uint ERROR_NOT_ALL_ASSIGNED = 1300;
      //public const uint ERROR_SOME_NOT_MAPPED = 1301;
      //public const uint ERROR_NO_QUOTAS_FOR_ACCOUNT = 1302;
      //public const uint ERROR_LOCAL_USER_SESSION_KEY = 1303;
      //public const uint ERROR_NULL_LM_PASSWORD = 1304;
      //public const uint ERROR_UNKNOWN_REVISION = 1305;
      //public const uint ERROR_REVISION_MISMATCH = 1306;
      //public const uint ERROR_INVALID_OWNER = 1307;
      //public const uint ERROR_INVALID_PRIMARY_GROUP = 1308;
      //public const uint ERROR_NO_IMPERSONATION_TOKEN = 1309;
      //public const uint ERROR_CANT_DISABLE_MANDATORY = 1310;
      //public const uint ERROR_NO_LOGON_SERVERS = 1311;
      //public const uint ERROR_NO_SUCH_LOGON_SESSION = 1312;
      //public const uint ERROR_NO_SUCH_PRIVILEGE = 1313;
      //public const uint ERROR_PRIVILEGE_NOT_HELD = 1314;
      //public const uint ERROR_INVALID_ACCOUNT_NAME = 1315;
      //public const uint ERROR_USER_EXISTS = 1316;
      //public const uint ERROR_NO_SUCH_USER = 1317;
      //public const uint ERROR_GROUP_EXISTS = 1318;
      //public const uint ERROR_NO_SUCH_GROUP = 1319;
      //public const uint ERROR_MEMBER_IN_GROUP = 1320;
      //public const uint ERROR_MEMBER_NOT_IN_GROUP = 1321;
      //public const uint ERROR_LAST_ADMIN = 1322;
      //public const uint ERROR_WRONG_PASSWORD = 1323;
      //public const uint ERROR_ILL_FORMED_PASSWORD = 1324;
      //public const uint ERROR_PASSWORD_RESTRICTION = 1325;
      //public const uint ERROR_LOGON_FAILURE = 1326;
      //public const uint ERROR_ACCOUNT_RESTRICTION = 1327;
      //public const uint ERROR_INVALID_LOGON_HOURS = 1328;
      //public const uint ERROR_INVALID_WORKSTATION = 1329;
      //public const uint ERROR_PASSWORD_EXPIRED = 1330;
      //public const uint ERROR_ACCOUNT_DISABLED = 1331;
      //public const uint ERROR_NONE_MAPPED = 1332;
      //public const uint ERROR_TOO_MANY_LUIDS_REQUESTED = 1333;
      //public const uint ERROR_LUIDS_EXHAUSTED = 1334;
      //public const uint ERROR_INVALID_SUB_AUTHORITY = 1335;
      //public const uint ERROR_INVALID_ACL = 1336;
      //public const uint ERROR_INVALID_SID = 1337;

      /// <summary>(1338) The security descriptor structure is invalid.</summary>
      public const uint ERROR_INVALID_SECURITY_DESCR = 1338;

      //public const uint ERROR_BAD_INHERITANCE_ACL = 1340;
      //public const uint ERROR_SERVER_DISABLED = 1341;
      //public const uint ERROR_SERVER_NOT_DISABLED = 1342;
      //public const uint ERROR_INVALID_ID_AUTHORITY = 1343;
      //public const uint ERROR_ALLOTTED_SPACE_EXCEEDED = 1344;
      //public const uint ERROR_INVALID_GROUP_ATTRIBUTES = 1345;
      //public const uint ERROR_BAD_IMPERSONATION_LEVEL = 1346;
      //public const uint ERROR_CANT_OPEN_ANONYMOUS = 1347;
      //public const uint ERROR_BAD_VALIDATION_CLASS = 1348;
      //public const uint ERROR_BAD_TOKEN_TYPE = 1349;
      //public const uint ERROR_NO_SECURITY_ON_OBJECT = 1350;
      //public const uint ERROR_CANT_ACCESS_DOMAIN_INFO = 1351;
      //public const uint ERROR_INVALID_SERVER_STATE = 1352;
      //public const uint ERROR_INVALID_DOMAIN_STATE = 1353;
      //public const uint ERROR_INVALID_DOMAIN_ROLE = 1354;
      //public const uint ERROR_NO_SUCH_DOMAIN = 1355;
      //public const uint ERROR_DOMAIN_EXISTS = 1356;
      //public const uint ERROR_DOMAIN_LIMIT_EXCEEDED = 1357;
      //public const uint ERROR_INTERNAL_DB_CORRUPTION = 1358;
      //public const uint ERROR_INTERNAL_ERROR = 1359;
      //public const uint ERROR_GENERIC_NOT_MAPPED = 1360;
      //public const uint ERROR_BAD_DESCRIPTOR_FORMAT = 1361;
      //public const uint ERROR_NOT_LOGON_PROCESS = 1362;
      //public const uint ERROR_LOGON_SESSION_EXISTS = 1363;
      //public const uint ERROR_NO_SUCH_PACKAGE = 1364;
      //public const uint ERROR_BAD_LOGON_SESSION_STATE = 1365;
      //public const uint ERROR_LOGON_SESSION_COLLISION = 1366;
      //public const uint ERROR_INVALID_LOGON_TYPE = 1367;
      //public const uint ERROR_CANNOT_IMPERSONATE = 1368;
      //public const uint ERROR_RXACT_INVALID_STATE = 1369;
      //public const uint ERROR_RXACT_COMMIT_FAILURE = 1370;
      //public const uint ERROR_SPECIAL_ACCOUNT = 1371;
      //public const uint ERROR_SPECIAL_GROUP = 1372;
      //public const uint ERROR_SPECIAL_USER = 1373;
      //public const uint ERROR_MEMBERS_PRIMARY_GROUP = 1374;
      //public const uint ERROR_TOKEN_ALREADY_IN_USE = 1375;
      //public const uint ERROR_NO_SUCH_ALIAS = 1376;
      //public const uint ERROR_MEMBER_NOT_IN_ALIAS = 1377;
      //public const uint ERROR_MEMBER_IN_ALIAS = 1378;
      //public const uint ERROR_ALIAS_EXISTS = 1379;
      //public const uint ERROR_LOGON_NOT_GRANTED = 1380;
      //public const uint ERROR_TOO_MANY_SECRETS = 1381;
      //public const uint ERROR_SECRET_TOO_LONG = 1382;
      //public const uint ERROR_INTERNAL_DB_ERROR = 1383;
      //public const uint ERROR_TOO_MANY_CONTEXT_IDS = 1384;
      //public const uint ERROR_LOGON_TYPE_NOT_GRANTED = 1385;
      //public const uint ERROR_NT_CROSS_ENCRYPTION_REQUIRED = 1386;
      //public const uint ERROR_NO_SUCH_MEMBER = 1387;
      //public const uint ERROR_INVALID_MEMBER = 1388;
      //public const uint ERROR_TOO_MANY_SIDS = 1389;
      //public const uint ERROR_LM_CROSS_ENCRYPTION_REQUIRED = 1390;
      //public const uint ERROR_NO_INHERITANCE = 1391;
      //public const uint ERROR_FILE_CORRUPT = 1392;
      //public const uint ERROR_DISK_CORRUPT = 1393;
      //public const uint ERROR_NO_USER_SESSION_KEY = 1394;
      //public const uint ERROR_LICENSE_QUOTA_EXCEEDED = 1395;
      //public const uint ERROR_WRONG_TARGET_NAME = 1396;
      //public const uint ERROR_MUTUAL_AUTH_FAILED = 1397;
      //public const uint ERROR_TIME_SKEW = 1398;
      //public const uint ERROR_CURRENT_DOMAIN_NOT_ALLOWED = 1399;
      //public const uint ERROR_INVALID_WINDOW_HANDLE = 1400;
      //public const uint ERROR_INVALID_MENU_HANDLE = 1401;
      //public const uint ERROR_INVALID_CURSOR_HANDLE = 1402;
      //public const uint ERROR_INVALID_ACCEL_HANDLE = 1403;
      //public const uint ERROR_INVALID_HOOK_HANDLE = 1404;
      //public const uint ERROR_INVALID_DWP_HANDLE = 1405;
      //public const uint ERROR_TLW_WITH_WSCHILD = 1406;
      //public const uint ERROR_CANNOT_FIND_WND_CLASS = 1407;
      //public const uint ERROR_WINDOW_OF_OTHER_THREAD = 1408;
      //public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;
      //public const uint ERROR_CLASS_ALREADY_EXISTS = 1410;
      //public const uint ERROR_CLASS_DOES_NOT_EXIST = 1411;
      //public const uint ERROR_CLASS_HAS_WINDOWS = 1412;
      //public const uint ERROR_INVALID_INDEX = 1413;
      //public const uint ERROR_INVALID_ICON_HANDLE = 1414;
      //public const uint ERROR_PRIVATE_DIALOG_INDEX = 1415;
      //public const uint ERROR_LISTBOX_ID_NOT_FOUND = 1416;
      //public const uint ERROR_NO_WILDCARD_CHARACTERS = 1417;
      //public const uint ERROR_CLIPBOARD_NOT_OPEN = 1418;
      //public const uint ERROR_HOTKEY_NOT_REGISTERED = 1419;
      //public const uint ERROR_WINDOW_NOT_DIALOG = 1420;
      //public const uint ERROR_CONTROL_ID_NOT_FOUND = 1421;
      //public const uint ERROR_INVALID_COMBOBOX_MESSAGE = 1422;
      //public const uint ERROR_WINDOW_NOT_COMBOBOX = 1423;
      //public const uint ERROR_INVALID_EDIT_HEIGHT = 1424;
      //public const uint ERROR_DC_NOT_FOUND = 1425;
      //public const uint ERROR_INVALID_HOOK_FILTER = 1426;
      //public const uint ERROR_INVALID_FILTER_PROC = 1427;
      //public const uint ERROR_HOOK_NEEDS_HMOD = 1428;
      //public const uint ERROR_GLOBAL_ONLY_HOOK = 1429;
      //public const uint ERROR_JOURNAL_HOOK_SET = 1430;
      //public const uint ERROR_HOOK_NOT_INSTALLED = 1431;
      //public const uint ERROR_INVALID_LB_MESSAGE = 1432;
      //public const uint ERROR_SETCOUNT_ON_BAD_LB = 1433;
      //public const uint ERROR_LB_WITHOUT_TABSTOPS = 1434;
      //public const uint ERROR_DESTROY_OBJECT_OF_OTHER_THREAD = 1435;
      //public const uint ERROR_CHILD_WINDOW_MENU = 1436;
      //public const uint ERROR_NO_SYSTEM_MENU = 1437;
      //public const uint ERROR_INVALID_MSGBOX_STYLE = 1438;
      //public const uint ERROR_INVALID_SPI_VALUE = 1439;
      //public const uint ERROR_SCREEN_ALREADY_LOCKED = 1440;
      //public const uint ERROR_HWNDS_HAVE_DIFF_PARENT = 1441;
      //public const uint ERROR_NOT_CHILD_WINDOW = 1442;
      //public const uint ERROR_INVALID_GW_COMMAND = 1443;
      //public const uint ERROR_INVALID_THREAD_ID = 1444;
      //public const uint ERROR_NON_MDICHILD_WINDOW = 1445;
      //public const uint ERROR_POPUP_ALREADY_ACTIVE = 1446;
      //public const uint ERROR_NO_SCROLLBARS = 1447;
      //public const uint ERROR_INVALID_SCROLLBAR_RANGE = 1448;
      //public const uint ERROR_INVALID_SHOWWIN_COMMAND = 1449;
      //public const uint ERROR_NO_SYSTEM_RESOURCES = 1450;
      //public const uint ERROR_NONPAGED_SYSTEM_RESOURCES = 1451;
      //public const uint ERROR_PAGED_SYSTEM_RESOURCES = 1452;
      //public const uint ERROR_WORKING_SET_QUOTA = 1453;
      //public const uint ERROR_PAGEFILE_QUOTA = 1454;
      //public const uint ERROR_COMMITMENT_LIMIT = 1455;
      //public const uint ERROR_MENU_ITEM_NOT_FOUND = 1456;
      //public const uint ERROR_INVALID_KEYBOARD_HANDLE = 1457;
      //public const uint ERROR_HOOK_TYPE_NOT_ALLOWED = 1458;
      //public const uint ERROR_REQUIRES_INTERACTIVE_WINDOWSTATION = 1459;
      //public const uint ERROR_TIMEOUT = 1460;
      //public const uint ERROR_INVALID_MONITOR_HANDLE = 1461;
      //public const uint ERROR_INCORRECT_SIZE = 1462;
      //public const uint ERROR_EVENTLOG_FILE_CORRUPT = 1500;
      //public const uint ERROR_EVENTLOG_CANT_START = 1501;
      //public const uint ERROR_LOG_FILE_FULL = 1502;
      //public const uint ERROR_EVENTLOG_FILE_CHANGED = 1503;
      //public const uint ERROR_INSTALL_SERVICE_FAILURE = 1601;
      //public const uint ERROR_INSTALL_USEREXIT = 1602;
      //public const uint ERROR_INSTALL_FAILURE = 1603;
      //public const uint ERROR_INSTALL_SUSPEND = 1604;
      //public const uint ERROR_UNKNOWN_PRODUCT = 1605;
      //public const uint ERROR_UNKNOWN_FEATURE = 1606;
      //public const uint ERROR_UNKNOWN_COMPONENT = 1607;
      //public const uint ERROR_UNKNOWN_PROPERTY = 1608;
      //public const uint ERROR_INVALID_HANDLE_STATE = 1609;
      //public const uint ERROR_BAD_CONFIGURATION = 1610;
      //public const uint ERROR_INDEX_ABSENT = 1611;
      //public const uint ERROR_INSTALL_SOURCE_ABSENT = 1612;
      //public const uint ERROR_INSTALL_PACKAGE_VERSION = 1613;
      //public const uint ERROR_PRODUCT_UNINSTALLED = 1614;
      //public const uint ERROR_BAD_QUERY_SYNTAX = 1615;
      //public const uint ERROR_INVALID_FIELD = 1616;
      //public const uint ERROR_DEVICE_REMOVED = 1617;
      //public const uint ERROR_INSTALL_ALREADY_RUNNING = 1618;
      //public const uint ERROR_INSTALL_PACKAGE_OPEN_FAILED = 1619;
      //public const uint ERROR_INSTALL_PACKAGE_INVALID = 1620;
      //public const uint ERROR_INSTALL_UI_FAILURE = 1621;
      //public const uint ERROR_INSTALL_LOG_FAILURE = 1622;
      //public const uint ERROR_INSTALL_LANGUAGE_UNSUPPORTED = 1623;
      //public const uint ERROR_INSTALL_TRANSFORM_FAILURE = 1624;
      //public const uint ERROR_INSTALL_PACKAGE_REJECTED = 1625;
      //public const uint ERROR_FUNCTION_NOT_CALLED = 1626;
      //public const uint ERROR_FUNCTION_FAILED = 1627;
      //public const uint ERROR_INVALID_TABLE = 1628;
      //public const uint ERROR_DATATYPE_MISMATCH = 1629;
      //public const uint ERROR_UNSUPPORTED_TYPE = 1630;
      //public const uint ERROR_CREATE_FAILED = 1631;
      //public const uint ERROR_INSTALL_TEMP_UNWRITABLE = 1632;
      //public const uint ERROR_INSTALL_PLATFORM_UNSUPPORTED = 1633;
      //public const uint ERROR_INSTALL_NOTUSED = 1634;
      //public const uint ERROR_PATCH_PACKAGE_OPEN_FAILED = 1635;
      //public const uint ERROR_PATCH_PACKAGE_INVALID = 1636;
      //public const uint ERROR_PATCH_PACKAGE_UNSUPPORTED = 1637;
      //public const uint ERROR_PRODUCT_VERSION = 1638;
      //public const uint ERROR_INVALID_COMMAND_LINE = 1639;
      //public const uint ERROR_INSTALL_REMOTE_DISALLOWED = 1640;

      /// <summary>(1641) The requested operation completed successfully.
      /// <para>The system will be restarted so the changes can take effect.</para>
      /// </summary>
      public const uint ERROR_SUCCESS_REBOOT_INITIATED = 1641;

      //public const uint ERROR_PATCH_TARGET_NOT_FOUND = 1642;
      //public const uint ERROR_PATCH_PACKAGE_REJECTED = 1643;
      //public const uint ERROR_INSTALL_TRANSFORM_REJECTED = 1644;
      //public const uint ERROR_INSTALL_REMOTE_PROHIBITED = 1645;
      //public const uint RPC_S_INVALID_STRING_BINDING = 1700;
      //public const uint RPC_S_WRONG_KIND_OF_BINDING = 1701;
      //public const uint RPC_S_INVALID_BINDING = 1702;
      //public const uint RPC_S_PROTSEQ_NOT_SUPPORTED = 1703;
      //public const uint RPC_S_INVALID_RPC_PROTSEQ = 1704;
      //public const uint RPC_S_INVALID_STRING_UUID = 1705;
      //public const uint RPC_S_INVALID_ENDPOINT_FORMAT = 1706;
      //public const uint RPC_S_INVALID_NET_ADDR = 1707;
      //public const uint RPC_S_NO_ENDPOINT_FOUND = 1708;
      //public const uint RPC_S_INVALID_TIMEOUT = 1709;
      //public const uint RPC_S_OBJECT_NOT_FOUND = 1710;
      //public const uint RPC_S_ALREADY_REGISTERED = 1711;
      //public const uint RPC_S_TYPE_ALREADY_REGISTERED = 1712;
      //public const uint RPC_S_ALREADY_LISTENING = 1713;
      //public const uint RPC_S_NO_PROTSEQS_REGISTERED = 1714;
      //public const uint RPC_S_NOT_LISTENING = 1715;
      //public const uint RPC_S_UNKNOWN_MGR_TYPE = 1716;
      //public const uint RPC_S_UNKNOWN_IF = 1717;
      //public const uint RPC_S_NO_BINDINGS = 1718;
      //public const uint RPC_S_NO_PROTSEQS = 1719;
      //public const uint RPC_S_CANT_CREATE_ENDPOINT = 1720;
      //public const uint RPC_S_OUT_OF_RESOURCES = 1721;
      //public const uint RPC_S_SERVER_UNAVAILABLE = 1722;
      //public const uint RPC_S_SERVER_TOO_BUSY = 1723;
      //public const uint RPC_S_INVALID_NETWORK_OPTIONS = 1724;
      //public const uint RPC_S_NO_CALL_ACTIVE = 1725;
      //public const uint RPC_S_CALL_FAILED = 1726;
      //public const uint RPC_S_CALL_FAILED_DNE = 1727;
      //public const uint RPC_S_PROTOCOL_ERROR = 1728;
      //public const uint RPC_S_UNSUPPORTED_TRANS_SYN = 1730;
      //public const uint RPC_S_UNSUPPORTED_TYPE = 1732;
      //public const uint RPC_S_INVALID_TAG = 1733;
      //public const uint RPC_S_INVALID_BOUND = 1734;
      //public const uint RPC_S_NO_ENTRY_NAME = 1735;
      //public const uint RPC_S_INVALID_NAME_SYNTAX = 1736;
      //public const uint RPC_S_UNSUPPORTED_NAME_SYNTAX = 1737;
      //public const uint RPC_S_UUID_NO_ADDRESS = 1739;
      //public const uint RPC_S_DUPLICATE_ENDPOINT = 1740;
      //public const uint RPC_S_UNKNOWN_AUTHN_TYPE = 1741;
      //public const uint RPC_S_MAX_CALLS_TOO_SMALL = 1742;
      //public const uint RPC_S_STRING_TOO_LONG = 1743;
      //public const uint RPC_S_PROTSEQ_NOT_FOUND = 1744;
      //public const uint RPC_S_PROCNUM_OUT_OF_RANGE = 1745;
      //public const uint RPC_S_BINDING_HAS_NO_AUTH = 1746;
      //public const uint RPC_S_UNKNOWN_AUTHN_SERVICE = 1747;
      //public const uint RPC_S_UNKNOWN_AUTHN_LEVEL = 1748;
      //public const uint RPC_S_INVALID_AUTH_IDENTITY = 1749;
      //public const uint RPC_S_UNKNOWN_AUTHZ_SERVICE = 1750;
      //public const uint EPT_S_INVALID_ENTRY = 1751;
      //public const uint EPT_S_CANT_PERFORM_OP = 1752;
      //public const uint EPT_S_NOT_REGISTERED = 1753;
      //public const uint RPC_S_NOTHING_TO_EXPORT = 1754;
      //public const uint RPC_S_INCOMPLETE_NAME = 1755;
      //public const uint RPC_S_INVALID_VERS_OPTION = 1756;
      //public const uint RPC_S_NO_MORE_MEMBERS = 1757;
      //public const uint RPC_S_NOT_ALL_OBJS_UNEXPORTED = 1758;
      //public const uint RPC_S_INTERFACE_NOT_FOUND = 1759;
      //public const uint RPC_S_ENTRY_ALREADY_EXISTS = 1760;
      //public const uint RPC_S_ENTRY_NOT_FOUND = 1761;
      //public const uint RPC_S_NAME_SERVICE_UNAVAILABLE = 1762;
      //public const uint RPC_S_INVALID_NAF_ID = 1763;
      //public const uint RPC_S_CANNOT_SUPPORT = 1764;
      //public const uint RPC_S_NO_CONTEXT_AVAILABLE = 1765;
      //public const uint RPC_S_INTERNAL_ERROR = 1766;
      //public const uint RPC_S_ZERO_DIVIDE = 1767;
      //public const uint RPC_S_ADDRESS_ERROR = 1768;
      //public const uint RPC_S_FP_DIV_ZERO = 1769;
      //public const uint RPC_S_FP_UNDERFLOW = 1770;
      //public const uint RPC_S_FP_OVERFLOW = 1771;
      //public const uint RPC_X_NO_MORE_ENTRIES = 1772;
      //public const uint RPC_X_SS_CHAR_TRANS_OPEN_FAIL = 1773;
      //public const uint RPC_X_SS_CHAR_TRANS_SHORT_FILE = 1774;
      //public const uint RPC_X_SS_IN_NULL_CONTEXT = 1775;
      //public const uint RPC_X_SS_CONTEXT_DAMAGED = 1777;
      //public const uint RPC_X_SS_HANDLES_MISMATCH = 1778;
      //public const uint RPC_X_SS_CANNOT_GET_CALL_HANDLE = 1779;
      //public const uint RPC_X_NULL_REF_POINTER = 1780;
      //public const uint RPC_X_ENUM_VALUE_OUT_OF_RANGE = 1781;
      //public const uint RPC_X_BYTE_COUNT_TOO_SMALL = 1782;

      /// <summary>(1783) The stub received bad data.</summary>
      public const uint RPC_X_BAD_STUB_DATA = 1783;

      //public const uint ERROR_INVALID_USER_BUFFER = 1784;
      //public const uint ERROR_UNRECOGNIZED_MEDIA = 1785;
      //public const uint ERROR_NO_TRUST_LSA_SECRET = 1786;
      //public const uint ERROR_NO_TRUST_SAM_ACCOUNT = 1787;
      //public const uint ERROR_TRUSTED_DOMAIN_FAILURE = 1788;
      //public const uint ERROR_TRUSTED_RELATIONSHIP_FAILURE = 1789;
      //public const uint ERROR_TRUST_FAILURE = 1790;
      //public const uint RPC_S_CALL_IN_PROGRESS = 1791;
      //public const uint ERROR_NETLOGON_NOT_STARTED = 1792;
      //public const uint ERROR_ACCOUNT_EXPIRED = 1793;
      //public const uint ERROR_REDIRECTOR_HAS_OPEN_HANDLES = 1794;
      //public const uint ERROR_PRINTER_DRIVER_ALREADY_INSTALLED = 1795;
      //public const uint ERROR_UNKNOWN_PORT = 1796;
      //public const uint ERROR_UNKNOWN_PRINTER_DRIVER = 1797;
      //public const uint ERROR_UNKNOWN_PRINTPROCESSOR = 1798;
      //public const uint ERROR_INVALID_SEPARATOR_FILE = 1799;
      //public const uint ERROR_INVALID_PRIORITY = 1800;
      //public const uint ERROR_INVALID_PRINTER_NAME = 1801;
      //public const uint ERROR_PRINTER_ALREADY_EXISTS = 1802;
      //public const uint ERROR_INVALID_PRINTER_COMMAND = 1803;
      //public const uint ERROR_INVALID_DATATYPE = 1804;
      //public const uint ERROR_INVALID_ENVIRONMENT = 1805;
      //public const uint RPC_S_NO_MORE_BINDINGS = 1806;
      //public const uint ERROR_NOLOGON_INTERDOMAIN_TRUST_ACCOUNT = 1807;
      //public const uint ERROR_NOLOGON_WORKSTATION_TRUST_ACCOUNT = 1808;
      //public const uint ERROR_NOLOGON_SERVER_TRUST_ACCOUNT = 1809;
      //public const uint ERROR_DOMAIN_TRUST_INCONSISTENT = 1810;
      //public const uint ERROR_SERVER_HAS_OPEN_HANDLES = 1811;
      //public const uint ERROR_RESOURCE_DATA_NOT_FOUND = 1812;
      //public const uint ERROR_RESOURCE_TYPE_NOT_FOUND = 1813;
      //public const uint ERROR_RESOURCE_NAME_NOT_FOUND = 1814;
      //public const uint ERROR_RESOURCE_LANG_NOT_FOUND = 1815;
      //public const uint ERROR_NOT_ENOUGH_QUOTA = 1816;
      //public const uint RPC_S_NO_INTERFACES = 1817;
      //public const uint RPC_S_CALL_CANCELLED = 1818;
      //public const uint RPC_S_BINDING_INCOMPLETE = 1819;
      //public const uint RPC_S_COMM_FAILURE = 1820;
      //public const uint RPC_S_UNSUPPORTED_AUTHN_LEVEL = 1821;
      //public const uint RPC_S_NO_PRINC_NAME = 1822;
      //public const uint RPC_S_NOT_RPC_ERROR = 1823;
      //public const uint RPC_S_UUID_LOCAL_ONLY = 1824;
      //public const uint RPC_S_SEC_PKG_ERROR = 1825;
      //public const uint RPC_S_NOT_CANCELLED = 1826;
      //public const uint RPC_X_INVALID_ES_ACTION = 1827;
      //public const uint RPC_X_WRONG_ES_VERSION = 1828;
      //public const uint RPC_X_WRONG_STUB_VERSION = 1829;
      //public const uint RPC_X_INVALID_PIPE_OBJECT = 1830;
      //public const uint RPC_X_WRONG_PIPE_ORDER = 1831;
      //public const uint RPC_X_WRONG_PIPE_VERSION = 1832;
      //public const uint RPC_S_GROUP_MEMBER_NOT_FOUND = 1898;
      //public const uint EPT_S_CANT_CREATE = 1899;
      //public const uint RPC_S_INVALID_OBJECT = 1900;
      //public const uint ERROR_INVALID_TIME = 1901;
      //public const uint ERROR_INVALID_FORM_NAME = 1902;
      //public const uint ERROR_INVALID_FORM_SIZE = 1903;
      //public const uint ERROR_ALREADY_WAITING = 1904;
      //public const uint ERROR_PRINTER_DELETED = 1905;
      //public const uint ERROR_INVALID_PRINTER_STATE = 1906;
      //public const uint ERROR_PASSWORD_MUST_CHANGE = 1907;
      //public const uint ERROR_DOMAIN_CONTROLLER_NOT_FOUND = 1908;
      //public const uint ERROR_ACCOUNT_LOCKED_OUT = 1909;
      //public const uint OR_INVALID_OXID = 1910;
      //public const uint OR_INVALID_OID = 1911;
      //public const uint OR_INVALID_SET = 1912;
      //public const uint RPC_S_SEND_INCOMPLETE = 1913;
      //public const uint RPC_S_INVALID_ASYNC_HANDLE = 1914;
      //public const uint RPC_S_INVALID_ASYNC_CALL = 1915;
      //public const uint RPC_X_PIPE_CLOSED = 1916;
      //public const uint RPC_X_PIPE_DISCIPLINE_ERROR = 1917;
      //public const uint RPC_X_PIPE_EMPTY = 1918;
      //public const uint ERROR_NO_SITENAME = 1919;
      //public const uint ERROR_CANT_ACCESS_FILE = 1920;
      //public const uint ERROR_CANT_RESOLVE_FILENAME = 1921;
      //public const uint RPC_S_ENTRY_TYPE_MISMATCH = 1922;
      //public const uint RPC_S_NOT_ALL_OBJS_EXPORTED = 1923;
      //public const uint RPC_S_INTERFACE_NOT_EXPORTED = 1924;
      //public const uint RPC_S_PROFILE_NOT_ADDED = 1925;
      //public const uint RPC_S_PRF_ELT_NOT_ADDED = 1926;
      //public const uint RPC_S_PRF_ELT_NOT_REMOVED = 1927;
      //public const uint RPC_S_GRP_ELT_NOT_ADDED = 1928;
      //public const uint RPC_S_GRP_ELT_NOT_REMOVED = 1929;
      //public const uint ERROR_KM_DRIVER_BLOCKED = 1930;
      //public const uint ERROR_CONTEXT_EXPIRED = 1931;
      //public const uint ERROR_PER_USER_TRUST_QUOTA_EXCEEDED = 1932;
      //public const uint ERROR_ALL_USER_TRUST_QUOTA_EXCEEDED = 1933;
      //public const uint ERROR_USER_DELETE_TRUST_QUOTA_EXCEEDED = 1934;
      //public const uint ERROR_AUTHENTICATION_FIREWALL_FAILED = 1935;
      //public const uint ERROR_REMOTE_PRINT_CONNECTIONS_BLOCKED = 1936;
      //public const uint ERROR_INVALID_PIXEL_FORMAT = 2000;
      //public const uint ERROR_BAD_DRIVER = 2001;
      //public const uint ERROR_INVALID_WINDOW_STYLE = 2002;
      //public const uint ERROR_METAFILE_NOT_SUPPORTED = 2003;
      //public const uint ERROR_TRANSFORM_NOT_SUPPORTED = 2004;
      //public const uint ERROR_CLIPPING_NOT_SUPPORTED = 2005;
      //public const uint ERROR_INVALID_CMM = 2010;
      //public const uint ERROR_INVALID_PROFILE = 2011;
      //public const uint ERROR_TAG_NOT_FOUND = 2012;
      //public const uint ERROR_TAG_NOT_PRESENT = 2013;
      //public const uint ERROR_DUPLICATE_TAG = 2014;
      //public const uint ERROR_PROFILE_NOT_ASSOCIATED_WITH_DEVICE = 2015;
      //public const uint ERROR_PROFILE_NOT_FOUND = 2016;
      //public const uint ERROR_INVALID_COLORSPACE = 2017;
      //public const uint ERROR_ICM_NOT_ENABLED = 2018;
      //public const uint ERROR_DELETING_ICM_XFORM = 2019;
      //public const uint ERROR_INVALID_TRANSFORM = 2020;
      //public const uint ERROR_COLORSPACE_MISMATCH = 2021;
      //public const uint ERROR_INVALID_COLORINDEX = 2022;
      //public const uint ERROR_CONNECTED_OTHER_PASSWORD = 2108;
      //public const uint ERROR_CONNECTED_OTHER_PASSWORD_DEFAULT = 2109;
      //public const uint ERROR_UNKNOWN_PRINT_MONITOR = 3000;
      //public const uint ERROR_PRINTER_DRIVER_IN_USE = 3001;
      //public const uint ERROR_SPOOL_FILE_NOT_FOUND = 3002;
      //public const uint ERROR_SPL_NO_STARTDOC = 3003;
      //public const uint ERROR_SPL_NO_ADDJOB = 3004;
      //public const uint ERROR_PRINT_PROCESSOR_ALREADY_INSTALLED = 3005;
      //public const uint ERROR_PRINT_MONITOR_ALREADY_INSTALLED = 3006;
      //public const uint ERROR_INVALID_PRINT_MONITOR = 3007;
      //public const uint ERROR_PRINT_MONITOR_IN_USE = 3008;
      //public const uint ERROR_PRINTER_HAS_JOBS_QUEUED = 3009;

      /// <summary>(3010) The requested operation is successful.
      /// <para>Changes will not be effective until the system is rebooted.</para>
      /// </summary>
      public const uint ERROR_SUCCESS_REBOOT_REQUIRED = 3010;

      /// <summary>(3011) The requested operation is successful.
      /// <para>Changes will not be effective until the service is restarted.</para>
      /// </summary>
      public const uint ERROR_SUCCESS_RESTART_REQUIRED = 3011;

      //public const uint ERROR_PRINTER_NOT_FOUND = 3012;
      //public const uint ERROR_PRINTER_DRIVER_WARNED = 3013;
      //public const uint ERROR_PRINTER_DRIVER_BLOCKED = 3014;
      //public const uint ERROR_WINS_INTERNAL = 4000;
      //public const uint ERROR_CAN_NOT_DEL_LOCAL_WINS = 4001;
      //public const uint ERROR_STATIC_INIT = 4002;
      //public const uint ERROR_INC_BACKUP = 4003;
      //public const uint ERROR_FULL_BACKUP = 4004;
      //public const uint ERROR_REC_NON_EXISTENT = 4005;
      //public const uint ERROR_RPL_NOT_ALLOWED = 4006;
      //public const uint ERROR_DHCP_ADDRESS_CONFLICT = 4100;
      //public const uint ERROR_WMI_GUID_NOT_FOUND = 4200;
      //public const uint ERROR_WMI_INSTANCE_NOT_FOUND = 4201;
      //public const uint ERROR_WMI_ITEMID_NOT_FOUND = 4202;
      //public const uint ERROR_WMI_TRY_AGAIN = 4203;
      //public const uint ERROR_WMI_DP_NOT_FOUND = 4204;
      //public const uint ERROR_WMI_UNRESOLVED_INSTANCE_REF = 4205;
      //public const uint ERROR_WMI_ALREADY_ENABLED = 4206;
      //public const uint ERROR_WMI_GUID_DISCONNECTED = 4207;
      //public const uint ERROR_WMI_SERVER_UNAVAILABLE = 4208;
      //public const uint ERROR_WMI_DP_FAILED = 4209;
      //public const uint ERROR_WMI_INVALID_MOF = 4210;
      //public const uint ERROR_WMI_INVALID_REGINFO = 4211;
      //public const uint ERROR_WMI_ALREADY_DISABLED = 4212;
      //public const uint ERROR_WMI_READ_ONLY = 4213;
      //public const uint ERROR_WMI_SET_FAILURE = 4214;
      //public const uint ERROR_INVALID_MEDIA = 4300;
      //public const uint ERROR_INVALID_LIBRARY = 4301;
      //public const uint ERROR_INVALID_MEDIA_POOL = 4302;
      //public const uint ERROR_DRIVE_MEDIA_MISMATCH = 4303;
      //public const uint ERROR_MEDIA_OFFLINE = 4304;
      //public const uint ERROR_LIBRARY_OFFLINE = 4305;
      //public const uint ERROR_EMPTY = 4306;
      //public const uint ERROR_NOT_EMPTY = 4307;
      //public const uint ERROR_MEDIA_UNAVAILABLE = 4308;
      //public const uint ERROR_RESOURCE_DISABLED = 4309;
      //public const uint ERROR_INVALID_CLEANER = 4310;
      //public const uint ERROR_UNABLE_TO_CLEAN = 4311;
      //public const uint ERROR_OBJECT_NOT_FOUND = 4312;
      //public const uint ERROR_DATABASE_FAILURE = 4313;
      //public const uint ERROR_DATABASE_FULL = 4314;
      //public const uint ERROR_MEDIA_INCOMPATIBLE = 4315;
      //public const uint ERROR_RESOURCE_NOT_PRESENT = 4316;
      //public const uint ERROR_INVALID_OPERATION = 4317;
      //public const uint ERROR_MEDIA_NOT_AVAILABLE = 4318;
      //public const uint ERROR_DEVICE_NOT_AVAILABLE = 4319;
      //public const uint ERROR_REQUEST_REFUSED = 4320;
      //public const uint ERROR_INVALID_DRIVE_OBJECT = 4321;
      //public const uint ERROR_LIBRARY_FULL = 4322;
      //public const uint ERROR_MEDIUM_NOT_ACCESSIBLE = 4323;
      //public const uint ERROR_UNABLE_TO_LOAD_MEDIUM = 4324;
      //public const uint ERROR_UNABLE_TO_INVENTORY_DRIVE = 4325;
      //public const uint ERROR_UNABLE_TO_INVENTORY_SLOT = 4326;
      //public const uint ERROR_UNABLE_TO_INVENTORY_TRANSPORT = 4327;
      //public const uint ERROR_TRANSPORT_FULL = 4328;
      //public const uint ERROR_CONTROLLING_IEPORT = 4329;
      //public const uint ERROR_UNABLE_TO_EJECT_MOUNTED_MEDIA = 4330;
      //public const uint ERROR_CLEANER_SLOT_SET = 4331;
      //public const uint ERROR_CLEANER_SLOT_NOT_SET = 4332;
      //public const uint ERROR_CLEANER_CARTRIDGE_SPENT = 4333;
      //public const uint ERROR_UNEXPECTED_OMID = 4334;
      //public const uint ERROR_CANT_DELETE_LAST_ITEM = 4335;
      //public const uint ERROR_MESSAGE_EXCEEDS_MAX_SIZE = 4336;
      //public const uint ERROR_VOLUME_CONTAINS_SYS_FILES = 4337;
      //public const uint ERROR_INDIGENOUS_TYPE = 4338;
      //public const uint ERROR_NO_SUPPORTING_DRIVES = 4339;
      //public const uint ERROR_CLEANER_CARTRIDGE_INSTALLED = 4340;
      //public const uint ERROR_IEPORT_FULL = 4341;
      //public const uint ERROR_FILE_OFFLINE = 4350;
      //public const uint ERROR_REMOTE_STORAGE_NOT_ACTIVE = 4351;
      //public const uint ERROR_REMOTE_STORAGE_MEDIA_ERROR = 4352;

      /// <summary>(4390) The file or directory is not a reparse point.</summary>
      public const uint ERROR_NOT_A_REPARSE_POINT = 4390;

      //public const uint ERROR_REPARSE_ATTRIBUTE_CONFLICT = 4391;

      /// <summary>The data present in the reparse point buffer is invalid.</summary>
      public const uint ERROR_INVALID_REPARSE_DATA = 4392;

      //public const uint ERROR_REPARSE_TAG_INVALID = 4393;
      //public const uint ERROR_REPARSE_TAG_MISMATCH = 4394;
      //public const uint ERROR_VOLUME_NOT_SIS_ENABLED = 4500;
      //public const uint ERROR_DEPENDENT_RESOURCE_EXISTS = 5001;
      //public const uint ERROR_DEPENDENCY_NOT_FOUND = 5002;
      //public const uint ERROR_DEPENDENCY_ALREADY_EXISTS = 5003;
      //public const uint ERROR_RESOURCE_NOT_ONLINE = 5004;
      //public const uint ERROR_HOST_NODE_NOT_AVAILABLE = 5005;
      //public const uint ERROR_RESOURCE_NOT_AVAILABLE = 5006;
      //public const uint ERROR_RESOURCE_NOT_FOUND = 5007;
      //public const uint ERROR_SHUTDOWN_CLUSTER = 5008;
      //public const uint ERROR_CANT_EVICT_ACTIVE_NODE = 5009;
      //public const uint ERROR_OBJECT_ALREADY_EXISTS = 5010;
      //public const uint ERROR_OBJECT_IN_LIST = 5011;
      //public const uint ERROR_GROUP_NOT_AVAILABLE = 5012;
      //public const uint ERROR_GROUP_NOT_FOUND = 5013;
      //public const uint ERROR_GROUP_NOT_ONLINE = 5014;
      //public const uint ERROR_HOST_NODE_NOT_RESOURCE_OWNER = 5015;
      //public const uint ERROR_HOST_NODE_NOT_GROUP_OWNER = 5016;
      //public const uint ERROR_RESMON_CREATE_FAILED = 5017;
      //public const uint ERROR_RESMON_ONLINE_FAILED = 5018;
      //public const uint ERROR_RESOURCE_ONLINE = 5019;
      //public const uint ERROR_QUORUM_RESOURCE = 5020;
      //public const uint ERROR_NOT_QUORUM_CAPABLE = 5021;
      //public const uint ERROR_CLUSTER_SHUTTING_DOWN = 5022;
      //public const uint ERROR_INVALID_STATE = 5023;
      //public const uint ERROR_RESOURCE_PROPERTIES_STORED = 5024;
      //public const uint ERROR_NOT_QUORUM_CLASS = 5025;
      //public const uint ERROR_CORE_RESOURCE = 5026;
      //public const uint ERROR_QUORUM_RESOURCE_ONLINE_FAILED = 5027;
      //public const uint ERROR_QUORUMLOG_OPEN_FAILED = 5028;
      //public const uint ERROR_CLUSTERLOG_CORRUPT = 5029;
      //public const uint ERROR_CLUSTERLOG_RECORD_EXCEEDS_MAXSIZE = 5030;
      //public const uint ERROR_CLUSTERLOG_EXCEEDS_MAXSIZE = 5031;
      //public const uint ERROR_CLUSTERLOG_CHKPOINT_NOT_FOUND = 5032;
      //public const uint ERROR_CLUSTERLOG_NOT_ENOUGH_SPACE = 5033;
      //public const uint ERROR_QUORUM_OWNER_ALIVE = 5034;
      //public const uint ERROR_NETWORK_NOT_AVAILABLE = 5035;
      //public const uint ERROR_NODE_NOT_AVAILABLE = 5036;
      //public const uint ERROR_ALL_NODES_NOT_AVAILABLE = 5037;
      //public const uint ERROR_RESOURCE_FAILED = 5038;
      //public const uint ERROR_CLUSTER_INVALID_NODE = 5039;
      //public const uint ERROR_CLUSTER_NODE_EXISTS = 5040;
      //public const uint ERROR_CLUSTER_JOIN_IN_PROGRESS = 5041;
      //public const uint ERROR_CLUSTER_NODE_NOT_FOUND = 5042;
      //public const uint ERROR_CLUSTER_LOCAL_NODE_NOT_FOUND = 5043;
      //public const uint ERROR_CLUSTER_NETWORK_EXISTS = 5044;
      //public const uint ERROR_CLUSTER_NETWORK_NOT_FOUND = 5045;
      //public const uint ERROR_CLUSTER_NETINTERFACE_EXISTS = 5046;
      //public const uint ERROR_CLUSTER_NETINTERFACE_NOT_FOUND = 5047;
      //public const uint ERROR_CLUSTER_INVALID_REQUEST = 5048;
      //public const uint ERROR_CLUSTER_INVALID_NETWORK_PROVIDER = 5049;
      //public const uint ERROR_CLUSTER_NODE_DOWN = 5050;
      //public const uint ERROR_CLUSTER_NODE_UNREACHABLE = 5051;
      //public const uint ERROR_CLUSTER_NODE_NOT_MEMBER = 5052;
      //public const uint ERROR_CLUSTER_JOIN_NOT_IN_PROGRESS = 5053;
      //public const uint ERROR_CLUSTER_INVALID_NETWORK = 5054;
      //public const uint ERROR_CLUSTER_NODE_UP = 5056;
      //public const uint ERROR_CLUSTER_IPADDR_IN_USE = 5057;
      //public const uint ERROR_CLUSTER_NODE_NOT_PAUSED = 5058;
      //public const uint ERROR_CLUSTER_NO_SECURITY_CONTEXT = 5059;
      //public const uint ERROR_CLUSTER_NETWORK_NOT_INTERNAL = 5060;
      //public const uint ERROR_CLUSTER_NODE_ALREADY_UP = 5061;
      //public const uint ERROR_CLUSTER_NODE_ALREADY_DOWN = 5062;
      //public const uint ERROR_CLUSTER_NETWORK_ALREADY_ONLINE = 5063;
      //public const uint ERROR_CLUSTER_NETWORK_ALREADY_OFFLINE = 5064;
      //public const uint ERROR_CLUSTER_NODE_ALREADY_MEMBER = 5065;
      //public const uint ERROR_CLUSTER_LAST_INTERNAL_NETWORK = 5066;
      //public const uint ERROR_CLUSTER_NETWORK_HAS_DEPENDENTS = 5067;
      //public const uint ERROR_INVALID_OPERATION_ON_QUORUM = 5068;
      //public const uint ERROR_DEPENDENCY_NOT_ALLOWED = 5069;
      //public const uint ERROR_CLUSTER_NODE_PAUSED = 5070;
      //public const uint ERROR_NODE_CANT_HOST_RESOURCE = 5071;
      //public const uint ERROR_CLUSTER_NODE_NOT_READY = 5072;
      //public const uint ERROR_CLUSTER_NODE_SHUTTING_DOWN = 5073;
      //public const uint ERROR_CLUSTER_JOIN_ABORTED = 5074;
      //public const uint ERROR_CLUSTER_INCOMPATIBLE_VERSIONS = 5075;
      //public const uint ERROR_CLUSTER_MAXNUM_OF_RESOURCES_EXCEEDED = 5076;
      //public const uint ERROR_CLUSTER_SYSTEM_CONFIG_CHANGED = 5077;
      //public const uint ERROR_CLUSTER_RESOURCE_TYPE_NOT_FOUND = 5078;
      //public const uint ERROR_CLUSTER_RESTYPE_NOT_SUPPORTED = 5079;
      //public const uint ERROR_CLUSTER_RESNAME_NOT_FOUND = 5080;
      //public const uint ERROR_CLUSTER_NO_RPC_PACKAGES_REGISTERED = 5081;
      //public const uint ERROR_CLUSTER_OWNER_NOT_IN_PREFLIST = 5082;
      //public const uint ERROR_CLUSTER_DATABASE_SEQMISMATCH = 5083;
      //public const uint ERROR_RESMON_INVALID_STATE = 5084;
      //public const uint ERROR_CLUSTER_GUM_NOT_LOCKER = 5085;
      //public const uint ERROR_QUORUM_DISK_NOT_FOUND = 5086;
      //public const uint ERROR_DATABASE_BACKUP_CORRUPT = 5087;
      //public const uint ERROR_CLUSTER_NODE_ALREADY_HAS_DFS_ROOT = 5088;
      //public const uint ERROR_RESOURCE_PROPERTY_UNCHANGEABLE = 5089;
      //public const uint ERROR_CLUSTER_MEMBERSHIP_INVALID_STATE = 5890;
      //public const uint ERROR_CLUSTER_QUORUMLOG_NOT_FOUND = 5891;
      //public const uint ERROR_CLUSTER_MEMBERSHIP_HALT = 5892;
      //public const uint ERROR_CLUSTER_INSTANCE_ID_MISMATCH = 5893;
      //public const uint ERROR_CLUSTER_NETWORK_NOT_FOUND_FOR_IP = 5894;
      //public const uint ERROR_CLUSTER_PROPERTY_DATA_TYPE_MISMATCH = 5895;
      //public const uint ERROR_CLUSTER_EVICT_WITHOUT_CLEANUP = 5896;
      //public const uint ERROR_CLUSTER_PARAMETER_MISMATCH = 5897;
      //public const uint ERROR_NODE_CANNOT_BE_CLUSTERED = 5898;
      //public const uint ERROR_CLUSTER_WRONG_OS_VERSION = 5899;
      //public const uint ERROR_CLUSTER_CANT_CREATE_DUP_CLUSTER_NAME = 5900;
      //public const uint ERROR_CLUSCFG_ALREADY_COMMITTED = 5901;
      //public const uint ERROR_CLUSCFG_ROLLBACK_FAILED = 5902;
      //public const uint ERROR_CLUSCFG_SYSTEM_DISK_DRIVE_LETTER_CONFLICT = 5903;
      //public const uint ERROR_CLUSTER_OLD_VERSION = 5904;
      //public const uint ERROR_CLUSTER_MISMATCHED_COMPUTER_ACCT_NAME = 5905;
      //public const uint ERROR_ENCRYPTION_FAILED = 6000;
      //public const uint ERROR_DECRYPTION_FAILED = 6001;
      //public const uint ERROR_FILE_ENCRYPTED = 6002;
      //public const uint ERROR_NO_RECOVERY_POLICY = 6003;
      //public const uint ERROR_NO_EFS = 6004;
      //public const uint ERROR_WRONG_EFS = 6005;
      //public const uint ERROR_NO_USER_KEYS = 6006;
      //public const uint ERROR_FILE_NOT_ENCRYPTED = 6007;
      //public const uint ERROR_NOT_EXPORT_FORMAT = 6008;

      /// <summary>(6009) The specified file is read only.</summary>
      public const uint ERROR_FILE_READ_ONLY = 6009;

      //public const uint ERROR_DIR_EFS_DISALLOWED = 6010;
      //public const uint ERROR_EFS_SERVER_NOT_TRUSTED = 6011;

      /// <summary>(6012) Recovery policy configured for this system contains invalid recovery certificate.</summary>
      public const uint ERROR_BAD_RECOVERY_POLICY = 6012;

      //public const uint ERROR_EFS_ALG_BLOB_TOO_BIG = 6013;
      //public const uint ERROR_VOLUME_NOT_SUPPORT_EFS = 6014;
      //public const uint ERROR_EFS_DISABLED = 6015;
      //public const uint ERROR_EFS_VERSION_NOT_SUPPORT = 6016;
      //public const uint ERROR_NO_BROWSER_SERVERS_FOUND = 6118;
      //public const uint SCHED_E_SERVICE_NOT_LOCALSYSTEM = 6200;

      /// <summary>(6700) The transaction handle associated with this operation is not valid.</summary>
      public const uint ERROR_INVALID_TRANSACTION = 6700;

      /// <summary>(6701) The requested operation was made in the context
      /// <para>of a transaction that is no longer active.</para>
      /// </summary>
      public const uint ERROR_TRANSACTION_NOT_ACTIVE = 6701;

      /// <summary>(6702) The requested operation is not valid
      /// <para>on the Transaction object in its current state.</para>
      /// </summary>
      public const uint ERROR_TRANSACTION_REQUEST_NOT_VALID = 6702;

      /// <summary>(6703) The caller has called a response API, but the response is not expected
      /// <para>because the TM did not issue the corresponding request to the caller.</para>
      /// </summary>
      public const uint ERROR_TRANSACTION_NOT_REQUESTED = 6703;

      /// <summary>(6704) It is too late to perform the requested operation,
      /// <para>since the Transaction has already been aborted.</para>
      /// </summary>
      public const uint ERROR_TRANSACTION_ALREADY_ABORTED = 6704;

      /// <summary>(6705) It is too late to perform the requested operation,
      /// <para>since the Transaction has already been committed.</para>
      /// </summary>
      public const uint ERROR_TRANSACTION_ALREADY_COMMITTED = 6705;

      /// <summary>(6800) The function attempted to use a name
      /// <para>that is reserved for use by another transaction.</para>
      /// </summary>
      public const uint ERROR_TRANSACTIONAL_CONFLICT = 6800;

      /// <summary>(6805) The remote server or share does not support transacted file operations.</summary>
      public const uint ERROR_TRANSACTIONS_UNSUPPORTED_REMOTE = 6805;

      //public const uint ERROR_CTX_WINSTATION_NAME_INVALID = 7001;
      //public const uint ERROR_CTX_INVALID_PD = 7002;
      //public const uint ERROR_CTX_PD_NOT_FOUND = 7003;
      //public const uint ERROR_CTX_WD_NOT_FOUND = 7004;
      //public const uint ERROR_CTX_CANNOT_MAKE_EVENTLOG_ENTRY = 7005;
      //public const uint ERROR_CTX_SERVICE_NAME_COLLISION = 7006;
      //public const uint ERROR_CTX_CLOSE_PENDING = 7007;
      //public const uint ERROR_CTX_NO_OUTBUF = 7008;
      //public const uint ERROR_CTX_MODEM_INF_NOT_FOUND = 7009;
      //public const uint ERROR_CTX_INVALID_MODEMNAME = 7010;
      //public const uint ERROR_CTX_MODEM_RESPONSE_ERROR = 7011;
      //public const uint ERROR_CTX_MODEM_RESPONSE_TIMEOUT = 7012;
      //public const uint ERROR_CTX_MODEM_RESPONSE_NO_CARRIER = 7013;
      //public const uint ERROR_CTX_MODEM_RESPONSE_NO_DIALTONE = 7014;
      //public const uint ERROR_CTX_MODEM_RESPONSE_BUSY = 7015;
      //public const uint ERROR_CTX_MODEM_RESPONSE_VOICE = 7016;
      //public const uint ERROR_CTX_TD_ERROR = 7017;
      //public const uint ERROR_CTX_WINSTATION_NOT_FOUND = 7022;
      //public const uint ERROR_CTX_WINSTATION_ALREADY_EXISTS = 7023;
      //public const uint ERROR_CTX_WINSTATION_BUSY = 7024;
      //public const uint ERROR_CTX_BAD_VIDEO_MODE = 7025;
      //public const uint ERROR_CTX_GRAPHICS_INVALID = 7035;
      //public const uint ERROR_CTX_LOGON_DISABLED = 7037;
      //public const uint ERROR_CTX_NOT_CONSOLE = 7038;
      //public const uint ERROR_CTX_CLIENT_QUERY_TIMEOUT = 7040;
      //public const uint ERROR_CTX_CONSOLE_DISCONNECT = 7041;
      //public const uint ERROR_CTX_CONSOLE_CONNECT = 7042;
      //public const uint ERROR_CTX_SHADOW_DENIED = 7044;
      //public const uint ERROR_CTX_WINSTATION_ACCESS_DENIED = 7045;
      //public const uint ERROR_CTX_INVALID_WD = 7049;
      //public const uint ERROR_CTX_SHADOW_INVALID = 7050;
      //public const uint ERROR_CTX_SHADOW_DISABLED = 7051;
      //public const uint ERROR_CTX_CLIENT_LICENSE_IN_USE = 7052;
      //public const uint ERROR_CTX_CLIENT_LICENSE_NOT_SET = 7053;
      //public const uint ERROR_CTX_LICENSE_NOT_AVAILABLE = 7054;
      //public const uint ERROR_CTX_LICENSE_CLIENT_INVALID = 7055;
      //public const uint ERROR_CTX_LICENSE_EXPIRED = 7056;
      //public const uint ERROR_CTX_SHADOW_NOT_RUNNING = 7057;
      //public const uint ERROR_CTX_SHADOW_ENDED_BY_MODE_CHANGE = 7058;
      //public const uint ERROR_ACTIVATION_COUNT_EXCEEDED = 7059;
      //public const uint FRS_ERR_INVALID_API_SEQUENCE = 8001;
      //public const uint FRS_ERR_STARTING_SERVICE = 8002;
      //public const uint FRS_ERR_STOPPING_SERVICE = 8003;
      //public const uint FRS_ERR_INTERNAL_API = 8004;
      //public const uint FRS_ERR_INTERNAL = 8005;
      //public const uint FRS_ERR_SERVICE_COMM = 8006;
      //public const uint FRS_ERR_INSUFFICIENT_PRIV = 8007;
      //public const uint FRS_ERR_AUTHENTICATION = 8008;
      //public const uint FRS_ERR_PARENT_INSUFFICIENT_PRIV = 8009;
      //public const uint FRS_ERR_PARENT_AUTHENTICATION = 8010;
      //public const uint FRS_ERR_CHILD_TO_PARENT_COMM = 8011;
      //public const uint FRS_ERR_PARENT_TO_CHILD_COMM = 8012;
      //public const uint FRS_ERR_SYSVOL_POPULATE = 8013;
      //public const uint FRS_ERR_SYSVOL_POPULATE_TIMEOUT = 8014;
      //public const uint FRS_ERR_SYSVOL_IS_BUSY = 8015;
      //public const uint FRS_ERR_SYSVOL_DEMOTE = 8016;
      //public const uint FRS_ERR_INVALID_SERVICE_PARAMETER = 8017;
      //public const uint ERROR_DS_NOT_INSTALLED = 8200;
      //public const uint ERROR_DS_MEMBERSHIP_EVALUATED_LOCALLY = 8201;
      //public const uint ERROR_DS_NO_ATTRIBUTE_OR_VALUE = 8202;
      //public const uint ERROR_DS_INVALID_ATTRIBUTE_SYNTAX = 8203;
      //public const uint ERROR_DS_ATTRIBUTE_TYPE_UNDEFINED = 8204;
      //public const uint ERROR_DS_ATTRIBUTE_OR_VALUE_EXISTS = 8205;
      //public const uint ERROR_DS_BUSY = 8206;
      //public const uint ERROR_DS_UNAVAILABLE = 8207;
      //public const uint ERROR_DS_NO_RIDS_ALLOCATED = 8208;
      //public const uint ERROR_DS_NO_MORE_RIDS = 8209;
      //public const uint ERROR_DS_INCORRECT_ROLE_OWNER = 8210;
      //public const uint ERROR_DS_RIDMGR_INIT_ERROR = 8211;
      //public const uint ERROR_DS_OBJ_CLASS_VIOLATION = 8212;
      //public const uint ERROR_DS_CANT_ON_NON_LEAF = 8213;
      //public const uint ERROR_DS_CANT_ON_RDN = 8214;
      //public const uint ERROR_DS_CANT_MOD_OBJ_CLASS = 8215;
      //public const uint ERROR_DS_CROSS_DOM_MOVE_ERROR = 8216;
      //public const uint ERROR_DS_GC_NOT_AVAILABLE = 8217;
      //public const uint ERROR_SHARED_POLICY = 8218;
      //public const uint ERROR_POLICY_OBJECT_NOT_FOUND = 8219;
      //public const uint ERROR_POLICY_ONLY_IN_DS = 8220;
      //public const uint ERROR_PROMOTION_ACTIVE = 8221;
      //public const uint ERROR_NO_PROMOTION_ACTIVE = 8222;
      //public const uint ERROR_DS_OPERATIONS_ERROR = 8224;
      //public const uint ERROR_DS_PROTOCOL_ERROR = 8225;
      //public const uint ERROR_DS_TIMELIMIT_EXCEEDED = 8226;
      //public const uint ERROR_DS_SIZELIMIT_EXCEEDED = 8227;
      //public const uint ERROR_DS_ADMIN_LIMIT_EXCEEDED = 8228;
      //public const uint ERROR_DS_COMPARE_FALSE = 8229;
      //public const uint ERROR_DS_COMPARE_TRUE = 8230;
      //public const uint ERROR_DS_AUTH_METHOD_NOT_SUPPORTED = 8231;
      //public const uint ERROR_DS_STRONG_AUTH_REQUIRED = 8232;
      //public const uint ERROR_DS_INAPPROPRIATE_AUTH = 8233;
      //public const uint ERROR_DS_AUTH_UNKNOWN = 8234;
      //public const uint ERROR_DS_REFERRAL = 8235;
      //public const uint ERROR_DS_UNAVAILABLE_CRIT_EXTENSION = 8236;
      //public const uint ERROR_DS_CONFIDENTIALITY_REQUIRED = 8237;
      //public const uint ERROR_DS_INAPPROPRIATE_MATCHING = 8238;
      //public const uint ERROR_DS_CONSTRAINT_VIOLATION = 8239;
      //public const uint ERROR_DS_NO_SUCH_OBJECT = 8240;
      //public const uint ERROR_DS_ALIAS_PROBLEM = 8241;
      //public const uint ERROR_DS_INVALID_DN_SYNTAX = 8242;
      //public const uint ERROR_DS_IS_LEAF = 8243;
      //public const uint ERROR_DS_ALIAS_DEREF_PROBLEM = 8244;
      //public const uint ERROR_DS_UNWILLING_TO_PERFORM = 8245;
      //public const uint ERROR_DS_LOOP_DETECT = 8246;
      //public const uint ERROR_DS_NAMING_VIOLATION = 8247;
      //public const uint ERROR_DS_OBJECT_RESULTS_TOO_LARGE = 8248;
      //public const uint ERROR_DS_AFFECTS_MULTIPLE_DSAS = 8249;
      //public const uint ERROR_DS_SERVER_DOWN = 8250;
      //public const uint ERROR_DS_LOCAL_ERROR = 8251;
      //public const uint ERROR_DS_ENCODING_ERROR = 8252;
      //public const uint ERROR_DS_DECODING_ERROR = 8253;
      //public const uint ERROR_DS_FILTER_UNKNOWN = 8254;
      //public const uint ERROR_DS_PARAM_ERROR = 8255;
      //public const uint ERROR_DS_NOT_SUPPORTED = 8256;
      //public const uint ERROR_DS_NO_RESULTS_RETURNED = 8257;
      //public const uint ERROR_DS_CONTROL_NOT_FOUND = 8258;
      //public const uint ERROR_DS_CLIENT_LOOP = 8259;
      //public const uint ERROR_DS_REFERRAL_LIMIT_EXCEEDED = 8260;
      //public const uint ERROR_DS_SORT_CONTROL_MISSING = 8261;
      //public const uint ERROR_DS_OFFSET_RANGE_ERROR = 8262;
      //public const uint ERROR_DS_ROOT_MUST_BE_NC = 8301;
      //public const uint ERROR_DS_ADD_REPLICA_INHIBITED = 8302;
      //public const uint ERROR_DS_ATT_NOT_DEF_IN_SCHEMA = 8303;
      //public const uint ERROR_DS_MAX_OBJ_SIZE_EXCEEDED = 8304;
      //public const uint ERROR_DS_OBJ_STRING_NAME_EXISTS = 8305;
      //public const uint ERROR_DS_NO_RDN_DEFINED_IN_SCHEMA = 8306;
      //public const uint ERROR_DS_RDN_DOESNT_MATCH_SCHEMA = 8307;
      //public const uint ERROR_DS_NO_REQUESTED_ATTS_FOUND = 8308;
      //public const uint ERROR_DS_USER_BUFFER_TO_SMALL = 8309;
      //public const uint ERROR_DS_ATT_IS_NOT_ON_OBJ = 8310;
      //public const uint ERROR_DS_ILLEGAL_MOD_OPERATION = 8311;
      //public const uint ERROR_DS_OBJ_TOO_LARGE = 8312;
      //public const uint ERROR_DS_BAD_INSTANCE_TYPE = 8313;
      //public const uint ERROR_DS_MASTERDSA_REQUIRED = 8314;
      //public const uint ERROR_DS_OBJECT_CLASS_REQUIRED = 8315;
      //public const uint ERROR_DS_MISSING_REQUIRED_ATT = 8316;
      //public const uint ERROR_DS_ATT_NOT_DEF_FOR_CLASS = 8317;
      //public const uint ERROR_DS_ATT_ALREADY_EXISTS = 8318;
      //public const uint ERROR_DS_CANT_ADD_ATT_VALUES = 8320;
      //public const uint ERROR_DS_SINGLE_VALUE_CONSTRAINT = 8321;
      //public const uint ERROR_DS_RANGE_CONSTRAINT = 8322;
      //public const uint ERROR_DS_ATT_VAL_ALREADY_EXISTS = 8323;
      //public const uint ERROR_DS_CANT_REM_MISSING_ATT = 8324;
      //public const uint ERROR_DS_CANT_REM_MISSING_ATT_VAL = 8325;
      //public const uint ERROR_DS_ROOT_CANT_BE_SUBREF = 8326;
      //public const uint ERROR_DS_NO_CHAINING = 8327;
      //public const uint ERROR_DS_NO_CHAINED_EVAL = 8328;
      //public const uint ERROR_DS_NO_PARENT_OBJECT = 8329;
      //public const uint ERROR_DS_PARENT_IS_AN_ALIAS = 8330;
      //public const uint ERROR_DS_CANT_MIX_MASTER_AND_REPS = 8331;
      //public const uint ERROR_DS_CHILDREN_EXIST = 8332;
      //public const uint ERROR_DS_OBJ_NOT_FOUND = 8333;
      //public const uint ERROR_DS_ALIASED_OBJ_MISSING = 8334;
      //public const uint ERROR_DS_BAD_NAME_SYNTAX = 8335;
      //public const uint ERROR_DS_ALIAS_POINTS_TO_ALIAS = 8336;
      //public const uint ERROR_DS_CANT_DEREF_ALIAS = 8337;
      //public const uint ERROR_DS_OUT_OF_SCOPE = 8338;
      //public const uint ERROR_DS_OBJECT_BEING_REMOVED = 8339;
      //public const uint ERROR_DS_CANT_DELETE_DSA_OBJ = 8340;
      //public const uint ERROR_DS_GENERIC_ERROR = 8341;
      //public const uint ERROR_DS_DSA_MUST_BE_INT_MASTER = 8342;
      //public const uint ERROR_DS_CLASS_NOT_DSA = 8343;
      //public const uint ERROR_DS_INSUFF_ACCESS_RIGHTS = 8344;
      //public const uint ERROR_DS_ILLEGAL_SUPERIOR = 8345;
      //public const uint ERROR_DS_ATTRIBUTE_OWNED_BY_SAM = 8346;
      //public const uint ERROR_DS_NAME_TOO_MANY_PARTS = 8347;
      //public const uint ERROR_DS_NAME_TOO_LONG = 8348;
      //public const uint ERROR_DS_NAME_VALUE_TOO_LONG = 8349;
      //public const uint ERROR_DS_NAME_UNPARSEABLE = 8350;
      //public const uint ERROR_DS_NAME_TYPE_UNKNOWN = 8351;
      //public const uint ERROR_DS_NOT_AN_OBJECT = 8352;
      //public const uint ERROR_DS_SEC_DESC_TOO_SHORT = 8353;
      //public const uint ERROR_DS_SEC_DESC_INVALID = 8354;
      //public const uint ERROR_DS_NO_DELETED_NAME = 8355;
      //public const uint ERROR_DS_SUBREF_MUST_HAVE_PARENT = 8356;
      //public const uint ERROR_DS_NCNAME_MUST_BE_NC = 8357;
      //public const uint ERROR_DS_CANT_ADD_SYSTEM_ONLY = 8358;
      //public const uint ERROR_DS_CLASS_MUST_BE_CONCRETE = 8359;
      //public const uint ERROR_DS_INVALID_DMD = 8360;
      //public const uint ERROR_DS_OBJ_GUID_EXISTS = 8361;
      //public const uint ERROR_DS_NOT_ON_BACKLINK = 8362;
      //public const uint ERROR_DS_NO_CROSSREF_FOR_NC = 8363;
      //public const uint ERROR_DS_SHUTTING_DOWN = 8364;
      //public const uint ERROR_DS_UNKNOWN_OPERATION = 8365;
      //public const uint ERROR_DS_INVALID_ROLE_OWNER = 8366;
      //public const uint ERROR_DS_COULDNT_CONTACT_FSMO = 8367;
      //public const uint ERROR_DS_CROSS_NC_DN_RENAME = 8368;
      //public const uint ERROR_DS_CANT_MOD_SYSTEM_ONLY = 8369;
      //public const uint ERROR_DS_REPLICATOR_ONLY = 8370;
      //public const uint ERROR_DS_OBJ_CLASS_NOT_DEFINED = 8371;
      //public const uint ERROR_DS_OBJ_CLASS_NOT_SUBCLASS = 8372;
      //public const uint ERROR_DS_NAME_REFERENCE_INVALID = 8373;
      //public const uint ERROR_DS_CROSS_REF_EXISTS = 8374;
      //public const uint ERROR_DS_CANT_DEL_MASTER_CROSSREF = 8375;
      //public const uint ERROR_DS_SUBTREE_NOTIFY_NOT_NC_HEAD = 8376;
      //public const uint ERROR_DS_NOTIFY_FILTER_TOO_COMPLEX = 8377;
      //public const uint ERROR_DS_DUP_RDN = 8378;
      //public const uint ERROR_DS_DUP_OID = 8379;
      //public const uint ERROR_DS_DUP_MAPI_ID = 8380;
      //public const uint ERROR_DS_DUP_SCHEMA_ID_GUID = 8381;
      //public const uint ERROR_DS_DUP_LDAP_DISPLAY_NAME = 8382;
      //public const uint ERROR_DS_SEMANTIC_ATT_TEST = 8383;
      //public const uint ERROR_DS_SYNTAX_MISMATCH = 8384;
      //public const uint ERROR_DS_EXISTS_IN_MUST_HAVE = 8385;
      //public const uint ERROR_DS_EXISTS_IN_MAY_HAVE = 8386;
      //public const uint ERROR_DS_NONEXISTENT_MAY_HAVE = 8387;
      //public const uint ERROR_DS_NONEXISTENT_MUST_HAVE = 8388;
      //public const uint ERROR_DS_AUX_CLS_TEST_FAIL = 8389;
      //public const uint ERROR_DS_NONEXISTENT_POSS_SUP = 8390;
      //public const uint ERROR_DS_SUB_CLS_TEST_FAIL = 8391;
      //public const uint ERROR_DS_BAD_RDN_ATT_ID_SYNTAX = 8392;
      //public const uint ERROR_DS_EXISTS_IN_AUX_CLS = 8393;
      //public const uint ERROR_DS_EXISTS_IN_SUB_CLS = 8394;
      //public const uint ERROR_DS_EXISTS_IN_POSS_SUP = 8395;
      //public const uint ERROR_DS_RECALCSCHEMA_FAILED = 8396;
      //public const uint ERROR_DS_TREE_DELETE_NOT_FINISHED = 8397;
      //public const uint ERROR_DS_CANT_DELETE = 8398;
      //public const uint ERROR_DS_ATT_SCHEMA_REQ_ID = 8399;
      //public const uint ERROR_DS_BAD_ATT_SCHEMA_SYNTAX = 8400;
      //public const uint ERROR_DS_CANT_CACHE_ATT = 8401;
      //public const uint ERROR_DS_CANT_CACHE_CLASS = 8402;
      //public const uint ERROR_DS_CANT_REMOVE_ATT_CACHE = 8403;
      //public const uint ERROR_DS_CANT_REMOVE_CLASS_CACHE = 8404;
      //public const uint ERROR_DS_CANT_RETRIEVE_DN = 8405;
      //public const uint ERROR_DS_MISSING_SUPREF = 8406;
      //public const uint ERROR_DS_CANT_RETRIEVE_INSTANCE = 8407;
      //public const uint ERROR_DS_CODE_INCONSISTENCY = 8408;
      //public const uint ERROR_DS_DATABASE_ERROR = 8409;
      //public const uint ERROR_DS_GOVERNSID_MISSING = 8410;
      //public const uint ERROR_DS_MISSING_EXPECTED_ATT = 8411;
      //public const uint ERROR_DS_NCNAME_MISSING_CR_REF = 8412;
      //public const uint ERROR_DS_SECURITY_CHECKING_ERROR = 8413;
      //public const uint ERROR_DS_SCHEMA_NOT_LOADED = 8414;
      //public const uint ERROR_DS_SCHEMA_ALLOC_FAILED = 8415;
      //public const uint ERROR_DS_ATT_SCHEMA_REQ_SYNTAX = 8416;
      //public const uint ERROR_DS_GCVERIFY_ERROR = 8417;
      //public const uint ERROR_DS_DRA_SCHEMA_MISMATCH = 8418;
      //public const uint ERROR_DS_CANT_FIND_DSA_OBJ = 8419;
      //public const uint ERROR_DS_CANT_FIND_EXPECTED_NC = 8420;
      //public const uint ERROR_DS_CANT_FIND_NC_IN_CACHE = 8421;
      //public const uint ERROR_DS_CANT_RETRIEVE_CHILD = 8422;
      //public const uint ERROR_DS_SECURITY_ILLEGAL_MODIFY = 8423;
      //public const uint ERROR_DS_CANT_REPLACE_HIDDEN_REC = 8424;
      //public const uint ERROR_DS_BAD_HIERARCHY_FILE = 8425;
      //public const uint ERROR_DS_BUILD_HIERARCHY_TABLE_FAILED = 8426;
      //public const uint ERROR_DS_CONFIG_PARAM_MISSING = 8427;
      //public const uint ERROR_DS_COUNTING_AB_INDICES_FAILED = 8428;
      //public const uint ERROR_DS_HIERARCHY_TABLE_MALLOC_FAILED = 8429;
      //public const uint ERROR_DS_INTERNAL_FAILURE = 8430;
      //public const uint ERROR_DS_UNKNOWN_ERROR = 8431;
      //public const uint ERROR_DS_ROOT_REQUIRES_CLASS_TOP = 8432;
      //public const uint ERROR_DS_REFUSING_FSMO_ROLES = 8433;
      //public const uint ERROR_DS_MISSING_FSMO_SETTINGS = 8434;
      //public const uint ERROR_DS_UNABLE_TO_SURRENDER_ROLES = 8435;
      //public const uint ERROR_DS_DRA_GENERIC = 8436;
      //public const uint ERROR_DS_DRA_INVALID_PARAMETER = 8437;
      //public const uint ERROR_DS_DRA_BUSY = 8438;
      //public const uint ERROR_DS_DRA_BAD_DN = 8439;
      //public const uint ERROR_DS_DRA_BAD_NC = 8440;
      //public const uint ERROR_DS_DRA_DN_EXISTS = 8441;
      //public const uint ERROR_DS_DRA_INTERNAL_ERROR = 8442;
      //public const uint ERROR_DS_DRA_INCONSISTENT_DIT = 8443;
      //public const uint ERROR_DS_DRA_CONNECTION_FAILED = 8444;
      //public const uint ERROR_DS_DRA_BAD_INSTANCE_TYPE = 8445;
      //public const uint ERROR_DS_DRA_OUT_OF_MEM = 8446;
      //public const uint ERROR_DS_DRA_MAIL_PROBLEM = 8447;
      //public const uint ERROR_DS_DRA_REF_ALREADY_EXISTS = 8448;
      //public const uint ERROR_DS_DRA_REF_NOT_FOUND = 8449;
      //public const uint ERROR_DS_DRA_OBJ_IS_REP_SOURCE = 8450;
      //public const uint ERROR_DS_DRA_DB_ERROR = 8451;
      //public const uint ERROR_DS_DRA_NO_REPLICA = 8452;
      //public const uint ERROR_DS_DRA_ACCESS_DENIED = 8453;
      //public const uint ERROR_DS_DRA_NOT_SUPPORTED = 8454;
      //public const uint ERROR_DS_DRA_RPC_CANCELLED = 8455;
      //public const uint ERROR_DS_DRA_SOURCE_DISABLED = 8456;
      //public const uint ERROR_DS_DRA_SINK_DISABLED = 8457;
      //public const uint ERROR_DS_DRA_NAME_COLLISION = 8458;
      //public const uint ERROR_DS_DRA_SOURCE_REINSTALLED = 8459;
      //public const uint ERROR_DS_DRA_MISSING_PARENT = 8460;
      //public const uint ERROR_DS_DRA_PREEMPTED = 8461;
      //public const uint ERROR_DS_DRA_ABANDON_SYNC = 8462;
      //public const uint ERROR_DS_DRA_SHUTDOWN = 8463;
      //public const uint ERROR_DS_DRA_INCOMPATIBLE_PARTIAL_SET = 8464;
      //public const uint ERROR_DS_DRA_SOURCE_IS_PARTIAL_REPLICA = 8465;
      //public const uint ERROR_DS_DRA_EXTN_CONNECTION_FAILED = 8466;
      //public const uint ERROR_DS_INSTALL_SCHEMA_MISMATCH = 8467;
      //public const uint ERROR_DS_DUP_LINK_ID = 8468;
      //public const uint ERROR_DS_NAME_ERROR_RESOLVING = 8469;
      //public const uint ERROR_DS_NAME_ERROR_NOT_FOUND = 8470;
      //public const uint ERROR_DS_NAME_ERROR_NOT_UNIQUE = 8471;
      //public const uint ERROR_DS_NAME_ERROR_NO_MAPPING = 8472;
      //public const uint ERROR_DS_NAME_ERROR_DOMAIN_ONLY = 8473;
      //public const uint ERROR_DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING = 8474;
      //public const uint ERROR_DS_CONSTRUCTED_ATT_MOD = 8475;
      //public const uint ERROR_DS_WRONG_OM_OBJ_CLASS = 8476;
      //public const uint ERROR_DS_DRA_REPL_PENDING = 8477;
      //public const uint ERROR_DS_DS_REQUIRED = 8478;
      //public const uint ERROR_DS_INVALID_LDAP_DISPLAY_NAME = 8479;
      //public const uint ERROR_DS_NON_BASE_SEARCH = 8480;
      //public const uint ERROR_DS_CANT_RETRIEVE_ATTS = 8481;
      //public const uint ERROR_DS_BACKLINK_WITHOUT_LINK = 8482;
      //public const uint ERROR_DS_EPOCH_MISMATCH = 8483;
      //public const uint ERROR_DS_SRC_NAME_MISMATCH = 8484;
      //public const uint ERROR_DS_SRC_AND_DST_NC_IDENTICAL = 8485;
      //public const uint ERROR_DS_DST_NC_MISMATCH = 8486;
      //public const uint ERROR_DS_NOT_AUTHORITIVE_FOR_DST_NC = 8487;
      //public const uint ERROR_DS_SRC_GUID_MISMATCH = 8488;
      //public const uint ERROR_DS_CANT_MOVE_DELETED_OBJECT = 8489;
      //public const uint ERROR_DS_PDC_OPERATION_IN_PROGRESS = 8490;
      //public const uint ERROR_DS_CROSS_DOMAIN_CLEANUP_REQD = 8491;
      //public const uint ERROR_DS_ILLEGAL_XDOM_MOVE_OPERATION = 8492;
      //public const uint ERROR_DS_CANT_WITH_ACCT_GROUP_MEMBERSHPS = 8493;
      //public const uint ERROR_DS_NC_MUST_HAVE_NC_PARENT = 8494;
      //public const uint ERROR_DS_CR_IMPOSSIBLE_TO_VALIDATE = 8495;
      //public const uint ERROR_DS_DST_DOMAIN_NOT_NATIVE = 8496;
      //public const uint ERROR_DS_MISSING_INFRASTRUCTURE_CONTAINER = 8497;
      //public const uint ERROR_DS_CANT_MOVE_ACCOUNT_GROUP = 8498;
      //public const uint ERROR_DS_CANT_MOVE_RESOURCE_GROUP = 8499;
      //public const uint ERROR_DS_INVALID_SEARCH_FLAG = 8500;
      //public const uint ERROR_DS_NO_TREE_DELETE_ABOVE_NC = 8501;
      //public const uint ERROR_DS_COULDNT_LOCK_TREE_FOR_DELETE = 8502;
      //public const uint ERROR_DS_COULDNT_IDENTIFY_OBJECTS_FOR_TREE_DELETE = 8503;
      //public const uint ERROR_DS_SAM_INIT_FAILURE = 8504;
      //public const uint ERROR_DS_SENSITIVE_GROUP_VIOLATION = 8505;
      //public const uint ERROR_DS_CANT_MOD_PRIMARYGROUPID = 8506;
      //public const uint ERROR_DS_ILLEGAL_BASE_SCHEMA_MOD = 8507;
      //public const uint ERROR_DS_NONSAFE_SCHEMA_CHANGE = 8508;
      //public const uint ERROR_DS_SCHEMA_UPDATE_DISALLOWED = 8509;
      //public const uint ERROR_DS_CANT_CREATE_UNDER_SCHEMA = 8510;
      //public const uint ERROR_DS_INSTALL_NO_SRC_SCH_VERSION = 8511;
      //public const uint ERROR_DS_INSTALL_NO_SCH_VERSION_IN_INIFILE = 8512;
      //public const uint ERROR_DS_INVALID_GROUP_TYPE = 8513;
      //public const uint ERROR_DS_NO_NEST_GLOBALGROUP_IN_MIXEDDOMAIN = 8514;
      //public const uint ERROR_DS_NO_NEST_LOCALGROUP_IN_MIXEDDOMAIN = 8515;
      //public const uint ERROR_DS_GLOBAL_CANT_HAVE_LOCAL_MEMBER = 8516;
      //public const uint ERROR_DS_GLOBAL_CANT_HAVE_UNIVERSAL_MEMBER = 8517;
      //public const uint ERROR_DS_UNIVERSAL_CANT_HAVE_LOCAL_MEMBER = 8518;
      //public const uint ERROR_DS_GLOBAL_CANT_HAVE_CROSSDOMAIN_MEMBER = 8519;
      //public const uint ERROR_DS_LOCAL_CANT_HAVE_CROSSDOMAIN_LOCAL_MEMBER = 8520;
      //public const uint ERROR_DS_HAVE_PRIMARY_MEMBERS = 8521;
      //public const uint ERROR_DS_STRING_SD_CONVERSION_FAILED = 8522;
      //public const uint ERROR_DS_NAMING_MASTER_GC = 8523;
      //public const uint ERROR_DS_DNS_LOOKUP_FAILURE = 8524;
      //public const uint ERROR_DS_COULDNT_UPDATE_SPNS = 8525;
      //public const uint ERROR_DS_CANT_RETRIEVE_SD = 8526;
      //public const uint ERROR_DS_KEY_NOT_UNIQUE = 8527;
      //public const uint ERROR_DS_WRONG_LINKED_ATT_SYNTAX = 8528;
      //public const uint ERROR_DS_SAM_NEED_BOOTKEY_PASSWORD = 8529;
      //public const uint ERROR_DS_SAM_NEED_BOOTKEY_FLOPPY = 8530;
      //public const uint ERROR_DS_CANT_START = 8531;
      //public const uint ERROR_DS_INIT_FAILURE = 8532;
      //public const uint ERROR_DS_NO_PKT_PRIVACY_ON_CONNECTION = 8533;
      //public const uint ERROR_DS_SOURCE_DOMAIN_IN_FOREST = 8534;
      //public const uint ERROR_DS_DESTINATION_DOMAIN_NOT_IN_FOREST = 8535;
      //public const uint ERROR_DS_DESTINATION_AUDITING_NOT_ENABLED = 8536;
      //public const uint ERROR_DS_CANT_FIND_DC_FOR_SRC_DOMAIN = 8537;
      //public const uint ERROR_DS_SRC_OBJ_NOT_GROUP_OR_USER = 8538;
      //public const uint ERROR_DS_SRC_SID_EXISTS_IN_FOREST = 8539;
      //public const uint ERROR_DS_SRC_AND_DST_OBJECT_CLASS_MISMATCH = 8540;
      //public const uint ERROR_SAM_INIT_FAILURE = 8541;
      //public const uint ERROR_DS_DRA_SCHEMA_INFO_SHIP = 8542;
      //public const uint ERROR_DS_DRA_SCHEMA_CONFLICT = 8543;
      //public const uint ERROR_DS_DRA_EARLIER_SCHEMA_CONFLICT = 8544;
      //public const uint ERROR_DS_DRA_OBJ_NC_MISMATCH = 8545;
      //public const uint ERROR_DS_NC_STILL_HAS_DSAS = 8546;
      //public const uint ERROR_DS_GC_REQUIRED = 8547;
      //public const uint ERROR_DS_LOCAL_MEMBER_OF_LOCAL_ONLY = 8548;
      //public const uint ERROR_DS_NO_FPO_IN_UNIVERSAL_GROUPS = 8549;
      //public const uint ERROR_DS_CANT_ADD_TO_GC = 8550;
      //public const uint ERROR_DS_NO_CHECKPOINT_WITH_PDC = 8551;
      //public const uint ERROR_DS_SOURCE_AUDITING_NOT_ENABLED = 8552;
      //public const uint ERROR_DS_CANT_CREATE_IN_NONDOMAIN_NC = 8553;
      //public const uint ERROR_DS_INVALID_NAME_FOR_SPN = 8554;
      //public const uint ERROR_DS_FILTER_USES_CONTRUCTED_ATTRS = 8555;
      //public const uint ERROR_DS_UNICODEPWD_NOT_IN_QUOTES = 8556;
      //public const uint ERROR_DS_MACHINE_ACCOUNT_QUOTA_EXCEEDED = 8557;
      //public const uint ERROR_DS_MUST_BE_RUN_ON_DST_DC = 8558;
      //public const uint ERROR_DS_SRC_DC_MUST_BE_SP4_OR_GREATER = 8559;
      //public const uint ERROR_DS_CANT_TREE_DELETE_CRITICAL_OBJ = 8560;
      //public const uint ERROR_DS_INIT_FAILURE_CONSOLE = 8561;
      //public const uint ERROR_DS_SAM_INIT_FAILURE_CONSOLE = 8562;
      //public const uint ERROR_DS_FOREST_VERSION_TOO_HIGH = 8563;
      //public const uint ERROR_DS_DOMAIN_VERSION_TOO_HIGH = 8564;
      //public const uint ERROR_DS_FOREST_VERSION_TOO_LOW = 8565;
      //public const uint ERROR_DS_DOMAIN_VERSION_TOO_LOW = 8566;
      //public const uint ERROR_DS_INCOMPATIBLE_VERSION = 8567;
      //public const uint ERROR_DS_LOW_DSA_VERSION = 8568;
      //public const uint ERROR_DS_NO_BEHAVIOR_VERSION_IN_MIXEDDOMAIN = 8569;
      //public const uint ERROR_DS_NOT_SUPPORTED_SORT_ORDER = 8570;
      //public const uint ERROR_DS_NAME_NOT_UNIQUE = 8571;
      //public const uint ERROR_DS_MACHINE_ACCOUNT_CREATED_PRENT4 = 8572;
      //public const uint ERROR_DS_OUT_OF_VERSION_STORE = 8573;
      //public const uint ERROR_DS_INCOMPATIBLE_CONTROLS_USED = 8574;
      //public const uint ERROR_DS_NO_REF_DOMAIN = 8575;
      //public const uint ERROR_DS_RESERVED_LINK_ID = 8576;
      //public const uint ERROR_DS_LINK_ID_NOT_AVAILABLE = 8577;
      //public const uint ERROR_DS_AG_CANT_HAVE_UNIVERSAL_MEMBER = 8578;
      //public const uint ERROR_DS_MODIFYDN_DISALLOWED_BY_INSTANCE_TYPE = 8579;
      //public const uint ERROR_DS_NO_OBJECT_MOVE_IN_SCHEMA_NC = 8580;
      //public const uint ERROR_DS_MODIFYDN_DISALLOWED_BY_FLAG = 8581;
      //public const uint ERROR_DS_MODIFYDN_WRONG_GRANDPARENT = 8582;
      //public const uint ERROR_DS_NAME_ERROR_TRUST_REFERRAL = 8583;
      //public const uint ERROR_NOT_SUPPORTED_ON_STANDARD_SERVER = 8584;
      //public const uint ERROR_DS_CANT_ACCESS_REMOTE_PART_OF_AD = 8585;
      //public const uint ERROR_DS_CR_IMPOSSIBLE_TO_VALIDATE_V2 = 8586;
      //public const uint ERROR_DS_THREAD_LIMIT_EXCEEDED = 8587;
      //public const uint ERROR_DS_NOT_CLOSEST = 8588;
      //public const uint ERROR_DS_CANT_DERIVE_SPN_WITHOUT_SERVER_REF = 8589;
      //public const uint ERROR_DS_SINGLE_USER_MODE_FAILED = 8590;
      //public const uint ERROR_DS_NTDSCRIPT_SYNTAX_ERROR = 8591;
      //public const uint ERROR_DS_NTDSCRIPT_PROCESS_ERROR = 8592;
      //public const uint ERROR_DS_DIFFERENT_REPL_EPOCHS = 8593;
      //public const uint ERROR_DS_DRS_EXTENSIONS_CHANGED = 8594;
      //public const uint ERROR_DS_REPLICA_SET_CHANGE_NOT_ALLOWED_ON_DISABLED_CR = 8595;
      //public const uint ERROR_DS_NO_MSDS_INTID = 8596;
      //public const uint ERROR_DS_DUP_MSDS_INTID = 8597;
      //public const uint ERROR_DS_EXISTS_IN_RDNATTID = 8598;
      //public const uint ERROR_DS_AUTHORIZATION_FAILED = 8599;
      //public const uint ERROR_DS_INVALID_SCRIPT = 8600;
      //public const uint ERROR_DS_REMOTE_CROSSREF_OP_FAILED = 8601;
      //public const uint ERROR_DS_CROSS_REF_BUSY = 8602;
      //public const uint ERROR_DS_CANT_DERIVE_SPN_FOR_DELETED_DOMAIN = 8603;
      //public const uint ERROR_DS_CANT_DEMOTE_WITH_WRITEABLE_NC = 8604;
      //public const uint ERROR_DS_DUPLICATE_ID_FOUND = 8605;
      //public const uint ERROR_DS_INSUFFICIENT_ATTR_TO_CREATE_OBJECT = 8606;
      //public const uint ERROR_DS_GROUP_CONVERSION_ERROR = 8607;
      //public const uint ERROR_DS_CANT_MOVE_APP_BASIC_GROUP = 8608;
      //public const uint ERROR_DS_CANT_MOVE_APP_QUERY_GROUP = 8609;
      //public const uint ERROR_DS_ROLE_NOT_VERIFIED = 8610;
      //public const uint ERROR_DS_WKO_CONTAINER_CANNOT_BE_SPECIAL = 8611;
      //public const uint ERROR_DS_DOMAIN_RENAME_IN_PROGRESS = 8612;
      //public const uint ERROR_DS_EXISTING_AD_CHILD_NC = 8613;
      //public const uint ERROR_DS_REPL_LIFETIME_EXCEEDED = 8614;
      //public const uint ERROR_DS_DISALLOWED_IN_SYSTEM_CONTAINER = 8615;
      //public const uint ERROR_DS_LDAP_SEND_QUEUE_FULL = 8616;
      //public const uint ERROR_DS_DRA_OUT_SCHEDULE_WINDOW = 8617;
      //public const uint DNS_ERROR_RESPONSE_CODES_BASE = 9000;
      //public const uint DNS_ERROR_RCODE_NO_ERROR = NO_ERROR;
      //public const uint DNS_ERROR_MASK = 0x00002328;
      //public const uint DNS_ERROR_RCODE_FORMAT_ERROR = 9001;
      //public const uint DNS_ERROR_RCODE_SERVER_FAILURE = 9002;
      //public const uint DNS_ERROR_RCODE_NAME_ERROR = 9003;
      //public const uint DNS_ERROR_RCODE_NOT_IMPLEMENTED = 9004;
      //public const uint DNS_ERROR_RCODE_REFUSED = 9005;
      //public const uint DNS_ERROR_RCODE_YXDOMAIN = 9006;
      //public const uint DNS_ERROR_RCODE_YXRRSET = 9007;
      //public const uint DNS_ERROR_RCODE_NXRRSET = 9008;
      //public const uint DNS_ERROR_RCODE_NOTAUTH = 9009;
      //public const uint DNS_ERROR_RCODE_NOTZONE = 9010;
      //public const uint DNS_ERROR_RCODE_BADSIG = 9016;
      //public const uint DNS_ERROR_RCODE_BADKEY = 9017;
      //public const uint DNS_ERROR_RCODE_BADTIME = 9018;
      //public const uint DNS_ERROR_RCODE_LAST = DNS_ERROR_RCODE_BADTIME;
      //public const uint DNS_ERROR_PACKET_FMT_BASE = 9500;
      //public const uint DNS_INFO_NO_RECORDS = 9501;
      //public const uint DNS_ERROR_BAD_PACKET = 9502;
      //public const uint DNS_ERROR_NO_PACKET = 9503;
      //public const uint DNS_ERROR_RCODE = 9504;
      //public const uint DNS_ERROR_UNSECURE_PACKET = 9505;
      //public const uint DNS_STATUS_PACKET_UNSECURE = DNS_ERROR_UNSECURE_PACKET;
      //public const uint DNS_ERROR_NO_MEMORY = ERROR_OUTOFMEMORY;
      //public const uint DNS_ERROR_INVALID_NAME = ERROR_INVALID_NAME;
      //public const uint DNS_ERROR_INVALID_DATA = ERROR_INVALID_DATA;
      //public const uint DNS_ERROR_GENERAL_API_BASE = 9550;
      //public const uint DNS_ERROR_INVALID_TYPE = 9551;
      //public const uint DNS_ERROR_INVALID_IP_ADDRESS = 9552;
      //public const uint DNS_ERROR_INVALID_PROPERTY = 9553;
      //public const uint DNS_ERROR_TRY_AGAIN_LATER = 9554;
      //public const uint DNS_ERROR_NOT_UNIQUE = 9555;
      //public const uint DNS_ERROR_NON_RFC_NAME = 9556;
      //public const uint DNS_STATUS_FQDN = 9557;
      //public const uint DNS_STATUS_DOTTED_NAME = 9558;
      //public const uint DNS_STATUS_SINGLE_PART_NAME = 9559;
      //public const uint DNS_ERROR_INVALID_NAME_CHAR = 9560;
      //public const uint DNS_ERROR_NUMERIC_NAME = 9561;
      //public const uint DNS_ERROR_NOT_ALLOWED_ON_ROOT_SERVER = 9562;
      //public const uint DNS_ERROR_NOT_ALLOWED_UNDER_DELEGATION = 9563;
      //public const uint DNS_ERROR_CANNOT_FIND_ROOT_HINTS = 9564;
      //public const uint DNS_ERROR_INCONSISTENT_ROOT_HINTS = 9565;
      //public const uint DNS_ERROR_ZONE_BASE = 9600;
      //public const uint DNS_ERROR_ZONE_DOES_NOT_EXIST = 9601;
      //public const uint DNS_ERROR_NO_ZONE_INFO = 9602;
      //public const uint DNS_ERROR_INVALID_ZONE_OPERATION = 9603;
      //public const uint DNS_ERROR_ZONE_CONFIGURATION_ERROR = 9604;
      //public const uint DNS_ERROR_ZONE_HAS_NO_SOA_RECORD = 9605;
      //public const uint DNS_ERROR_ZONE_HAS_NO_NS_RECORDS = 9606;
      //public const uint DNS_ERROR_ZONE_LOCKED = 9607;
      //public const uint DNS_ERROR_ZONE_CREATION_FAILED = 9608;
      //public const uint DNS_ERROR_ZONE_ALREADY_EXISTS = 9609;
      //public const uint DNS_ERROR_AUTOZONE_ALREADY_EXISTS = 9610;
      //public const uint DNS_ERROR_INVALID_ZONE_TYPE = 9611;
      //public const uint DNS_ERROR_SECONDARY_REQUIRES_MASTER_IP = 9612;
      //public const uint DNS_ERROR_ZONE_NOT_SECONDARY = 9613;
      //public const uint DNS_ERROR_NEED_SECONDARY_ADDRESSES = 9614;
      //public const uint DNS_ERROR_WINS_INIT_FAILED = 9615;
      //public const uint DNS_ERROR_NEED_WINS_SERVERS = 9616;
      //public const uint DNS_ERROR_NBSTAT_INIT_FAILED = 9617;
      //public const uint DNS_ERROR_SOA_DELETE_INVALID = 9618;
      //public const uint DNS_ERROR_FORWARDER_ALREADY_EXISTS = 9619;
      //public const uint DNS_ERROR_ZONE_REQUIRES_MASTER_IP = 9620;
      //public const uint DNS_ERROR_ZONE_IS_SHUTDOWN = 9621;
      //public const uint DNS_ERROR_DATAFILE_BASE = 9650;
      //public const uint DNS_ERROR_PRIMARY_REQUIRES_DATAFILE = 9651;
      //public const uint DNS_ERROR_INVALID_DATAFILE_NAME = 9652;
      //public const uint DNS_ERROR_DATAFILE_OPEN_FAILURE = 9653;
      //public const uint DNS_ERROR_FILE_WRITEBACK_FAILED = 9654;
      //public const uint DNS_ERROR_DATAFILE_PARSING = 9655;
      //public const uint DNS_ERROR_DATABASE_BASE = 9700;
      //public const uint DNS_ERROR_RECORD_DOES_NOT_EXIST = 9701;
      //public const uint DNS_ERROR_RECORD_FORMAT = 9702;
      //public const uint DNS_ERROR_NODE_CREATION_FAILED = 9703;
      //public const uint DNS_ERROR_UNKNOWN_RECORD_TYPE = 9704;
      //public const uint DNS_ERROR_RECORD_TIMED_OUT = 9705;
      //public const uint DNS_ERROR_NAME_NOT_IN_ZONE = 9706;
      //public const uint DNS_ERROR_CNAME_LOOP = 9707;
      //public const uint DNS_ERROR_NODE_IS_CNAME = 9708;
      //public const uint DNS_ERROR_CNAME_COLLISION = 9709;
      //public const uint DNS_ERROR_RECORD_ONLY_AT_ZONE_ROOT = 9710;
      //public const uint DNS_ERROR_RECORD_ALREADY_EXISTS = 9711;
      //public const uint DNS_ERROR_SECONDARY_DATA = 9712;
      //public const uint DNS_ERROR_NO_CREATE_CACHE_DATA = 9713;
      //public const uint DNS_ERROR_NAME_DOES_NOT_EXIST = 9714;
      //public const uint DNS_WARNING_PTR_CREATE_FAILED = 9715;
      //public const uint DNS_WARNING_DOMAIN_UNDELETED = 9716;
      //public const uint DNS_ERROR_DS_UNAVAILABLE = 9717;
      //public const uint DNS_ERROR_DS_ZONE_ALREADY_EXISTS = 9718;
      //public const uint DNS_ERROR_NO_BOOTFILE_IF_DS_ZONE = 9719;
      //public const uint DNS_ERROR_OPERATION_BASE = 9750;
      //public const uint DNS_INFO_AXFR_COMPLETE = 9751;
      //public const uint DNS_ERROR_AXFR = 9752;
      //public const uint DNS_INFO_ADDED_LOCAL_WINS = 9753;
      //public const uint DNS_ERROR_SECURE_BASE = 9800;
      //public const uint DNS_STATUS_CONTINUE_NEEDED = 9801;
      //public const uint DNS_ERROR_SETUP_BASE = 9850;
      //public const uint DNS_ERROR_NO_TCPIP = 9851;
      //public const uint DNS_ERROR_NO_DNS_SERVERS = 9852;
      //public const uint DNS_ERROR_DP_BASE = 9900;
      //public const uint DNS_ERROR_DP_DOES_NOT_EXIST = 9901;
      //public const uint DNS_ERROR_DP_ALREADY_EXISTS = 9902;
      //public const uint DNS_ERROR_DP_NOT_ENLISTED = 9903;
      //public const uint DNS_ERROR_DP_ALREADY_ENLISTED = 9904;
      //public const uint DNS_ERROR_DP_NOT_AVAILABLE = 9905;
      //public const uint DNS_ERROR_DP_FSMO_ERROR = 9906;
      //public const uint WSABASEERR = 10000;
      //public const uint WSAEINTR = 10004;
      //public const uint WSAEBADF = 10009;
      //public const uint WSAEACCES = 10013;
      //public const uint WSAEFAULT = 10014;
      //public const uint WSAEINVAL = 10022;
      //public const uint WSAEMFILE = 10024;
      //public const uint WSAEWOULDBLOCK = 10035;
      //public const uint WSAEINPROGRESS = 10036;
      //public const uint WSAEALREADY = 10037;
      //public const uint WSAENOTSOCK = 10038;
      //public const uint WSAEDESTADDRREQ = 10039;
      //public const uint WSAEMSGSIZE = 10040;
      //public const uint WSAEPROTOTYPE = 10041;
      //public const uint WSAENOPROTOOPT = 10042;
      //public const uint WSAEPROTONOSUPPORT = 10043;
      //public const uint WSAESOCKTNOSUPPORT = 10044;
      //public const uint WSAEOPNOTSUPP = 10045;
      //public const uint WSAEPFNOSUPPORT = 10046;
      //public const uint WSAEAFNOSUPPORT = 10047;
      //public const uint WSAEADDRINUSE = 10048;
      //public const uint WSAEADDRNOTAVAIL = 10049;
      //public const uint WSAENETDOWN = 10050;
      //public const uint WSAENETUNREACH = 10051;
      //public const uint WSAENETRESET = 10052;
      //public const uint WSAECONNABORTED = 10053;
      //public const uint WSAECONNRESET = 10054;
      //public const uint WSAENOBUFS = 10055;
      //public const uint WSAEISCONN = 10056;
      //public const uint WSAENOTCONN = 10057;
      //public const uint WSAESHUTDOWN = 10058;
      //public const uint WSAETOOMANYREFS = 10059;
      //public const uint WSAETIMEDOUT = 10060;
      //public const uint WSAECONNREFUSED = 10061;
      //public const uint WSAELOOP = 10062;
      //public const uint WSAENAMETOOLONG = 10063;
      //public const uint WSAEHOSTDOWN = 10064;
      //public const uint WSAEHOSTUNREACH = 10065;
      //public const uint WSAENOTEMPTY = 10066;
      //public const uint WSAEPROCLIM = 10067;
      //public const uint WSAEUSERS = 10068;
      //public const uint WSAEDQUOT = 10069;
      //public const uint WSAESTALE = 10070;
      //public const uint WSAEREMOTE = 10071;
      //public const uint WSASYSNOTREADY = 10091;
      //public const uint WSAVERNOTSUPPORTED = 10092;
      //public const uint WSANOTINITIALISED = 10093;
      //public const uint WSAEDISCON = 10101;
      //public const uint WSAENOMORE = 10102;
      //public const uint WSAECANCELLED = 10103;
      //public const uint WSAEINVALIDPROCTABLE = 10104;
      //public const uint WSAEINVALIDPROVIDER = 10105;
      //public const uint WSAEPROVIDERFAILEDINIT = 10106;
      //public const uint WSASYSCALLFAILURE = 10107;
      //public const uint WSASERVICE_NOT_FOUND = 10108;
      //public const uint WSATYPE_NOT_FOUND = 10109;
      //public const uint WSA_E_NO_MORE = 10110;
      //public const uint WSA_E_CANCELLED = 10111;
      //public const uint WSAEREFUSED = 10112;
      //public const uint WSAHOST_NOT_FOUND = 11001;
      //public const uint WSATRY_AGAIN = 11002;
      //public const uint WSANO_RECOVERY = 11003;
      //public const uint WSANO_DATA = 11004;
      //public const uint WSA_QOS_RECEIVERS = 11005;
      //public const uint WSA_QOS_SENDERS = 11006;
      //public const uint WSA_QOS_NO_SENDERS = 11007;
      //public const uint WSA_QOS_NO_RECEIVERS = 11008;
      //public const uint WSA_QOS_REQUEST_CONFIRMED = 11009;
      //public const uint WSA_QOS_ADMISSION_FAILURE = 11010;
      //public const uint WSA_QOS_POLICY_FAILURE = 11011;
      //public const uint WSA_QOS_BAD_STYLE = 11012;
      //public const uint WSA_QOS_BAD_OBJECT = 11013;
      //public const uint WSA_QOS_TRAFFIC_CTRL_ERROR = 11014;
      //public const uint WSA_QOS_GENERIC_ERROR = 11015;
      //public const uint WSA_QOS_ESERVICETYPE = 11016;
      //public const uint WSA_QOS_EFLOWSPEC = 11017;
      //public const uint WSA_QOS_EPROVSPECBUF = 11018;
      //public const uint WSA_QOS_EFILTERSTYLE = 11019;
      //public const uint WSA_QOS_EFILTERTYPE = 11020;
      //public const uint WSA_QOS_EFILTERCOUNT = 11021;
      //public const uint WSA_QOS_EOBJLENGTH = 11022;
      //public const uint WSA_QOS_EFLOWCOUNT = 11023;
      //public const uint WSA_QOS_EUNKOWNPSOBJ = 11024;
      //public const uint WSA_QOS_EPOLICYOBJ = 11025;
      //public const uint WSA_QOS_EFLOWDESC = 11026;
      //public const uint WSA_QOS_EPSFLOWSPEC = 11027;
      //public const uint WSA_QOS_EPSFILTERSPEC = 11028;
      //public const uint WSA_QOS_ESDMODEOBJ = 11029;
      //public const uint WSA_QOS_ESHAPERATEOBJ = 11030;
      //public const uint WSA_QOS_RESERVED_PETYPE = 11031;
      //public const uint ERROR_SXS_SECTION_NOT_FOUND = 14000;
      //public const uint ERROR_SXS_CANT_GEN_ACTCTX = 14001;
      //public const uint ERROR_SXS_INVALID_ACTCTXDATA_FORMAT = 14002;
      //public const uint ERROR_SXS_ASSEMBLY_NOT_FOUND = 14003;
      //public const uint ERROR_SXS_MANIFEST_FORMAT_ERROR = 14004;
      //public const uint ERROR_SXS_MANIFEST_PARSE_ERROR = 14005;
      //public const uint ERROR_SXS_ACTIVATION_CONTEXT_DISABLED = 14006;
      //public const uint ERROR_SXS_KEY_NOT_FOUND = 14007;
      //public const uint ERROR_SXS_VERSION_CONFLICT = 14008;
      //public const uint ERROR_SXS_WRONG_SECTION_TYPE = 14009;
      //public const uint ERROR_SXS_THREAD_QUERIES_DISABLED = 14010;
      //public const uint ERROR_SXS_PROCESS_DEFAULT_ALREADY_SET = 14011;
      //public const uint ERROR_SXS_UNKNOWN_ENCODING_GROUP = 14012;
      //public const uint ERROR_SXS_UNKNOWN_ENCODING = 14013;
      //public const uint ERROR_SXS_INVALID_XML_NAMESPACE_URI = 14014;
      //public const uint ERROR_SXS_ROOT_MANIFEST_DEPENDENCY_NOT_INSTALLED = 14015;
      //public const uint ERROR_SXS_LEAF_MANIFEST_DEPENDENCY_NOT_INSTALLED = 14016;
      //public const uint ERROR_SXS_INVALID_ASSEMBLY_IDENTITY_ATTRIBUTE = 14017;
      //public const uint ERROR_SXS_MANIFEST_MISSING_REQUIRED_DEFAULT_NAMESPACE = 14018;
      //public const uint ERROR_SXS_MANIFEST_INVALID_REQUIRED_DEFAULT_NAMESPACE = 14019;
      //public const uint ERROR_SXS_PRIVATE_MANIFEST_CROSS_PATH_WITH_REPARSE_POINT = 14020;
      //public const uint ERROR_SXS_DUPLICATE_DLL_NAME = 14021;
      //public const uint ERROR_SXS_DUPLICATE_WINDOWCLASS_NAME = 14022;
      //public const uint ERROR_SXS_DUPLICATE_CLSID = 14023;
      //public const uint ERROR_SXS_DUPLICATE_IID = 14024;
      //public const uint ERROR_SXS_DUPLICATE_TLBID = 14025;
      //public const uint ERROR_SXS_DUPLICATE_PROGID = 14026;
      //public const uint ERROR_SXS_DUPLICATE_ASSEMBLY_NAME = 14027;
      //public const uint ERROR_SXS_FILE_HASH_MISMATCH = 14028;
      //public const uint ERROR_SXS_POLICY_PARSE_ERROR = 14029;
      //public const uint ERROR_SXS_XML_E_MISSINGQUOTE = 14030;
      //public const uint ERROR_SXS_XML_E_COMMENTSYNTAX = 14031;
      //public const uint ERROR_SXS_XML_E_BADSTARTNAMECHAR = 14032;
      //public const uint ERROR_SXS_XML_E_BADNAMECHAR = 14033;
      //public const uint ERROR_SXS_XML_E_BADCHARINSTRING = 14034;
      //public const uint ERROR_SXS_XML_E_XMLDECLSYNTAX = 14035;
      //public const uint ERROR_SXS_XML_E_BADCHARDATA = 14036;
      //public const uint ERROR_SXS_XML_E_MISSINGWHITESPACE = 14037;
      //public const uint ERROR_SXS_XML_E_EXPECTINGTAGEND = 14038;
      //public const uint ERROR_SXS_XML_E_MISSINGSEMICOLON = 14039;
      //public const uint ERROR_SXS_XML_E_UNBALANCEDPAREN = 14040;
      //public const uint ERROR_SXS_XML_E_INTERNALERROR = 14041;
      //public const uint ERROR_SXS_XML_E_UNEXPECTED_WHITESPACE = 14042;
      //public const uint ERROR_SXS_XML_E_INCOMPLETE_ENCODING = 14043;
      //public const uint ERROR_SXS_XML_E_MISSING_PAREN = 14044;
      //public const uint ERROR_SXS_XML_E_EXPECTINGCLOSEQUOTE = 14045;
      //public const uint ERROR_SXS_XML_E_MULTIPLE_COLONS = 14046;
      //public const uint ERROR_SXS_XML_E_INVALID_DECIMAL = 14047;
      //public const uint ERROR_SXS_XML_E_INVALID_HEXIDECIMAL = 14048;
      //public const uint ERROR_SXS_XML_E_INVALID_UNICODE = 14049;
      //public const uint ERROR_SXS_XML_E_WHITESPACEORQUESTIONMARK = 14050;
      //public const uint ERROR_SXS_XML_E_UNEXPECTEDENDTAG = 14051;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDTAG = 14052;
      //public const uint ERROR_SXS_XML_E_DUPLICATEATTRIBUTE = 14053;
      //public const uint ERROR_SXS_XML_E_MULTIPLEROOTS = 14054;
      //public const uint ERROR_SXS_XML_E_INVALIDATROOTLEVEL = 14055;
      //public const uint ERROR_SXS_XML_E_BADXMLDECL = 14056;
      //public const uint ERROR_SXS_XML_E_MISSINGROOT = 14057;
      //public const uint ERROR_SXS_XML_E_UNEXPECTEDEOF = 14058;
      //public const uint ERROR_SXS_XML_E_BADPEREFINSUBSET = 14059;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDSTARTTAG = 14060;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDENDTAG = 14061;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDSTRING = 14062;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDCOMMENT = 14063;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDDECL = 14064;
      //public const uint ERROR_SXS_XML_E_UNCLOSEDCDATA = 14065;
      //public const uint ERROR_SXS_XML_E_RESERVEDNAMESPACE = 14066;
      //public const uint ERROR_SXS_XML_E_INVALIDENCODING = 14067;
      //public const uint ERROR_SXS_XML_E_INVALIDSWITCH = 14068;
      //public const uint ERROR_SXS_XML_E_BADXMLCASE = 14069;
      //public const uint ERROR_SXS_XML_E_INVALID_STANDALONE = 14070;
      //public const uint ERROR_SXS_XML_E_UNEXPECTED_STANDALONE = 14071;
      //public const uint ERROR_SXS_XML_E_INVALID_VERSION = 14072;
      //public const uint ERROR_SXS_XML_E_MISSINGEQUALS = 14073;
      //public const uint ERROR_SXS_PROTECTION_RECOVERY_FAILED = 14074;
      //public const uint ERROR_SXS_PROTECTION_PUBLIC_KEY_TOO_SHORT = 14075;
      //public const uint ERROR_SXS_PROTECTION_CATALOG_NOT_VALID = 14076;
      //public const uint ERROR_SXS_UNTRANSLATABLE_HRESULT = 14077;
      //public const uint ERROR_SXS_PROTECTION_CATALOG_FILE_MISSING = 14078;
      //public const uint ERROR_SXS_MISSING_ASSEMBLY_IDENTITY_ATTRIBUTE = 14079;
      //public const uint ERROR_SXS_INVALID_ASSEMBLY_IDENTITY_ATTRIBUTE_NAME = 14080;
      //public const uint ERROR_IPSEC_QM_POLICY_EXISTS = 13000;
      //public const uint ERROR_IPSEC_QM_POLICY_NOT_FOUND = 13001;
      //public const uint ERROR_IPSEC_QM_POLICY_IN_USE = 13002;
      //public const uint ERROR_IPSEC_MM_POLICY_EXISTS = 13003;
      //public const uint ERROR_IPSEC_MM_POLICY_NOT_FOUND = 13004;
      //public const uint ERROR_IPSEC_MM_POLICY_IN_USE = 13005;
      //public const uint ERROR_IPSEC_MM_FILTER_EXISTS = 13006;
      //public const uint ERROR_IPSEC_MM_FILTER_NOT_FOUND = 13007;
      //public const uint ERROR_IPSEC_TRANSPORT_FILTER_EXISTS = 13008;
      //public const uint ERROR_IPSEC_TRANSPORT_FILTER_NOT_FOUND = 13009;
      //public const uint ERROR_IPSEC_MM_AUTH_EXISTS = 13010;
      //public const uint ERROR_IPSEC_MM_AUTH_NOT_FOUND = 13011;
      //public const uint ERROR_IPSEC_MM_AUTH_IN_USE = 13012;
      //public const uint ERROR_IPSEC_DEFAULT_MM_POLICY_NOT_FOUND = 13013;
      //public const uint ERROR_IPSEC_DEFAULT_MM_AUTH_NOT_FOUND = 13014;
      //public const uint ERROR_IPSEC_DEFAULT_QM_POLICY_NOT_FOUND = 13015;
      //public const uint ERROR_IPSEC_TUNNEL_FILTER_EXISTS = 13016;
      //public const uint ERROR_IPSEC_TUNNEL_FILTER_NOT_FOUND = 13017;
      //public const uint ERROR_IPSEC_MM_FILTER_PENDING_DELETION = 13018;
      //public const uint ERROR_IPSEC_TRANSPORT_FILTER_PENDING_DELETION = 13019;
      //public const uint ERROR_IPSEC_TUNNEL_FILTER_PENDING_DELETION = 13020;
      //public const uint ERROR_IPSEC_MM_POLICY_PENDING_DELETION = 13021;
      //public const uint ERROR_IPSEC_MM_AUTH_PENDING_DELETION = 13022;
      //public const uint ERROR_IPSEC_QM_POLICY_PENDING_DELETION = 13023;
      //public const uint WARNING_IPSEC_MM_POLICY_PRUNED = 13024;
      //public const uint WARNING_IPSEC_QM_POLICY_PRUNED = 13025;
      //public const uint ERROR_IPSEC_IKE_NEG_STATUS_BEGIN = 13800;
      //public const uint ERROR_IPSEC_IKE_AUTH_FAIL = 13801;
      //public const uint ERROR_IPSEC_IKE_ATTRIB_FAIL = 13802;
      //public const uint ERROR_IPSEC_IKE_NEGOTIATION_PENDING = 13803;
      //public const uint ERROR_IPSEC_IKE_GENERAL_PROCESSING_ERROR = 13804;
      //public const uint ERROR_IPSEC_IKE_TIMED_OUT = 13805;
      //public const uint ERROR_IPSEC_IKE_NO_CERT = 13806;
      //public const uint ERROR_IPSEC_IKE_SA_DELETED = 13807;
      //public const uint ERROR_IPSEC_IKE_SA_REAPED = 13808;
      //public const uint ERROR_IPSEC_IKE_MM_ACQUIRE_DROP = 13809;
      //public const uint ERROR_IPSEC_IKE_QM_ACQUIRE_DROP = 13810;
      //public const uint ERROR_IPSEC_IKE_QUEUE_DROP_MM = 13811;
      //public const uint ERROR_IPSEC_IKE_QUEUE_DROP_NO_MM = 13812;
      //public const uint ERROR_IPSEC_IKE_DROP_NO_RESPONSE = 13813;
      //public const uint ERROR_IPSEC_IKE_MM_DELAY_DROP = 13814;
      //public const uint ERROR_IPSEC_IKE_QM_DELAY_DROP = 13815;
      //public const uint ERROR_IPSEC_IKE_ERROR = 13816;
      //public const uint ERROR_IPSEC_IKE_CRL_FAILED = 13817;
      //public const uint ERROR_IPSEC_IKE_INVALID_KEY_USAGE = 13818;
      //public const uint ERROR_IPSEC_IKE_INVALID_CERT_TYPE = 13819;
      //public const uint ERROR_IPSEC_IKE_NO_PRIVATE_KEY = 13820;
      //public const uint ERROR_IPSEC_IKE_DH_FAIL = 13822;
      //public const uint ERROR_IPSEC_IKE_INVALID_HEADER = 13824;
      //public const uint ERROR_IPSEC_IKE_NO_POLICY = 13825;
      //public const uint ERROR_IPSEC_IKE_INVALID_SIGNATURE = 13826;
      //public const uint ERROR_IPSEC_IKE_KERBEROS_ERROR = 13827;
      //public const uint ERROR_IPSEC_IKE_NO_PUBLIC_KEY = 13828;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR = 13829;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_SA = 13830;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_PROP = 13831;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_TRANS = 13832;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_KE = 13833;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_ID = 13834;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_CERT = 13835;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_CERT_REQ = 13836;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_HASH = 13837;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_SIG = 13838;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_NONCE = 13839;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_NOTIFY = 13840;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_DELETE = 13841;
      //public const uint ERROR_IPSEC_IKE_PROCESS_ERR_VENDOR = 13842;
      //public const uint ERROR_IPSEC_IKE_INVALID_PAYLOAD = 13843;
      //public const uint ERROR_IPSEC_IKE_LOAD_SOFT_SA = 13844;
      //public const uint ERROR_IPSEC_IKE_SOFT_SA_TORN_DOWN = 13845;
      //public const uint ERROR_IPSEC_IKE_INVALID_COOKIE = 13846;
      //public const uint ERROR_IPSEC_IKE_NO_PEER_CERT = 13847;
      //public const uint ERROR_IPSEC_IKE_PEER_CRL_FAILED = 13848;
      //public const uint ERROR_IPSEC_IKE_POLICY_CHANGE = 13849;
      //public const uint ERROR_IPSEC_IKE_NO_MM_POLICY = 13850;
      //public const uint ERROR_IPSEC_IKE_NOTCBPRIV = 13851;
      //public const uint ERROR_IPSEC_IKE_SECLOADFAIL = 13852;
      //public const uint ERROR_IPSEC_IKE_FAILSSPINIT = 13853;
      //public const uint ERROR_IPSEC_IKE_FAILQUERYSSP = 13854;
      //public const uint ERROR_IPSEC_IKE_SRVACQFAIL = 13855;
      //public const uint ERROR_IPSEC_IKE_SRVQUERYCRED = 13856;
      //public const uint ERROR_IPSEC_IKE_GETSPIFAIL = 13857;
      //public const uint ERROR_IPSEC_IKE_INVALID_FILTER = 13858;
      //public const uint ERROR_IPSEC_IKE_OUT_OF_MEMORY = 13859;
      //public const uint ERROR_IPSEC_IKE_ADD_UPDATE_KEY_FAILED = 13860;
      //public const uint ERROR_IPSEC_IKE_INVALID_POLICY = 13861;
      //public const uint ERROR_IPSEC_IKE_UNKNOWN_DOI = 13862;
      //public const uint ERROR_IPSEC_IKE_INVALID_SITUATION = 13863;
      //public const uint ERROR_IPSEC_IKE_DH_FAILURE = 13864;
      //public const uint ERROR_IPSEC_IKE_INVALID_GROUP = 13865;
      //public const uint ERROR_IPSEC_IKE_ENCRYPT = 13866;
      //public const uint ERROR_IPSEC_IKE_DECRYPT = 13867;
      //public const uint ERROR_IPSEC_IKE_POLICY_MATCH = 13868;
      //public const uint ERROR_IPSEC_IKE_UNSUPPORTED_ID = 13869;
      //public const uint ERROR_IPSEC_IKE_INVALID_HASH = 13870;
      //public const uint ERROR_IPSEC_IKE_INVALID_HASH_ALG = 13871;
      //public const uint ERROR_IPSEC_IKE_INVALID_HASH_SIZE = 13872;
      //public const uint ERROR_IPSEC_IKE_INVALID_ENCRYPT_ALG = 13873;
      //public const uint ERROR_IPSEC_IKE_INVALID_AUTH_ALG = 13874;
      //public const uint ERROR_IPSEC_IKE_INVALID_SIG = 13875;
      //public const uint ERROR_IPSEC_IKE_LOAD_FAILED = 13876;
      //public const uint ERROR_IPSEC_IKE_RPC_DELETE = 13877;
      //public const uint ERROR_IPSEC_IKE_BENIGN_REINIT = 13878;
      //public const uint ERROR_IPSEC_IKE_INVALID_RESPONDER_LIFETIME_NOTIFY = 13879;
      //public const uint ERROR_IPSEC_IKE_INVALID_CERT_KEYLEN = 13881;
      //public const uint ERROR_IPSEC_IKE_MM_LIMIT = 13882;
      //public const uint ERROR_IPSEC_IKE_NEGOTIATION_DISABLED = 13883;
      //public const uint ERROR_IPSEC_IKE_NEG_STATUS_END = 13884;
      //public const uint SEVERITY_SUCCESS = 0;
      //public const uint SEVERITY_ERROR = 1;
      //public const uint NOERROR = 0;

      //public const uint E_UNEXPECTED = 0x8000FFFF;
      //public const uint E_NOTIMPL = 0x80004001;
      //public const uint E_OUTOFMEMORY = 0x8007000E;
      //public const uint E_INVALIDARG = 0x80070057;
      //public const uint E_NOINTERFACE = 0x80004002;
      public const uint E_POINTER = 0x80004003;
      //public const uint E_HANDLE = 0x80070006;
      //public const uint E_ABORT = 0x80004004;
      //public const uint E_FAIL = 0x80004005;
      //public const uint E_ACCESSDENIED = 0x80070005;
      //public const uint E_PENDING = 0x8000000A;
      //public const uint CO_E_INIT_TLS = 0x80004006;
      //public const uint CO_E_INIT_SHARED_ALLOCATOR = 0x80004007;
      //public const uint CO_E_INIT_MEMORY_ALLOCATOR = 0x80004008;
      //public const uint CO_E_INIT_CLASS_CACHE = 0x80004009;
      //public const uint CO_E_INIT_RPC_CHANNEL = 0x8000400A;
      //public const uint CO_E_INIT_TLS_SET_CHANNEL_CONTROL = 0x8000400B;
      //public const uint CO_E_INIT_TLS_CHANNEL_CONTROL = 0x8000400C;
      //public const uint CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = 0x8000400D;
      //public const uint CO_E_INIT_SCM_MUTEX_EXISTS = 0x8000400E;
      //public const uint CO_E_INIT_SCM_FILE_MAPPING_EXISTS = 0x8000400F;
      //public const uint CO_E_INIT_SCM_MAP_VIEW_OF_FILE = 0x80004010;
      //public const uint CO_E_INIT_SCM_EXEC_FAILURE = 0x80004011;
      //public const uint CO_E_INIT_ONLY_SINGLE_THREADED = 0x80004012;
      //public const uint CO_E_CANT_REMOTE = 0x80004013;
      //public const uint CO_E_BAD_SERVER_NAME = 0x80004014;
      //public const uint CO_E_WRONG_SERVER_IDENTITY = 0x80004015;
      //public const uint CO_E_OLE1DDE_DISABLED = 0x80004016;
      //public const uint CO_E_RUNAS_SYNTAX = 0x80004017;
      //public const uint CO_E_CREATEPROCESS_FAILURE = 0x80004018;
      //public const uint CO_E_RUNAS_CREATEPROCESS_FAILURE = 0x80004019;
      //public const uint CO_E_RUNAS_LOGON_FAILURE = 0x8000401A;
      //public const uint CO_E_LAUNCH_PERMSSION_DENIED = 0x8000401B;
      //public const uint CO_E_START_SERVICE_FAILURE = 0x8000401C;
      //public const uint CO_E_REMOTE_COMMUNICATION_FAILURE = 0x8000401D;
      //public const uint CO_E_SERVER_START_TIMEOUT = 0x8000401E;
      //public const uint CO_E_CLSREG_INCONSISTENT = 0x8000401F;
      //public const uint CO_E_IIDREG_INCONSISTENT = 0x80004020;
      //public const uint CO_E_NOT_SUPPORTED = 0x80004021;
      //public const uint CO_E_RELOAD_DLL = 0x80004022;
      //public const uint CO_E_MSI_ERROR = 0x80004023;
      //public const uint CO_E_ATTEMPT_TO_CREATE_OUTSIDE_CLIENT_CONTEXT = 0x80004024;
      //public const uint CO_E_SERVER_PAUSED = 0x80004025;
      //public const uint CO_E_SERVER_NOT_PAUSED = 0x80004026;
      //public const uint CO_E_CLASS_DISABLED = 0x80004027;
      //public const uint CO_E_CLRNOTAVAILABLE = 0x80004028;
      //public const uint CO_E_ASYNC_WORK_REJECTED = 0x80004029;
      //public const uint CO_E_SERVER_INIT_TIMEOUT = 0x8000402A;
      //public const uint CO_E_NO_SECCTX_IN_ACTIVATE = 0x8000402B;
      //public const uint CO_E_TRACKER_CONFIG = 0x80004030;
      //public const uint CO_E_THREADPOOL_CONFIG = 0x80004031;
      //public const uint CO_E_SXS_CONFIG = 0x80004032;
      //public const uint CO_E_MALFORMED_SPN = 0x80004033;

      /// <summary>(0) The operation completed successfully.</summary>
      public const uint S_OK = 0x00000000;

      //public const uint S_FALSE = 0x00000001;
      //public const uint OLE_E_FIRST = 0x80040000;
      //public const uint OLE_E_LAST = 0x800400FF;
      //public const uint OLE_S_FIRST = 0x00040000;
      //public const uint OLE_S_LAST = 0x000400FF;
      //public const uint OLE_E_OLEVERB = 0x80040000;
      //public const uint OLE_E_ADVF = 0x80040001;
      //public const uint OLE_E_ENUM_NOMORE = 0x80040002;
      //public const uint OLE_E_ADVISENOTSUPPORTED = 0x80040003;
      //public const uint OLE_E_NOCONNECTION = 0x80040004;
      //public const uint OLE_E_NOTRUNNING = 0x80040005;
      //public const uint OLE_E_NOCACHE = 0x80040006;
      //public const uint OLE_E_BLANK = 0x80040007;
      //public const uint OLE_E_CLASSDIFF = 0x80040008;
      //public const uint OLE_E_CANT_GETMONIKER = 0x80040009;
      //public const uint OLE_E_CANT_BINDTOSOURCE = 0x8004000A;
      //public const uint OLE_E_STATIC = 0x8004000B;
      //public const uint OLE_E_PROMPTSAVECANCELLED = 0x8004000C;
      //public const uint OLE_E_INVALIDRECT = 0x8004000D;
      //public const uint OLE_E_WRONGCOMPOBJ = 0x8004000E;
      //public const uint OLE_E_INVALIDHWND = 0x8004000F;
      //public const uint OLE_E_NOT_INPLACEACTIVE = 0x80040010;
      //public const uint OLE_E_CANTCONVERT = 0x80040011;
      //public const uint OLE_E_NOSTORAGE = 0x80040012;
      //public const uint DV_E_FORMATETC = 0x80040064;
      //public const uint DV_E_DVTARGETDEVICE = 0x80040065;
      //public const uint DV_E_STGMEDIUM = 0x80040066;
      //public const uint DV_E_STATDATA = 0x80040067;
      //public const uint DV_E_LINDEX = 0x80040068;
      //public const uint DV_E_TYMED = 0x80040069;
      //public const uint DV_E_CLIPFORMAT = 0x8004006A;
      //public const uint DV_E_DVASPECT = 0x8004006B;
      //public const uint DV_E_DVTARGETDEVICE_SIZE = 0x8004006C;
      //public const uint DV_E_NOIVIEWOBJECT = 0x8004006D;
      //public const uint DRAGDROP_E_FIRST = 0x80040100;
      //public const uint DRAGDROP_E_LAST = 0x8004010F;
      //public const uint DRAGDROP_S_FIRST = 0x00040100;
      //public const uint DRAGDROP_S_LAST = 0x0004010F;
      //public const uint DRAGDROP_E_NOTREGISTERED = 0x80040100;
      //public const uint DRAGDROP_E_ALREADYREGISTERED = 0x80040101;
      //public const uint DRAGDROP_E_INVALIDHWND = 0x80040102;
      //public const uint CLASSFACTORY_E_FIRST = 0x80040110;
      //public const uint CLASSFACTORY_E_LAST = 0x8004011F;
      //public const uint CLASSFACTORY_S_FIRST = 0x00040110;
      //public const uint CLASSFACTORY_S_LAST = 0x0004011F;
      //public const uint CLASS_E_NOAGGREGATION = 0x80040110;
      //public const uint CLASS_E_CLASSNOTAVAILABLE = 0x80040111;
      //public const uint CLASS_E_NOTLICENSED = 0x80040112;
      //public const uint MARSHAL_E_FIRST = 0x80040120;
      //public const uint MARSHAL_E_LAST = 0x8004012F;
      //public const uint MARSHAL_S_FIRST = 0x00040120;
      //public const uint MARSHAL_S_LAST = 0x0004012F;
      //public const uint DATA_E_FIRST = 0x80040130;
      //public const uint DATA_E_LAST = 0x8004013F;
      //public const uint DATA_S_FIRST = 0x00040130;
      //public const uint DATA_S_LAST = 0x0004013F;
      //public const uint VIEW_E_FIRST = 0x80040140;
      //public const uint VIEW_E_LAST = 0x8004014F;
      //public const uint VIEW_S_FIRST = 0x00040140;
      //public const uint VIEW_S_LAST = 0x0004014F;
      //public const uint VIEW_E_DRAW = 0x80040140;
      //public const uint REGDB_E_FIRST = 0x80040150;
      //public const uint REGDB_E_LAST = 0x8004015F;
      //public const uint REGDB_S_FIRST = 0x00040150;
      //public const uint REGDB_S_LAST = 0x0004015F;
      //public const uint REGDB_E_READREGDB = 0x80040150;
      //public const uint REGDB_E_WRITEREGDB = 0x80040151;
      //public const uint REGDB_E_KEYMISSING = 0x80040152;
      //public const uint REGDB_E_INVALIDVALUE = 0x80040153;
      //public const uint REGDB_E_CLASSNOTREG = 0x80040154;
      //public const uint REGDB_E_IIDNOTREG = 0x80040155;
      //public const uint REGDB_E_BADTHREADINGMODEL = 0x80040156;
      //public const uint CAT_E_FIRST = 0x80040160;
      //public const uint CAT_E_LAST = 0x80040161;
      //public const uint CAT_E_CATIDNOEXIST = 0x80040160;
      //public const uint CAT_E_NODESCRIPTION = 0x80040161;
      //public const uint CS_E_FIRST = 0x80040164;
      //public const uint CS_E_LAST = 0x8004016F;
      //public const uint CS_E_PACKAGE_NOTFOUND = 0x80040164;
      //public const uint CS_E_NOT_DELETABLE = 0x80040165;
      //public const uint CS_E_CLASS_NOTFOUND = 0x80040166;
      //public const uint CS_E_INVALID_VERSION = 0x80040167;
      //public const uint CS_E_NO_CLASSSTORE = 0x80040168;
      //public const uint CS_E_OBJECT_NOTFOUND = 0x80040169;
      //public const uint CS_E_OBJECT_ALREADY_EXISTS = 0x8004016A;
      //public const uint CS_E_INVALID_PATH = 0x8004016B;
      //public const uint CS_E_NETWORK_ERROR = 0x8004016C;
      //public const uint CS_E_ADMIN_LIMIT_EXCEEDED = 0x8004016D;
      //public const uint CS_E_SCHEMA_MISMATCH = 0x8004016E;
      //public const uint CS_E_INTERNAL_ERROR = 0x8004016F;
      //public const uint CACHE_E_FIRST = 0x80040170;
      //public const uint CACHE_E_LAST = 0x8004017F;
      //public const uint CACHE_S_FIRST = 0x00040170;
      //public const uint CACHE_S_LAST = 0x0004017F;
      //public const uint CACHE_E_NOCACHE_UPDATED = 0x80040170;
      //public const uint OLEOBJ_E_FIRST = 0x80040180;
      //public const uint OLEOBJ_E_LAST = 0x8004018F;
      //public const uint OLEOBJ_S_FIRST = 0x00040180;
      //public const uint OLEOBJ_S_LAST = 0x0004018F;
      //public const uint OLEOBJ_E_NOVERBS = 0x80040180;
      //public const uint OLEOBJ_E_INVALIDVERB = 0x80040181;
      //public const uint CLIENTSITE_E_FIRST = 0x80040190;
      //public const uint CLIENTSITE_E_LAST = 0x8004019F;
      //public const uint CLIENTSITE_S_FIRST = 0x00040190;
      //public const uint CLIENTSITE_S_LAST = 0x0004019F;
      //public const uint INPLACE_E_NOTUNDOABLE = 0x800401A0;
      //public const uint INPLACE_E_NOTOOLSPACE = 0x800401A1;
      //public const uint INPLACE_E_FIRST = 0x800401A0;
      //public const uint INPLACE_E_LAST = 0x800401AF;
      //public const uint INPLACE_S_FIRST = 0x000401A0;
      //public const uint INPLACE_S_LAST = 0x000401AF;
      //public const uint ENUM_E_FIRST = 0x800401B0;
      //public const uint ENUM_E_LAST = 0x800401BF;
      //public const uint ENUM_S_FIRST = 0x000401B0;
      //public const uint ENUM_S_LAST = 0x000401BF;
      //public const uint CONVERT10_E_FIRST = 0x800401C0;
      //public const uint CONVERT10_E_LAST = 0x800401CF;
      //public const uint CONVERT10_S_FIRST = 0x000401C0;
      //public const uint CONVERT10_S_LAST = 0x000401CF;
      //public const uint CONVERT10_E_OLESTREAM_GET = 0x800401C0;
      //public const uint CONVERT10_E_OLESTREAM_PUT = 0x800401C1;
      //public const uint CONVERT10_E_OLESTREAM_FMT = 0x800401C2;
      //public const uint CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = 0x800401C3;
      //public const uint CONVERT10_E_STG_FMT = 0x800401C4;
      //public const uint CONVERT10_E_STG_NO_STD_STREAM = 0x800401C5;
      //public const uint CONVERT10_E_STG_DIB_TO_BITMAP = 0x800401C6;
      //public const uint CLIPBRD_E_FIRST = 0x800401D0;
      //public const uint CLIPBRD_E_LAST = 0x800401DF;
      //public const uint CLIPBRD_S_FIRST = 0x000401D0;
      //public const uint CLIPBRD_S_LAST = 0x000401DF;
      //public const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;
      //public const uint CLIPBRD_E_CANT_EMPTY = 0x800401D1;
      //public const uint CLIPBRD_E_CANT_SET = 0x800401D2;
      //public const uint CLIPBRD_E_BAD_DATA = 0x800401D3;
      //public const uint CLIPBRD_E_CANT_CLOSE = 0x800401D4;
      //public const uint MK_E_FIRST = 0x800401E0;
      //public const uint MK_E_LAST = 0x800401EF;
      //public const uint MK_S_FIRST = 0x000401E0;
      //public const uint MK_S_LAST = 0x000401EF;
      //public const uint MK_E_CONNECTMANUALLY = 0x800401E0;
      //public const uint MK_E_EXCEEDEDDEADLINE = 0x800401E1;
      //public const uint MK_E_NEEDGENERIC = 0x800401E2;
      //public const uint MK_E_UNAVAILABLE = 0x800401E3;
      //public const uint MK_E_SYNTAX = 0x800401E4;
      //public const uint MK_E_NOOBJECT = 0x800401E5;
      //public const uint MK_E_INVALIDEXTENSION = 0x800401E6;
      //public const uint MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = 0x800401E7;
      //public const uint MK_E_NOTBINDABLE = 0x800401E8;
      //public const uint MK_E_NOTBOUND = 0x800401E9;
      //public const uint MK_E_CANTOPENFILE = 0x800401EA;
      //public const uint MK_E_MUSTBOTHERUSER = 0x800401EB;
      //public const uint MK_E_NOINVERSE = 0x800401EC;
      //public const uint MK_E_NOSTORAGE = 0x800401ED;
      //public const uint MK_E_NOPREFIX = 0x800401EE;
      //public const uint MK_E_ENUMERATION_FAILED = 0x800401EF;
      //public const uint CO_E_FIRST = 0x800401F0;
      //public const uint CO_E_LAST = 0x800401FF;
      //public const uint CO_S_FIRST = 0x000401F0;
      //public const uint CO_S_LAST = 0x000401FF;
      //public const uint CO_E_NOTINITIALIZED = 0x800401F0;
      //public const uint CO_E_ALREADYINITIALIZED = 0x800401F1;
      //public const uint CO_E_CANTDETERMINECLASS = 0x800401F2;
      //public const uint CO_E_CLASSSTRING = 0x800401F3;
      //public const uint CO_E_IIDSTRING = 0x800401F4;
      //public const uint CO_E_APPNOTFOUND = 0x800401F5;
      //public const uint CO_E_APPSINGLEUSE = 0x800401F6;
      //public const uint CO_E_ERRORINAPP = 0x800401F7;
      //public const uint CO_E_DLLNOTFOUND = 0x800401F8;
      //public const uint CO_E_ERRORINDLL = 0x800401F9;
      //public const uint CO_E_WRONGOSFORAPP = 0x800401FA;
      //public const uint CO_E_OBJNOTREG = 0x800401FB;
      //public const uint CO_E_OBJISREG = 0x800401FC;
      //public const uint CO_E_OBJNOTCONNECTED = 0x800401FD;
      //public const uint CO_E_APPDIDNTREG = 0x800401FE;
      //public const uint CO_E_RELEASED = 0x800401FF;
      //public const uint EVENT_E_FIRST = 0x80040200;
      //public const uint EVENT_E_LAST = 0x8004021F;
      //public const uint EVENT_S_FIRST = 0x00040200;
      //public const uint EVENT_S_LAST = 0x0004021F;
      //public const uint EVENT_S_SOME_SUBSCRIBERS_FAILED = 0x00040200;
      //public const uint EVENT_E_ALL_SUBSCRIBERS_FAILED = 0x80040201;
      //public const uint EVENT_S_NOSUBSCRIBERS = 0x00040202;
      //public const uint EVENT_E_QUERYSYNTAX = 0x80040203;
      //public const uint EVENT_E_QUERYFIELD = 0x80040204;
      //public const uint EVENT_E_INTERNALEXCEPTION = 0x80040205;
      //public const uint EVENT_E_INTERNALERROR = 0x80040206;
      //public const uint EVENT_E_INVALID_PER_USER_SID = 0x80040207;
      //public const uint EVENT_E_USER_EXCEPTION = 0x80040208;
      //public const uint EVENT_E_TOO_MANY_METHODS = 0x80040209;
      //public const uint EVENT_E_MISSING_EVENTCLASS = 0x8004020A;
      //public const uint EVENT_E_NOT_ALL_REMOVED = 0x8004020B;
      //public const uint EVENT_E_COMPLUS_NOT_INSTALLED = 0x8004020C;
      //public const uint EVENT_E_CANT_MODIFY_OR_DELETE_UNCONFIGURED_OBJECT = 0x8004020D;
      //public const uint EVENT_E_CANT_MODIFY_OR_DELETE_CONFIGURED_OBJECT = 0x8004020E;
      //public const uint EVENT_E_INVALID_EVENT_CLASS_PARTITION = 0x8004020F;
      //public const uint EVENT_E_PER_USER_SID_NOT_LOGGED_ON = 0x80040210;
      //public const uint XACT_E_FIRST = 0x8004D000;
      //public const uint XACT_E_LAST = 0x8004D029;
      //public const uint XACT_S_FIRST = 0x0004D000;
      //public const uint XACT_S_LAST = 0x0004D010;
      //public const uint XACT_E_ALREADYOTHERSINGLEPHASE = 0x8004D000;
      //public const uint XACT_E_CANTRETAIN = 0x8004D001;
      //public const uint XACT_E_COMMITFAILED = 0x8004D002;
      //public const uint XACT_E_COMMITPREVENTED = 0x8004D003;
      //public const uint XACT_E_HEURISTICABORT = 0x8004D004;
      //public const uint XACT_E_HEURISTICCOMMIT = 0x8004D005;
      //public const uint XACT_E_HEURISTICDAMAGE = 0x8004D006;
      //public const uint XACT_E_HEURISTICDANGER = 0x8004D007;
      //public const uint XACT_E_ISOLATIONLEVEL = 0x8004D008;
      //public const uint XACT_E_NOASYNC = 0x8004D009;
      //public const uint XACT_E_NOENLIST = 0x8004D00A;
      //public const uint XACT_E_NOISORETAIN = 0x8004D00B;
      //public const uint XACT_E_NORESOURCE = 0x8004D00C;
      //public const uint XACT_E_NOTCURRENT = 0x8004D00D;
      //public const uint XACT_E_NOTRANSACTION = 0x8004D00E;
      //public const uint XACT_E_NOTSUPPORTED = 0x8004D00F;
      //public const uint XACT_E_UNKNOWNRMGRID = 0x8004D010;
      //public const uint XACT_E_WRONGSTATE = 0x8004D011;
      //public const uint XACT_E_WRONGUOW = 0x8004D012;
      //public const uint XACT_E_XTIONEXISTS = 0x8004D013;
      //public const uint XACT_E_NOIMPORTOBJECT = 0x8004D014;
      //public const uint XACT_E_INVALIDCOOKIE = 0x8004D015;
      //public const uint XACT_E_INDOUBT = 0x8004D016;
      //public const uint XACT_E_NOTIMEOUT = 0x8004D017;
      //public const uint XACT_E_ALREADYINPROGRESS = 0x8004D018;
      //public const uint XACT_E_ABORTED = 0x8004D019;
      //public const uint XACT_E_LOGFULL = 0x8004D01A;
      //public const uint XACT_E_TMNOTAVAILABLE = 0x8004D01B;
      //public const uint XACT_E_CONNECTION_DOWN = 0x8004D01C;
      //public const uint XACT_E_CONNECTION_DENIED = 0x8004D01D;
      //public const uint XACT_E_REENLISTTIMEOUT = 0x8004D01E;
      //public const uint XACT_E_TIP_CONNECT_FAILED = 0x8004D01F;
      //public const uint XACT_E_TIP_PROTOCOL_ERROR = 0x8004D020;
      //public const uint XACT_E_TIP_PULL_FAILED = 0x8004D021;
      //public const uint XACT_E_DEST_TMNOTAVAILABLE = 0x8004D022;
      //public const uint XACT_E_TIP_DISABLED = 0x8004D023;
      //public const uint XACT_E_NETWORK_TX_DISABLED = 0x8004D024;
      //public const uint XACT_E_PARTNER_NETWORK_TX_DISABLED = 0x8004D025;
      //public const uint XACT_E_XA_TX_DISABLED = 0x8004D026;
      //public const uint XACT_E_UNABLE_TO_READ_DTC_CONFIG = 0x8004D027;
      //public const uint XACT_E_UNABLE_TO_LOAD_DTC_PROXY = 0x8004D028;
      //public const uint XACT_E_ABORTING = 0x8004D029;
      //public const uint XACT_E_CLERKNOTFOUND = 0x8004D080;
      //public const uint XACT_E_CLERKEXISTS = 0x8004D081;
      //public const uint XACT_E_RECOVERYINPROGRESS = 0x8004D082;
      //public const uint XACT_E_TRANSACTIONCLOSED = 0x8004D083;
      //public const uint XACT_E_INVALIDLSN = 0x8004D084;
      //public const uint XACT_E_REPLAYREQUEST = 0x8004D085;
      //public const uint XACT_S_ASYNC = 0x0004D000;
      //public const uint XACT_S_DEFECT = 0x0004D001;
      //public const uint XACT_S_READONLY = 0x0004D002;
      //public const uint XACT_S_SOMENORETAIN = 0x0004D003;
      //public const uint XACT_S_OKINFORM = 0x0004D004;
      //public const uint XACT_S_MADECHANGESCONTENT = 0x0004D005;
      //public const uint XACT_S_MADECHANGESINFORM = 0x0004D006;
      //public const uint XACT_S_ALLNORETAIN = 0x0004D007;
      //public const uint XACT_S_ABORTING = 0x0004D008;
      //public const uint XACT_S_SINGLEPHASE = 0x0004D009;
      //public const uint XACT_S_LOCALLY_OK = 0x0004D00A;
      //public const uint XACT_S_LASTRESOURCEMANAGER = 0x0004D010;
      //public const uint CONTEXT_E_FIRST = 0x8004E000;
      //public const uint CONTEXT_E_LAST = 0x8004E02F;
      //public const uint CONTEXT_S_FIRST = 0x0004E000;
      //public const uint CONTEXT_S_LAST = 0x0004E02F;
      //public const uint CONTEXT_E_ABORTED = 0x8004E002;
      //public const uint CONTEXT_E_ABORTING = 0x8004E003;
      //public const uint CONTEXT_E_NOCONTEXT = 0x8004E004;
      //public const uint CONTEXT_E_WOULD_DEADLOCK = 0x8004E005;
      //public const uint CONTEXT_E_SYNCH_TIMEOUT = 0x8004E006;
      //public const uint CONTEXT_E_OLDREF = 0x8004E007;
      //public const uint CONTEXT_E_ROLENOTFOUND = 0x8004E00C;
      //public const uint CONTEXT_E_TMNOTAVAILABLE = 0x8004E00F;
      //public const uint CO_E_ACTIVATIONFAILED = 0x8004E021;
      //public const uint CO_E_ACTIVATIONFAILED_EVENTLOGGED = 0x8004E022;
      //public const uint CO_E_ACTIVATIONFAILED_CATALOGERROR = 0x8004E023;
      //public const uint CO_E_ACTIVATIONFAILED_TIMEOUT = 0x8004E024;
      //public const uint CO_E_INITIALIZATIONFAILED = 0x8004E025;
      //public const uint CONTEXT_E_NOJIT = 0x8004E026;
      //public const uint CONTEXT_E_NOTRANSACTION = 0x8004E027;
      //public const uint CO_E_THREADINGMODEL_CHANGED = 0x8004E028;
      //public const uint CO_E_NOIISINTRINSICS = 0x8004E029;
      //public const uint CO_E_NOCOOKIES = 0x8004E02A;
      //public const uint CO_E_DBERROR = 0x8004E02B;
      //public const uint CO_E_NOTPOOLED = 0x8004E02C;
      //public const uint CO_E_NOTCONSTRUCTED = 0x8004E02D;
      //public const uint CO_E_NOSYNCHRONIZATION = 0x8004E02E;
      //public const uint CO_E_ISOLEVELMISMATCH = 0x8004E02F;
      //public const uint OLE_S_USEREG = 0x00040000;
      //public const uint OLE_S_STATIC = 0x00040001;
      //public const uint OLE_S_MAC_CLIPFORMAT = 0x00040002;
      //public const uint DRAGDROP_S_DROP = 0x00040100;
      //public const uint DRAGDROP_S_CANCEL = 0x00040101;
      //public const uint DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
      //public const uint DATA_S_SAMEFORMATETC = 0x00040130;
      //public const uint VIEW_S_ALREADY_FROZEN = 0x00040140;
      //public const uint CACHE_S_FORMATETC_NOTSUPPORTED = 0x00040170;
      //public const uint CACHE_S_SAMECACHE = 0x00040171;
      //public const uint CACHE_S_SOMECACHES_NOTUPDATED = 0x00040172;
      //public const uint OLEOBJ_S_INVALIDVERB = 0x00040180;
      //public const uint OLEOBJ_S_CANNOT_DOVERB_NOW = 0x00040181;
      //public const uint OLEOBJ_S_INVALIDHWND = 0x00040182;
      //public const uint INPLACE_S_TRUNCATED = 0x000401A0;
      //public const uint CONVERT10_S_NO_PRESENTATION = 0x000401C0;
      //public const uint MK_S_REDUCED_TO_SELF = 0x000401E2;
      //public const uint MK_S_ME = 0x000401E4;
      //public const uint MK_S_HIM = 0x000401E5;
      //public const uint MK_S_US = 0x000401E6;
      //public const uint MK_S_MONIKERALREADYREGISTERED = 0x000401E7;
      //public const uint SCHED_S_TASK_READY = 0x00041300;
      //public const uint SCHED_S_TASK_RUNNING = 0x00041301;
      //public const uint SCHED_S_TASK_DISABLED = 0x00041302;
      //public const uint SCHED_S_TASK_HAS_NOT_RUN = 0x00041303;
      //public const uint SCHED_S_TASK_NO_MORE_RUNS = 0x00041304;
      //public const uint SCHED_S_TASK_NOT_SCHEDULED = 0x00041305;
      //public const uint SCHED_S_TASK_TERMINATED = 0x00041306;
      //public const uint SCHED_S_TASK_NO_VALID_TRIGGERS = 0x00041307;
      //public const uint SCHED_S_EVENT_TRIGGER = 0x00041308;
      //public const uint SCHED_E_TRIGGER_NOT_FOUND = 0x80041309;
      //public const uint SCHED_E_TASK_NOT_READY = 0x8004130A;
      //public const uint SCHED_E_TASK_NOT_RUNNING = 0x8004130B;
      //public const uint SCHED_E_SERVICE_NOT_INSTALLED = 0x8004130C;
      //public const uint SCHED_E_CANNOT_OPEN_TASK = 0x8004130D;
      //public const uint SCHED_E_INVALID_TASK = 0x8004130E;
      //public const uint SCHED_E_ACCOUNT_INFORMATION_NOT_SET = 0x8004130F;
      //public const uint SCHED_E_ACCOUNT_NAME_NOT_FOUND = 0x80041310;
      //public const uint SCHED_E_ACCOUNT_DBASE_CORRUPT = 0x80041311;
      //public const uint SCHED_E_NO_SECURITY_SERVICES = 0x80041312;
      //public const uint SCHED_E_UNKNOWN_OBJECT_VERSION = 0x80041313;
      //public const uint SCHED_E_UNSUPPORTED_ACCOUNT_OPTION = 0x80041314;
      //public const uint SCHED_E_SERVICE_NOT_RUNNING = 0x80041315;
      //public const uint CO_E_CLASS_CREATE_FAILED = 0x80080001;
      //public const uint CO_E_SCM_ERROR = 0x80080002;
      //public const uint CO_E_SCM_RPC_FAILURE = 0x80080003;
      //public const uint CO_E_BAD_PATH = 0x80080004;
      //public const uint CO_E_SERVER_EXEC_FAILURE = 0x80080005;
      //public const uint CO_E_OBJSRV_RPC_FAILURE = 0x80080006;
      //public const uint MK_E_NO_NORMALIZED = 0x80080007;
      //public const uint CO_E_SERVER_STOPPING = 0x80080008;
      //public const uint MEM_E_INVALID_ROOT = 0x80080009;
      //public const uint MEM_E_INVALID_LINK = 0x80080010;
      //public const uint MEM_E_INVALID_SIZE = 0x80080011;
      //public const uint CO_S_NOTALLINTERFACES = 0x00080012;
      //public const uint CO_S_MACHINENAMENOTFOUND = 0x00080013;
      //public const uint DISP_E_UNKNOWNINTERFACE = 0x80020001;
      //public const uint DISP_E_MEMBERNOTFOUND = 0x80020003;
      //public const uint DISP_E_PARAMNOTFOUND = 0x80020004;
      //public const uint DISP_E_TYPEMISMATCH = 0x80020005;
      //public const uint DISP_E_UNKNOWNNAME = 0x80020006;
      //public const uint DISP_E_NONAMEDARGS = 0x80020007;
      //public const uint DISP_E_BADVARTYPE = 0x80020008;
      //public const uint DISP_E_EXCEPTION = 0x80020009;
      //public const uint DISP_E_OVERFLOW = 0x8002000A;
      //public const uint DISP_E_BADINDEX = 0x8002000B;
      //public const uint DISP_E_UNKNOWNLCID = 0x8002000C;
      //public const uint DISP_E_ARRAYISLOCKED = 0x8002000D;
      //public const uint DISP_E_BADPARAMCOUNT = 0x8002000E;
      //public const uint DISP_E_PARAMNOTOPTIONAL = 0x8002000F;
      //public const uint DISP_E_BADCALLEE = 0x80020010;
      //public const uint DISP_E_NOTACOLLECTION = 0x80020011;
      //public const uint DISP_E_DIVBYZERO = 0x80020012;
      //public const uint DISP_E_BUFFERTOOSMALL = 0x80020013;
      //public const uint TYPE_E_BUFFERTOOSMALL = 0x80028016;
      //public const uint TYPE_E_FIELDNOTFOUND = 0x80028017;
      //public const uint TYPE_E_INVDATAREAD = 0x80028018;
      //public const uint TYPE_E_UNSUPFORMAT = 0x80028019;
      //public const uint TYPE_E_REGISTRYACCESS = 0x8002801C;
      //public const uint TYPE_E_LIBNOTREGISTERED = 0x8002801D;
      //public const uint TYPE_E_UNDEFINEDTYPE = 0x80028027;
      //public const uint TYPE_E_QUALIFIEDNAMEDISALLOWED = 0x80028028;
      //public const uint TYPE_E_INVALIDSTATE = 0x80028029;
      //public const uint TYPE_E_WRONGTYPEKIND = 0x8002802A;
      //public const uint TYPE_E_ELEMENTNOTFOUND = 0x8002802B;
      //public const uint TYPE_E_AMBIGUOUSNAME = 0x8002802C;
      //public const uint TYPE_E_NAMECONFLICT = 0x8002802D;
      //public const uint TYPE_E_UNKNOWNLCID = 0x8002802E;
      //public const uint TYPE_E_DLLFUNCTIONNOTFOUND = 0x8002802F;
      //public const uint TYPE_E_BADMODULEKIND = 0x800288BD;
      //public const uint TYPE_E_SIZETOOBIG = 0x800288C5;
      //public const uint TYPE_E_DUPLICATEID = 0x800288C6;
      //public const uint TYPE_E_INVALIDID = 0x800288CF;
      //public const uint TYPE_E_TYPEMISMATCH = 0x80028CA0;
      //public const uint TYPE_E_OUTOFBOUNDS = 0x80028CA1;
      //public const uint TYPE_E_IOERROR = 0x80028CA2;
      //public const uint TYPE_E_CANTCREATETMPFILE = 0x80028CA3;
      //public const uint TYPE_E_CANTLOADLIBRARY = 0x80029C4A;
      //public const uint TYPE_E_INCONSISTENTPROPFUNCS = 0x80029C83;
      //public const uint TYPE_E_CIRCULARTYPE = 0x80029C84;
      //public const uint STG_E_INVALIDFUNCTION = 0x80030001;
      //public const uint STG_E_FILENOTFOUND = 0x80030002;
      //public const uint STG_E_PATHNOTFOUND = 0x80030003;
      //public const uint STG_E_TOOMANYOPENFILES = 0x80030004;
      //public const uint STG_E_ACCESSDENIED = 0x80030005;
      //public const uint STG_E_INVALIDHANDLE = 0x80030006;
      //public const uint STG_E_INSUFFICIENTMEMORY = 0x80030008;
      //public const uint STG_E_INVALIDPOINTER = 0x80030009;
      //public const uint STG_E_NOMOREFILES = 0x80030012;
      //public const uint STG_E_DISKISWRITEPROTECTED = 0x80030013;
      //public const uint STG_E_SEEKERROR = 0x80030019;
      //public const uint STG_E_WRITEFAULT = 0x8003001D;
      //public const uint STG_E_READFAULT = 0x8003001E;
      //public const uint STG_E_SHAREVIOLATION = 0x80030020;
      //public const uint STG_E_LOCKVIOLATION = 0x80030021;
      //public const uint STG_E_FILEALREADYEXISTS = 0x80030050;
      //public const uint STG_E_INVALIDPARAMETER = 0x80030057;
      //public const uint STG_E_MEDIUMFULL = 0x80030070;
      //public const uint STG_E_PROPSETMISMATCHED = 0x800300F0;
      //public const uint STG_E_ABNORMALAPIEXIT = 0x800300FA;
      //public const uint STG_E_INVALIDHEADER = 0x800300FB;
      //public const uint STG_E_INVALIDNAME = 0x800300FC;
      //public const uint STG_E_UNKNOWN = 0x800300FD;
      //public const uint STG_E_UNIMPLEMENTEDFUNCTION = 0x800300FE;
      //public const uint STG_E_INVALIDFLAG = 0x800300FF;
      //public const uint STG_E_INUSE = 0x80030100;
      //public const uint STG_E_NOTCURRENT = 0x80030101;
      //public const uint STG_E_REVERTED = 0x80030102;
      //public const uint STG_E_CANTSAVE = 0x80030103;
      //public const uint STG_E_OLDFORMAT = 0x80030104;
      //public const uint STG_E_OLDDLL = 0x80030105;
      //public const uint STG_E_SHAREREQUIRED = 0x80030106;
      //public const uint STG_E_NOTFILEBASEDSTORAGE = 0x80030107;
      //public const uint STG_E_EXTANTMARSHALLINGS = 0x80030108;
      //public const uint STG_E_DOCFILECORRUPT = 0x80030109;
      //public const uint STG_E_BADBASEADDRESS = 0x80030110;
      //public const uint STG_E_DOCFILETOOLARGE = 0x80030111;
      //public const uint STG_E_NOTSIMPLEFORMAT = 0x80030112;
      //public const uint STG_E_INCOMPLETE = 0x80030201;
      //public const uint STG_E_TERMINATED = 0x80030202;
      //public const uint STG_S_CONVERTED = 0x00030200;
      //public const uint STG_S_BLOCK = 0x00030201;
      //public const uint STG_S_RETRYNOW = 0x00030202;
      //public const uint STG_S_MONITORING = 0x00030203;
      //public const uint STG_S_MULTIPLEOPENS = 0x00030204;
      //public const uint STG_S_CONSOLIDATIONFAILED = 0x00030205;
      //public const uint STG_S_CANNOTCONSOLIDATE = 0x00030206;
      //public const uint STG_E_STATUS_COPY_PROTECTION_FAILURE = 0x80030305;
      //public const uint STG_E_CSS_AUTHENTICATION_FAILURE = 0x80030306;
      //public const uint STG_E_CSS_KEY_NOT_PRESENT = 0x80030307;
      //public const uint STG_E_CSS_KEY_NOT_ESTABLISHED = 0x80030308;
      //public const uint STG_E_CSS_SCRAMBLED_SECTOR = 0x80030309;
      //public const uint STG_E_CSS_REGION_MISMATCH = 0x8003030A;
      //public const uint STG_E_RESETS_EXHAUSTED = 0x8003030B;
      //public const uint RPC_E_CALL_REJECTED = 0x80010001;
      //public const uint RPC_E_CALL_CANCELED = 0x80010002;
      //public const uint RPC_E_CANTPOST_INSENDCALL = 0x80010003;
      //public const uint RPC_E_CANTCALLOUT_INASYNCCALL = 0x80010004;
      //public const uint RPC_E_CANTCALLOUT_INEXTERNALCALL = 0x80010005;
      //public const uint RPC_E_CONNECTION_TERMINATED = 0x80010006;
      //public const uint RPC_E_SERVER_DIED = 0x80010007;
      //public const uint RPC_E_CLIENT_DIED = 0x80010008;
      //public const uint RPC_E_INVALID_DATAPACKET = 0x80010009;
      //public const uint RPC_E_CANTTRANSMIT_CALL = 0x8001000A;
      //public const uint RPC_E_CLIENT_CANTMARSHAL_DATA = 0x8001000B;
      //public const uint RPC_E_CLIENT_CANTUNMARSHAL_DATA = 0x8001000C;
      //public const uint RPC_E_SERVER_CANTMARSHAL_DATA = 0x8001000D;
      //public const uint RPC_E_SERVER_CANTUNMARSHAL_DATA = 0x8001000E;
      //public const uint RPC_E_INVALID_DATA = 0x8001000F;
      //public const uint RPC_E_INVALID_PARAMETER = 0x80010010;
      //public const uint RPC_E_CANTCALLOUT_AGAIN = 0x80010011;
      //public const uint RPC_E_SERVER_DIED_DNE = 0x80010012;
      //public const uint RPC_E_SYS_CALL_FAILED = 0x80010100;
      //public const uint RPC_E_OUT_OF_RESOURCES = 0x80010101;
      //public const uint RPC_E_ATTEMPTED_MULTITHREAD = 0x80010102;
      //public const uint RPC_E_NOT_REGISTERED = 0x80010103;
      //public const uint RPC_E_FAULT = 0x80010104;
      //public const uint RPC_E_SERVERFAULT = 0x80010105;
      //public const uint RPC_E_CHANGED_MODE = 0x80010106;
      //public const uint RPC_E_INVALIDMETHOD = 0x80010107;
      //public const uint RPC_E_DISCONNECTED = 0x80010108;
      //public const uint RPC_E_RETRY = 0x80010109;
      //public const uint RPC_E_SERVERCALL_RETRYLATER = 0x8001010A;
      //public const uint RPC_E_SERVERCALL_REJECTED = 0x8001010B;
      //public const uint RPC_E_INVALID_CALLDATA = 0x8001010C;
      //public const uint RPC_E_CANTCALLOUT_ININPUTSYNCCALL = 0x8001010D;
      //public const uint RPC_E_WRONG_THREAD = 0x8001010E;
      //public const uint RPC_E_THREAD_NOT_INIT = 0x8001010F;
      //public const uint RPC_E_VERSION_MISMATCH = 0x80010110;
      //public const uint RPC_E_INVALID_HEADER = 0x80010111;
      //public const uint RPC_E_INVALID_EXTENSION = 0x80010112;
      //public const uint RPC_E_INVALID_IPID = 0x80010113;
      //public const uint RPC_E_INVALID_OBJECT = 0x80010114;
      //public const uint RPC_S_CALLPENDING = 0x80010115;
      //public const uint RPC_S_WAITONTIMER = 0x80010116;
      //public const uint RPC_E_CALL_COMPLETE = 0x80010117;
      //public const uint RPC_E_UNSECURE_CALL = 0x80010118;
      //public const uint RPC_E_TOO_LATE = 0x80010119;
      //public const uint RPC_E_NO_GOOD_SECURITY_PACKAGES = 0x8001011A;
      //public const uint RPC_E_ACCESS_DENIED = 0x8001011B;
      //public const uint RPC_E_REMOTE_DISABLED = 0x8001011C;
      //public const uint RPC_E_INVALID_OBJREF = 0x8001011D;
      //public const uint RPC_E_NO_CONTEXT = 0x8001011E;
      //public const uint RPC_E_TIMEOUT = 0x8001011F;
      //public const uint RPC_E_NO_SYNC = 0x80010120;
      //public const uint RPC_E_FULLSIC_REQUIRED = 0x80010121;
      //public const uint RPC_E_INVALID_STD_NAME = 0x80010122;
      //public const uint CO_E_FAILEDTOIMPERSONATE = 0x80010123;
      //public const uint CO_E_FAILEDTOGETSECCTX = 0x80010124;
      //public const uint CO_E_FAILEDTOOPENTHREADTOKEN = 0x80010125;
      //public const uint CO_E_FAILEDTOGETTOKENINFO = 0x80010126;
      //public const uint CO_E_TRUSTEEDOESNTMATCHCLIENT = 0x80010127;
      //public const uint CO_E_FAILEDTOQUERYCLIENTBLANKET = 0x80010128;
      //public const uint CO_E_FAILEDTOSETDACL = 0x80010129;
      //public const uint CO_E_ACCESSCHECKFAILED = 0x8001012A;
      //public const uint CO_E_NETACCESSAPIFAILED = 0x8001012B;
      //public const uint CO_E_WRONGTRUSTEENAMESYNTAX = 0x8001012C;
      //public const uint CO_E_INVALIDSID = 0x8001012D;
      //public const uint CO_E_CONVERSIONFAILED = 0x8001012E;
      //public const uint CO_E_NOMATCHINGSIDFOUND = 0x8001012F;
      //public const uint CO_E_LOOKUPACCSIDFAILED = 0x80010130;
      //public const uint CO_E_NOMATCHINGNAMEFOUND = 0x80010131;
      //public const uint CO_E_LOOKUPACCNAMEFAILED = 0x80010132;
      //public const uint CO_E_SETSERLHNDLFAILED = 0x80010133;
      //public const uint CO_E_FAILEDTOGETWINDIR = 0x80010134;
      //public const uint CO_E_PATHTOOLONG = 0x80010135;
      //public const uint CO_E_FAILEDTOGENUUID = 0x80010136;
      //public const uint CO_E_FAILEDTOCREATEFILE = 0x80010137;
      //public const uint CO_E_FAILEDTOCLOSEHANDLE = 0x80010138;
      //public const uint CO_E_EXCEEDSYSACLLIMIT = 0x80010139;
      //public const uint CO_E_ACESINWRONGORDER = 0x8001013A;
      //public const uint CO_E_INCOMPATIBLESTREAMVERSION = 0x8001013B;
      //public const uint CO_E_FAILEDTOOPENPROCESSTOKEN = 0x8001013C;
      //public const uint CO_E_DECODEFAILED = 0x8001013D;
      //public const uint CO_E_ACNOTINITIALIZED = 0x8001013F;
      //public const uint CO_E_CANCEL_DISABLED = 0x80010140;
      //public const uint RPC_E_UNEXPECTED = 0x8001FFFF;
      //public const uint ERROR_AUDITING_DISABLED = 0xC0090001;
      //public const uint ERROR_ALL_SIDS_FILTERED = 0xC0090002;
      //public const uint NTE_BAD_UID = 0x80090001;
      //public const uint NTE_BAD_HASH = 0x80090002;
      //public const uint NTE_BAD_KEY = 0x80090003;
      //public const uint NTE_BAD_LEN = 0x80090004;
      //public const uint NTE_BAD_DATA = 0x80090005;
      //public const uint NTE_BAD_SIGNATURE = 0x80090006;
      //public const uint NTE_BAD_VER = 0x80090007;
      //public const uint NTE_BAD_ALGID = 0x80090008;
      //public const uint NTE_BAD_FLAGS = 0x80090009;
      //public const uint NTE_BAD_TYPE = 0x8009000A;
      //public const uint NTE_BAD_KEY_STATE = 0x8009000B;
      //public const uint NTE_BAD_HASH_STATE = 0x8009000C;
      //public const uint NTE_NO_KEY = 0x8009000D;
      //public const uint NTE_NO_MEMORY = 0x8009000E;
      //public const uint NTE_EXISTS = 0x8009000F;
      //public const uint NTE_PERM = 0x80090010;
      //public const uint NTE_NOT_FOUND = 0x80090011;
      //public const uint NTE_DOUBLE_ENCRYPT = 0x80090012;
      //public const uint NTE_BAD_PROVIDER = 0x80090013;
      //public const uint NTE_BAD_PROV_TYPE = 0x80090014;
      //public const uint NTE_BAD_PUBLIC_KEY = 0x80090015;
      //public const uint NTE_BAD_KEYSET = 0x80090016;
      //public const uint NTE_PROV_TYPE_NOT_DEF = 0x80090017;
      //public const uint NTE_PROV_TYPE_ENTRY_BAD = 0x80090018;
      //public const uint NTE_KEYSET_NOT_DEF = 0x80090019;
      //public const uint NTE_KEYSET_ENTRY_BAD = 0x8009001A;
      //public const uint NTE_PROV_TYPE_NO_MATCH = 0x8009001B;
      //public const uint NTE_SIGNATURE_FILE_BAD = 0x8009001C;
      //public const uint NTE_PROVIDER_DLL_FAIL = 0x8009001D;
      //public const uint NTE_PROV_DLL_NOT_FOUND = 0x8009001E;
      //public const uint NTE_BAD_KEYSET_PARAM = 0x8009001F;
      //public const uint NTE_FAIL = 0x80090020;
      //public const uint NTE_SYS_ERR = 0x80090021;
      //public const uint NTE_SILENT_CONTEXT = 0x80090022;
      //public const uint NTE_TOKEN_KEYSET_STORAGE_FULL = 0x80090023;
      //public const uint NTE_TEMPORARY_PROFILE = 0x80090024;
      //public const uint NTE_FIXEDPARAMETER = 0x80090025;
      //public const uint SEC_E_INSUFFICIENT_MEMORY = 0x80090300;
      //public const uint SEC_E_INVALID_HANDLE = 0x80090301;
      //public const uint SEC_E_UNSUPPORTED_FUNCTION = 0x80090302;
      //public const uint SEC_E_TARGET_UNKNOWN = 0x80090303;
      //public const uint SEC_E_INTERNAL_ERROR = 0x80090304;
      //public const uint SEC_E_SECPKG_NOT_FOUND = 0x80090305;
      //public const uint SEC_E_NOT_OWNER = 0x80090306;
      //public const uint SEC_E_CANNOT_INSTALL = 0x80090307;
      //public const uint SEC_E_INVALID_TOKEN = 0x80090308;
      //public const uint SEC_E_CANNOT_PACK = 0x80090309;
      //public const uint SEC_E_QOP_NOT_SUPPORTED = 0x8009030A;
      //public const uint SEC_E_NO_IMPERSONATION = 0x8009030B;
      //public const uint SEC_E_LOGON_DENIED = 0x8009030C;
      //public const uint SEC_E_UNKNOWN_CREDENTIALS = 0x8009030D;
      //public const uint SEC_E_NO_CREDENTIALS = 0x8009030E;
      //public const uint SEC_E_MESSAGE_ALTERED = 0x8009030F;
      //public const uint SEC_E_OUT_OF_SEQUENCE = 0x80090310;
      //public const uint SEC_E_NO_AUTHENTICATING_AUTHORITY = 0x80090311;
      //public const uint SEC_I_CONTINUE_NEEDED = 0x00090312;
      //public const uint SEC_I_COMPLETE_NEEDED = 0x00090313;
      //public const uint SEC_I_COMPLETE_AND_CONTINUE = 0x00090314;
      //public const uint SEC_I_LOCAL_LOGON = 0x00090315;
      //public const uint SEC_E_BAD_PKGID = 0x80090316;
      //public const uint SEC_E_CONTEXT_EXPIRED = 0x80090317;
      //public const uint SEC_I_CONTEXT_EXPIRED = 0x00090317;
      //public const uint SEC_E_INCOMPLETE_MESSAGE = 0x80090318;
      //public const uint SEC_E_INCOMPLETE_CREDENTIALS = 0x80090320;
      //public const uint SEC_E_BUFFER_TOO_SMALL = 0x80090321;
      //public const uint SEC_I_INCOMPLETE_CREDENTIALS = 0x00090320;
      //public const uint SEC_I_RENEGOTIATE = 0x00090321;
      //public const uint SEC_E_WRONG_PRINCIPAL = 0x80090322;
      //public const uint SEC_I_NO_LSA_CONTEXT = 0x00090323;
      //public const uint SEC_E_TIME_SKEW = 0x80090324;
      //public const uint SEC_E_UNTRUSTED_ROOT = 0x80090325;
      //public const uint SEC_E_ILLEGAL_MESSAGE = 0x80090326;
      //public const uint SEC_E_CERT_UNKNOWN = 0x80090327;
      //public const uint SEC_E_CERT_EXPIRED = 0x80090328;
      //public const uint SEC_E_ENCRYPT_FAILURE = 0x80090329;
      //public const uint SEC_E_DECRYPT_FAILURE = 0x80090330;
      //public const uint SEC_E_ALGORITHM_MISMATCH = 0x80090331;
      //public const uint SEC_E_SECURITY_QOS_FAILED = 0x80090332;
      //public const uint SEC_E_UNFINISHED_CONTEXT_DELETED = 0x80090333;
      //public const uint SEC_E_NO_TGT_REPLY = 0x80090334;
      //public const uint SEC_E_NO_IP_ADDRESSES = 0x80090335;
      //public const uint SEC_E_WRONG_CREDENTIAL_HANDLE = 0x80090336;
      //public const uint SEC_E_CRYPTO_SYSTEM_INVALID = 0x80090337;
      //public const uint SEC_E_MAX_REFERRALS_EXCEEDED = 0x80090338;
      //public const uint SEC_E_MUST_BE_KDC = 0x80090339;
      //public const uint SEC_E_STRONG_CRYPTO_NOT_SUPPORTED = 0x8009033A;
      //public const uint SEC_E_TOO_MANY_PRINCIPALS = 0x8009033B;
      //public const uint SEC_E_NO_PA_DATA = 0x8009033C;
      //public const uint SEC_E_PKINIT_NAME_MISMATCH = 0x8009033D;
      //public const uint SEC_E_SMARTCARD_LOGON_REQUIRED = 0x8009033E;
      //public const uint SEC_E_SHUTDOWN_IN_PROGRESS = 0x8009033F;
      //public const uint SEC_E_KDC_INVALID_REQUEST = 0x80090340;
      //public const uint SEC_E_KDC_UNABLE_TO_REFER = 0x80090341;
      //public const uint SEC_E_KDC_UNKNOWN_ETYPE = 0x80090342;
      //public const uint SEC_E_UNSUPPORTED_PREAUTH = 0x80090343;
      //public const uint SEC_E_DELEGATION_REQUIRED = 0x80090345;
      //public const uint SEC_E_BAD_BINDINGS = 0x80090346;
      //public const uint SEC_E_MULTIPLE_ACCOUNTS = 0x80090347;
      //public const uint SEC_E_NO_KERB_KEY = 0x80090348;
      //public const uint SEC_E_CERT_WRONG_USAGE = 0x80090349;
      //public const uint SEC_E_DOWNGRADE_DETECTED = 0x80090350;
      //public const uint SEC_E_SMARTCARD_CERT_REVOKED = 0x80090351;
      //public const uint SEC_E_ISSUING_CA_UNTRUSTED = 0x80090352;
      //public const uint SEC_E_REVOCATION_OFFLINE_C = 0x80090353;
      //public const uint SEC_E_PKINIT_CLIENT_FAILURE = 0x80090354;
      //public const uint SEC_E_SMARTCARD_CERT_EXPIRED = 0x80090355;
      //public const uint SEC_E_NO_S4U_PROT_SUPPORT = 0x80090356;
      //public const uint SEC_E_CROSSREALM_DELEGATION_FAILURE = 0x80090357;
      //public const uint SEC_E_REVOCATION_OFFLINE_KDC = 0x80090358;
      //public const uint SEC_E_ISSUING_CA_UNTRUSTED_KDC = 0x80090359;
      //public const uint SEC_E_KDC_CERT_EXPIRED = 0x8009035A;
      //public const uint SEC_E_KDC_CERT_REVOKED = 0x8009035B;
      //public const uint SEC_E_NO_SPM = SEC_E_INTERNAL_ERROR;
      //public const uint SEC_E_NOT_SUPPORTED = SEC_E_UNSUPPORTED_FUNCTION;
      //public const uint CRYPT_E_MSG_ERROR = 0x80091001;
      //public const uint CRYPT_E_UNKNOWN_ALGO = 0x80091002;
      //public const uint CRYPT_E_OID_FORMAT = 0x80091003;
      //public const uint CRYPT_E_INVALID_MSG_TYPE = 0x80091004;
      //public const uint CRYPT_E_UNEXPECTED_ENCODING = 0x80091005;
      //public const uint CRYPT_E_AUTH_ATTR_MISSING = 0x80091006;
      //public const uint CRYPT_E_HASH_VALUE = 0x80091007;
      //public const uint CRYPT_E_INVALID_INDEX = 0x80091008;
      //public const uint CRYPT_E_ALREADY_DECRYPTED = 0x80091009;
      //public const uint CRYPT_E_NOT_DECRYPTED = 0x8009100A;
      //public const uint CRYPT_E_RECIPIENT_NOT_FOUND = 0x8009100B;
      //public const uint CRYPT_E_CONTROL_TYPE = 0x8009100C;
      //public const uint CRYPT_E_ISSUER_SERIALNUMBER = 0x8009100D;
      //public const uint CRYPT_E_SIGNER_NOT_FOUND = 0x8009100E;
      //public const uint CRYPT_E_ATTRIBUTES_MISSING = 0x8009100F;
      //public const uint CRYPT_E_STREAM_MSG_NOT_READY = 0x80091010;
      //public const uint CRYPT_E_STREAM_INSUFFICIENT_DATA = 0x80091011;
      //public const uint CRYPT_I_NEW_PROTECTION_REQUIRED = 0x00091012;
      //public const uint CRYPT_E_BAD_LEN = 0x80092001;
      //public const uint CRYPT_E_BAD_ENCODE = 0x80092002;
      //public const uint CRYPT_E_FILE_ERROR = 0x80092003;
      //public const uint CRYPT_E_NOT_FOUND = 0x80092004;
      //public const uint CRYPT_E_EXISTS = 0x80092005;
      //public const uint CRYPT_E_NO_PROVIDER = 0x80092006;
      //public const uint CRYPT_E_SELF_SIGNED = 0x80092007;
      //public const uint CRYPT_E_DELETED_PREV = 0x80092008;
      //public const uint CRYPT_E_NO_MATCH = 0x80092009;
      //public const uint CRYPT_E_UNEXPECTED_MSG_TYPE = 0x8009200A;
      //public const uint CRYPT_E_NO_KEY_PROPERTY = 0x8009200B;
      //public const uint CRYPT_E_NO_DECRYPT_CERT = 0x8009200C;
      //public const uint CRYPT_E_BAD_MSG = 0x8009200D;
      //public const uint CRYPT_E_NO_SIGNER = 0x8009200E;
      //public const uint CRYPT_E_PENDING_CLOSE = 0x8009200F;
      //public const uint CRYPT_E_REVOKED = 0x80092010;
      //public const uint CRYPT_E_NO_REVOCATION_DLL = 0x80092011;
      //public const uint CRYPT_E_NO_REVOCATION_CHECK = 0x80092012;
      //public const uint CRYPT_E_REVOCATION_OFFLINE = 0x80092013;
      //public const uint CRYPT_E_NOT_IN_REVOCATION_DATABASE = 0x80092014;
      //public const uint CRYPT_E_INVALID_NUMERIC_STRING = 0x80092020;
      //public const uint CRYPT_E_INVALID_PRINTABLE_STRING = 0x80092021;
      //public const uint CRYPT_E_INVALID_IA5_STRING = 0x80092022;
      //public const uint CRYPT_E_INVALID_X500_STRING = 0x80092023;
      //public const uint CRYPT_E_NOT_CHAR_STRING = 0x80092024;
      //public const uint CRYPT_E_FILERESIZED = 0x80092025;
      //public const uint CRYPT_E_SECURITY_SETTINGS = 0x80092026;
      //public const uint CRYPT_E_NO_VERIFY_USAGE_DLL = 0x80092027;
      //public const uint CRYPT_E_NO_VERIFY_USAGE_CHECK = 0x80092028;
      //public const uint CRYPT_E_VERIFY_USAGE_OFFLINE = 0x80092029;
      //public const uint CRYPT_E_NOT_IN_CTL = 0x8009202A;
      //public const uint CRYPT_E_NO_TRUSTED_SIGNER = 0x8009202B;
      //public const uint CRYPT_E_MISSING_PUBKEY_PARA = 0x8009202C;
      //public const uint CRYPT_E_OSS_ERROR = 0x80093000;
      //public const uint OSS_MORE_BUF = 0x80093001;
      //public const uint OSS_NEGATIVE_longEGER = 0x80093002;
      //public const uint OSS_PDU_RANGE = 0x80093003;
      //public const uint OSS_MORE_INPUT = 0x80093004;
      //public const uint OSS_DATA_ERROR = 0x80093005;
      //public const uint OSS_BAD_ARG = 0x80093006;
      //public const uint OSS_BAD_VERSION = 0x80093007;
      //public const uint OSS_OUT_MEMORY = 0x80093008;
      //public const uint OSS_PDU_MISMATCH = 0x80093009;
      //public const uint OSS_LIMITED = 0x8009300A;
      //public const uint OSS_BAD_PTR = 0x8009300B;
      //public const uint OSS_BAD_TIME = 0x8009300C;
      //public const uint OSS_INDEFINITE_NOT_SUPPORTED = 0x8009300D;
      //public const uint OSS_MEM_ERROR = 0x8009300E;
      //public const uint OSS_BAD_TABLE = 0x8009300F;
      //public const uint OSS_TOO_LONG = 0x80093010;
      //public const uint OSS_CONSTRAINT_VIOLATED = 0x80093011;
      //public const uint OSS_FATAL_ERROR = 0x80093012;
      //public const uint OSS_ACCESS_SERIALIZATION_ERROR = 0x80093013;
      //public const uint OSS_NULL_TBL = 0x80093014;
      //public const uint OSS_NULL_FCN = 0x80093015;
      //public const uint OSS_BAD_ENCRULES = 0x80093016;
      //public const uint OSS_UNAVAIL_ENCRULES = 0x80093017;
      //public const uint OSS_CANT_OPEN_TRACE_WINDOW = 0x80093018;
      //public const uint OSS_UNIMPLEMENTED = 0x80093019;
      //public const uint OSS_OID_DLL_NOT_LINKED = 0x8009301A;
      //public const uint OSS_CANT_OPEN_TRACE_FILE = 0x8009301B;
      //public const uint OSS_TRACE_FILE_ALREADY_OPEN = 0x8009301C;
      //public const uint OSS_TABLE_MISMATCH = 0x8009301D;
      //public const uint OSS_TYPE_NOT_SUPPORTED = 0x8009301E;
      //public const uint OSS_REAL_DLL_NOT_LINKED = 0x8009301F;
      //public const uint OSS_REAL_CODE_NOT_LINKED = 0x80093020;
      //public const uint OSS_OUT_OF_RANGE = 0x80093021;
      //public const uint OSS_COPIER_DLL_NOT_LINKED = 0x80093022;
      //public const uint OSS_CONSTRAINT_DLL_NOT_LINKED = 0x80093023;
      //public const uint OSS_COMPARATOR_DLL_NOT_LINKED = 0x80093024;
      //public const uint OSS_COMPARATOR_CODE_NOT_LINKED = 0x80093025;
      //public const uint OSS_MEM_MGR_DLL_NOT_LINKED = 0x80093026;
      //public const uint OSS_PDV_DLL_NOT_LINKED = 0x80093027;
      //public const uint OSS_PDV_CODE_NOT_LINKED = 0x80093028;
      //public const uint OSS_API_DLL_NOT_LINKED = 0x80093029;
      //public const uint OSS_BERDER_DLL_NOT_LINKED = 0x8009302A;
      //public const uint OSS_PER_DLL_NOT_LINKED = 0x8009302B;
      //public const uint OSS_OPEN_TYPE_ERROR = 0x8009302C;
      //public const uint OSS_MUTEX_NOT_CREATED = 0x8009302D;
      //public const uint OSS_CANT_CLOSE_TRACE_FILE = 0x8009302E;
      //public const uint CRYPT_E_ASN1_ERROR = 0x80093100;
      //public const uint CRYPT_E_ASN1_INTERNAL = 0x80093101;
      //public const uint CRYPT_E_ASN1_EOD = 0x80093102;
      //public const uint CRYPT_E_ASN1_CORRUPT = 0x80093103;
      //public const uint CRYPT_E_ASN1_LARGE = 0x80093104;
      //public const uint CRYPT_E_ASN1_CONSTRAINT = 0x80093105;
      //public const uint CRYPT_E_ASN1_MEMORY = 0x80093106;
      //public const uint CRYPT_E_ASN1_OVERFLOW = 0x80093107;
      //public const uint CRYPT_E_ASN1_BADPDU = 0x80093108;
      //public const uint CRYPT_E_ASN1_BADARGS = 0x80093109;
      //public const uint CRYPT_E_ASN1_BADREAL = 0x8009310A;
      //public const uint CRYPT_E_ASN1_BADTAG = 0x8009310B;
      //public const uint CRYPT_E_ASN1_CHOICE = 0x8009310C;
      //public const uint CRYPT_E_ASN1_RULE = 0x8009310D;
      //public const uint CRYPT_E_ASN1_UTF8 = 0x8009310E;
      //public const uint CRYPT_E_ASN1_PDU_TYPE = 0x80093133;
      //public const uint CRYPT_E_ASN1_NYI = 0x80093134;
      //public const uint CRYPT_E_ASN1_EXTENDED = 0x80093201;
      //public const uint CRYPT_E_ASN1_NOEOD = 0x80093202;
      //public const uint CERTSRV_E_BAD_REQUESTSUBJECT = 0x80094001;
      //public const uint CERTSRV_E_NO_REQUEST = 0x80094002;
      //public const uint CERTSRV_E_BAD_REQUESTSTATUS = 0x80094003;
      //public const uint CERTSRV_E_PROPERTY_EMPTY = 0x80094004;
      //public const uint CERTSRV_E_INVALID_CA_CERTIFICATE = 0x80094005;
      //public const uint CERTSRV_E_SERVER_SUSPENDED = 0x80094006;
      //public const uint CERTSRV_E_ENCODING_LENGTH = 0x80094007;
      //public const uint CERTSRV_E_ROLECONFLICT = 0x80094008;
      //public const uint CERTSRV_E_RESTRICTEDOFFICER = 0x80094009;
      //public const uint CERTSRV_E_KEY_ARCHIVAL_NOT_CONFIGURED = 0x8009400A;
      //public const uint CERTSRV_E_NO_VALID_KRA = 0x8009400B;
      //public const uint CERTSRV_E_BAD_REQUEST_KEY_ARCHIVAL = 0x8009400C;
      //public const uint CERTSRV_E_NO_CAADMIN_DEFINED = 0x8009400D;
      //public const uint CERTSRV_E_BAD_RENEWAL_CERT_ATTRIBUTE = 0x8009400E;
      //public const uint CERTSRV_E_NO_DB_SESSIONS = 0x8009400F;
      //public const uint CERTSRV_E_ALIGNMENT_FAULT = 0x80094010;
      //public const uint CERTSRV_E_ENROLL_DENIED = 0x80094011;
      //public const uint CERTSRV_E_TEMPLATE_DENIED = 0x80094012;
      //public const uint CERTSRV_E_DOWNLEVEL_DC_SSL_OR_UPGRADE = 0x80094013;
      //public const uint CERTSRV_E_UNSUPPORTED_CERT_TYPE = 0x80094800;
      //public const uint CERTSRV_E_NO_CERT_TYPE = 0x80094801;
      //public const uint CERTSRV_E_TEMPLATE_CONFLICT = 0x80094802;
      //public const uint CERTSRV_E_SUBJECT_ALT_NAME_REQUIRED = 0x80094803;
      //public const uint CERTSRV_E_ARCHIVED_KEY_REQUIRED = 0x80094804;
      //public const uint CERTSRV_E_SMIME_REQUIRED = 0x80094805;
      //public const uint CERTSRV_E_BAD_RENEWAL_SUBJECT = 0x80094806;
      //public const uint CERTSRV_E_BAD_TEMPLATE_VERSION = 0x80094807;
      //public const uint CERTSRV_E_TEMPLATE_POLICY_REQUIRED = 0x80094808;
      //public const uint CERTSRV_E_SIGNATURE_POLICY_REQUIRED = 0x80094809;
      //public const uint CERTSRV_E_SIGNATURE_COUNT = 0x8009480A;
      //public const uint CERTSRV_E_SIGNATURE_REJECTED = 0x8009480B;
      //public const uint CERTSRV_E_ISSUANCE_POLICY_REQUIRED = 0x8009480C;
      //public const uint CERTSRV_E_SUBJECT_UPN_REQUIRED = 0x8009480D;
      //public const uint CERTSRV_E_SUBJECT_DIRECTORY_GUID_REQUIRED = 0x8009480E;
      //public const uint CERTSRV_E_SUBJECT_DNS_REQUIRED = 0x8009480F;
      //public const uint CERTSRV_E_ARCHIVED_KEY_UNEXPECTED = 0x80094810;
      //public const uint CERTSRV_E_KEY_LENGTH = 0x80094811;
      //public const uint CERTSRV_E_SUBJECT_EMAIL_REQUIRED = 0x80094812;
      //public const uint CERTSRV_E_UNKNOWN_CERT_TYPE = 0x80094813;
      //public const uint CERTSRV_E_CERT_TYPE_OVERLAP = 0x80094814;
      //public const uint XENROLL_E_KEY_NOT_EXPORTABLE = 0x80095000;
      //public const uint XENROLL_E_CANNOT_ADD_ROOT_CERT = 0x80095001;
      //public const uint XENROLL_E_RESPONSE_KA_HASH_NOT_FOUND = 0x80095002;
      //public const uint XENROLL_E_RESPONSE_UNEXPECTED_KA_HASH = 0x80095003;
      //public const uint XENROLL_E_RESPONSE_KA_HASH_MISMATCH = 0x80095004;
      //public const uint XENROLL_E_KEYSPEC_SMIME_MISMATCH = 0x80095005;
      //public const uint TRUST_E_SYSTEM_ERROR = 0x80096001;
      //public const uint TRUST_E_NO_SIGNER_CERT = 0x80096002;
      //public const uint TRUST_E_COUNTER_SIGNER = 0x80096003;
      //public const uint TRUST_E_CERT_SIGNATURE = 0x80096004;
      //public const uint TRUST_E_TIME_STAMP = 0x80096005;
      //public const uint TRUST_E_BAD_DIGEST = 0x80096010;
      //public const uint TRUST_E_BASIC_CONSTRAINTS = 0x80096019;
      //public const uint TRUST_E_FINANCIAL_CRITERIA = 0x8009601E;
      //public const uint MSSIPOTF_E_OUTOFMEMRANGE = 0x80097001;
      //public const uint MSSIPOTF_E_CANTGETOBJECT = 0x80097002;
      //public const uint MSSIPOTF_E_NOHEADTABLE = 0x80097003;
      //public const uint MSSIPOTF_E_BAD_MAGICNUMBER = 0x80097004;
      //public const uint MSSIPOTF_E_BAD_OFFSET_TABLE = 0x80097005;
      //public const uint MSSIPOTF_E_TABLE_TAGORDER = 0x80097006;
      //public const uint MSSIPOTF_E_TABLE_LONGWORD = 0x80097007;
      //public const uint MSSIPOTF_E_BAD_FIRST_TABLE_PLACEMENT = 0x80097008;
      //public const uint MSSIPOTF_E_TABLES_OVERLAP = 0x80097009;
      //public const uint MSSIPOTF_E_TABLE_PADBYTES = 0x8009700A;
      //public const uint MSSIPOTF_E_FILETOOSMALL = 0x8009700B;
      //public const uint MSSIPOTF_E_TABLE_CHECKSUM = 0x8009700C;
      //public const uint MSSIPOTF_E_FILE_CHECKSUM = 0x8009700D;
      //public const uint MSSIPOTF_E_FAILED_POLICY = 0x80097010;
      //public const uint MSSIPOTF_E_FAILED_HINTS_CHECK = 0x80097011;
      //public const uint MSSIPOTF_E_NOT_OPENTYPE = 0x80097012;
      //public const uint MSSIPOTF_E_FILE = 0x80097013;
      //public const uint MSSIPOTF_E_CRYPT = 0x80097014;
      //public const uint MSSIPOTF_E_BADVERSION = 0x80097015;
      //public const uint MSSIPOTF_E_DSIG_STRUCTURE = 0x80097016;
      //public const uint MSSIPOTF_E_PCONST_CHECK = 0x80097017;
      //public const uint MSSIPOTF_E_STRUCTURE = 0x80097018;
      //public const uint NTE_OP_OK = 0;
      //public const uint TRUST_E_PROVIDER_UNKNOWN = 0x800B0001;
      //public const uint TRUST_E_ACTION_UNKNOWN = 0x800B0002;
      //public const uint TRUST_E_SUBJECT_FORM_UNKNOWN = 0x800B0003;
      //public const uint TRUST_E_SUBJECT_NOT_TRUSTED = 0x800B0004;
      //public const uint DIGSIG_E_ENCODE = 0x800B0005;
      //public const uint DIGSIG_E_DECODE = 0x800B0006;
      //public const uint DIGSIG_E_EXTENSIBILITY = 0x800B0007;
      //public const uint DIGSIG_E_CRYPTO = 0x800B0008;
      //public const uint PERSIST_E_SIZEDEFINITE = 0x800B0009;
      //public const uint PERSIST_E_SIZEINDEFINITE = 0x800B000A;
      //public const uint PERSIST_E_NOTSELFSIZING = 0x800B000B;
      //public const uint TRUST_E_NOSIGNATURE = 0x800B0100;
      //public const uint CERT_E_EXPIRED = 0x800B0101;
      //public const uint CERT_E_VALIDITYPERIODNESTING = 0x800B0102;
      //public const uint CERT_E_ROLE = 0x800B0103;
      //public const uint CERT_E_PATHLENCONST = 0x800B0104;
      //public const uint CERT_E_CRITICAL = 0x800B0105;
      //public const uint CERT_E_PURPOSE = 0x800B0106;
      //public const uint CERT_E_ISSUERCHAINING = 0x800B0107;
      //public const uint CERT_E_MALFORMED = 0x800B0108;
      //public const uint CERT_E_UNTRUSTEDROOT = 0x800B0109;
      //public const uint CERT_E_CHAINING = 0x800B010A;
      //public const uint TRUST_E_FAIL = 0x800B010B;
      //public const uint CERT_E_REVOKED = 0x800B010C;
      //public const uint CERT_E_UNTRUSTEDTESTROOT = 0x800B010D;
      //public const uint CERT_E_REVOCATION_FAILURE = 0x800B010E;
      //public const uint CERT_E_CN_NO_MATCH = 0x800B010F;
      //public const uint CERT_E_WRONG_USAGE = 0x800B0110;
      //public const uint TRUST_E_EXPLICIT_DISTRUST = 0x800B0111;
      //public const uint CERT_E_UNTRUSTEDCA = 0x800B0112;
      //public const uint CERT_E_INVALID_POLICY = 0x800B0113;
      //public const uint CERT_E_INVALID_NAME = 0x800B0114;
      //public const uint SPAPI_E_EXPECTED_SECTION_NAME = 0x800F0000;
      //public const uint SPAPI_E_BAD_SECTION_NAME_LINE = 0x800F0001;
      //public const uint SPAPI_E_SECTION_NAME_TOO_LONG = 0x800F0002;
      //public const uint SPAPI_E_GENERAL_SYNTAX = 0x800F0003;
      //public const uint SPAPI_E_WRONG_INF_STYLE = 0x800F0100;
      //public const uint SPAPI_E_SECTION_NOT_FOUND = 0x800F0101;
      //public const uint SPAPI_E_LINE_NOT_FOUND = 0x800F0102;
      //public const uint SPAPI_E_NO_BACKUP = 0x800F0103;
      //public const uint SPAPI_E_NO_ASSOCIATED_CLASS = 0x800F0200;
      //public const uint SPAPI_E_CLASS_MISMATCH = 0x800F0201;
      //public const uint SPAPI_E_DUPLICATE_FOUND = 0x800F0202;
      //public const uint SPAPI_E_NO_DRIVER_SELECTED = 0x800F0203;
      //public const uint SPAPI_E_KEY_DOES_NOT_EXIST = 0x800F0204;
      //public const uint SPAPI_E_INVALID_DEVINST_NAME = 0x800F0205;
      //public const uint SPAPI_E_INVALID_CLASS = 0x800F0206;
      //public const uint SPAPI_E_DEVINST_ALREADY_EXISTS = 0x800F0207;
      //public const uint SPAPI_E_DEVINFO_NOT_REGISTERED = 0x800F0208;
      //public const uint SPAPI_E_INVALID_REG_PROPERTY = 0x800F0209;
      //public const uint SPAPI_E_NO_INF = 0x800F020A;
      //public const uint SPAPI_E_NO_SUCH_DEVINST = 0x800F020B;
      //public const uint SPAPI_E_CANT_LOAD_CLASS_ICON = 0x800F020C;
      //public const uint SPAPI_E_INVALID_CLASS_INSTALLER = 0x800F020D;
      //public const uint SPAPI_E_DI_DO_DEFAULT = 0x800F020E;
      //public const uint SPAPI_E_DI_NOFILECOPY = 0x800F020F;
      //public const uint SPAPI_E_INVALID_HWPROFILE = 0x800F0210;
      //public const uint SPAPI_E_NO_DEVICE_SELECTED = 0x800F0211;
      //public const uint SPAPI_E_DEVINFO_LIST_LOCKED = 0x800F0212;
      //public const uint SPAPI_E_DEVINFO_DATA_LOCKED = 0x800F0213;
      //public const uint SPAPI_E_DI_BAD_PATH = 0x800F0214;
      //public const uint SPAPI_E_NO_CLASSINSTALL_PARAMS = 0x800F0215;
      //public const uint SPAPI_E_FILEQUEUE_LOCKED = 0x800F0216;
      //public const uint SPAPI_E_BAD_SERVICE_INSTALLSECT = 0x800F0217;
      //public const uint SPAPI_E_NO_CLASS_DRIVER_LIST = 0x800F0218;
      //public const uint SPAPI_E_NO_ASSOCIATED_SERVICE = 0x800F0219;
      //public const uint SPAPI_E_NO_DEFAULT_DEVICE_INTERFACE = 0x800F021A;
      //public const uint SPAPI_E_DEVICE_INTERFACE_ACTIVE = 0x800F021B;
      //public const uint SPAPI_E_DEVICE_INTERFACE_REMOVED = 0x800F021C;
      //public const uint SPAPI_E_BAD_INTERFACE_INSTALLSECT = 0x800F021D;
      //public const uint SPAPI_E_NO_SUCH_INTERFACE_CLASS = 0x800F021E;
      //public const uint SPAPI_E_INVALID_REFERENCE_STRING = 0x800F021F;
      //public const uint SPAPI_E_INVALID_MACHINENAME = 0x800F0220;
      //public const uint SPAPI_E_REMOTE_COMM_FAILURE = 0x800F0221;
      //public const uint SPAPI_E_MACHINE_UNAVAILABLE = 0x800F0222;
      //public const uint SPAPI_E_NO_CONFIGMGR_SERVICES = 0x800F0223;
      //public const uint SPAPI_E_INVALID_PROPPAGE_PROVIDER = 0x800F0224;
      //public const uint SPAPI_E_NO_SUCH_DEVICE_INTERFACE = 0x800F0225;
      //public const uint SPAPI_E_DI_POSTPROCESSING_REQUIRED = 0x800F0226;
      //public const uint SPAPI_E_INVALID_COINSTALLER = 0x800F0227;
      //public const uint SPAPI_E_NO_COMPAT_DRIVERS = 0x800F0228;
      //public const uint SPAPI_E_NO_DEVICE_ICON = 0x800F0229;
      //public const uint SPAPI_E_INVALID_INF_LOGCONFIG = 0x800F022A;
      //public const uint SPAPI_E_DI_DONT_INSTALL = 0x800F022B;
      //public const uint SPAPI_E_INVALID_FILTER_DRIVER = 0x800F022C;
      //public const uint SPAPI_E_NON_WINDOWS_NT_DRIVER = 0x800F022D;
      //public const uint SPAPI_E_NON_WINDOWS_DRIVER = 0x800F022E;
      //public const uint SPAPI_E_NO_CATALOG_FOR_OEM_INF = 0x800F022F;
      //public const uint SPAPI_E_DEVINSTALL_QUEUE_NONNATIVE = 0x800F0230;
      //public const uint SPAPI_E_NOT_DISABLEABLE = 0x800F0231;
      //public const uint SPAPI_E_CANT_REMOVE_DEVINST = 0x800F0232;
      //public const uint SPAPI_E_INVALID_TARGET = 0x800F0233;
      //public const uint SPAPI_E_DRIVER_NONNATIVE = 0x800F0234;
      //public const uint SPAPI_E_IN_WOW64 = 0x800F0235;
      //public const uint SPAPI_E_SET_SYSTEM_RESTORE_POINT = 0x800F0236;
      //public const uint SPAPI_E_INCORRECTLY_COPIED_INF = 0x800F0237;
      //public const uint SPAPI_E_SCE_DISABLED = 0x800F0238;
      //public const uint SPAPI_E_UNKNOWN_EXCEPTION = 0x800F0239;
      //public const uint SPAPI_E_PNP_REGISTRY_ERROR = 0x800F023A;
      //public const uint SPAPI_E_REMOTE_REQUEST_UNSUPPORTED = 0x800F023B;
      //public const uint SPAPI_E_NOT_AN_INSTALLED_OEM_INF = 0x800F023C;
      //public const uint SPAPI_E_INF_IN_USE_BY_DEVICES = 0x800F023D;
      //public const uint SPAPI_E_DI_FUNCTION_OBSOLETE = 0x800F023E;
      //public const uint SPAPI_E_NO_AUTHENTICODE_CATALOG = 0x800F023F;
      //public const uint SPAPI_E_AUTHENTICODE_DISALLOWED = 0x800F0240;
      //public const uint SPAPI_E_AUTHENTICODE_TRUSTED_PUBLISHER = 0x800F0241;
      //public const uint SPAPI_E_AUTHENTICODE_TRUST_NOT_ESTABLISHED = 0x800F0242;
      //public const uint SPAPI_E_AUTHENTICODE_PUBLISHER_NOT_TRUSTED = 0x800F0243;
      //public const uint SPAPI_E_SIGNATURE_OSATTRIBUTE_MISMATCH = 0x800F0244;
      //public const uint SPAPI_E_ONLY_VALIDATE_VIA_AUTHENTICODE = 0x800F0245;
      //public const uint SPAPI_E_UNRECOVERABLE_STACK_OVERFLOW = 0x800F0300;
      //public const uint SPAPI_E_ERROR_NOT_INSTALLED = 0x800F1000;
      //public const uint SCARD_S_SUCCESS = NO_ERROR;
      //public const uint SCARD_F_INTERNAL_ERROR = 0x80100001;
      //public const uint SCARD_E_CANCELLED = 0x80100002;
      //public const uint SCARD_E_INVALID_HANDLE = 0x80100003;
      //public const uint SCARD_E_INVALID_PARAMETER = 0x80100004;
      //public const uint SCARD_E_INVALID_TARGET = 0x80100005;
      //public const uint SCARD_E_NO_MEMORY = 0x80100006;
      //public const uint SCARD_F_WAITED_TOO_LONG = 0x80100007;
      //public const uint SCARD_E_INSUFFICIENT_BUFFER = 0x80100008;
      //public const uint SCARD_E_UNKNOWN_READER = 0x80100009;
      //public const uint SCARD_E_TIMEOUT = 0x8010000A;
      //public const uint SCARD_E_SHARING_VIOLATION = 0x8010000B;
      //public const uint SCARD_E_NO_SMARTCARD = 0x8010000C;
      //public const uint SCARD_E_UNKNOWN_CARD = 0x8010000D;
      //public const uint SCARD_E_CANT_DISPOSE = 0x8010000E;
      //public const uint SCARD_E_PROTO_MISMATCH = 0x8010000F;
      //public const uint SCARD_E_NOT_READY = 0x80100010;
      //public const uint SCARD_E_INVALID_VALUE = 0x80100011;
      //public const uint SCARD_E_SYSTEM_CANCELLED = 0x80100012;
      //public const uint SCARD_F_COMM_ERROR = 0x80100013;
      //public const uint SCARD_F_UNKNOWN_ERROR = 0x80100014;
      //public const uint SCARD_E_INVALID_ATR = 0x80100015;
      //public const uint SCARD_E_NOT_TRANSACTED = 0x80100016;
      //public const uint SCARD_E_READER_UNAVAILABLE = 0x80100017;
      //public const uint SCARD_P_SHUTDOWN = 0x80100018;
      //public const uint SCARD_E_PCI_TOO_SMALL = 0x80100019;
      //public const uint SCARD_E_READER_UNSUPPORTED = 0x8010001A;
      //public const uint SCARD_E_DUPLICATE_READER = 0x8010001B;
      //public const uint SCARD_E_CARD_UNSUPPORTED = 0x8010001C;
      //public const uint SCARD_E_NO_SERVICE = 0x8010001D;
      //public const uint SCARD_E_SERVICE_STOPPED = 0x8010001E;
      //public const uint SCARD_E_UNEXPECTED = 0x8010001F;
      //public const uint SCARD_E_ICC_INSTALLATION = 0x80100020;
      //public const uint SCARD_E_ICC_CREATEORDER = 0x80100021;
      //public const uint SCARD_E_UNSUPPORTED_FEATURE = 0x80100022;
      //public const uint SCARD_E_DIR_NOT_FOUND = 0x80100023;
      //public const uint SCARD_E_FILE_NOT_FOUND = 0x80100024;
      //public const uint SCARD_E_NO_DIR = 0x80100025;
      //public const uint SCARD_E_NO_FILE = 0x80100026;
      //public const uint SCARD_E_NO_ACCESS = 0x80100027;
      //public const uint SCARD_E_WRITE_TOO_MANY = 0x80100028;
      //public const uint SCARD_E_BAD_SEEK = 0x80100029;
      //public const uint SCARD_E_INVALID_CHV = 0x8010002A;
      //public const uint SCARD_E_UNKNOWN_RES_MNG = 0x8010002B;
      //public const uint SCARD_E_NO_SUCH_CERTIFICATE = 0x8010002C;
      //public const uint SCARD_E_CERTIFICATE_UNAVAILABLE = 0x8010002D;
      //public const uint SCARD_E_NO_READERS_AVAILABLE = 0x8010002E;
      //public const uint SCARD_E_COMM_DATA_LOST = 0x8010002F;
      //public const uint SCARD_E_NO_KEY_CONTAINER = 0x80100030;
      //public const uint SCARD_E_SERVER_TOO_BUSY = 0x80100031;
      //public const uint SCARD_W_UNSUPPORTED_CARD = 0x80100065;
      //public const uint SCARD_W_UNRESPONSIVE_CARD = 0x80100066;
      //public const uint SCARD_W_UNPOWERED_CARD = 0x80100067;
      //public const uint SCARD_W_RESET_CARD = 0x80100068;
      //public const uint SCARD_W_REMOVED_CARD = 0x80100069;
      //public const uint SCARD_W_SECURITY_VIOLATION = 0x8010006A;
      //public const uint SCARD_W_WRONG_CHV = 0x8010006B;
      //public const uint SCARD_W_CHV_BLOCKED = 0x8010006C;
      //public const uint SCARD_W_EOF = 0x8010006D;
      //public const uint SCARD_W_CANCELLED_BY_USER = 0x8010006E;
      //public const uint SCARD_W_CARD_NOT_AUTHENTICATED = 0x8010006F;
      //public const uint COMADMIN_E_OBJECTERRORS = 0x80110401;
      //public const uint COMADMIN_E_OBJECTINVALID = 0x80110402;
      //public const uint COMADMIN_E_KEYMISSING = 0x80110403;
      //public const uint COMADMIN_E_ALREADYINSTALLED = 0x80110404;
      //public const uint COMADMIN_E_APP_FILE_WRITEFAIL = 0x80110407;
      //public const uint COMADMIN_E_APP_FILE_READFAIL = 0x80110408;
      //public const uint COMADMIN_E_APP_FILE_VERSION = 0x80110409;
      //public const uint COMADMIN_E_BADPATH = 0x8011040A;
      //public const uint COMADMIN_E_APPLICATIONEXISTS = 0x8011040B;
      //public const uint COMADMIN_E_ROLEEXISTS = 0x8011040C;
      //public const uint COMADMIN_E_CANTCOPYFILE = 0x8011040D;
      //public const uint COMADMIN_E_NOUSER = 0x8011040F;
      //public const uint COMADMIN_E_INVALIDUSERIDS = 0x80110410;
      //public const uint COMADMIN_E_NOREGISTRYCLSID = 0x80110411;
      //public const uint COMADMIN_E_BADREGISTRYPROGID = 0x80110412;
      //public const uint COMADMIN_E_AUTHENTICATIONLEVEL = 0x80110413;
      //public const uint COMADMIN_E_USERPASSWDNOTVALID = 0x80110414;
      //public const uint COMADMIN_E_CLSIDORIIDMISMATCH = 0x80110418;
      //public const uint COMADMIN_E_REMOTEINTERFACE = 0x80110419;
      //public const uint COMADMIN_E_DLLREGISTERSERVER = 0x8011041A;
      //public const uint COMADMIN_E_NOSERVERSHARE = 0x8011041B;
      //public const uint COMADMIN_E_DLLLOADFAILED = 0x8011041D;
      //public const uint COMADMIN_E_BADREGISTRYLIBID = 0x8011041E;
      //public const uint COMADMIN_E_APPDIRNOTFOUND = 0x8011041F;
      //public const uint COMADMIN_E_REGISTRARFAILED = 0x80110423;
      //public const uint COMADMIN_E_COMPFILE_DOESNOTEXIST = 0x80110424;
      //public const uint COMADMIN_E_COMPFILE_LOADDLLFAIL = 0x80110425;
      //public const uint COMADMIN_E_COMPFILE_GETCLASSOBJ = 0x80110426;
      //public const uint COMADMIN_E_COMPFILE_CLASSNOTAVAIL = 0x80110427;
      //public const uint COMADMIN_E_COMPFILE_BADTLB = 0x80110428;
      //public const uint COMADMIN_E_COMPFILE_NOTINSTALLABLE = 0x80110429;
      //public const uint COMADMIN_E_NOTCHANGEABLE = 0x8011042A;
      //public const uint COMADMIN_E_NOTDELETEABLE = 0x8011042B;
      //public const uint COMADMIN_E_SESSION = 0x8011042C;
      //public const uint COMADMIN_E_COMP_MOVE_LOCKED = 0x8011042D;
      //public const uint COMADMIN_E_COMP_MOVE_BAD_DEST = 0x8011042E;
      //public const uint COMADMIN_E_REGISTERTLB = 0x80110430;
      //public const uint COMADMIN_E_SYSTEMAPP = 0x80110433;
      //public const uint COMADMIN_E_COMPFILE_NOREGISTRAR = 0x80110434;
      //public const uint COMADMIN_E_COREQCOMPINSTALLED = 0x80110435;
      //public const uint COMADMIN_E_SERVICENOTINSTALLED = 0x80110436;
      //public const uint COMADMIN_E_PROPERTYSAVEFAILED = 0x80110437;
      //public const uint COMADMIN_E_OBJECTEXISTS = 0x80110438;
      //public const uint COMADMIN_E_COMPONENTEXISTS = 0x80110439;
      //public const uint COMADMIN_E_REGFILE_CORRUPT = 0x8011043B;
      //public const uint COMADMIN_E_PROPERTY_OVERFLOW = 0x8011043C;
      //public const uint COMADMIN_E_NOTINREGISTRY = 0x8011043E;
      //public const uint COMADMIN_E_OBJECTNOTPOOLABLE = 0x8011043F;
      //public const uint COMADMIN_E_APPLID_MATCHES_CLSID = 0x80110446;
      //public const uint COMADMIN_E_ROLE_DOES_NOT_EXIST = 0x80110447;
      //public const uint COMADMIN_E_START_APP_NEEDS_COMPONENTS = 0x80110448;
      //public const uint COMADMIN_E_REQUIRES_DIFFERENT_PLATFORM = 0x80110449;
      //public const uint COMADMIN_E_CAN_NOT_EXPORT_APP_PROXY = 0x8011044A;
      //public const uint COMADMIN_E_CAN_NOT_START_APP = 0x8011044B;
      //public const uint COMADMIN_E_CAN_NOT_EXPORT_SYS_APP = 0x8011044C;
      //public const uint COMADMIN_E_CANT_SUBSCRIBE_TO_COMPONENT = 0x8011044D;
      //public const uint COMADMIN_E_EVENTCLASS_CANT_BE_SUBSCRIBER = 0x8011044E;
      //public const uint COMADMIN_E_LIB_APP_PROXY_INCOMPATIBLE = 0x8011044F;
      //public const uint COMADMIN_E_BASE_PARTITION_ONLY = 0x80110450;
      //public const uint COMADMIN_E_START_APP_DISABLED = 0x80110451;
      //public const uint COMADMIN_E_CAT_DUPLICATE_PARTITION_NAME = 0x80110457;
      //public const uint COMADMIN_E_CAT_INVALID_PARTITION_NAME = 0x80110458;
      //public const uint COMADMIN_E_CAT_PARTITION_IN_USE = 0x80110459;
      //public const uint COMADMIN_E_FILE_PARTITION_DUPLICATE_FILES = 0x8011045A;
      //public const uint COMADMIN_E_CAT_IMPORTED_COMPONENTS_NOT_ALLOWED = 0x8011045B;
      //public const uint COMADMIN_E_AMBIGUOUS_APPLICATION_NAME = 0x8011045C;
      //public const uint COMADMIN_E_AMBIGUOUS_PARTITION_NAME = 0x8011045D;
      //public const uint COMADMIN_E_REGDB_NOTINITIALIZED = 0x80110472;
      //public const uint COMADMIN_E_REGDB_NOTOPEN = 0x80110473;
      //public const uint COMADMIN_E_REGDB_SYSTEMERR = 0x80110474;
      //public const uint COMADMIN_E_REGDB_ALREADYRUNNING = 0x80110475;
      //public const uint COMADMIN_E_MIG_VERSIONNOTSUPPORTED = 0x80110480;
      //public const uint COMADMIN_E_MIG_SCHEMANOTFOUND = 0x80110481;
      //public const uint COMADMIN_E_CAT_BITNESSMISMATCH = 0x80110482;
      //public const uint COMADMIN_E_CAT_UNACCEPTABLEBITNESS = 0x80110483;
      //public const uint COMADMIN_E_CAT_WRONGAPPBITNESS = 0x80110484;
      //public const uint COMADMIN_E_CAT_PAUSE_RESUME_NOT_SUPPORTED = 0x80110485;
      //public const uint COMADMIN_E_CAT_SERVERFAULT = 0x80110486;
      //public const uint COMQC_E_APPLICATION_NOT_QUEUED = 0x80110600;
      //public const uint COMQC_E_NO_QUEUEABLE_INTERFACES = 0x80110601;
      //public const uint COMQC_E_QUEUING_SERVICE_NOT_AVAILABLE = 0x80110602;
      //public const uint COMQC_E_NO_IPERSISTSTREAM = 0x80110603;
      //public const uint COMQC_E_BAD_MESSAGE = 0x80110604;
      //public const uint COMQC_E_UNAUTHENTICATED = 0x80110605;
      //public const uint COMQC_E_UNTRUSTED_ENQUEUER = 0x80110606;
      //public const uint MSDTC_E_DUPLICATE_RESOURCE = 0x80110701;
      //public const uint COMADMIN_E_OBJECT_PARENT_MISSING = 0x80110808;
      //public const uint COMADMIN_E_OBJECT_DOES_NOT_EXIST = 0x80110809;
      //public const uint COMADMIN_E_APP_NOT_RUNNING = 0x8011080A;
      //public const uint COMADMIN_E_INVALID_PARTITION = 0x8011080B;
      //public const uint COMADMIN_E_SVCAPP_NOT_POOLABLE_OR_RECYCLABLE = 0x8011080D;
      //public const uint COMADMIN_E_USER_IN_SET = 0x8011080E;
      //public const uint COMADMIN_E_CANTRECYCLELIBRARYAPPS = 0x8011080F;
      //public const uint COMADMIN_E_CANTRECYCLESERVICEAPPS = 0x80110811;
      //public const uint COMADMIN_E_PROCESSALREADYRECYCLED = 0x80110812;
      //public const uint COMADMIN_E_PAUSEDPROCESSMAYNOTBERECYCLED = 0x80110813;
      //public const uint COMADMIN_E_CANTMAKEINPROCSERVICE = 0x80110814;
      //public const uint COMADMIN_E_PROGIDINUSEBYCLSID = 0x80110815;
      //public const uint COMADMIN_E_DEFAULT_PARTITION_NOT_IN_SET = 0x80110816;
      //public const uint COMADMIN_E_RECYCLEDPROCESSMAYNOTBEPAUSED = 0x80110817;
      //public const uint COMADMIN_E_PARTITION_ACCESSDENIED = 0x80110818;
      //public const uint COMADMIN_E_PARTITION_MSI_ONLY = 0x80110819;
      //public const uint COMADMIN_E_LEGACYCOMPS_NOT_ALLOWED_IN_1_0_FORMAT = 0x8011081A;
      //public const uint COMADMIN_E_LEGACYCOMPS_NOT_ALLOWED_IN_NONBASE_PARTITIONS = 0x8011081B;
      //public const uint COMADMIN_E_COMP_MOVE_SOURCE = 0x8011081C;
      //public const uint COMADMIN_E_COMP_MOVE_DEST = 0x8011081D;
      //public const uint COMADMIN_E_COMP_MOVE_PRIVATE = 0x8011081E;
      //public const uint COMADMIN_E_BASEPARTITION_REQUIRED_IN_SET = 0x8011081F;
      //public const uint COMADMIN_E_CANNOT_ALIAS_EVENTCLASS = 0x80110820;
      //public const uint COMADMIN_E_PRIVATE_ACCESSDENIED = 0x80110821;
      //public const uint COMADMIN_E_SAFERINVALID = 0x80110822;
      //public const uint COMADMIN_E_REGISTRY_ACCESSDENIED = 0x80110823;
      //public const uint COMADMIN_E_PARTITIONS_DISABLED = 0x80110824;


      #region Network Management Error Codes

      // http://msdn.microsoft.com/en-us/library/windows/desktop/aa370674%28v=vs.85%29.aspx

      /// <summary>(0) The operation completed successfully.</summary>
      public const uint NERR_Success = 0;

      ///// <summary>The workstation driver is not installed.</summary>
      //public const uint NERR_NetNotStarted = 2102;

      ///// <summary>The server could not be located.</summary>
      //public const uint NERR_UnknownServer = 2103;

      ///// <summary>An internal error occurred. The network cannot access a shared memory segment.</summary>
      //public const uint NERR_ShareMem = 2104;

      ///// <summary>A network resource shortage occurred.</summary>
      //public const uint NERR_NoNetworkResource = 2105;

      ///// <summary>This operation is not supported on workstations.</summary>
      //public const uint NERR_RemoteOnly = 2106;

      ///// <summary>The device is not connected.</summary>
      //public const uint NERR_DevNotRedirected = 2107;

      ///// <summary>The Server service is not started.</summary>
      //public const uint NERR_ServerNotStarted = 2114;

      ///// <summary>The queue is empty.</summary>
      //public const uint NERR_ItemNotFound = 2115;

      ///// <summary>The device or directory does not exist.</summary>
      //public const uint NERR_UnknownDevDir = 2116;

      ///// <summary>The operation is invalid on a redirected resource.</summary>
      //public const uint NERR_RedirectedPath = 2117;

      ///// <summary>The name has already been shared.</summary>
      //public const uint NERR_DuplicateShare = 2118;

      ///// <summary>The server is currently out of the requested resource.</summary>
      //public const uint NERR_NoRoom = 2119;

      ///// <summary>Requested addition of items exceeds the maximum allowed.</summary>
      //public const uint NERR_TooManyItems = 2121;

      ///// <summary>The Peer service supports only two simultaneous users.</summary>
      //public const uint NERR_InvalidMaxUsers = 2122;

      ///// <summary>The API return buffer is too small.</summary>
      //public const uint NERR_BufTooSmall = 2123;

      ///// <summary>A remote API error occurred.</summary>
      //public const uint NERR_RemoteErr = 2127;

      ///// <summary>An error occurred when opening or reading the configuration file.</summary>
      //public const uint NERR_LanmanIniError = 2131;

      ///// <summary>A general network error occurred.</summary>
      //public const uint NERR_NetworkError = 2136;

      ///// <summary>The Workstation service is in an inconsistent state. Restart the computer before restarting the Workstation service.</summary>
      //public const uint NERR_WkstaInconsistentState = 2137;

      ///// <summary>The Workstation service has not been started.</summary>
      //public const uint NERR_WkstaNotStarted = 2138;

      ///// <summary>The requested information is not available.</summary>
      //public const uint NERR_BrowserNotStarted = 2139;

      ///// <summary>An internal error occurred.</summary>
      //public const uint NERR_InternalError = 2140;

      ///// <summary>The server is not configured for transactions.</summary>
      //public const uint NERR_BadTransactConfig = 2141;

      ///// <summary>The requested API is not supported on the remote server.</summary>
      //public const uint NERR_InvalidAPI = 2142;

      ///// <summary>The event name is invalid.</summary>
      //public const uint NERR_BadEventName = 2143;

      ///// <summary>The computer name already exists on the network. Change it and restart the computer.</summary>
      //public const uint NERR_DupNameReboot = 2144;

      ///// <summary>The specified component could not be found in the configuration information.</summary>
      //public const uint NERR_CfgCompNotFound = 2146;

      ///// <summary>The specified parameter could not be found in the configuration information.</summary>
      //public const uint NERR_CfgParamNotFound = 2147;

      ///// <summary>A line in the configuration file is too long.</summary>
      //public const uint NERR_LineTooLong = 2149;

      ///// <summary>The printer does not exist.</summary>
      //public const uint NERR_QNotFound = 2150;

      ///// <summary>The print job does not exist.</summary>
      //public const uint NERR_JobNotFound = 2151;

      ///// <summary>The printer destination cannot be found.</summary>
      //public const uint NERR_DestNotFound = 2152;

      ///// <summary>The printer destination already exists.</summary>
      //public const uint NERR_DestExists = 2153;

      ///// <summary>The printer queue already exists.</summary>
      //public const uint NERR_QExists = 2154;

      ///// <summary>No more printers can be added.</summary>
      //public const uint NERR_QNoRoom = 2155;

      ///// <summary>No more print jobs can be added.</summary>
      //public const uint NERR_JobNoRoom = 2156;

      ///// <summary>No more printer destinations can be added.</summary>
      //public const uint NERR_DestNoRoom = 2157;

      ///// <summary>This printer destination is idle and cannot accept control operations.</summary>
      //public const uint NERR_DestIdle = 2158;

      ///// <summary>This printer destination request contains an invalid control function.</summary>
      //public const uint NERR_DestInvalidOp = 2159;

      ///// <summary>The print processor is not responding.</summary>
      //public const uint NERR_ProcNoRespond = 2160;

      ///// <summary>The spooler is not running.</summary>
      //public const uint NERR_SpoolerNotLoaded = 2161;

      ///// <summary>This operation cannot be performed on the print destination in its current state.</summary>
      //public const uint NERR_DestInvalidState = 2162;

      ///// <summary>This operation cannot be performed on the printer queue in its current state.</summary>
      //public const uint NERR_QinvalidState = 2163;

      ///// <summary>This operation cannot be performed on the print job in its current state.</summary>
      //public const uint NERR_JobInvalidState = 2164;

      ///// <summary>A spooler memory allocation failure occurred.</summary>
      //public const uint NERR_SpoolNoMemory = 2165;

      ///// <summary>The device driver does not exist.</summary>
      //public const uint NERR_DriverNotFound = 2166;

      ///// <summary>The data type is not supported by the print processor.</summary>
      //public const uint NERR_DataTypeInvalid = 2167;

      ///// <summary>The print processor is not installed.</summary>
      //public const uint NERR_ProcNotFound = 2168;

      ///// <summary>The service database is locked.</summary>
      //public const uint NERR_ServiceTableLocked = 2180;

      ///// <summary>The service table is full.</summary>
      //public const uint NERR_ServiceTableFull = 2181;

      ///// <summary>The requested service has already been started.</summary>
      //public const uint NERR_ServiceInstalled = 2182;

      ///// <summary>The service does not respond to control actions.</summary>
      //public const uint NERR_ServiceEntryLocked = 2183;

      ///// <summary>The service has not been started.</summary>
      //public const uint NERR_ServiceNotInstalled = 2184;

      ///// <summary>The service name is invalid.</summary>
      //public const uint NERR_BadServiceName = 2185;

      ///// <summary>The service is not responding to the control function.</summary>
      //public const uint NERR_ServiceCtlTimeout = 2186;

      ///// <summary>The service control is busy.</summary>
      //public const uint NERR_ServiceCtlBusy = 2187;

      ///// <summary>The configuration file contains an invalid service program name.</summary>
      //public const uint NERR_BadServiceProgName = 2188;

      ///// <summary>The service could not be controlled in its present state.</summary>
      //public const uint NERR_ServiceNotCtrl = 2189;

      ///// <summary>The service ended abnormally.</summary>
      //public const uint NERR_ServiceKillProc = 2190;

      ///// <summary>The requested pause or stop is not valid for this service.</summary>
      //public const uint NERR_ServiceCtlNotValid = 2191;

      ///// <summary>The service control dispatcher could not find the service name in the dispatch table.</summary>
      //public const uint NERR_NotInDispatchTbl = 2192;

      ///// <summary>The service control dispatcher pipe read failed.</summary>
      //public const uint NERR_BadControlRecv = 2193;

      ///// <summary>A thread for the new service could not be created.</summary>
      //public const uint NERR_ServiceNotStarting = 2194;

      ///// <summary>This workstation is already logged on to the local-area network.</summary>
      //public const uint NERR_AlreadyLoggedOn = 2200;

      ///// <summary>The workstation is not logged on to the local-area network.</summary>
      //public const uint NERR_NotLoggedOn = 2201;

      ///// <summary>The user name or group name parameter is invalid.</summary>
      //public const uint NERR_BadUsername = 2202;

      ///// <summary>The password parameter is invalid.</summary>
      //public const uint NERR_BadPassword = 2203;

      ///// <summary>@W The logon processor did not add the message alias.</summary>
      //public const uint NERR_UnableToAddName_W = 2204;

      ///// <summary>The logon processor did not add the message alias.</summary>
      //public const uint NERR_UnableToAddName_F = 2205;

      ///// <summary>@W The logoff processor did not delete the message alias.</summary>
      //public const uint NERR_UnableToDelName_W = 2206;

      ///// <summary>The logoff processor did not delete the message alias.</summary>
      //public const uint NERR_UnableToDelName_F = 2207;

      ///// <summary>Network logons are paused.</summary>
      //public const uint NERR_LogonsPaused = 2209;

      ///// <summary>A centralized logon-server conflict occurred.</summary>
      //public const uint NERR_LogonServerConflict = 2210;

      ///// <summary>The server is configured without a valid user path.</summary>
      //public const uint NERR_LogonNoUserPath = 2211;

      ///// <summary>An error occurred while loading or running the logon script.</summary>
      //public const uint NERR_LogonScriptError = 2212;

      ///// <summary>The logon server was not specified. Your computer will be logged on as STANDALONE.</summary>
      //public const uint NERR_StandaloneLogon = 2214;

      ///// <summary>The logon server could not be found.</summary>
      //public const uint NERR_LogonServerNotFound = 2215;

      ///// <summary>There is already a logon domain for this computer.</summary>
      //public const uint NERR_LogonDomainExists = 2216;

      ///// <summary>The logon server could not validate the logon.</summary>
      //public const uint NERR_NonValidatedLogon = 2217;

      ///// <summary>The security database could not be found.</summary>
      //public const uint NERR_ACFNotFound = 2219;

      ///// <summary>The group name could not be found.</summary>
      //public const uint NERR_GroupNotFound = 2220;

      ///// <summary>The user name could not be found.</summary>
      //public const uint NERR_UserNotFound = 2221;

      ///// <summary>The resource name could not be found.</summary>
      //public const uint NERR_ResourceNotFound = 2222;

      ///// <summary>The group already exists.</summary>
      //public const uint NERR_GroupExists = 2223;

      ///// <summary>The user account already exists.</summary>
      //public const uint NERR_UserExists = 2224;

      ///// <summary>The resource permission list already exists.</summary>
      //public const uint NERR_ResourceExists = 2225;

      ///// <summary>This operation is only allowed on the primary domain controller of the domain.</summary>
      //public const uint NERR_NotPrimary = 2226;

      ///// <summary>The security database has not been started.</summary>
      //public const uint NERR_ACFNotLoaded = 2227;

      ///// <summary>There are too many names in the user accounts database.</summary>
      //public const uint NERR_ACFNoRoom = 2228;

      ///// <summary>A disk I/O failure occurred.</summary>
      //public const uint NERR_ACFFileIOFail = 2229;

      ///// <summary>The limit of 64 entries per resource was exceeded.</summary>
      //public const uint NERR_ACFTooManyLists = 2230;

      ///// <summary>Deleting a user with a session is not allowed.</summary>
      //public const uint NERR_UserLogon = 2231;

      ///// <summary>The parent directory could not be located.</summary>
      //public const uint NERR_ACFNoParent = 2232;

      ///// <summary>Unable to add to the security database session cache segment.</summary>
      //public const uint NERR_CanNotGrowSegment = 2233;

      ///// <summary>This operation is not allowed on this special group.</summary>
      //public const uint NERR_SpeGroupOp = 2234;

      ///// <summary>This user is not cached in user accounts database session cache.</summary>
      //public const uint NERR_NotInCache = 2235;

      ///// <summary>The user already belongs to this group.</summary>
      //public const uint NERR_UserInGroup = 2236;

      ///// <summary>The user does not belong to this group.</summary>
      //public const uint NERR_UserNotInGroup = 2237;

      ///// <summary>This user account is undefined.</summary>
      //public const uint NERR_AccountUndefined = 2238;

      ///// <summary>This user account has expired.</summary>
      //public const uint NERR_AccountExpired = 2239;

      ///// <summary>The user is not allowed to log on from this workstation.</summary>
      //public const uint NERR_InvalidWorkstation = 2240;

      ///// <summary>The user is not allowed to log on at this time.</summary>
      //public const uint NERR_InvalidLogonHours = 2241;

      ///// <summary>The password of this user has expired.</summary>
      //public const uint NERR_PasswordExpired = 2242;

      ///// <summary>The password of this user cannot change.</summary>
      //public const uint NERR_PasswordCantChange = 2243;

      ///// <summary>This password cannot be used now.</summary>
      //public const uint NERR_PasswordHistConflict = 2244;

      ///// <summary>The password does not meet the password policy requirements. Check the minimum password length, password complexity and password history requirements.</summary>
      //public const uint NERR_PasswordTooShort = 2245;

      ///// <summary>The password of this user is too recent to change.</summary>
      //public const uint NERR_PasswordTooRecent = 2246;

      ///// <summary>The security database is corrupted.</summary>
      //public const uint NERR_InvalidDatabase = 2247;

      ///// <summary>No updates are necessary to this replicant network/local security database.</summary>
      //public const uint NERR_DatabaseUpToDate = 2248;

      ///// <summary>This replicant database is outdated; synchronization is required.</summary>
      //public const uint NERR_SyncRequired = 2249;

      //// <summary>(2250) The network connection could not be found.</summary>
      //public const uint NERR_UseNotFound = 2250;

      ///// <summary>This asg_type is invalid.</summary>
      //public const uint NERR_BadAsgType = 2251;

      ///// <summary>This device is currently being shared.</summary>
      //public const uint NERR_DeviceIsShared = 2252;

      ///// <summary>The computer name could not be added as a message alias. The name may already exist on the network.</summary>
      //public const uint NERR_NoComputerName = 2270;

      ///// <summary>The Messenger service is already started.</summary>
      //public const uint NERR_MsgAlreadyStarted = 2271;

      ///// <summary>The Messenger service failed to start.</summary>
      //public const uint NERR_MsgInitFailed = 2272;

      ///// <summary>The message alias could not be found on the network.</summary>
      //public const uint NERR_NameNotFound = 2273;

      ///// <summary>This message alias has already been forwarded.</summary>
      //public const uint NERR_AlreadyForwarded = 2274;

      ///// <summary>This message alias has been added but is still forwarded.</summary>
      //public const uint NERR_AddForwarded = 2275;

      ///// <summary>This message alias already exists locally.</summary>
      //public const uint NERR_AlreadyExists = 2276;

      ///// <summary>The maximum number of added message aliases has been exceeded.</summary>
      //public const uint NERR_TooManyNames = 2277;

      ///// <summary>The computer name could not be deleted.</summary>
      //public const uint NERR_DelComputerName = 2278;

      ///// <summary>Messages cannot be forwarded back to the same workstation.</summary>
      //public const uint NERR_LocalForward = 2279;

      ///// <summary>An error occurred in the domain message processor.</summary>
      //public const uint NERR_GrpMsgProcessor = 2280;

      ///// <summary>The message was sent, but the recipient has paused the Messenger service.</summary>
      //public const uint NERR_PausedRemote = 2281;

      ///// <summary>The message was sent but not received.</summary>
      //public const uint NERR_BadReceive = 2282;

      ///// <summary>The message alias is currently in use. Try again later.</summary>
      //public const uint NERR_NameInUse = 2283;

      ///// <summary>The Messenger service has not been started.</summary>
      //public const uint NERR_MsgNotStarted = 2284;

      ///// <summary>The name is not on the local computer.</summary>
      //public const uint NERR_NotLocalName = 2285;

      ///// <summary>The forwarded message alias could not be found on the network.</summary>
      //public const uint NERR_NoForwardName = 2286;

      ///// <summary>The message alias table on the remote station is full.</summary>
      //public const uint NERR_RemoteFull = 2287;

      ///// <summary>Messages for this alias are not currently being forwarded.</summary>
      //public const uint NERR_NameNotForwarded = 2288;

      ///// <summary>The broadcast message was truncated.</summary>
      //public const uint NERR_TruncatedBroadcast = 2289;

      ///// <summary>This is an invalid device name.</summary>
      //public const uint NERR_InvalidDevice = 2294;

      ///// <summary>A write fault occurred.</summary>
      //public const uint NERR_WriteFault = 2295;

      ///// <summary>A duplicate message alias exists on the network.</summary>
      //public const uint NERR_DuplicateName = 2297;

      ///// <summary>@W This message alias will be deleted later.</summary>
      //public const uint NERR_DeleteLater = 2298;

      ///// <summary>The message alias was not successfully deleted from all networks.</summary>
      //public const uint NERR_IncompleteDel = 2299;

      ///// <summary>This operation is not supported on computers with multiple networks.</summary>
      //public const uint NERR_MultipleNets = 2300;

      //// <summary>(2310) This shared resource does not exist.</summary>
      //public const uint NERR_NetNameNotFound = 2310;

      ///// <summary>This device is not shared.</summary>
      //public const uint NERR_DeviceNotShared = 2311;

      ///// <summary>A session does not exist with that computer name.</summary>
      //public const uint NERR_ClientNameNotFound = 2312;

      /// <summary>(2314) There is not an open file with that identification number.</summary>
      public const uint NERR_FileIdNotFound = 2314;

      ///// <summary>A failure occurred when executing a remote administration command.</summary>
      //public const uint NERR_ExecFailure = 2315;

      ///// <summary>A failure occurred when opening a remote temporary file.</summary>
      //public const uint NERR_TmpFile = 2316;

      ///// <summary>The data returned from a remote administration command has been truncated to 64K.</summary>
      //public const uint NERR_TooMuchData = 2317;

      ///// <summary>This device cannot be shared as both a spooled and a non-spooled resource.</summary>
      //public const uint NERR_DeviceShareConflict = 2318;

      ///// <summary>The information in the list of servers may be incorrect.</summary>
      //public const uint NERR_BrowserTableIncomplete = 2319;

      ///// <summary>The computer is not active in this domain.</summary>
      //public const uint NERR_NotLocalDomain = 2320;

      ///// <summary>The share must be removed from the Distributed File System before it can be deleted.</summary>
      //public const uint NERR_IsDfsShare = 2321;

      ///// <summary>The operation is invalid for this device.</summary>
      //public const uint NERR_DevInvalidOpCode = 2331;

      ///// <summary>This device cannot be shared.</summary>
      //public const uint NERR_DevNotFound = 2332;

      ///// <summary>This device was not open.</summary>
      //public const uint 	NERR_DevNotOpen = 2333;

      ///// <summary>This device name list is invalid.</summary>
      //public const uint NERR_BadQueueDevString = 2334;

      ///// <summary>The queue priority is invalid.</summary>
      //public const uint NERR_BadQueuePriority = 2335;

      ///// <summary>There are no shared communication devices.</summary>
      //public const uint NERR_NoCommDevs = 2337;

      ///// <summary>The queue you specified does not exist.</summary>
      //public const uint NERR_QueueNotFound = 2338;

      ///// <summary>This list of devices is invalid.</summary>
      //public const uint NERR_BadDevString = 2340;

      ///// <summary>The requested device is invalid.</summary>
      //public const uint NERR_BadDev = 2341;      

      ///// <summary>This device is already in use by the spooler.</summary>
      //public const uint NERR_InUseBySpooler = 2342;

      ///// <summary>This device is already in use as a communication device.</summary>
      //public const uint NERR_CommDevInUse = 2343;

      ///// <summary>This computer name is invalid.</summary>
      //public const uint NERR_InvalidComputer = 2351;

      ///// <summary>The string and prefix specified are too long.</summary>
      //public const uint NERR_MaxLenExceeded = 2354;

      ///// <summary>This path component is invalid.</summary>
      //public const uint NERR_BadComponent = 2356;

      ///// <summary>Could not determine the type of input.</summary>
      //public const uint NERR_CantType = 2357;

      ///// <summary>The buffer for types is not big enough.</summary>
      //public const uint NERR_TooManyEntries = 2362;

      ///// <summary>Profile files cannot exceed 64K.</summary>
      //public const uint NERR_ProfileFileTooBig = 2370;      

      ///// <summary>The start offset is out of range.</summary>
      //public const uint NERR_ProfileOffset = 2371;

      ///// <summary>The system cannot delete current connections to network resources.</summary>
      //public const uint NERR_ProfileCleanup = 2372;

      ///// <summary>The system was unable to parse the command line in this file.</summary>
      //public const uint NERR_ProfileUnknownCmd = 2373;

      ///// <summary>An error occurred while loading the profile file.</summary>
      //public const uint NERR_ProfileLoadErr = 2374;

      ///// <summary>@W Errors occurred while saving the profile file. The profile was partially saved.</summary>
      //public const uint NERR_ProfileSaveErr = 2375;

      ///// <summary>Log file %1 is full.</summary>
      //public const uint NERR_LogOverflow = 2377;

      ///// <summary>This log file has changed between reads.</summary>
      //public const uint NERR_LogFileChanged = 2378;

      ///// <summary>Log file %1 is corrupt.</summary>
      //public const uint NERR_LogFileCorrupt = 2379;      

      ///// <summary>The source path cannot be a directory.</summary>
      //public const uint NERR_SourceIsDir = 2380;

      ///// <summary>The source path is illegal.</summary>
      //public const uint NERR_BadSource = 2381;

      ///// <summary>The destination path is illegal.</summary>
      //public const uint NERR_BadDest = 2382;

      ///// <summary>The source and destination paths are on different servers.</summary>
      //public const uint NERR_DifferentServers = 2383;

      ///// <summary>The Run server you requested is paused.</summary>
      //public const uint NERR_RunSrvPaused = 2385;

      ///// <summary>An error occurred when communicating with a Run server.</summary>
      //public const uint NERR_ErrCommRunSrv = 2389;

      ///// <summary>An error occurred when starting a background process.</summary>
      //public const uint NERR_ErrorExecingGhost = 2391;

      ///// <summary>The shared resource you are connected to could not be found.</summary>
      //public const uint NERR_ShareNotFound = 2392;      

      ///// <summary>The LAN adapter number is invalid.</summary>
      //public const uint NERR_InvalidLana = 2400;

      ///// <summary>There are open files on the connection.</summary>
      //public const uint NERR_OpenFiles = 2401;

      ///// <summary>Active connections still exist.</summary>
      //public const uint NERR_ActiveConns = 2402;

      ///// <summary>This share name or password is invalid.</summary>
      //public const uint NERR_BadPasswordCore = 2403;

      ///// <summary>The device is being accessed by an active process.</summary>
      //public const uint NERR_DevInUse = 2404;

      ///// <summary>The drive letter is in use locally.</summary>
      //public const uint NERR_LocalDrive = 2405;

      ///// <summary>The specified client is already registered for the specified event.</summary>
      //public const uint NERR_AlertExists = 2430;

      ///// <summary>The alert table is full.</summary>
      //public const uint NERR_TooManyAlerts = 2431;      

      ///// <summary>An invalid or nonexistent alert name was raised.</summary>
      //public const uint NERR_NoSuchAlert = 2432;

      ///// <summary>The alert recipient is invalid.</summary>
      //public const uint NERR_BadRecipient = 2433;

      ///// <summary>A user's session with this server has been deleted</summary>
      //public const uint NERR_AcctLimitExceeded = 2434;

      ///// <summary>The log file does not contain the requested record number.</summary>
      //public const uint NERR_InvalidLogSeek = 2440;

      ///// <summary>The user accounts database is not configured correctly.</summary>
      //public const uint NERR_BadUasConfig = 2450;

      ///// <summary>This operation is not permitted when the Netlogon service is running.</summary>
      //public const uint NERR_InvalidUASOp = 2451;

      ///// <summary>This operation is not allowed on the last administrative account.</summary>
      //public const uint NERR_LastAdmin = 2452;

      ///// <summary>Could not find domain controller for this domain.</summary>
      //public const uint NERR_DCNotFound = 2453;      

      ///// <summary>Could not set logon information for this user.</summary>
      //public const uint NERR_LogonTrackingError = 2454;

      ///// <summary>The Netlogon service has not been started.</summary>
      //public const uint NERR_NetlogonNotStarted = 2455;

      ///// <summary>Unable to add to the user accounts database.</summary>
      //public const uint NERR_CanNotGrowUASFile = 2456;

      ///// <summary>This server's clock is not synchronized with the primary domain controller's clock.</summary>
      //public const uint NERR_TimeDiffAtDC = 2457;

      ///// <summary>A password mismatch has been detected.</summary>
      //public const uint NERR_PasswordMismatch = 2458;

      ///// <summary>The server identification does not specify a valid server.</summary>
      //public const uint NERR_NoSuchServer = 2460;

      ///// <summary>The session identification does not specify a valid session.</summary>
      //public const uint NERR_NoSuchSession = 2461;

      ///// <summary>The connection identification does not specify a valid connection.</summary>
      //public const uint NERR_NoSuchConnection = 2462;      

      ///// <summary>There is no space for another entry in the table of available servers.</summary>
      //public const uint NERR_TooManyServers = 2463;

      ///// <summary>The server has reached the maximum number of sessions it supports.</summary>
      //public const uint NERR_TooManySessions = 2464;

      ///// <summary>The server has reached the maximum number of connections it supports.</summary>
      //public const uint NERR_TooManyConnections = 2465;

      ///// <summary>The server cannot open more files because it has reached its maximum number.</summary>
      //public const uint NERR_TooManyFiles = 2466;

      ///// <summary>There are no alternate servers registered on this server.</summary>
      //public const uint NERR_NoAlternateServers = 2467;

      ///// <summary>Try down-level (remote admin protocol) version of API instead.</summary>
      //public const uint NERR_TryDownLevel = 2470;

      ///// <summary>The UPS driver could not be accessed by the UPS service.</summary>
      //public const uint NERR_UPSDriverNotStarted = 2480;

      ///// <summary>The UPS service is not configured correctly.</summary>
      //public const uint NERR_UPSInvalidConfig = 2481;      

      ///// <summary>The UPS service could not access the specified Comm Port.</summary>
      //public const uint NERR_UPSInvalidCommPort = 2482;

      ///// <summary>The UPS indicated a line fail or low battery situation. Service not started.</summary>
      //public const uint NERR_UPSSignalAsserted = 2483;

      ///// <summary>The UPS service failed to perform a system shut down.</summary>
      //public const uint NERR_UPSShutdownFailed = 2484;

      ///// <summary>The program below returned an MS-DOS error code:</summary>
      //public const uint NERR_BadDosRetCode = 2500;

      ///// <summary>The program below needs more memory:</summary>
      //public const uint NERR_ProgNeedsExtraMem = 2501;

      ///// <summary>The program below called an unsupported MS-DOS function:</summary>
      //public const uint NERR_BadDosFunction = 2502;

      ///// <summary>The workstation failed to boot.</summary>
      //public const uint NERR_RemoteBootFailed = 2503;

      ///// <summary>The file below is corrupt.</summary>
      //public const uint NERR_BadFileCheckSum = 2504;      

      ///// <summary>No loader is specified in the boot-block definition file.</summary>
      //public const uint NERR_NoRplBootSystem = 2505;

      ///// <summary>NetBIOS returned an error: The NCB and SMB are dumped above.</summary>
      //public const uint NERR_RplLoadrNetBiosErr = 2506;

      ///// <summary>A disk I/O error occurred.</summary>
      //public const uint NERR_RplLoadrDiskErr = 2507;

      ///// <summary>Image parameter substitution failed.</summary>
      //public const uint NERR_ImageParamErr = 2508;

      ///// <summary>Too many image parameters cross disk sector boundaries.</summary>
      //public const uint NERR_TooManyImageParams = 2509;

      ///// <summary>The image was not generated from an MS-DOS diskette formatted with /S.</summary>
      //public const uint NERR_NonDosFloppyUsed = 2510;

      ///// <summary>Remote boot will be restarted later.</summary>
      //public const uint NERR_RplBootRestart = 2511;

      ///// <summary>The call to the Remoteboot server failed.</summary>
      //public const uint NERR_RplSrvrCallFailed = 2512;      

      ///// <summary>Cannot connect to the Remoteboot server.</summary>
      //public const uint NERR_CantConnectRplSrvr = 2513;

      ///// <summary>Cannot open image file on the Remoteboot server.</summary>
      //public const uint NERR_CantOpenImageFile = 2514;

      ///// <summary>Connecting to the Remoteboot server.</summary>
      //public const uint NERR_CallingRplSrvr = 2515;

      ///// <summary>Connecting to the Remoteboot server.</summary>
      //public const uint NERR_StartingRplBoot = 2516;

      ///// <summary>Remote boot service was stopped; check the error log for the cause of the problem.</summary>
      //public const uint NERR_RplBootServiceTerm = 2517;

      ///// <summary>Remote boot startup failed; check the error log for the cause of the problem.</summary>
      //public const uint NERR_RplBootStartFailed = 2518;

      ////// <summary>A second connection to a Remoteboot resource is not allowed.</summary>
      //public const uint NERR_RplConnected = 2519;

      ///// <summary>The browser service was configured with MaintainServerList=No.</summary>
      //public const uint NERR_BrowserConfiguredToNotRun = 2550;      

      ///// <summary>Service failed to start since none of the network adapters started with this service.</summary>
      //public const uint NERR_RplNoAdaptersStarted = 2610;

      ///// <summary>Service failed to start due to bad startup information in the registry.</summary>
      //public const uint NERR_RplBadRegistry = 2611;

      ///// <summary>Service failed to start because its database is absent or corrupt.</summary>
      //public const uint NERR_RplBadDatabase = 2612;

      ///// <summary>Service failed to start because RPLFILES share is absent.</summary>
      //public const uint NERR_RplRplfilesShare = 2613;

      ///// <summary>Service failed to start because RPLUSER group is absent.</summary>
      //public const uint NERR_RplNotRplServer = 2614;

      ///// <summary>Cannot enumerate service records.</summary>
      //public const uint NERR_RplCannotEnum = 2615;

      ///// <summary>Workstation record information has been corrupted.</summary>
      //public const uint NERR_RplWkstaInfoCorrupted = 2616;

      ///// <summary>Workstation record was not found.</summary>
      //public const uint NERR_RplWkstaNotFound = 2617;      

      ///// <summary>Workstation name is in use by some other workstation.</summary>
      //public const uint NERR_RplWkstaNameUnavailable = 2618;

      ///// <summary>Profile record information has been corrupted.</summary>
      //public const uint NERR_RplProfileInfoCorrupted = 2619;

      ///// <summary>Profile record was not found.</summary>
      //public const uint NERR_RplProfileNotFound = 2620;

      ///// <summary>Profile name is in use by some other profile.</summary>
      //public const uint NERR_RplProfileNameUnavailable = 2621;

      ///// <summary>There are workstations using this profile.</summary>
      //public const uint NERR_RplProfileNotEmpty = 2622;

      ///// <summary>Configuration record information has been corrupted.</summary>
      //public const uint NERR_RplConfigInfoCorrupted = 2623;

      ///// <summary>Configuration record was not found.</summary>
      //public const uint NERR_RplConfigNotFound = 2624;

      ///// <summary>Adapter ID record information has been corrupted.</summary>
      //public const uint NERR_RplAdapterInfoCorrupted = 2625;      

      ///// <summary>An internal service error has occurred.</summary>
      //public const uint NERR_RplInternal = 2626;

      ///// <summary>Vendor ID record information has been corrupted.</summary>
      //public const uint NERR_RplVendorInfoCorrupted = 2627;

      ///// <summary>Boot block record information has been corrupted.</summary>
      //public const uint NERR_RplBootInfoCorrupted = 2628;

      ///// <summary>The user account for this workstation record is missing.</summary>
      //public const uint NERR_RplWkstaNeedsUserAcct = 2629;

      ///// <summary>The RPLUSER local group could not be found.</summary>
      //public const uint NERR_RplNeedsRPLUSERAcct = 2630;

      ///// <summary>Boot block record was not found.</summary>
      //public const uint NERR_RplBootNotFound = 2631;

      ///// <summary>Chosen profile is incompatible with this workstation.</summary>
      //public const uint NERR_RplIncompatibleProfile = 2632;

      ///// <summary>Chosen network adapter ID is in use by some other workstation.</summary>
      //public const uint NERR_RplAdapterNameUnavailable = 2633;      

      ///// <summary>There are profiles using this configuration.</summary>
      //public const uint NERR_RplConfigNotEmpty = 2634;

      ///// <summary>There are workstations, profiles, or configurations using this boot block.</summary>
      //public const uint NERR_RplBootInUse = 2635;

      ///// <summary>Service failed to backup Remoteboot database.</summary>
      //public const uint NERR_RplBackupDatabase = 2636;

      ///// <summary>Adapter record was not found.</summary>
      //public const uint NERR_RplAdapterNotFound = 2637;

      ///// <summary>Vendor record was not found.</summary>
      //public const uint NERR_RplVendorNotFound = 2638;

      ///// <summary>Vendor name is in use by some other vendor record.</summary>
      //public const uint NERR_RplVendorNameUnavailable = 2639;

      ///// <summary>(boot name, vendor ID) is in use by some other boot block record.</summary>
      //public const uint NERR_RplBootNameUnavailable = 2640;

      ///// <summary>Configuration name is in use by some other configuration.</summary>
      //public const uint NERR_RplConfigNameUnavailable = 2641;

      ///// <summary>The internal database maintained by the Dfs service is corrupt.</summary>
      //public const uint NERR_DfsInternalCorruption = 2660;

      ///// <summary>One of the records in the internal Dfs database is corrupt.</summary>
      //public const uint NERR_DfsVolumeDataCorrupt = 2661;

      ///// <summary>There is no DFS name whose entry path matches the input Entry Path.</summary>
      //public const uint NERR_DfsNoSuchVolume = 2662;

      ///// <summary>A root or link with the given name already exists.</summary>
      //public const uint NERR_DfsVolumeAlreadyExists = 2663;

      ///// <summary>The server share specified is already shared in the Dfs.</summary>
      //public const uint NERR_DfsAlreadyShared = 2664;

      ///// <summary>The indicated server share does not support the indicated DFS namespace.</summary>
      //public const uint NERR_DfsNoSuchShare = 2665;

      ///// <summary>The operation is not valid on this portion of the namespace.</summary>
      //public const uint NERR_DfsNotALeafVolume = 2666;

      ///// <summary>The operation is not valid on this portion of the namespace.</summary>
      //public const uint NERR_DfsLeafVolume = 2667;

      ///// <summary>The operation is ambiguous because the link has multiple servers.</summary>
      //public const uint NERR_DfsVolumeHasMultipleServers = 2668;

      ///// <summary>Unable to create a link.</summary>
      //public const uint NERR_DfsCantCreateJunctionPoint = 2669;

      ///// <summary>The server is not Dfs Aware.</summary>
      //public const uint NERR_DfsServerNotDfsAware = 2670;

      ///// <summary>The specified rename target path is invalid.</summary>
      //public const uint NERR_DfsBadRenamePath = 2671;

      ///// <summary>The specified DFS link is offline.</summary>
      //public const uint NERR_DfsVolumeIsOffline = 2672;

      ///// <summary>The specified server is not a server for this link.</summary>
      //public const uint NERR_DfsNoSuchServer = 2673;

      ///// <summary>A cycle in the Dfs name was detected.</summary>
      //public const uint NERR_DfsCyclicalName = 2674;

      ///// <summary>The operation is not supported on a server-based Dfs.</summary>
      //public const uint NERR_DfsNotSupportedInServerDfs = 2675;

      ///// <summary>This link is already supported by the specified server-share.</summary>
      //public const uint NERR_DfsDuplicateService = 2676;

      ///// <summary>Can't remove the last server-share supporting this root or link.</summary>
      //public const uint NERR_DfsCantRemoveLastServerShare = 2677;

      ///// <summary>The operation is not supported for an Inter-DFS link.</summary>
      //public const uint NERR_DfsVolumeIsInterDfs = 2678;

      ///// <summary>The internal state of the Dfs Service has become inconsistent.</summary>
      //public const uint NERR_DfsInconsistent = 2679;

      ///// <summary>The Dfs Service has been installed on the specified server.</summary>
      //public const uint NERR_DfsServerUpgraded = 2680;

      ///// <summary>The Dfs data being reconciled is identical.</summary>
      //public const uint NERR_DfsDataIsIdentical = 2681;

      ///// <summary>The DFS root cannot be deleted. Uninstall DFS if required.</summary>
      //public const uint NERR_DfsCantRemoveDfsRoot = 2682;

      ///// <summary>A child or parent directory of the share is already in a Dfs.</summary>
      //public const uint NERR_DfsChildOrParentInDfs = 2683;

      ///// <summary>Dfs internal error.</summary>
      //public const uint NERR_DfsInternalError = 2690;

      ///// <summary>This computer is already joined to a domain.</summary>
      //public const uint NERR_SetupAlreadyJoined = 2691;

      ///// <summary>This computer is not currently joined to a domain.</summary>
      //public const uint NERR_SetupNotJoined = 2692;

      ///// <summary>This computer is a domain controller and cannot be unjoined from a domain.</summary>
      //public const uint NERR_SetupDomainController = 2693;

      ///// <summary>The destination domain controller does not support creating machine accounts in OUs.</summary>
      //public const uint NERR_DefaultJoinRequired = 2694;

      ///// <summary>The specified workgroup name is invalid.</summary>
      //public const uint NERR_InvalidWorkgroupName = 2695;

      ///// <summary>The specified computer name is incompatible with the default language used on the domain controller.</summary>
      //public const uint NERR_NameUsesIncompatibleCodePage = 2696;

      ///// <summary>The specified computer account could not be found.</summary>
      //public const uint NERR_ComputerAccountNotFound = 2697;

      ///// <summary>This version of Windows cannot be joined to a domain.</summary>
      //public const uint NERR_PersonalSku = 2698;

      ///// <summary>The password must change at the next logon.</summary>
      //public const uint NERR_PasswordMustChange = 2701;

      ///// <summary>The account is locked out.</summary>
      //public const uint NERR_AccountLockedOut = 2702;

      ///// <summary>The password is too long.</summary>
      //public const uint NERR_PasswordTooLong = 2703;

      ///// <summary>The password does not meet the complexity policy.</summary>
      //public const uint NERR_PasswordNotComplexEnough = 2704;

      ///// <summary>The password does not meet the requirements of the password filter DLLs.</summary>
      //public const uint NERR_PasswordFilterError = 2705;

      ///// <summary>The offline join completion information was not found.</summary>
      //public const uint NERR_NoOfflineJoinInfo = 2709;

      ///// <summary>The offline join completion information was bad.</summary>
      //public const uint NERR_BadOfflineJoinInfo = 2710;

      ///// <summary>Unable to create offline join information. Please ensure you have access to the specified path location and permissions to modify its contents. Running as an elevated administrator may be required.</summary>
      //public const uint NERR_CantCreateJoinInfo = 2711;

      ///// <summary>The domain join info being saved was incomplete or bad.</summary>
      //public const uint NERR_BadDomainJoinInfo = 2712;

      ///// <summary>Offline join operation successfully completed but a restart is needed.</summary>
      //public const uint NERR_JoinPerformedMustRestart = 2713;

      ///// <summary>There was no offline join operation pending.</summary>
      //public const uint NERR_NoJoinPending = 2714;

      ///// <summary>Unable to set one or more requested machine or domain name values on the local computer.</summary>
      //public const uint NERR_ValuesNotSet = 2715;

      ///// <summary>Could not verify the current machine's hostname against the saved value in the join completion information.</summary>
      //public const uint NERR_CantVerifyHostname = 2716;

      ///// <summary>Unable to load the specified offline registry hive. Please ensure you have access to the specified path location and permissions to modify its contents. Running as an elevated administrator may be required.</summary>
      //public const uint NERR_CantLoadOfflineHive = 2717;

      ///// <summary>The minimum session security requirements for this operation were not met.</summary>
      //public const uint NERR_ConnectionInsecure = 2718;

      ///// <summary>Computer account provisioning blob version is not supported.</summary>
      //public const uint NERR_RplBootInUse = 2719;

      #endregion // Network Management Error Codes

      #region Configuration Manager Error Codes

      /// <summary>(0) The operation completed successfully.</summary>
      public const uint CR_SUCCESS = 0;

      //public const uint CR_DEFAULT = 1;
      //public const uint CR_OUT_OF_MEMORY = 2;
      //public const uint CR_INVALID_POINTER = 3;
      //public const uint CR_INVALID_FLAG = 4;
      //public const uint CR_INVALID_DEVNODE = 5;
      //public const uint CR_INVALID_DEVINST = CR_INVALID_DEVNODE;
      //public const uint CR_INVALID_RES_DES = 6;
      //public const uint CR_INVALID_LOG_CONF = 7;
      //public const uint CR_INVALID_ARBITRATOR = 8;
      //public const uint CR_INVALID_NODELIST = 9;
      //public const uint CR_DEVNODE_HAS_REQS = 10;
      //public const uint CR_DEVINST_HAS_REQS = CR_DEVNODE_HAS_REQS;
      //public const uint CR_INVALID_RESOURCEID = 11;
      //public const uint CR_DLVXD_NOT_FOUND = 12; // WIN 95 ONLY 
      //public const uint CR_NO_SUCH_DEVNODE = 13;
      //public const uint CR_NO_SUCH_DEVINST = CR_NO_SUCH_DEVNODE;
      //public const uint CR_NO_MORE_LOG_CONF = 14;
      //public const uint CR_NO_MORE_RES_DES = 15;
      //public const uint CR_ALREADY_SUCH_DEVNODE = 16;
      //public const uint CR_ALREADY_SUCH_DEVINST = CR_ALREADY_SUCH_DEVNODE;
      //public const uint CR_INVALID_RANGE_LIST = 17;
      //public const uint CR_INVALID_RANGE = 18;
      //public const uint CR_FAILURE = 19;
      //public const uint CR_NO_SUCH_LOGICAL_DEV = 20;
      //public const uint CR_CREATE_BLOCKED = 21;
      //public const uint CR_NOT_SYSTEM_VM = 22; // WIN 95 ONLY 
      //public const uint CR_REMOVE_VETOED = 23;
      //public const uint CR_APM_VETOED = 24;
      //public const uint CR_INVALID_LOAD_TYPE = 25;
      //public const uint CR_BUFFER_SMALL = 26;
      //public const uint CR_NO_ARBITRATOR = 27;
      //public const uint CR_NO_REGISTRY_HANDLE = 28;
      //public const uint CR_REGISTRY_ERROR = 29;
      //public const uint CR_INVALID_DEVICE_ID = 30;
      //public const uint CR_INVALID_DATA = 31;
      //public const uint CR_INVALID_API = 32;
      //public const uint CR_DEVLOADER_NOT_READY = 33;
      //public const uint CR_NEED_RESTART = 34;
      //public const uint CR_NO_MORE_HW_PROFILES = 35;
      //public const uint CR_DEVICE_NOT_THERE = 36;
      //public const uint CR_NO_SUCH_VALUE = 37;
      //public const uint CR_WRONG_TYPE = 38;
      //public const uint CR_INVALID_PRIORITY = 39;
      //public const uint CR_NOT_DISABLEABLE = 40;
      //public const uint CR_FREE_RESOURCES = 41;
      //public const uint CR_QUERY_VETOED = 42;
      //public const uint CR_CANT_SHARE_IRQ = 43;
      //public const uint CR_NO_DEPENDENT = 44;
      //public const uint CR_SAME_RESOURCES = 45;
      //public const uint CR_NO_SUCH_REGISTRY_KEY = 46;
      //public const uint CR_INVALID_MACHINENAME = 47; // NT ONLY 
      //public const uint CR_REMOTE_COMM_FAILURE = 48; // NT ONLY 
      //public const uint CR_MACHINE_UNAVAILABLE = 49; // NT ONLY 
      //public const uint CR_NO_CM_SERVICES = 50; // NT ONLY 
      //public const uint CR_ACCESS_DENIED = 51; // NT ONLY 
      //public const uint CR_CALL_NOT_IMPLEMENTED = 52;
      //public const uint CR_INVALID_PROPERTY = 53;
      //public const uint CR_DEVICE_INTERFACE_ACTIVE = 54;
      //public const uint CR_NO_SUCH_DEVICE_INTERFACE = 55;
      //public const uint CR_INVALID_REFERENCE_STRING = 56;
      //public const uint NUM_CR_RESULTS = 57;

      #endregion // Configuration Manager Error Codes
   }
}
