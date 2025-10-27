using System;
using System.Text;

namespace winPEAS.Helpers
{
    // String obfuscation helper to evade static analysis
    internal static class StringObfuscator
    {
        // XOR-based string deobfuscation
        private static string D(byte[] data, byte key)
        {
            var result = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (char)(data[i] ^ key);
            }
            return new string(result);
        }

        // Build strings from char arrays to avoid static detection
        private static string FromChars(params char[] chars)
        {
            return new string(chars);
        }

        // Concatenate string parts to break up patterns
        private static string Cat(params string[] parts)
        {
            return string.Concat(parts);
        }

        // Common sensitive strings - obfuscated
        public static string Mimikatz => D(new byte[] { 0x2c, 0x30, 0x2c, 0x30, 0x28, 0x26, 0x3d, 0x3b }, 0x41);
        public static string Lsass => Cat("ls", "a", "ss");
        public static string Credential => FromChars('C', 'r', 'e', 'd', 'e', 'n', 't', 'i', 'a', 'l');
        public static string Credentials => Credential + "s";
        public static string Password => Cat("Pass", "word");
        public static string Passwords => Password + "s";
        public static string Token => FromChars('T', 'o', 'k', 'e', 'n');
        public static string Tokens => Token + "s";
        public static string Hash => FromChars('H', 'a', 's', 'h');
        public static string Hashes => Hash + "es";
        public static string Inject => Cat("Inj", "ect");
        public static string Injection => Inject + "ion";

        // Registry/File paths
        public static string Cpassword => FromChars('c', 'p', 'a', 's', 's', 'w', 'o', 'r', 'd');
        public static string VncPassword => Cat("VNC", " ", Password);
        public static string GitCredentials => Cat(".git-", Credential + "s");

        // Process/Service names
        public static string ProcDump => Cat("proc", "dump");
        public static string RunPE => Cat("Run", "PE");

        // Methods to generate common patterns dynamically
        public static string GetVirtualString()
        {
            return new string(new[] { 'V', 'I', 'R', 'T', 'U', 'A', 'L' });
        }

        public static string GetVMWare()
        {
            return Cat("vm", "ware");
        }

        public static string GetVirtualBox()
        {
            return Cat("Virtual", "Box");
        }

        // Generic obfuscation method for any string
        public static byte[] Obfuscate(string input, byte key)
        {
            var result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)(input[i] ^ key);
            }
            return result;
        }

        public static string Deobfuscate(byte[] data, byte key)
        {
            return D(data, key);
        }
    }
}
