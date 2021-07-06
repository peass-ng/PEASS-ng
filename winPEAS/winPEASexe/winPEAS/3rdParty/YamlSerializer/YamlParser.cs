using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;
using System.Diagnostics;

namespace System.Yaml
{
    /// <summary>
    /// <para>A text parser for<br/>
    /// YAML Ain’t Markup Language (YAML™) Version 1.2<br/>
    /// 3rd Edition (2009-07-21)<br/>
    /// http://yaml.org/spec/1.2/spec.html </para>
    /// 
    /// <para>This class parse a YAML document and compose representing <see cref="YamlNode"/> graph.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// string yaml = LoadYamlSource();
    /// YamlParser parser = new YamlParser();
    /// Node[] result = null;
    /// try {
    ///     result = parser.Parse(yaml);
    ///     ...
    ///     // you can reuse parser as many times you want
    ///     ...
    ///     
    /// } catch( ParseErrorException e ) {
    ///     MessageBox.Show(e.Message);
    /// }
    /// if(result != null) {
    ///     ...
    /// 
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>Currently, this parser violates the YAML 1.2 specification in the following points.</para>
    /// <para>- line breaks are not normalized.</para>
    /// <para>- omission of the final line break is allowed in plain / literal / folded text.</para>
    /// <para>- ':' followed by ns-indicator is excluded from ns-plain-char.</para>
    /// </remarks>
    internal class YamlParser: Parser<YamlParser.State>
    {
        /// <summary>
        /// Initialize a YAML parser.
        /// </summary>
        public YamlParser()
        {
            Anchors = new AnchorDictionary(Error);

            TagPrefixes = new YamlTagPrefixes(Error);

            Warnings = new List<string>();
        }

        YamlConfig config;
        List<YamlNode> ParseResult = new List<YamlNode>();
        /// <summary>
        /// Parse YAML text and returns a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text to be parsed.</param>
        /// <returns>A list of <see cref="YamlNode"/> parsed from the given text</returns>
        public List<YamlNode> Parse(string yaml)
        {
            return Parse(yaml, YamlNode.DefaultConfig);
        }
        /// <summary>
        /// Parse YAML text and returns a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text to be parsed.</param>
        /// <param name="config"><see cref="YamlConfig">YAML Configuration</see> to be used in parsing.</param>
        /// <returns>A list of <see cref="YamlNode"/> parsed from the given text</returns>
        public List<YamlNode> Parse(string yaml, YamlConfig config)
        {
            this.config = config;
            Warnings.Clear();
            ParseResult.Clear();
            AlreadyWarnedChars.Clear();
            if ( base.Parse(lYamlStream, yaml + "\0") ) // '\0' = guard char
                return ParseResult;
            return new List<YamlNode>();
        }

        internal bool IsValidPlainText(string plain, YamlConfig config)
        {
            this.config = config;
            Warnings.Clear();
            ParseResult.Clear();
            AlreadyWarnedChars.Clear();
            return base.Parse(() => nsPlain(0, Context.BlockKey) && EndOfFile(), plain + "\0"); // '\0' = guard char
        }

        #region Warnings
        /// <summary>
        /// Warnings that are made while parsing a YAML text.
        /// This property is cleared by new call for <see cref="Parse(string)"/> method.
        /// </summary>
        public List<string> Warnings { get; private set; }
        Dictionary<string, bool> WarningAdded = new Dictionary<string, bool>();
        /// <summary>
        /// Add message in <see cref="Warnings"/> property.
        /// </summary>
        /// <param name="message"></param>
        protected override void StoreWarning(string message)
        {
            // Warnings will not be rewound.
            // We have to avoid same warnings from being repeatedly reported.
            if ( !WarningAdded.ContainsKey(message) ) {
                Warnings.Add(message);
                WarningAdded[message] = true;
            }
        }

        /// <summary>
        /// Invoked when unknown directive is found in YAML document.
        /// </summary>
        /// <param name="name">Name of the directive</param>
        /// <param name="args">Parameters for the directive</param>
        protected virtual void ReservedDirective(string name, params string[] args)
        {
            Warning("Custom directive %{0} was ignored", name);
        }
        /// <summary>
        /// Invoked when YAML directive is found in YAML document.
        /// </summary>
        /// <param name="version">Given version</param>
        protected virtual void YamlDirective(string version)
        {
            if ( version != "1.2" )
                Warning("YAML version %{0} was specified but ignored", version);
        }
        Dictionary<char, bool> AlreadyWarnedChars = new Dictionary<char, bool>();
        void WarnIfCharWasBreakInYAML1_1()
        {
            if ( Charsets.nbCharWithWarning(text[p]) && !AlreadyWarnedChars.ContainsKey(text[p]) ) {
                Warning("{0} is treated as non-break character unlike YAML 1.1",
                    text[p] < 0x100 ? string.Format("\\x{0:x2}", (int)text[p]) :
                                      string.Format("\\u{0:x4}", (int)text[p])
                    );
                AlreadyWarnedChars.Add(text[p], true);
            }
        }
        #endregion

        #region Debug.Assert
#if DEBUG
        /// <summary>
        /// Since System.Diagnostics.Debug.Assert is too anoying while development,
        /// this class temporarily override Debug.Assert action.
        /// </summary>
        private class Debug
        {
            public static void Assert(bool condition)
            {
                Assert(condition, "");
            }
            public static void Assert(bool condition, string message)
            {
                if ( !condition )
                    throw new Exception("assertion failed: " + message);
            }
        }
        #endif
        #endregion

        #region Status / Value
        /// <summary>
        /// additional fields to be rewound
        /// </summary>
        public struct State
        {
            /// <summary>
            /// tag for the next value (will be cleared when the next value is created)
            /// </summary>
            public string tag;
            /// <summary>
            /// anchor for the next value (will be cleared when the next value is created)
            /// </summary>
            public string anchor;   
            /// <summary>
            /// current value
            /// </summary>
            public YamlNode value;      
            /// <summary>
            /// anchor rewinding position
            /// </summary>
            public int anchor_depth;
        }

        /// <summary>
        /// rewinding action
        /// </summary>
        protected override void Rewind()
        {
            Anchors.RewindDeapth = state.anchor_depth;
        }

        bool SetValue(YamlNode v)
        {
            if ( state.value != null && v != null )
                throw new Exception();
            state.value = v;
            v.OnLoaded();
            return true;
        }
        YamlNode GetValue()
        {
            var v = state.value;
            state.value = null;
            return v;
        }
        #endregion

        YamlTagPrefixes TagPrefixes;

        /// <summary>
        /// set status.tag with tag resolution
        /// </summary>
        /// <param name="tag_handle"></param>
        /// <param name="tag_suffix"></param>
        /// <returns></returns>
        private bool SetTag(string tag_handle, string tag_suffix)
        {
            return SetTag(TagPrefixes.Resolve(tag_handle, tag_suffix));
        }
        /// <summary>
        /// set status.tag with verbatim tag value
        /// </summary>
        /// <param name="verbatim_tag">verbatim tag</param>
        /// <returns></returns>
        private bool SetTag(string verbatim_tag)
        {
            Debug.Assert(verbatim_tag != "");
            // validate tag
            if ( verbatim_tag.StartsWith("!") ) {
                if ( verbatim_tag == "!" )
                    Error("Empty local tag was found.");
            } else {
                if ( !TagValidator.IsValid(verbatim_tag) )
                    Warning("Invalid global tag name '{0}' (c.f. RFC 4151) found", verbatim_tag);
            }
            state.tag = verbatim_tag;
            return true;
        }
        YamlTagValidator TagValidator = new YamlTagValidator();

        AnchorDictionary Anchors;
        private void RegisterAnchorFor(YamlNode value)
        {
            if ( state.anchor != null ) {
                Anchors.Add(state.anchor, value);
                state.anchor = null;
                state.anchor_depth = Anchors.RewindDeapth;
            }
        }

        /// <summary>
        /// Used when the parser resolves a tag for a scalar node from its value.
        /// 
        /// New resolution rules can be add before calling <see cref="Parse(string)"/> method.
        /// </summary>
        private void AutoDetectTag(string from_style)
        {
            if ( from_style != null )
                from_style = YamlNode.ExpandTag(from_style);

            if ( state.tag != null )
                return;

            if ( from_style == null )
                from_style = config.TagResolver.Resolve(stringValue.ToString());

            if ( from_style != null )
                state.tag = from_style;
            return;
        }
        private YamlScalar CreateScalar(string auto_detected_tag, Position pos)
        {
            AutoDetectTag(auto_detected_tag);
            if ( state.tag == null || state.tag == "" /* ! was specified */ )
                state.tag = YamlNode.DefaultTagPrefix + "str";
            var value = new YamlScalar(state.tag, stringValue.ToString());
            value.Raw = pos.Raw;
            value.Column = pos.Column;
            stringValue.Length = 0;
            RegisterAnchorFor(value);
            state.tag = null;
            return value;
        }
        private YamlSequence CreateSequence(Position pos)
        {
            if ( state.tag == null || state.tag == "" /* ! was specified */ )
                state.tag = YamlNode.DefaultTagPrefix + "seq";
            var seq = new YamlSequence();
            seq.Tag = state.tag;
            seq.Raw = pos.Raw;
            seq.Column = pos.Column;
            RegisterAnchorFor(seq);
            state.tag = null;
            return seq;
        }
        private YamlMapping CreateMapping(Position pos)
        {
            if ( state.tag == null || state.tag == "" /* ! was specified */ )
                state.tag = YamlNode.DefaultTagPrefix + "map";
            var map = new YamlMapping();
            map.Tag = state.tag;
            map.Raw = pos.Raw;
            map.Column = pos.Column;
            RegisterAnchorFor(map);
            state.tag = null;
            return map;
        }

        #region The BNF syntax for YAML 1.2

        #region Context
        enum Context
        {
            BlockIn,
            BlockOut,
            FlowIn,
            FlowOut,
            BlockKey,
            FlowKey,
            Folded,
        }
        #endregion

        #region Chapter 5. Character Set
        class Charsets 
        {
            static Charsets()
            {
                // [1]
                cPrintable = Charset(c =>
                    /*  ( 0x10000 < c && c < 0x110000 ) || */
                    ( 0xe000 <= c && c <= 0xfffd ) ||
                    ( 0xa0 <= c && c <= 0xd7ff ) ||
                    c == 0x85 ||
                    ( 0x20 <= c && c <= 0x7e ) ||
                    c == 0x0d ||
                    c == 0x0a ||
                    c == 0x09
                );
                // [22]
                cIndicator = Charset(c =>
                    c < 0x100 &&
                    "-?:,[]{}#&*!|>'\"%@`".Contains(c)
                    );
                // [23]
                cFlowIndicator = Charset(c =>
                    c < 0x100 &&
                    ",[]{}".Contains(c)
                    );
                nsDecDigit = Charset(c =>
                    c < 0x100 &&
                    ( '0' <= c && c <= '9' )
                    );
                nsHexDigit = Charset(c =>
                    c < 0x100 && (
                        nsDecDigit(c) ||
                        ( 'A' <= c && c <= 'F' ) ||
                        ( 'a' <= c && c <= 'f' )
                        )
                    );
                nbChar = Charset(c =>
                    //  ( 0x10000 < c && c < 0x110000 ) || 
                    ( 0xe000 <= c && c <= 0xfffd && c != 0xFEFF ) ||
                    ( 0xa0 <= c && c <= 0xd7ff ) ||
                    c == 0x85 ||
                    ( 0x20 <= c && c <= 0x7e ) ||
                        //  c == 0x0d ||
                        //  c == 0x0a ||
                    c == 0x09
                );
                nbCharWithWarning = Charset(c =>
                    c == 0x2029 ||  // paragraph separator
                    c == 0x2028 ||  // line separator
                    c == 0x85 ||    // next line
                    c == 0x0c       // form feed
                    );
                sSpace = c => c == ' ';
                sWhite = c => c == ' ' || c == '\t';
                nsChar = Charset(c =>
                    // nbChar(c) && !sWhite(c)
                    //  ( 0x10000 < c && c < 0x110000 ) || 
                    ( 0xe000 <= c && c <= 0xfffd && c != 0xFEFF ) ||
                    ( 0xa0 <= c && c <= 0xd7ff ) ||
                    c == 0x85 ||
                    ( 0x21 <= c && c <= 0x7e )
                    //  c == 0x0d ||
                    //  c == 0x0a ||
                    //  c == 0x09
                    );
                nsAsciiLetter = Charset(c =>
                    c < 0x100 && (
                        ( 'A' <= c && c <= 'Z' ) ||
                        ( 'a' <= c && c <= 'z' )
                        )
                    );
                nsWordChar = Charset(c =>
                    c < 0x100 && (
                        nsDecDigit(c) ||
                        nsAsciiLetter(c) ||
                        c == '-'
                        )
                    );
                nsUriCharSub = Charset(c =>
                    c < 0x100 && (
                        nsWordChar(c) ||
                        @"#;/?:@&=$,_.!~*'()[]".Contains(c)
                        )
                    );
                nsTagCharSub = Charset(c =>
                    c < 0x100 &&
                    nsUriCharSub(c) && !( c == '!' || cFlowIndicator(c) )
                    );
                nsAnchorChar = Charset(c =>
                    nsChar(c) && !cFlowIndicator(c)
                    );
                nsPlainSafeOut = c => nsChar(c);
                nsPlainSafeIn = Charset(c =>
                    nsPlainSafeOut(c) && !cFlowIndicator(c)
                    );
                nsPlainFirstSub = Charset(c =>
                    nsChar(c) && !cIndicator(c)
                    );
            }

            public static Func<char, bool> cPrintable; // [1] 
            public static bool nbJson(char c) // [2] 
            {
                return c == 0x09 || ( 0x20 <= c /* && c<=0x10ffff */ );
            }
            public static bool cByteOrdermark(char c) // [3] 
            {
                return c == '\uFEFF';
            }
            /// <summary>
            /// [22]
            /// </summary>
            public static Func<char, bool> cIndicator;
            /// <summary>
            /// [23]
            /// </summary>
            public static Func<char, bool> cFlowIndicator;
            public static Func<char, bool> nsDecDigit;
            public static Func<char, bool> nsHexDigit;
            public static Func<char, bool> nsAsciiLetter;
            public static Func<char, bool> nsWordChar;
            public static Func<char, bool> sSpace;
            public static Func<char, bool> sWhite;
            public static Func<char, bool> nbChar;
            public static Func<char, bool> nbCharWithWarning;
            public static Func<char, bool> nsChar;
            public static Func<char, bool> nsUriCharSub;
            public static Func<char, bool> nsTagCharSub;
            public static Func<char, bool> nsAnchorChar;
            public static Func<char, bool> nsPlainSafeIn;
            public static Func<char, bool> nsPlainSafeOut;
            public static Func<char, bool> nsPlainFirstSub;
            public static bool bChar(char c) { return c == '\n' || c == '\r'; }
        }

        bool nbChar() // [27] 
        {
            WarnIfCharWasBreakInYAML1_1();
            if ( Charsets.nbChar(text[p]) ) {
                p++;
                return true;
            }
            return false;
        }
        bool bBreak() // [28] 
        {   // \r\n? | \n 
            if ( text[p] == '\r' ) {
                p++;
                if ( text[p] == '\n' )
                    p++;
                return true;
            }
            if ( text[p] == '\n' ) {
                p++;
                return true;
            }
            return false;
        }
        bool bAsLineFeed() // [29] 
        {
            if ( config.NormalizeLineBreaks ) {
                if ( bBreak() ) {
                    stringValue.Append(config.LineBreakForInput);
                    return true;
                }
                return false;
            } else {
                return Save(() => bBreak(), s => stringValue.Append(s));
            }
        }
        bool bNonContent() // [30] 
        {
            return bBreak();
        }
        bool sWhite() // [33] 
        {
            if ( text[p] == ' ' || text[p] == '\t' ) {
                p++;
                return true;
            }
            return false;
        }
        bool Repeat_sWhiteAsString()
        {
            var start = p;
            while ( Charsets.sWhite(text[p]) )
                stringValue.Append(text[p++]);
            return true;
        }
        bool nsChar() // [34] 
        {
            WarnIfCharWasBreakInYAML1_1();
            if ( Charsets.nsChar(text[p]) ) {
                p++;
                return true;
            }
            return false;
        }
        bool nsUriChar() // [39] 
        {
            if ( Charsets.nsUriCharSub(text[p]) ) {
                stringValue.Append(text[p++]);
                return true;
            }
            return nsUriEscapedChar();
        }
        bool nsUriEscapedChar()
        {
            if ( text[p] == '+' ) {
                stringValue.Append(' ');
                p++;
                return true;
            }
            if ( text[p] != '%' )
                return false;
            // http://www.cresc.co.jp/tech/java/URLencoding/JavaScript_URLEncoding.htm
            int v1 = -1, v2 = -1, v3 = -1, v4 = -1;
            ErrorUnless(
                text[p] == '%' && HexValue(p + 1, out v1) &&
                ( v1 < 0x80 || ( text[p + 3] == '%' && HexValue(p + 4, out v2) ) ) &&
                ( v1 < 0xe0 || ( text[p + 6] == '%' && HexValue(p + 7, out v3) ) ) &&
                ( v1 < 0xf1 || ( text[p + 9] == '%' && HexValue(p + 10, out v4) ) ),
                "Invalid URI escape."
                );
            if ( v2 == -1 ) { // 1 byte code
                stringValue.Append(
                    (char)v1
                    );
                p += 3;
                return true;
            }
            if ( v3 == -1 ) {
                stringValue.Append(
                    (char)( ( ( v1 & 0x1f ) << 6 ) + ( v2 & 0x7f ) )
                    );
                p += 6;
                return true;
            }
            if ( v4 == -1 ) {
                stringValue.Append(
                    (char)( ( ( v1 & 0x0f ) << 12 ) + ( ( v2 & 0x7f ) << 6 ) + ( v3 & 0x7f ) )
                    );
                p += 9;
                return true;
            }
            stringValue.Append(
                (char)( ( ( v1 & 0x07 ) << 18 ) + ( ( v2 & 0x7f ) << 12 ) + ( ( v3 & 0x7f ) << 6 ) + ( v4 & 0x7f ) )
                );
            p += 12;
            return true;
        }
        bool nsTagChar() // [40] 
        {
            if ( Charsets.nsTagCharSub(text[p]) ) {
                stringValue.Append(text[p++]);
                return true;
            }
            return nsUriEscapedChar();
        }
        bool c_nsEscChar() // [62] 
        {
            if ( text[p] != '\\' ) 
                return false;

            char c = '\0';
            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            int v4 = 0;
            switch ( text[p + 1] ) {
            case '0':
                c = '\0';
                break;
            case 'a':
                c = '\a';
                break;
            case 'b':
                c = '\b';
                break;
            case 't':
            case '\x09':
                c = '\t';
                break;
            case 'n':
                c = '\n';
                break;
            case 'v':
                c = '\v';
                break;
            case 'f':
                c = '\f';
                break;
            case 'r':
                c = '\r';
                break;
            case 'e':
                c = '\x1b';
                break;
            case ' ':
                c = ' ';
                break;
            case '"':
                c = '"';
                break;
            case '/':
                c = '/';
                break;
            case '\\':
                c = '\\';
                break;
            case 'N':
                c = '\x85';
                break;
            case '_':
                c = '\xa0';
                break;
            case 'L':
                c = '\u2028';
                break;
            case 'P':
                c = '\u2029';
                break;
            case 'x':
                if(!HexValue(p + 2, out v1))
                    InvalidEscapeSequence(4);
                c = (char)v1;
                p+=2;
                break;
            case 'u':
                if(!(HexValue(p + 2, out v1) && HexValue(p + 4, out v2)))
                    InvalidEscapeSequence(6);
                c = (char)( ( v1 << 8 ) + v2 );
                p+=4;
                break;
            case 'U':
                if(!(HexValue(p + 2, out v1) && HexValue(p + 4, out v2) && HexValue(p + 6, out v3) && HexValue(p + 8, out v4)))
                    InvalidEscapeSequence(10);
                c = (char)( ( v1 << 24 ) + ( v2 << 16 ) + ( v3 << 8 ) + v4 );
                p += 8;
                break;
            default:
                // escaped line break or error
                if ( text[p + 1] != '\n' && text[p + 1] != '\r' )
                    InvalidEscapeSequence(2);
                return false;
            }
            p += 2;
            stringValue.Append(c);
            return true;
        }
        void InvalidEscapeSequence(int n)
        {   // n chars from the current point should be reported by not acrossing " nor EOF
            var s = "";
            for ( int i = 0; i < n; i++ )
                if ( text[p + i] != '"' && Charsets.nbJson(text[p + i]) ) {
                    s += text[p + i];
                } else
                    break;
            Error("{0} is not a valid escape sequence.", s);
        }
        bool HexValue(int p, out int v)
        {
            v = 0;
            if ( text.Length <= p + 1 || !Charsets.nsHexDigit(text[p]) || !Charsets.nsHexDigit(text[p + 1]) )
                return false;
            v = ( HexNibble(text[p]) << 4 ) + HexNibble(text[p + 1]);
            return true;
        }
        int HexNibble(char c)
        {
            if ( c <= '9' )
                return c - '0';
            if ( c < 'Z' )
                return c - 'A' + 10;
            return c - 'a' + 10;
        }
        #endregion

        #region Chapter 6. Basic Structures 
        #region 6.1 Indentation Spaces
        bool TabCharFoundForIndentation = false;
        bool sIndent(int n) // [63] 
        {
            TabCharFoundForIndentation = false;
            Debug.Assert(StartOfLine() || EndOfFile());
            for ( int i = 0; i < n; i++ )
                if ( text[p + i] != ' ' ) {
                    if ( text[p + i] == '\t' )
                        TabCharFoundForIndentation = true;
                    return false;
                }
            p += n;
            return true;
        }
        bool sIndentLT(int n) // [64] 
        {
            Debug.Assert(StartOfLine() || EndOfFile());
            int i = 0;
            while ( Charsets.sSpace(text[p + i]) )
                i++;
            if ( i < n ) {
                p += i;
                return true;
            }
            return false;
        }
        bool sIndentLE(int n) // [65] 
        {
            return sIndentLT(n + 1);
        }
        bool sIndentCounted(int n, out int m) // [185, 187]
        {
            m = 0;
            while ( n < 0 || text[p] == ' ' ) {
                n++;
                p++;
                m++;
            }
            return m > 0;
        }
        #endregion
        #region 6.2 Separation Spaces
        private bool sSeparateInLine() // [66] 
        {
            return OneAndRepeat(Charsets.sWhite) || StartOfLine();
        }
        private bool StartOfLine() // [66, 79, 206]
        {   // TODO: how about "---" ?
            return p == 0 || text[p - 1] == '\n' || text[p - 1] == '\r' || text[p - 1] == '\ufeff';
        }
        #endregion
        #region 6.3 Line Prefixes
        private bool sLinePrefix(int n, Context c) // [67] 
        {
            switch ( c ) {
            case Context.Folded:
            case Context.BlockOut:
            case Context.BlockIn:
                return sBlockLinePrefix(n);
            case Context.FlowOut:
            case Context.FlowIn:
                return sFlowLinePrefix(n);
            default:
                throw new NotImplementedException();
            }
        }
        private bool sBlockLinePrefix(int n) // [68] 
        {
            return sIndent(n);
        }
        bool sFlowLinePrefix(int n) // [69] 
        {
            return sIndent(n) && Optional(sSeparateInLine);
        }
        #endregion
        #region 6.4 Empty Lines
        private bool lEmpty(int n, Context c) // [70] 
        {
            return
                RewindUnless(() => ( sLinePrefix(n, c) || sIndentLT(n) ) && bAsLineFeed());
        }
        #endregion
        #region 6.5 Line Folding
        private bool b_lTrimmed(int n, Context c) // [71] 
        {
            return RewindUnless(() =>
                bNonContent() && OneAndRepeat(() => lEmpty(n, c))
                );
        }
        bool bAsSpace() // [72] 
        {
            return 
                bBreak() &&
                Action(()=>stringValue.Append(' '));
        }
        private bool b_lFolded(int n, Context c) // [73] 
        {
            return b_lTrimmed(n, c) || bAsSpace();
        }
        private bool sFlowFolded(int n) // [74] 
        {   
            return RewindUnless(() =>
                Optional(sSeparateInLine) &&
                b_lFolded(n, Context.FlowIn) &&
                !cForbidden() &&
                sFlowLinePrefix(n) 
            );
        }
        #endregion
        #region 6.6 Comments
        private bool c_nbCommentText() // [75] 
        {
            return text[p] == '#' && Repeat(nbChar);
        }
        bool bComment() // [76] 
        {
            return bNonContent() || EndOfFile();
        }
        bool EndOfFile() // [76, 206]
        {
            return p == text.Length - 1; // text[text.Length-1] == '\0' /* guard char */
        }
        bool s_bComment() // [77] 
        {
            return RewindUnless(() =>
              	Optional(sSeparateInLine() && Optional(c_nbCommentText)) &&
                bComment()
            );
        }
        bool lComment() // [78] 
        {
            return RewindUnless(() =>
                sSeparateInLine() &&
                Optional(c_nbCommentText) &&
                bComment()
                );

        }
        bool s_lComments() // [79] 
        {
            return ( s_bComment() || StartOfLine() ) && Repeat(lComment);
        }
        #endregion
        #region 6.7 Separation Lines
        bool sSeparate(int n, Context c) // [80] 
        {
            switch ( c ) {
            case Context.BlockOut:
            case Context.BlockIn:
            case Context.FlowOut:
            case Context.FlowIn:
                return sSeparateLines(n);
            case Context.BlockKey:
            case Context.FlowKey:
                return sSeparateInLine();
            default:
                throw new NotImplementedException();
            }
        }
        bool sSeparateLines(int n) // [81] 
        {
            return
                RewindUnless(() => s_lComments() && sFlowLinePrefix(n)) ||
                sSeparateInLine();
        }
        #endregion
        #region 6.8 Directives
        bool lDirective() // [82] 
        {
            return RewindUnless(() =>
                text[p++] == '%' &&
                RewindUnless(() =>
                    nsYamlDirective() ||
                    nsTagDirective() ||
                    nsReservedDirective()) &&
                s_lComments()
                );
        }
        bool nsReservedDirective() // [83] 
        {
            var name = "";
            var args = new List<string>();
            return RewindUnless(() =>
                Save(() => OneAndRepeat(nsChar), ref name) &&
                Repeat(() =>
                    sSeparateInLine() && Save(() => OneAndRepeat(nsChar), s => args.Add(s))
                )
            ) &&
            Action(() => ReservedDirective(name, args.ToArray()) );
        }
        bool YamlDirectiveAlreadyAppeared = false;
        bool nsYamlDirective() // [86] 
        {
            string version = "";
            return RewindUnless(() =>
                Accept("YAML") &&
                sSeparateInLine() &&
                Save(() =>
                    OneAndRepeat(Charsets.nsHexDigit) &&
                    text[p++] == '.' &&
                    OneAndRepeat(Charsets.nsHexDigit),
                    ref version)
                ) &&
                Action(() => {
                    if ( YamlDirectiveAlreadyAppeared )
                        Error("The YAML directive must only be given at most once per document.");
                    YamlDirective(version);
                    YamlDirectiveAlreadyAppeared = true;
                });
        }
        bool nsTagDirective() // [88] 
        {
            string tag_handle = "";
            string tag_prefix = "";
            return RewindUnless(() =>
                Accept("TAG") && sSeparateInLine() && 
                ErrorUnless(()=>
                    text[p++] == '!' &&
                    cTagHandle(out tag_handle) && sSeparateInLine() && 
                    nsTagPrefix(out tag_prefix),
                    "Invalid TAG directive found."
                )
            ) &&
            Action(() => TagPrefixes.Add(tag_handle, tag_prefix) );
        }
        private bool cTagHandle(out string tag_handle) // [89]' 
        {
            var _tag_handle = tag_handle = "";
            if ( Save(() => Optional(RewindUnless(() => 
                    Repeat(Charsets.nsWordChar) && text[p++] == '!'
                    )), 
                    s => _tag_handle = s) ) {
                tag_handle = "!" + _tag_handle;
                return true;
            }
            return false;
        }
        private bool nsTagPrefix(out string tag_prefix) // [93] 
        {
            return
                c_nsLocalTagPrefix(out tag_prefix) ||
                nsGlobalTagPrefix(out tag_prefix);
        }
        private bool c_nsLocalTagPrefix(out string tag_prefix) // [94] 
        {
            Debug.Assert(stringValue.Length == 0);
            if ( RewindUnless(() =>
                    text[p++] == '!' &&
                    Repeat(nsUriChar)
                ) ) {
                tag_prefix = "!" + stringValue.ToString();
                stringValue.Length = 0;
                return true;
            }
            tag_prefix = "";
            return false;
        }
        private bool nsGlobalTagPrefix(out string tag_prefix) // [95] 
        {
            Debug.Assert(stringValue.Length == 0);
            if(RewindUnless(()=> nsTagChar() && Repeat(nsUriChar) )){
                tag_prefix = stringValue.ToString();
                stringValue.Length = 0;
                return true;
            }
            tag_prefix = "";
            return false;
        }
        #endregion
        #region 6.9 Node Properties
        bool c_nsProperties(int n, Context c) // [96] 
        {
            state.anchor = null;
            state.tag = null;
            return
                ( c_nsTagProperty() && Optional(RewindUnless(()=> sSeparate(n, c) && c_nsAnchorProperty()) )) ||
                ( c_nsAnchorProperty() && Optional(RewindUnless(()=>sSeparate(n, c) && c_nsTagProperty()) ));
        }
        bool c_nsTagProperty() // [97]' 
        {
            if(text[p] != '!')
                return false;

            // reduce '!' here to improve perfomance
            p++;
            return
                cVerbatimTag() ||
                c_nsShorthandTag() ||
                cNonSpecificTag();
        }
        private bool cVerbatimTag() // [98]' 
        {
            return
                text[p] == '<' &&
                ErrorUnless(
                    text[p++] == '<' &&
                    OneAndRepeat(nsUriChar) &&
                    text[p++] == '>',
                    "Invalid verbatim tag"
                ) &&
                SetTag(GetStringValue());
        }

        private bool c_nsShorthandTag() // [99]' 
        {
            var tag_handle = "";
            return RewindUnless(() =>
                cTagHandle(out tag_handle) &&
                ErrorUnlessWithAdditionalCondition(() =>
                    OneAndRepeat(nsTagChar),
                    tag_handle != "!",
                    string.Format("The {0} handle has no suffix.", tag_handle)
                ) &&
                SetTag(tag_handle, GetStringValue())
            );
        }
        string GetStringValue()
        {
            var s = stringValue.ToString();
            stringValue.Length = 0;
            return s;
        }
        private bool cNonSpecificTag() // [100]' 
        {
            // disable tag resolution to restrict tag to be ( map | seq | str )
            state.tag = "";
            return true; /* empty */
        }
        bool c_nsAnchorProperty() // [101] 
        {
            if ( text[p] != '&' )
                return false;
            p++;
            return Save(nsAnchorName, s => state.anchor = s);
        }
        private bool nsAnchorName() // [103] 
        {
            return OneAndRepeat(Charsets.nsAnchorChar);
        }
        #endregion
        #endregion

        #region Chapter 7. Flow Styles
        #region 7.1 Alias Nodes
        private bool c_nsAliasNode() // [104] 
        {
            string anchor_name = "";
            var pos = CurrentPosition;
            return RewindUnless(() =>
                text[p++] == '*' &&
                Save(() => nsAnchorName(), s => anchor_name = s)
            ) &&
            SetValue(Anchors[anchor_name]);
        }
        #endregion
        #region 7.2 Empty Nodes
        /// <summary>
        /// [105]
        /// </summary>
        private bool eScalar()
        {
            Debug.Assert(stringValue.Length == 0);
            return SetValue(CreateScalar("!!null", CurrentPosition)); /* empty */
        }
        /// <summary>
        /// [106]
        /// </summary>
        private bool eNode()
        {
            return eScalar();
        }
        #endregion
        #region 7.3 Flow Scalar Styles
        #region 7.3.1 Double-Quoted Style
        private bool nbDoubleChar() // [107] 
        {
            if ( text[p] != '\\' && text[p] != '"' && Charsets.nbJson(text[p]) ) {
                stringValue.Append(text[p++]);
                return true;
            }
            return c_nsEscChar();
        }
        bool nsDoubleChar() // [108] 
        {
            return !Charsets.sWhite(text[p]) && nbDoubleChar();
        }
        private bool cDoubleQuoted(int n, Context c) // [109] 
        {
            Position pos = CurrentPosition;
            Debug.Assert(stringValue.Length == 0);
            return text[p] == '"' &&
                ErrorUnlessWithAdditionalCondition(() =>
                    text[p++] == '"' &&
                    nbDoubleText(n, c) &&
                    text[p++] == '"',
                    c == Context.FlowOut,
                    "Closing quotation \" was not found." +
                    ( TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "" )
                ) &&
                SetValue(CreateScalar("!!str", pos));
        }
        private bool nbDoubleText(int n, Context c) // [110] 
        {
            switch ( c ) {
            case Context.FlowOut:
            case Context.FlowIn:
                return nbDoubleMultiLine(n);
            case Context.BlockKey:
            case Context.FlowKey:
                return nbDoubleOneLine(n);
            default:
                throw new NotImplementedException();
            }
        }
        private bool nbDoubleOneLine(int n) // [111] 
        {
            return Repeat(nbDoubleChar);
        }
        private bool sDoubleEscaped(int n) // [112] 
        {
            return RewindUnless(() =>
                Repeat_sWhiteAsString() &&
                text[p++] == '\\' && bNonContent() &&
                Repeat(() => lEmpty(n, Context.FlowIn)) &&
                sFlowLinePrefix(n)
                );
        }
        private bool sDoubleBreak(int n) // [113] 
        {
            return sDoubleEscaped(n) || sFlowFolded(n);
        }
        private bool nb_nsDoubleInLine() // [114] 
        {
            return Repeat(() => RewindUnless(()=> Repeat_sWhiteAsString() && OneAndRepeat(nsDoubleChar)) );
        }
        private bool sDoubleNextLine(int n) // [115] 
        {
            return
                sDoubleBreak(n) &&
                Optional(RewindUnless(() =>
                    nsDoubleChar() &&
                    nb_nsDoubleInLine() &&
                    ( sDoubleNextLine(n) || Repeat(Repeat_sWhiteAsString) )
                    ))
                ;
        }
        private bool nbDoubleMultiLine(int n) // [116] 
        {
            return nb_nsDoubleInLine() &&
                ( sDoubleNextLine(n) || Repeat(Repeat_sWhiteAsString) );
        }
        #endregion
        #region 7.3.2 Single-Quoted Style
        bool nbSingleChar() // [118] 
        {
            if ( text[p] != '\'' && Charsets.nbJson(text[p]) ) {
                stringValue.Append(text[p++]);
                return true;
            }
            // [117] cQuotedQuote
            if ( text[p] == '\'' && text[p + 1] == '\'' ) {
                stringValue.Append('\'');
                p += 2;
                return true;
            }
            return false;
        }
        bool nsSingleChar() // [119] 
        {
            return !Charsets.sWhite(text[p]) && nbSingleChar();
        }
        private bool cSingleQuoted(int n, Context c) // [120] 
        {
            Debug.Assert(stringValue.Length == 0);
            Position pos = CurrentPosition;
            return text[p] == '\'' &&
                ErrorUnlessWithAdditionalCondition(()=>
                    text[p++] == '\'' &&
                    nbSingleText(n, c) &&
                    text[p++] == '\'',
                    c == Context.FlowOut,
                    "Closing quotation \' was not found." +
                    (TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "")
                ) &&
                SetValue(CreateScalar("!!str", pos));
        }
        private bool nbSingleText(int n, Context c) // [121] 
        {
            switch ( c ) {
            case Context.FlowOut:
            case Context.FlowIn:
                return nbSingleMultiLine(n);
            case Context.BlockKey:
            case Context.FlowKey:
                return nbSingleOneLine(n);
            default:
                throw new NotImplementedException();
            }
        }
        private bool nbSingleOneLine(int n) // [122] 
        {
            return Repeat(nbSingleChar);
        }
        private bool nb_nsSingleInLine() // [123] 
        {   
            return Repeat(() => RewindUnless(()=> Repeat_sWhiteAsString() && OneAndRepeat(nsSingleChar)));
        }
        private bool sSingleNextLine(int n) // [124] 
        {
            return RewindUnless(() =>
                sFlowFolded(n) && (
                    nsSingleChar() &&
                    nb_nsSingleInLine() &&
                    Optional(sSingleNextLine(n) || Repeat_sWhiteAsString() )
                    )
                );
        }
        private bool nbSingleMultiLine(int n) // [125] 
        {
            return nb_nsSingleInLine() &&
                ( sSingleNextLine(n) || Repeat_sWhiteAsString() );
        }
        #endregion
        #region 7.3.3 Plain Style
        private bool nsPlainFirst(Context c) // [126] 
        {
            if ( Charsets.nsPlainFirstSub(text[p]) ||
                   ( ( text[p] == '?' || text[p] == ':' || text[p] == '-' ) && nsPlainSafe(c, text[p+1]) ) ) {
                WarnIfCharWasBreakInYAML1_1();
                stringValue.Append(text[p++]);
                return true;
            }
            return false;
        }
        private bool nsPlainSafe(Context c) // [127] 
        {
            if ( !nsPlainSafe(c, text[p]) )
                return false;
            WarnIfCharWasBreakInYAML1_1();
            stringValue.Append(text[p++]);
            return true;
        }
        private bool nsPlainSafe(Context c, char cc) // [127] 
        {
            switch ( c ) {
            case Context.FlowOut:
            case Context.BlockKey:
                return Charsets.nsPlainSafeOut(cc);
            case Context.FlowIn:
            case Context.FlowKey:
                return Charsets.nsPlainSafeIn(cc);
            default:
                throw new NotImplementedException();
            }
        }
        private bool nsPlainChar(Context c) // [130] 
        {
            if ( text[p]!= ':' && text[p]!='#' && nsPlainSafe(c) )
                return true;
            if ( ( /* An ns-char preceding '#' */
                    p != 0 &&
                    Charsets.nsChar(text[p - 1]) &&
                    text[p] == '#' ) ||
                ( /* ':' Followed by an ns-char */
                    text[p] == ':' && nsPlainSafe(c, text[p+1]) )
                ) {
                stringValue.Append(text[p++]);
                return true;
            }                             
            return false;
        }
        private bool nsPlain(int n, Context c) // [131] 
        {
            if ( cForbidden() )
                return false;
            var pos = CurrentPosition;
            Debug.Assert(stringValue.Length == 0);
            switch ( c ) {
            case Context.FlowOut:
            case Context.FlowIn:
                return
                    nsPlainMultiLine(n, c) &&
                    SetValue(CreateScalar(null, pos));
            case Context.BlockKey:
            case Context.FlowKey:
                return nsPlainOneLine(c) &&
                    SetValue(CreateScalar(null, pos));
            default:
                throw new NotImplementedException();
            }
        }
        private bool nb_nsPlainInLine(Context c) // [132] 
        {   
            return Repeat(() => RewindUnless(() => 
                Repeat_sWhiteAsString() && 
                OneAndRepeat(() => nsPlainChar(c))
            ));
        }
        private bool nsPlainOneLine(Context c) // [133] 
        {
            return nsPlainFirst(c) && nb_nsPlainInLine(c);
        }
        private bool s_nsPlainNextLine(int n, Context c) // [134] 
        {
            return RewindUnless(() =>
                sFlowFolded(n) &&
                nsPlainChar(c) &&
                nb_nsPlainInLine(c)
            );
        }
        private bool nsPlainMultiLine(int n, Context c) // [135] 
        {
            return
                nsPlainOneLine(c) &&
                Repeat(() => s_nsPlainNextLine(n, c));
        }
        #endregion
        #endregion
        #region 7.4 Flow Collection Styles
        private Context InFlow(Context c) // [136] 
        {
            switch ( c ) {
            case Context.FlowOut:
            case Context.FlowIn:
                return Context.FlowIn;
            case Context.BlockKey:
            case Context.FlowKey:
                return Context.FlowKey;
            default:
                throw new NotImplementedException();
            }
        }
        #region 7.4.1 Flow Sequences
        private bool cFlowSequence(int n, Context c) // [137] 
        {
            YamlSequence sequence = null;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                text[p++] == '[' &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowSeqEntries(n, InFlow(c),
                                sequence = CreateSequence(pos))) &&
                    text[p++] == ']',
                    c == Context.FlowOut,
                    "Closing brace ] was not found." +
                    ( TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "" )
                )
            ) &&
            SetValue(sequence);
        }

        private bool ns_sFlowSeqEntries(int n, Context c, YamlSequence sequence) // [138] 
        {
            return
                nsFlowSeqEntry(n, c) &&
                Action(() => sequence.Add(GetValue()) ) &&
                Optional(sSeparate(n, c)) &&
                Optional(RewindUnless(() =>
                    text[p++] == ',' &&
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowSeqEntries(n, c, sequence))
                    ));
        }
        private bool nsFlowSeqEntry(int n, Context c) // [139] 
        {
            YamlNode key = null;
            Position pos = CurrentPosition;
            return 
                RewindUnless(()=>
                    nsFlowPair(n, c, ref key) && 
                    Action(()=>{
                        var map= CreateMapping(pos);
                        map.Add(key, GetValue());
                        SetValue(map);
                    })
                ) || 
                nsFlowNode(n, c);
        }
        #endregion
        #region 7.4.2 Flow Mappings
        private bool cFlowMapping(int n, Context c) // [140] 
        {
            Position pos = CurrentPosition;
            YamlMapping mapping = null;
            return RewindUnless(() =>
                text[p++] == '{' &&
                Optional(sSeparate(n, c)) &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Optional(ns_sFlowMapEntries(n, InFlow(c), mapping = CreateMapping(pos))) &&
                    text[p++] == '}',
                    c == Context.FlowOut,
                    "Closing brace }} was not found." +
                    ( TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "" )
                )
            ) &&
            SetValue(mapping);
        }
        private bool ns_sFlowMapEntries(int n, Context c, YamlMapping mapping) // [141] 
        {
            YamlNode key = null;
            return
                nsFlowMapEntry(n, c, ref key) &&
                Action(() => mapping.Add(key, GetValue()) ) &&
                Optional(sSeparate(n, c)) &&
                Optional(RewindUnless(() =>
                    text[p++] == ',' &&
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowMapEntries(n, c, mapping))
                ));
        }
        private bool nsFlowMapEntry(int n, Context c, ref YamlNode key) // [142] 
        {
            YamlNode _key = null;
            return (
                RewindUnless(() => text[p++] == '?' && sSeparate(n, c) && nsFlowMapExplicitEntry(n, c, ref _key)) ||
                nsFlowMapImplicitEntry(n, c, ref _key)
            ) &&
            Assign(out key, _key);
        }
        private bool nsFlowMapExplicitEntry(int n, Context c, ref YamlNode key) // [143] 
        {
            return nsFlowMapImplicitEntry(n, c, ref key) || (
                eNode() /* Key */ &&
                Assign(out key, GetValue()) &&
                eNode() /* Value */
            );
        }
        private bool nsFlowMapImplicitEntry(int n, Context c, ref YamlNode key) // [144] 
        {
            return
                nsFlowMapYamlKeyEntry(n, c, ref key) ||
                c_nsFlowMapEmptyKeyEntry(n, c, ref key) ||
                c_nsFlowMapJsonKeyEntry(n, c, ref key);
        }
        private bool nsFlowMapYamlKeyEntry(int n, Context c, ref YamlNode key) // [145] 
        {
            return
                nsFlowYamlNode(n, c) &&
                Assign(out key, GetValue()) && (
                    RewindUnless(() => ( Optional(sSeparate(n, c)) && c_nsFlowMapSeparateValue(n, c) )) ||
                    eNode()
                );
        }
        private bool c_nsFlowMapEmptyKeyEntry(int n, Context c, ref YamlNode key) // [146] 
        {
            YamlNode _key = null;
            return RewindUnless(() =>
                eNode() /* Key */ &&
                Assign(out _key, GetValue()) &&
                c_nsFlowMapSeparateValue(n, c)
            ) &&
            Assign(out key, _key);
        }
        private bool c_nsFlowMapSeparateValue(int n, Context c) // [147] 
        {
            return RewindUnless(() =>
                text[p++] == ':' && !nsPlainSafe(c, text[p]) && (
                    RewindUnless(() => sSeparate(n, c) && nsFlowNode(n, c)) ||
                    eNode() /* Value */
                )
            );
        }
        private bool c_nsFlowMapJsonKeyEntry(int n, Context c, ref YamlNode key) // [148] 
        {
            return
                cFlowJsonNode(n, c) &&
                Assign(out key, GetValue()) && (
                    RewindUnless(() => Optional(sSeparate(n, c)) && c_nsFlowMapAdjacentValue(n, c)) ||
                    eNode()
                );
        }
        private bool c_nsFlowMapAdjacentValue(int n, Context c) // [149] 
        {
            return RewindUnless(() =>
                text[p++] == ':' && (
                    RewindUnless(() => Optional(sSeparate(n, c)) && nsFlowNode(n, c)) ||
                    eNode() /* Value */
                    )
                );
        }
        private bool nsFlowPair(int n, Context c, ref YamlNode key) // [150] 
        {
            YamlNode _key = null;
            return (
                RewindUnless(() => text[p++] == '?' && sSeparate(n, c) && nsFlowMapExplicitEntry(n, c, ref _key)) ||
                nsFlowPairEntry(n, c, ref _key)
            ) &&
            Assign(out key, _key);
        }
        private bool nsFlowPairEntry(int n, Context c, ref YamlNode key) // [151] 
        {
            return
                nsFlowPairYamlKeyEntry(n, c, ref key) ||
                c_nsFlowMapEmptyKeyEntry(n, c, ref key) ||
                c_nsFlowPairJsonKeyEntry(n, c, ref key);
        }
        private bool nsFlowPairYamlKeyEntry(int n, Context c, ref YamlNode key) // [152] 
        {
            return
                ns_sImplicitYamlKey(Context.FlowKey) &&
                Assign(out key, GetValue()) &&
                c_nsFlowMapSeparateValue(n, c);
        }
        private bool c_nsFlowPairJsonKeyEntry(int n, Context c, ref YamlNode key) // [153] 
        {
            return
                c_sImplicitJsonKey(Context.FlowKey) &&
                Assign(out key, GetValue()) &&
                c_nsFlowMapAdjacentValue(n, c);
        }
        private bool ns_sImplicitYamlKey(Context c) // [154] 
        {
            /* At most 1024 characters altogether */
            int start = p;
            if ( nsFlowYamlNode(-1 /* not used */, c) && Optional(sSeparateInLine) ) {
                ErrorUnless(( p - start ) < 1024, "The implicit key was too long.");
                return true;
            }
            return false;
        }
        private bool c_sImplicitJsonKey(Context c) // [155] 
        {
            /* At most 1024 characters altogether */
            int start = p;
            if ( cFlowJsonNode(-1 /* not used */, c) && Optional(sSeparateInLine) ) {
                ErrorUnless(( p - start ) < 1024, "The implicit key was too long.");
                return true;
            }
            return false;
        }
        #endregion
        #endregion
        #region 7.5 Flow Nodes
        private bool nsFlowYamlContent(int n, Context c) // [156] 
        {
            return nsPlain(n, c);
        }
        private bool cFlowJsonContent(int n, Context c) // [157] 
        {
            return cFlowSequence(n, c) || cFlowMapping(n, c) ||
                   cSingleQuoted(n, c) || cDoubleQuoted(n, c);
        }
        private bool nsFlowContent(int n, Context c) // [158] 
        {
            return
                nsFlowYamlContent(n, c) ||
                cFlowJsonContent(n, c);
        }
        private bool nsFlowYamlNode(int n, Context c) // [159] 
        {
            return 
                c_nsAliasNode() ||
                nsFlowYamlContent(n, c) ||
                ( c_nsProperties(n, c) && (
                    RewindUnless(()=> sSeparate(n, c) && nsFlowYamlContent(n, c) ) || eScalar() ) );
        }
        private bool cFlowJsonNode(int n, Context c) // [160] 
        {
            return
                Optional(RewindUnless(() => c_nsProperties(n, c) && sSeparate(n, c))) &&
                cFlowJsonContent(n, c);
        }
        private bool nsFlowNode(int n, Context c) // [161] 
        {
            if( c_nsAliasNode() ||
                nsFlowContent(n, c) ||
                RewindUnless(() => c_nsProperties(n, c) &&
                    ( RewindUnless(() => sSeparate(n, c) && nsFlowContent(n, c)) || eScalar() )) )
                return true;
            if( text[p] == '@' || text[p] == '`' )
                Error("Reserved indicators '@' and '`' can't start a plain scalar.");
            return false;
        }
        #endregion
        #endregion

        #region Chapter 8. Block Styles
        #region 8.1 Block Scalar Styles
        #region 8.1.1 Block Scalar Headers
        enum ChompingIndicator
        {
            Strip,
            Keep,
            Clip
        }
        private bool c_bBlockHeader(out int m, out ChompingIndicator t) // [162] 
        {
            var _m = m = 0;
            var _t = t = ChompingIndicator.Clip;
            if ( RewindUnless(() =>
                    ( ( cIndentationIndicator(ref _m) && Optional(cChompingIndicator(ref _t)) ) ||
                      ( Optional(cChompingIndicator(ref _t)) && Optional(cIndentationIndicator(ref _m)) ) ) &&
                    s_bComment()
                ) ) {
                m = _m;
                t = _t;
                return true;
            }
            return false;
        }
        bool cIndentationIndicator(ref int m) // [163] 
        {
            if ( Charsets.nsDecDigit(text[p]) ) {
                m = text[p] - '0';
                p++;
                return true;
            }
            return false;
        }
        bool cChompingIndicator(ref ChompingIndicator t) // [164] 
        {
            switch ( text[p] ) {
            case '-':
                p++;
                t = ChompingIndicator.Strip;
                return true;
            case '+':
                p++;
                t = ChompingIndicator.Keep;
                return true;
            }
            return false;
        }
        private bool bChompedLast(ChompingIndicator t) // [165] 
        {
            return EndOfFile() || (
                ( t == ChompingIndicator.Strip ) ? bBreak() : bAsLineFeed() 
            );
        }
        private bool lChompedEmpty(int n, ChompingIndicator t) // [166] 
        {
            return ( t == ChompingIndicator.Keep ) ? lKeepEmpty(n) : lStripEmpty(n);
        }
        private bool lStripEmpty(int n) // [167] 
        {
            return Repeat(() =>RewindUnless(()=> sIndentLE(n) && bNonContent())) &&
                   Optional(lTrailComments(n));
        }
        private bool lKeepEmpty(int n) // [168] 
        {
            return Repeat(() => lEmpty(n, Context.BlockIn)) &&
                   Optional(lTrailComments(n));
        }
        private bool lTrailComments(int n) // [169] 
        {
            return RewindUnless(() =>
                sIndentLT(n) &&
                c_nbCommentText() &&
                bComment() &&
                Repeat(lComment)
            );
        }
        int AutoDetectIndentation(int n) // [170, 183]
        {
            int m = 0, max = 0, maxp = 0;
            RewindUnless(() =>
                Repeat(() => RewindUnless(() =>
                    Save(() => Repeat(Charsets.sSpace), s => {
                        if ( s.Length > max ) {
                            max = s.Length;
                            maxp = p;
                        }
                    }) && bBreak())
                ) &&
                Save(() => Repeat(Charsets.sSpace), s => m = s.Length - n) &&
                Action(() => { if ( text[p] == '\t' ) TabCharFoundForIndentation = true; }) &&
                false // force Rewind
            );
            if ( m < 1 && TabCharFoundForIndentation )
                Error("Tab character found for indentation.");
            if ( m < max - n ) {
                p = maxp;
                Error("Too many indentation was found.");
            }
            return m <= 1 ? 1 : m;
        }
        #endregion
        #region 8.1.2. Literal Style
        bool c_lLiteral(int n) // [170] 
        {
            Debug.Assert(stringValue.Length == 0);

            int m = 0;
            var t = ChompingIndicator.Clip;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                text[p++] == '|' &&
                c_bBlockHeader(out m, out t) &&
                Action(() => { if ( m == 0 ) m = AutoDetectIndentation(n); }) &&
                ErrorUnless(lLiteralContent(n + m, t), "Irregal literal text found.")
            ) &&
            SetValue(CreateScalar("!!str", pos));
        }
        bool l_nbLiteralText(int n) // [171] 
        {
            return RewindUnless(() =>
                Repeat(() => lEmpty(n, Context.BlockIn)) &&
                sIndent(n) &&
                Save(() => Repeat(nbChar), s => stringValue.Append(s) )
            );
        }
        bool b_nbLiteralNext(int n) // [172] 
        {
            return RewindUnless(() =>
                bAsLineFeed() &&
                !cForbidden() &&
                l_nbLiteralText(n)
            );                                                            
        }
        private bool lLiteralContent(int n, ChompingIndicator t) // [173] 
        {
            return RewindUnless(()=>
                Optional(RewindUnless(()=>l_nbLiteralText(n) && Repeat(() => b_nbLiteralNext(n)) && bChompedLast(t))) &&
                lChompedEmpty(n, t)
            );
        }
        #endregion
        #region 8.1.3. Folded Style
        private bool c_lFolded(int n) // [174] 
        {
            Debug.Assert(stringValue.Length == 0);

            int m = 0;
            var t = ChompingIndicator.Clip;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                text[p++] == '>' &&
                c_bBlockHeader(out m, out t) &&
                WarningIf(t== ChompingIndicator.Keep,       
                  "Keep line breaks for folded text '>+' is invalid") &&
                Action(() => { if ( m == 0 ) m = AutoDetectIndentation(n); }) &&
                ErrorUnless(lFoldedContent(n + m, t), "Irregal folded string found.")
            ) &&
            SetValue(CreateScalar("!!str", pos));
        }
        private bool s_nbFoldedText(int n) // [175] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                Save(() => nsChar() && Repeat(nbChar), s => stringValue.Append(s))
            );
        }
        private bool l_nbFoldedLines(int n) // [176] 
        {
            return s_nbFoldedText(n) &&
                Repeat(() => RewindUnless(() => b_lFolded(n, Context.BlockIn) && s_nbFoldedText(n)));
        }
        private bool s_nbSpacedText(int n) // [177] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                Save(() => sWhite() && Repeat(nbChar), s => stringValue.Append(s))
            );
        }
        private bool b_lSpaced(int n) // [178] 
        {
            return
                bAsLineFeed() &&
                !cForbidden() &&
                Repeat(() => lEmpty(n, Context.Folded));
        }
        private bool l_nbSpacedLines(int n) // [179] 
        {
            return RewindUnless(() =>
                s_nbSpacedText(n) &&
                Repeat(() => RewindUnless(() => b_lSpaced(n) && s_nbSpacedText(n)))
            );
        }
        private bool l_nbSameLines(int n) // [180] 
        {
            return RewindUnless(() =>
                Repeat(() => lEmpty(n, Context.BlockIn)) &&
                ( l_nbFoldedLines(n) || l_nbSpacedLines(n) )
            );
        }
        private bool l_nbDiffLines(int n) // [181] 
        {
            return 
                l_nbSameLines(n) &&
                Repeat(() => RewindUnless(() => bAsLineFeed() && !cForbidden() && l_nbSameLines(n)));
        }
        private bool lFoldedContent(int n, ChompingIndicator t) // [182] 
        {
            return RewindUnless(()=>
                Optional(RewindUnless(() => l_nbDiffLines(n) && bChompedLast(t))) &&
                lChompedEmpty(n, t)
            );
        }
        #endregion
        #endregion
        #region 8.2. Block Collection Styles
        #region 8.2.1 Block Sequences
        private bool lBlockSequence(int n) // [183] 
        {
            int m = AutoDetectIndentation(n);
            YamlSequence sequence = null;
            Position pos = new Position();
            return OneAndRepeat(() => RewindUnless(() =>
                sIndent(n + m) &&
                Action(() => { if ( sequence == null ) pos = CurrentPosition; }) &&
                text[p] == '-' && !Charsets.nsChar(text[p + 1]) &&
                Action(() => { if ( sequence == null ) sequence = CreateSequence(pos); }) &&
                c_lBlockSeqEntry(n + m, sequence)
            )) &&
            SetValue(sequence);
        }
        private bool c_lBlockSeqEntry(int n, YamlSequence sequence) // [184] 
        {
            Debug.Assert(text[p] == '-' && !Charsets.nsChar(text[p + 1]));
            p++;
            return 
                s_lBlockIndented(n, Context.BlockIn) &&
                Action(()=> sequence.Add(GetValue()) );
        }
        bool s_lBlockIndented(int n, Context c) // [185] 
        {
            int m;
            return
                RewindUnless(() => sIndentCounted(n, out m) &&
                    ( ns_lCompactSequence(n + 1 + m) || ns_lCompactMapping(n + 1 + m) )) ||
                s_lBlockNode(n, c) ||
                ( eNode() && s_lComments() );
        }
        private bool ns_lCompactSequence(int n) // [186] 
        {
            YamlSequence sequence = null;
            Position pos = CurrentPosition;
            return
                text[p] == '-' && !Charsets.nsChar(text[p + 1]) &&
                Action(() => sequence = CreateSequence(pos)) && 
                c_lBlockSeqEntry(n, sequence) &&
                Repeat(() => RewindUnless(() => 
                    sIndent(n) &&
                    text[p] == '-' && !Charsets.nsChar(text[p + 1]) &&
                    c_lBlockSeqEntry(n, sequence))) &&
                SetValue(sequence);
        }
        #endregion
        #region 8.2.2 Block Mappings
        private bool lBlockMapping(int n) // [187] 
        {
            YamlMapping mapping = null;
            int m = 0;
            YamlNode key = null;
            return OneAndRepeat(() =>
                sIndent(n + m) &&
                ( m > 0 || sIndentCounted(n, out m) ) &&
                Action(() => {
                    if ( mapping == null ) {
                        mapping = CreateMapping(CurrentPosition);
                    }
                }) &&
                ns_lBlockMapEntry(n + m, ref key) &&
                Action(() => mapping.Add(key, GetValue()))
            ) &&
            SetValue(mapping);
        }
        private bool ns_lBlockMapEntry(int n, ref YamlNode key) // [188] 
        {
            return c_lBlockMapExplicitEntry(n, ref key) ||
                   ns_lBlockMapImplicitEntry(n, ref key);
        }
        private bool c_lBlockMapExplicitEntry(int n, ref YamlNode key) // [189] 
        {
            YamlNode _key= null;
            return RewindUnless(() =>
                c_lBlockMapExplicitKey(n, ref _key) &&
                ErrorUnless(
                    ( lBlockMapExplicitValue(n) || eNode() ),
                    "irregal block mapping explicit entry"
                )
            ) &&
            Assign(out key, _key);
        }
        private bool c_lBlockMapExplicitKey(int n, ref YamlNode key) // [190] 
        {
            return RewindUnless(() =>
                text[p++] == '?' &&
                s_lBlockIndented(n, Context.BlockOut)
            ) &&
            Assign(out key, GetValue());
        }
        private bool lBlockMapExplicitValue(int n) // [191] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                text[p++] == ':' &&
                s_lBlockIndented(n, Context.BlockOut)
            );
        }
        private bool ns_lBlockMapImplicitEntry(int n, ref YamlNode key) // [192] 
        {
            YamlNode _key = null;
            return RewindUnless(() =>
                ( ns_sBlockMapImplicitKey() || eNode() ) &&
                Assign(out _key, GetValue()) &&
                c_lBlockMapImplicitValue(n)
            ) &&
            Assign(out key, _key);
        }
        private bool ns_sBlockMapImplicitKey() // [193] 
        {
            return c_sImplicitJsonKey(Context.BlockKey) ||
                   ns_sImplicitYamlKey(Context.BlockKey);
        }
        private bool c_lBlockMapImplicitValue(int n) // [194] 
        {
            return RewindUnless(() =>
                text[p++] == ':' &&
                ( s_lBlockNode(n, Context.BlockOut) || ( eNode() && s_lComments() ) )
            );
        }
        private bool ns_lCompactMapping(int n) // [195] 
        {
            var mapping = CreateMapping(CurrentPosition);
            YamlNode key = null;
            return RewindUnless(() =>
                ns_lBlockMapEntry(n, ref key) &&
                Action(() => mapping.Add(key, GetValue())) &&
                Repeat(() => RewindUnless(() => 
                    sIndent(n) && 
                    ns_lBlockMapEntry(n, ref key) &&
                    Action(() => mapping.Add(key, GetValue()))
                ))
            ) &&
            SetValue(mapping);
        }
        #endregion
        #region 8.2.3 Block Nodes
        bool s_lBlockNode(int n, Context c) // [196] 
        {
            return
                s_lBlockInBlock(n, c) ||
                s_lFlowInBlock(n);
        }
        bool s_lFlowInBlock(int n) // [197] 
        {
            return RewindUnless(() =>
                sSeparate(n + 1, Context.FlowOut) &&
                nsFlowNode(n + 1, Context.FlowOut) &&
                s_lComments()
                );
        }
        bool s_lBlockInBlock(int n, Context c) // [198] 
        {
            Debug.Assert(stringValue.Length == 0);
            return
                s_lBlockScalar(n, c) ||
                s_lBlockCollection(n, c);
        }
        bool s_lBlockScalar(int n, Context c) // [199] 
        {
            return RewindUnless(() =>
                sSeparate(n + 1, c) &&
                Optional(RewindUnless(() => c_nsProperties(n + 1, c) && sSeparate(n + 1, c))) &&
                ( c_lLiteral(n) || c_lFolded(n) )
                );
        }
        bool s_lBlockCollection(int n, Context c) // [200]
        {
            return RewindUnless(() =>
                Optional(RewindUnless(() => sSeparate(n + 1, c) && c_nsProperties(n + 1, c))) &&
                s_lComments() &&
                ( lBlockSequence(SeqSpaces(n, c)) || lBlockMapping(n) )
            ) ||
            RewindUnless(() =>
                s_lComments() &&
                ( lBlockSequence(SeqSpaces(n, c)) || lBlockMapping(n) )
            );
        }
        private int SeqSpaces(int n, Context c) // [201]
        {
            switch ( c ) {
            case Context.BlockOut:
                return n - 1;
            case Context.BlockIn:
                return n;
            default:
                throw new NotImplementedException();
            }
        }
        #endregion
        #endregion
        #endregion

        #region Chapter 9. YAML Character Stream
        #region 9.1. Documents
        private bool lDocumentPrefix() // [202] 
        {
            return Optional(Charsets.cByteOrdermark) && Repeat(lComment);
        }
        private bool cDirectivesEnd() // [203] 
        {
            return Accept("---");
        }
        private bool cDocumentEnd() // [204] 
        {
            return Accept("...");
        }
        bool lDocumentSuffix() // [205] 
        {
            return RewindUnless(() => 
                cDocumentEnd() && 
                s_lComments()
            );
        }
        bool cForbidden() // [206] 
        {
            if ( !StartOfLine() || ( text.Length - p ) < 3 )
                return false;
            var s = text.Substring(p, 3);
            if ( s != "---" && s != "..." )
                return false;
            return
                text.Length - p == 3 + 1 ||
                Charsets.sWhite(text[p + 3]) ||
                Charsets.bChar(text[p + 3]);
        }
        bool lBareDocument() // [207] 
        {
            var length = stringValue.Length;
            var s = stringValue.ToString();
            stringValue.Length = 0;
            Debug.Assert(length == 0, "stringValue should be empty but '" + s + "' was found");
            state.value = null;

            TagPrefixes.SetupDefaultTagPrefixes();
            return
                s_lBlockNode(-1, Context.BlockIn) &&
                Action(() => ParseResult.Add(GetValue()));
        }
        bool lExplicitDocument() // [208] 
        {
            return RewindUnless(() =>
                cDirectivesEnd() &&
                ( lBareDocument() || eNode() && s_lComments() && Action(() => ParseResult.Add(GetValue())) )
            );
        }
        bool lDirectiveDocument() // [209] 
        {
            YamlDirectiveAlreadyAppeared = false;
            return RewindUnless(() =>
                OneAndRepeat(lDirective) && lExplicitDocument()
            );
        }
        #endregion
        #region 9.2. Streams
        bool lAnyDocument() // [210] 
        {
            return
                lDirectiveDocument() ||
                lExplicitDocument() ||
                lBareDocument();
        }
        private bool lYamlStream() // [211] 
        {
            TagPrefixes.Reset();
            Anchors.RewindDeapth = 0;
            state.anchor_depth = 0;
            WarningAdded.Clear();
            Warnings.Clear();
            stringValue.Length = 0;
            bool BomReduced = false;
            if ( Repeat(lDocumentPrefix) &&
                Optional(lAnyDocument) &&
                Repeat(() =>
                    TagPrefixes.Reset() &&
                    RewindUnless(() =>
                        OneAndRepeat(() => lDocumentSuffix() && Action(() => BomReduced = false)) &&
                        Repeat(lDocumentPrefix) && Optional(lAnyDocument)) ||
                    RewindUnless(() =>
                        Repeat(() => Action(() => BomReduced |= Charsets.cByteOrdermark(text[p])) && lDocumentPrefix() ) &&
                        Optional(lExplicitDocument() && Action(() => BomReduced = false)))
                    ) &&
                EndOfFile() )
                return true;
            if ( BomReduced ) {
                Error("A BOM (\\ufeff) must not appear inside a document.");
            }else
            if(Charsets.cIndicator(text[p])){
                Error("Plain text can not start with indicator characters -?:,[]{{}}#&*!|>'\"%@`");
            }else
            if ( text[p] == ' ' && StartOfLine() ) {
                Error("Extra line was found. Maybe indentation was incorrect.");
            } else 
            if ( Charsets.nbChar(text[p]) ){
                Error("Extra content was found. Maybe indentation was incorrect.");
            } else {
                Error("An irregal character {0} appeared.", 
                        (text[p]<0x100) ? 
                            string.Format("'\\x{0:x2}'", (int)text[p]) :
                            string.Format("'\\u{0:x4}'", (int)text[p])
                    );
            }
            return false;
        }
        #endregion
        #endregion

        #endregion
    }
}
