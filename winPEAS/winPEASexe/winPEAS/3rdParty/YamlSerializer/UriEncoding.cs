using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

namespace System.Yaml
{
    /// <summary>
    /// Add string class two methods: .UriEscape(), .UriUnescape()
    /// 
    /// Charset that is not escaped is represented NonUriChar member.
    /// 
    /// NonUriChar = new Regex(@"[^0-9A-Za-z\-_.!~*'()\\;/?:@&amp;=$,\[\]]");
    /// </summary>
    internal static class StringUriEncodingExtention
    {
        /// <summary>
        /// Escape the string in URI encoding format.
        /// </summary>
        /// <param name="s">String to be escaped.</param>
        /// <returns>Escaped string.</returns>
        public static string UriEscape(this string s)
        {
            return UriEncoding.Escape(s);
        }

        /// <summary>
        /// Escape the string in URI encoding format.
        /// </summary>
        /// <param name="s">String to be escaped.</param>
        /// <returns>Escaped string.</returns>
        public static string UriEscapeForTag(this string s)
        {
            return UriEncoding.EscapeForTag(s);
        }

        /// <summary>
        /// Unescape the string escaped in URI encoding format.
        /// </summary>
        /// <param name="s">String to be unescape.</param>
        /// <returns>Unescaped string.</returns>
        public static string UriUnescape(this string s)
        {
            return UriEncoding.Unescape(s);
        }
    }

    /// <summary>
    /// Escape / Unescape string in URI encoding format
    /// 
    /// Charset that is not escaped is represented NonUriChar member.
    /// 
    /// NonUriChar = new Regex(@"[^0-9A-Za-z\-_.!~*'()\\;/?:@&amp;=$,\[\]]");
    /// </summary>
    internal class UriEncoding
    {
        public static string Escape(string s)
        {
            return NonUriChar.Replace(s, m => {
                var c = m.Value[0];
                return ( c == ' ' ) ? "+" :
                       ( c < 0x80 ) ? IntToHex(c) :
                       ( c < 0x0800 ) ? IntToHex(( ( c >> 6 ) & 0x1f ) + 0xc0, ( c & 0x3f ) + 0x80) :
                       IntToHex(( ( c >> 12 ) & 0x0f ) + 0xe0, ( ( c >> 6 ) & 0x3f ) + 0x80, ( c & 0x3f ) + 0x80);
            }
            );
        }
        static Regex NonUriChar = new Regex(@"[^0-9A-Za-z\-_.!~*'()\\;/?:@&=$,\[\]]");

        public static string EscapeForTag(string s)
        {
            return NonTagChar.Replace(s, m => {
                var c = m.Value[0];
                return ( c == ' ' ) ? "+" :
                       ( c < 0x80 ) ? IntToHex(c) :
                       ( c < 0x0800 ) ? IntToHex(( ( c >> 6 ) & 0x1f ) + 0xc0, ( c & 0x3f ) + 0x80) :
                       IntToHex(( ( c >> 12 ) & 0x0f ) + 0xe0, ( ( c >> 6 ) & 0x3f ) + 0x80, ( c & 0x3f ) + 0x80);
            }
            );
        }
        static Regex NonTagChar = new Regex(@"[^0-9A-Za-z\-_.!~*'()\\;/?:@&=$]");

        static char[] intToHex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        static string IntToHex(int c)
        {
            return new string(new char[] {
                '%', intToHex[c>>4], intToHex[c&0x0f], 
            });
        }
        static string IntToHex(int c1, int c2)
        {
            return new string(new char[] {
                '%', intToHex[c1>>4], intToHex[c1&0x0f], 
                '%', intToHex[c2>>4], intToHex[c2&0x0f], 
            });
        }
        static string IntToHex(int c1, int c2, int c3)
        {
            return new string(new char[] {
                '%', intToHex[c1>>4], intToHex[c1&0x0f], 
                '%', intToHex[c2>>4], intToHex[c2&0x0f], 
                '%', intToHex[c3>>4], intToHex[c3&0x0f], 
            });
        }

        public static string Unescape(string s)
        {
            s = s.Replace('+', ' ');

            var result = new StringBuilder();
            var p = 0;
            int pp;
            while ( ( pp = s.IndexOf('%', p) ) >= 0 ) {
                result.Append(s.Substring(p, pp - p));
                p = pp;
                var c0 = ( HexToInt(s[p + 1]) << 4 ) + HexToInt(s[p + 2]);
                if ( c0 < 0x80 ) {
                    p += 3;
                    result.Append((char)c0);
                    continue;
                }
                var c1 = ( HexToInt(s[p + 4]) << 4 ) + HexToInt(s[p + 5]);
                if ( c0 < 0xe0 ) {
                    p += 6;
                    var c = (char)( ( ( c0 & 0x1f ) << 6 ) + ( c1 & 0x7f ) );
                    result.Append(c);
                    continue;
                }
                var c2 = ( HexToInt(s[p + 7]) << 4 ) + HexToInt(s[p + 8]);
                if ( c0 < 0xf1 ) {
                    p += 9;
                    var c = (char)( ( ( c0 & 0x0f ) << 12 ) + ( ( c1 & 0x7f ) << 6 ) + ( c2 & 0x7f ) );
                    result.Append(c);
                    continue;
                }
                throw new FormatException("Charcorde over 0xffff is not supported");
            }
            return result.Append(s.Substring(p)).ToString();
        }
        static int HexToInt(char c)
        {
            return c <= '9' ? c - '0' : c < 'Z' ? c - 'A' + 10 : c - 'a' + 10;

        }
    }

}
