using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;

namespace System.Yaml
{
    /// <summary>
    /// Converts YamlNode tree into yaml text.
    /// </summary>
    /// <example>
    /// <code>
    /// YamlNode node;
    /// YamlPresenter.ToYaml(node);
    /// 
    /// YamlNode node1;
    /// YamlNode node2;
    /// YamlNode node3;
    /// YamlPresenter.ToYaml(node1, node2, node3);
    /// </code>
    /// </example>
    internal class YamlPresenter
    {
        TextWriter yaml;
        int column, raw;
        YamlConfig config;

        public string ToYaml(YamlNode node)
        {
            return ToYaml(node, YamlNode.DefaultConfig);
        }

        public string ToYaml(YamlNode node, YamlConfig config)
        {
            yaml = new StringWriter();
            ToYaml(yaml, node, config);
            return yaml.ToString();
        }

        public void ToYaml(Stream s, YamlNode node, YamlConfig config)
        {
            using ( var yaml = new StreamWriter(s) )
                ToYaml(yaml, node, config);
        }

        public void ToYaml(TextWriter yaml, YamlNode node, YamlConfig config)
        {
            this.config = config;
            this.yaml = yaml;
            MarkMultiTimeAppearingChildNodesToBeAnchored(node);
            yaml.NewLine = config.LineBreakForOutput;

            column = 1;
            raw = 1;
            WriteLine("%YAML 1.2");
            WriteLine("---");
            NodeToYaml(node, "", Context.Normal);
            WriteLine("...");
        }

        static void MarkMultiTimeAppearingChildNodesToBeAnchored(YamlNode node)
        {
            var AlreadyAppeared = new Dictionary<YamlNode, bool>(
                    TypeUtils.EqualityComparerByRef<YamlNode>.Default);
            var anchor = "";

            Action<YamlNode> analyze = null;
            analyze = n => {
                if ( !AlreadyAppeared.ContainsKey(n) ) {
                    n.Properties.Remove("ToBeAnchored");
                    n.Properties.Remove("Anchor");
                    AlreadyAppeared[n] = true;
                } else {
                    if ( !n.Properties.ContainsKey("Anchor") ) {
                        anchor = NextAnchor(anchor);
                        n.Properties["ToBeAnchored"] = "true";
                        n.Properties["Anchor"] = anchor;
                    }
                    return;
                }
                if ( n is YamlSequence ) {
                    var seq = (YamlSequence)n;
                    foreach ( var child in seq )
                        analyze(child);
                }
                if ( n is YamlMapping ) {
                    var map = (YamlMapping)n;
                    foreach ( var child in map ) {
                        analyze(child.Key);
                        analyze(child.Value);
                    }
                }
            };
            analyze(node);
        }

        internal static string NextAnchor(string anchor) // this is "protected" for test use 
        {
            if ( anchor == "" ) {
                return "A";
            } else
            if ( anchor[anchor.Length - 1] != 'Z' ) {
                return anchor.Substring(0, anchor.Length - 1) + ((char)( anchor[anchor.Length - 1] + 1 )).ToString();
            } else {
                return NextAnchor(anchor.Substring(0, anchor.Length - 1)) + "A";
            }
        }

        internal enum Context
        {
            Normal,
            List,
            Map,
            NoBreak
        }

        void Write(string s)
        {
            int start = 0;
            for ( int p = 0; p < s.Length; ) {
                if ( s[p] != '\r' && s[p] != '\n' ) {
                    // proceed until finding a line break
                    p++;
                } else {
                    int pp = p;
                    if ( p + 1 < s.Length && s[p] == '\r' && s[p + 1] == '\n' )
                        p++;
                    p++;
                    if ( config.NormalizeLineBreaks ) {
                        // output with normalized line break
                        yaml.WriteLine(s.Substring(start, pp - start));
                    } else {
                        // output with native line break
                        yaml.Write(s.Substring(start, p - start));
                    }
                    raw++;
                    column = 1;
                    start = p;
                }
            }
            // rest of the string
            s = s.Substring(start, s.Length - start);
            column += s.Length;
            yaml.Write(s);
        }

        void WriteLine(string s)
        {
            Write(s);
            WriteLine();
        }

        void WriteLine()
        {
            yaml.WriteLine();
            raw++;
            column = 1;
        }

        private void NodeToYaml(YamlNode node, string pres, Context c)
        {
            if ( node.Properties.ContainsKey("ToBeAnchored") ) {
                node.Raw = raw;
                node.Column = column;
                Write("&" + node.Properties["Anchor"] + " ");
                node.Properties.Remove("ToBeAnchored");
                c = Context.Map;
            } else {
                if ( node.Properties.ContainsKey("Anchor") ) {
                    Write("*" + node.Properties["Anchor"]);
                    if ( c != Context.NoBreak ) {
                        WriteLine();
                    }
                    return;
                }
                node.Raw = raw;
                node.Column = column;
            }

            if ( node is YamlSequence ) {
                SequenceToYaml((YamlSequence)node, pres, c);
            } else
            if ( node is YamlMapping ) {
                MappingToYaml((YamlMapping)node, pres, c);
            } else {
                ScalarToYaml((YamlScalar)node, pres, c);
            }
        }

        private static string GetPropertyOrNull(YamlNode node, string name)
        {
            string result;
            if ( node.Properties.TryGetValue(name, out result) )
                return result;
            return null;
        }

        private void ScalarToYaml(YamlScalar node, string pres, Context c)
        {
            var s = node.Value;

            // If tag can be resolved from the content, or tag is !!str, 
            // no need to explicitly specify it.
            var auto_tag = YamlNode.ShorthandTag(AutoTagResolver.Resolve(s));
            var tag = TagToYaml(node, auto_tag);
            if ( tag != "" && tag != "!!str" )
                Write(tag + " ");

            if ( IsValidPlainText(s, c) && !( node.ShorthandTag() == "!!str" && auto_tag != null && !node.Properties.ContainsKey("plainText")) ) {
                // one line plain style
                Write(s);
                if ( c != Context.NoBreak ) 
                    WriteLine();
            } else {
                if ( ForbiddenChars.IsMatch(s) || OneLine.IsMatch(s) || 
                     ( config.ExplicitlyPreserveLineBreaks && 
                       GetPropertyOrNull(node, "Don'tCareLineBreaks") == null ) ) {
                    // double quoted
                    Write(DoubleQuotedString.Quote(s, pres, c));
                    if ( c != Context.NoBreak ) 
                        WriteLine();
                } else {
                    // Literal style
                    if ( s[s.Length - 1] == '\n' || s[s.Length - 1] == '\r' ) {
                        WriteLine("|+2");
                    } else {
                        WriteLine("|-2");
                        s += "\r\n"; // guard
                    }
                    var press = pres + "  ";
                    for ( int p = 0; p < s.Length; ) {
                        var m = UntilBreak.Match(s, p); // does not fail because of the guard
                        Write(pres + s.Substring(p, m.Length));
                        p += m.Length;
                    }
                }
            }
        }

        private bool IsValidPlainText(string s, Context c)
        {
            if ( s == "" )
                return true;
            switch ( c ) {
            case Context.Normal:    // Block Key
            case Context.Map:       // BlockValue
            case Context.List:      // ListItem
            case Context.NoBreak:   // Flow Key
                return ( s == "" || PlainChecker.IsValidPlainText(s, config) );
            default:
                throw new NotImplementedException();
            }
        }
        private static DoubleQuote DoubleQuotedString = new DoubleQuote();
        private static Regex ForbiddenChars = new Regex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]");
        private static Regex OneLine = new Regex(@"^([^\n\r]|\n)*(\r?\n|\r)?$");
        private static Regex UntilBreak = new Regex(@"[^\r\n]*(\r?\n|\r)");

        public class DoubleQuote: Parser<DoubleQuote.State>
        {
            internal struct State { }
            Func<char, bool> nbDoubleSafeCharset = Charset(c =>
                ( 0x100 <= c && c != '\u2028' && c != '\u2029' ) ||
                c == 0x09 ||
                ( 0x20 <= c && c < 0x100 && c != '\\' && c != '"' && c != 0x85 && c != 0xA0 )
            );

            public DoubleQuote()
            {
                CharEscaping.Add('\x00', @"\0");
                CharEscaping.Add('\x07', @"\a");
                CharEscaping.Add('\x08', @"\b");
                CharEscaping.Add('\x0B', @"\v");
                CharEscaping.Add('\x0C', @"\f");
                CharEscaping.Add('\x1B', @"\e");
                CharEscaping.Add('\x22', @"\""");
                CharEscaping.Add('\x5C', @"\\");
                CharEscaping.Add('\x85', @"\N");
                CharEscaping.Add('\xA0', @"\_");
                CharEscaping.Add('\u2028', @"\L");
                CharEscaping.Add('\u2029', @"\P");
            }

            bool nbDoubleSafeChar()
            {
                if ( nbDoubleSafeCharset(text[p]) ) {
                    stringValue.Append(text[p++]);
                    return true;
                }
                return false;
            }

            public string Quote(string s, string pres, Context c)
            {
                base.Parse(() => DoubleQuoteString(pres, c), s);
                return "\"" + stringValue.ToString() + "\"";
            }

            bool DoubleQuoteString(string pres, Context c)
            {
                return Repeat(() => cDoubleQuoteChar(pres, c));
            }

            bool cDoubleQuoteChar(string pres, Context c)
            {
                return
                    !EndOfString() && (
                        nbDoubleSafeChar() ||
                        bBreak(pres, c) ||
                        nsEscapedChar()
                    );
            }

            Dictionary<char, string> CharEscaping = new Dictionary<char, string>();
            private bool nsEscapedChar()
            {
                var c= text[p];
                string escaped;
                if ( CharEscaping.TryGetValue(c, out escaped) ) {
                    stringValue.Append(escaped);
                } else {
                    if ( c < 0x100 ) {
                        stringValue.Append(string.Format(@"\x{0:x2}", (int)c));
                    } else {
                        stringValue.Append(string.Format(@"\u{0:x4}", (int)c));
                    }
                }
                p++;
                return true;
            }

            private bool bBreak(string pres, Context c)
            {
                if ( text[p] == '\r' ) {
                    stringValue.Append(@"\r");
                    p++;
                    if ( !EndOfString() && text[p] == '\n' ) {
                        stringValue.Append(@"\n");
                        p++;
                    }
                } else
                if ( text[p] == '\n' ) {
                    stringValue.Append(@"\n");
                    p++;
                } else {
                    return false;
                }
                if ( EndOfString() || c == Context.NoBreak )
                    return true;

                // fold the string with escaping line break
                stringValue.AppendLine(@"\");
                stringValue.Append(pres);

                // if the following line starts from space char, escape it.
                if ( text[p] == ' ' )
                    stringValue.Append(@"\");
                return true;
            }

            private bool EndOfString()
            {
                return p == text.Length;
            }
        }

        private static YamlTagResolver AutoTagResolver = new YamlTagResolver();
        private static YamlParser PlainChecker = new YamlParser();

        private void SequenceToYaml(YamlSequence node, string pres, Context c)
        {
            if ( node.Count == 0 || GetPropertyOrNull(node, "Compact") != null ) {
                FlowSequenceToYaml(node, pres, c);
            } else {
                BlockSequenceToYaml(node, pres, c);
            }
        }

        private void BlockSequenceToYaml(YamlSequence node, string pres, Context c)
        {
            var tag = TagToYaml(node, "!!seq");
            if ( tag != "" || c == Context.Map ) {
                WriteLine(tag);
                c = Context.Normal;
            }
            string press = pres + "  ";
            foreach ( var item in node ) {
                if ( c == Context.Normal )
                    Write(pres);
                Write("- ");
                NodeToYaml(item, press, Context.List);
                c = Context.Normal;
            }
        }

        private void FlowSequenceToYaml(YamlSequence node, string pres, Context c)
        {
            var tag = TagToYaml(node, "!!seq");
            if ( column > 80 ) {
                WriteLine();
                Write(pres);
            }
            if ( tag != "" && tag != "!!seq" )
                Write(tag + " ");
            Write("[");
            foreach ( var item in node ) {
                if ( item != node.First() )
                    Write(", ");
                if ( column > 100 ) {
                    WriteLine();
                    Write(pres);
                }
                NodeToYaml(item, pres, Context.NoBreak);
            }
            Write("]");
            if ( c != Context.NoBreak )
                WriteLine();
        }

        private void MappingToYaml(YamlMapping node, string pres, Context c)
        {
            var tag = TagToYaml(node, "!!map");
            if ( node.Count > 0 ) {
                if ( tag != "" || c == Context.Map ) {
                    WriteLine(tag);
                    c = Context.Normal;
                }
                string press = pres + "  ";
                foreach ( var item in node ) {
                    if ( c != Context.List )
                        Write(pres);
                    c = Context.Normal;
                    if ( WriteImplicitKeyIfPossible(item.Key, press, Context.NoBreak) ) {
                        Write(": ");
                        NodeToYaml(item.Value, press, Context.Map);
                    } else {
                        // explicit key
                        Write("? ");
                        NodeToYaml(item.Key, press, Context.List);
                        Write(pres);
                        Write(": ");
                        NodeToYaml(item.Value, press, Context.List);
                    }
                }
            } else {
                if ( tag != "" && tag != "!!map" )
                    Write(tag + " ");
                Write("{}");
                if ( c != Context.NoBreak ) 
                    WriteLine();
            }
        }

        bool WriteImplicitKeyIfPossible(YamlNode node, string pres, Context c)
        {
            if ( !( node is YamlScalar ) )
                return false;
            int raw_saved = raw;
            int col_saved = column;
            var yaml_saved = yaml;
            var result = "";
            using ( yaml = new StringWriter() ) {
                NodeToYaml(node, pres, c);
                result = yaml.ToString();
            }
            if ( result.Length < 80 && result.IndexOf('\n') < 0 ) {
                yaml = yaml_saved;
                yaml.Write(result);
                return true;
            } else {
                yaml = yaml_saved;
                raw = raw_saved;
                column = col_saved;
                return false;
            };
        }

        private string TagToYaml(YamlNode node, string defaultTag)
        {
            var tag = node.ShorthandTag();
            if ( tag == YamlNode.ShorthandTag(defaultTag) )
                return "";
            if ( tag == YamlNode.ShorthandTag(GetPropertyOrNull(node, "expectedTag")) )
                return "";
            if ( config.DontUseVerbatimTag ) {
                if ( tag.StartsWith("!") ) {
                    tag = tag.UriEscapeForTag();
                } else {
                    tag = "!<" + tag.UriEscape() + ">";
                }
            } else {
                tag = tag.UriEscape();
                if ( !CanBeShorthand.IsMatch(tag) )
                    tag = "!<" + tag + ">";
            }
            return tag;
        }
        // has a tag handle and the body contains only ns-tag-char.
        static Regex CanBeShorthand = new Regex(@"^!([-0-9a-zA-Z]*!)?[-0-9a-zA-Z%#;/?:@&=+$_.^*'\(\)]*$");
    }
}
