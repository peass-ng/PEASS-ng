using System;
using System.Runtime.InteropServices;

namespace winPEAS.KnownFileCreds.Kerberos
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_QUERY_TKT_CACHE_REQUEST
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public LUID LogonId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_STRING_IN
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public string Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LSA_STRING_OUT
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_LOGON_SESSION_DATA
    {
        public UInt32 Size;
        public LUID LoginID;
        public LSA_STRING_OUT Username;
        public LSA_STRING_OUT LoginDomain;
        public LSA_STRING_OUT AuthenticationPackage;
        public UInt32 LogonType;
        public UInt32 Session;
        public IntPtr PSiD;
        public UInt64 LoginTime;
        public LSA_STRING_OUT LogonServer;
        public LSA_STRING_OUT DnsDomainName;
        public LSA_STRING_OUT Upn;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_QUERY_TKT_CACHE_RESPONSE
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public int CountOfTickets;
        // public KERB_TICKET_CACHE_INFO[] Tickets;
        public IntPtr Tickets;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_TICKET_CACHE_INFO
    {
        public LSA_STRING_OUT ServerName;
        public LSA_STRING_OUT RealmName;
        public Int64 StartTime;
        public Int64 EndTime;
        public Int64 RenewTime;
        public Int32 EncryptionType;
        public UInt32 TicketFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_RETRIEVE_TKT_RESPONSE
    {
        public KERB_EXTERNAL_TICKET Ticket;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_CRYPTO_KEY
    {
        public Int32 KeyType;
        public Int32 Length;
        public IntPtr Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_EXTERNAL_TICKET
    {
        public IntPtr ServiceName;
        public IntPtr TargetName;
        public IntPtr ClientName;
        public LSA_STRING_OUT DomainName;
        public LSA_STRING_OUT TargetDomainName;
        public LSA_STRING_OUT AltTargetDomainName;
        public KERB_CRYPTO_KEY SessionKey;
        public UInt32 TicketFlags;
        public UInt32 Flags;
        public Int64 KeyExpirationTime;
        public Int64 StartTime;
        public Int64 EndTime;
        public Int64 RenewUntil;
        public Int64 TimeSkew;
        public Int32 EncodedTicketSize;
        public IntPtr EncodedTicket;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_RETRIEVE_TKT_REQUEST
    {
        public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
        public LUID LogonId;
        public LSA_STRING_IN TargetName;
        public UInt64 TicketFlags;
        public KERB_CACHE_OPTIONS CacheOptions;
        public Int64 EncryptionType;
        public SECURITY_HANDLE CredentialsHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_HANDLE
    {
        public IntPtr LowPart;
        public IntPtr HighPart;
        public SECURITY_HANDLE(int dummy)
        {
            LowPart = HighPart = IntPtr.Zero;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct KERB_EXTERNAL_NAME
    {
        public Int16 NameType;
        public UInt16 NameCount;
        public LSA_STRING_OUT Names;
    }
}
