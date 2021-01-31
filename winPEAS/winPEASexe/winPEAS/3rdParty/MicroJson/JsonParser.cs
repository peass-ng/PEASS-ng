using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace winPEAS._3rdParty.MicroJson
{
    //
    // JsonParser.cs
    //
    // Author:
    //       Michael Ganss <michael@ganss.org>
    //
    // Copyright (c) 2011 Michael Ganss
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.
    namespace MicroJson
    {
#pragma warning disable 1591
        public class ParserException : Exception
        {
            public int Line { get; private set; }
            public int Column { get; private set; }

            public ParserException(string msg, int line, int col)
                : base(msg)
            {
                Line = line;
                Column = col;
            }
        }

        public interface ILogger
        {
            void WriteLine(string message, params object[] arguments);
        }

        public class TextWriterLogger : ILogger
        {
            public TextWriter Writer { get; set; }

            public TextWriterLogger(TextWriter writer)
            {
                Writer = writer;
            }

            public void WriteLine(string message, params object[] arguments)
            {
                if (arguments != null && arguments.Length > 0)
                    Writer.WriteLine(message, arguments);
                else
                    Writer.WriteLine(message);
            }
        }
#pragma warning restore 1591

        /// <summary>
        /// Parses JSON into POCOs.
        /// </summary>
        public class JsonParser
        {
            string Input { get; set; }
            int InputLength { get; set; }
            int Pos { get; set; }
            int Line { get; set; }
            int Col { get; set; }

            /// <summary>
            /// Gets or sets the logger.
            /// </summary>
            /// <value>
            /// The logger.
            /// </value>
            public ILogger Logger { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to collect line info during parsing.
            /// </summary>
            /// <value>
            /// <c>true</c> if line info should be collected during parsing; otherwise, <c>false</c>.
            /// </value>
            public bool CollectLineInfo { get; set; }

            /// <summary>
            /// Parse the specified JSON text.
            /// </summary>
            /// <param name='text'>
            /// The JSON text to parse.
            /// </param>
            public object Parse(string text)
            {
                if (text == null)
                    throw BuildParserException("input is null");

                Input = text;
                InputLength = text.Length;
                Pos = 0;
                Line = 1;
                Col = 1;

                var o = Value();

                SkipWhitespace();

                if (Pos != InputLength)
                    throw BuildParserException("extra characters at end");

                return o;
            }

            private void WriteLineLog(string msg, params object[] args)
            {
                if (Logger != null)
                {
                    Logger.WriteLine(msg, args);
                }
            }

            private ParserException BuildParserException(string msg)
            {
                if (CollectLineInfo)
                {
                    return new ParserException(string.Format(CultureInfo.InvariantCulture, "Parse error: {0} at line {1}, column {2}.", msg, Line, Col), Line, Col);
                }
                else
                {
                    return new ParserException("Parse error: " + msg + ".", 0, 0);
                }
            }

            private void AdvanceInput(int n)
            {
                if (CollectLineInfo)
                {
                    for (int i = Pos; i < Pos + n; i++)
                    {
                        var c = Input[i];

                        if (c == '\n')
                        {
                            Line++;
                            Col = 1;
                        }
                        else
                        {
                            Col++;
                        }
                    }
                }

                Pos += n;
            }

            private string Accept(string s)
            {
                var len = s.Length;

                if (Pos + len > InputLength)
                {
                    return null;
                }

                if (Input.IndexOf(s, Pos, len, StringComparison.Ordinal) != -1)
                {
                    var match = Input.Substring(Pos, len);
                    AdvanceInput(len);
                    return match;
                }

                return null;
            }

            private char Expect(char c)
            {
                if (Pos >= InputLength || Input[Pos] != c)
                {
                    throw BuildParserException("expected '" + c + "'");
                }

                AdvanceInput(1);

                return c;
            }

            private object Value()
            {
                SkipWhitespace();

                if (Pos >= InputLength)
                {
                    throw BuildParserException("input contains no value");
                }

                var nextChar = Input[Pos];

                if (nextChar == '"')
                {
                    AdvanceInput(1);
                    return String();
                }
                else if (nextChar == '[')
                {
                    AdvanceInput(1);
                    return List();
                }
                else if (nextChar == '{')
                {
                    AdvanceInput(1);
                    return Dictionary();
                }
                else if (char.IsDigit(nextChar) || nextChar == '-')
                {
                    return Number();
                }
                else
                {
                    return Literal();
                }
            }

            private object Number()
            {
                int currentPos = Pos;
                bool dotSeen = false;

                Accept(c => c == '-', ref currentPos);
                ExpectDigits(ref currentPos);

                if (Accept(c => c == '.', ref currentPos))
                {
                    dotSeen = true;
                    ExpectDigits(ref currentPos);
                }

                if (Accept(c => (c == 'e' || c == 'E'), ref currentPos))
                {
                    Accept(c => (c == '-' || c == '+'), ref currentPos);
                    ExpectDigits(ref currentPos);
                }

                var len = currentPos - Pos;
                var num = Input.Substring(Pos, len);

                if (dotSeen)
                {
                    decimal d;
                    if (decimal.TryParse(num, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out d))
                    {
                        WriteLineLog("decimal: {0}", d);
                        AdvanceInput(len);
                        return d;
                    }
                    else
                    {
                        double dbl;
                        if (double.TryParse(num, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out dbl))
                        {
                            WriteLineLog("double: {0}", dbl);
                            AdvanceInput(len);
                            return dbl;
                        }

                        throw BuildParserException("cannot parse decimal number");
                    }
                }
                else
                {
                    int i;
                    if (int.TryParse(num, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out i))
                    {
                        WriteLineLog("int: {0}", i);
                        AdvanceInput(len);
                        return i;
                    }
                    else
                    {
                        long l;
                        if (long.TryParse(num, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out l))
                        {
                            WriteLineLog("long: {0}", l);
                            AdvanceInput(len);
                            return l;
                        }

                        throw BuildParserException("cannot parse integer number");
                    }
                }
            }

            private bool Accept(Predicate<char> predicate, ref int pos)
            {
                if (pos < InputLength && predicate(Input[pos]))
                {
                    pos++;
                    return true;
                }

                return false;
            }

            private void ExpectDigits(ref int pos)
            {
                int start = pos;
                while (pos < InputLength && char.IsDigit(Input[pos])) pos++;
                if (start == pos) throw BuildParserException("not a number");
            }

            private string String()
            {
                int currentPos = Pos;
                StringBuilder sb = new StringBuilder();

                while (true)
                {
                    if (currentPos >= InputLength)
                    {
                        throw BuildParserException("unterminated string");
                    }

                    var c = Input[currentPos];

                    if (c == '"')
                    {
                        var len = currentPos - Pos;
                        AdvanceInput(len + 1);
                        WriteLineLog("string: {0}", sb);
                        return sb.ToString();
                    }
                    else if (c == '\\')
                    {
                        currentPos++;

                        if (currentPos >= InputLength)
                        {
                            throw BuildParserException("unterminated escape sequence string");
                        }

                        c = Input[currentPos];

                        switch (c)
                        {
                            case '"':
                            case '/':
                            case '\\':
                                sb.Append(c);
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                currentPos += 4;
                                if (currentPos >= InputLength)
                                    throw BuildParserException("unterminated unicode escape in string");
                                else
                                {
                                    int u;
                                    if (!int.TryParse(Input.Substring(currentPos - 3, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out u))
                                        throw BuildParserException("not a well-formed unicode escape sequence in string");
                                    sb.Append((char)u);
                                }
                                break;
                            default:
                                throw BuildParserException("unknown escape sequence in string");
                        }
                    }
                    else if ((int)c < 0x20)
                    {
                        throw BuildParserException("control character in string");
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    currentPos++;
                }
            }

            private object Literal()
            {
                if (Accept("true") != null)
                {
                    WriteLineLog("bool: true");
                    return true;
                }

                if (Accept("false") != null)
                {
                    WriteLineLog("bool: false");
                    return false;
                }

                if (Accept("null") != null)
                {
                    WriteLineLog("null");
                    return null;
                }

                throw BuildParserException("unknown token");
            }

            private IList<object> List()
            {
                WriteLineLog("list: [");

                List<object> list = new List<object>();

                SkipWhitespace();
                if (IsNext(']'))
                {
                    AdvanceInput(1); return list;
                }

                object obj = null;
                do
                {
                    SkipWhitespace();
                    obj = Value();
                    if (obj != null)
                    {
                        list.Add(obj);
                        SkipWhitespace();
                        if (IsNext(']')) break;
                        Expect(',');
                    }
                }
                while (obj != null);

                Expect(']');

                WriteLineLog("]");

                return list;
            }

            private IDictionary<string, object> Dictionary()
            {
                WriteLineLog("Dictionary: {");

                Dictionary<string, object> dict = new Dictionary<string, object>();

                SkipWhitespace();
                if (IsNext('}'))
                {
                    AdvanceInput(1); return dict;
                }

                KeyValuePair<string, object>? kvp = null;
                do
                {
                    SkipWhitespace();

                    kvp = KeyValuePair();

                    if (kvp.HasValue)
                    {
                        dict[kvp.Value.Key] = kvp.Value.Value;
                    }

                    SkipWhitespace();
                    if (IsNext('}')) break;
                    Expect(',');
                }
                while (kvp != null);

                Expect('}');

                WriteLineLog("}");

                return dict;
            }

            private KeyValuePair<string, object>? KeyValuePair()
            {
                Expect('"');

                var key = String();

                SkipWhitespace();

                Expect(':');

                var obj = Value();

                return new KeyValuePair<string, object>(key, obj);
            }

            private void SkipWhitespace()
            {
                int n = Pos;
                while (IsWhiteSpace(n)) n++;
                if (n != Pos)
                {
                    AdvanceInput(n - Pos);
                }
            }

            private bool IsWhiteSpace(int n)
            {
                if (n >= InputLength) return false;
                char c = Input[n];
                return c == ' ' || c == '\t' || c == '\r' || c == '\n';
            }

            private bool IsNext(char c)
            {
                return Pos < InputLength && Input[Pos] == c;
            }
        }
    }

}
