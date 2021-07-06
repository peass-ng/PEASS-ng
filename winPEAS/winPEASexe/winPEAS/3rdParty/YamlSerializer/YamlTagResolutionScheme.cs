using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

namespace System.Yaml
{
    /// <summary>
    /// Represents the way to automatically resolve Tag from the Value of a YamlScalar.
    /// </summary>
    internal class YamlTagResolver
    {
        /// <summary>
        /// Create TagResolver with default resolution rules.
        /// </summary>
        public YamlTagResolver()
        {
            AddDefaultRules();
        }

        /// <summary>
        /// Add default tag resolution rules to the rule list.
        /// </summary>
        void AddDefaultRules()
        {
            BeginUpdate();
            AddRule<int>("!!int", @"([-+]?(0|[1-9][0-9_]*))", 
                m => Convert.ToInt32(m.Value.Replace("_", "")), null);
            AddRule<int>("!!int", @"([-+]?)0b([01_]+)", m => {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 2);
                return m.Groups[1].Value == "-" ? -v : v;
                }, null);
            AddRule<int>("!!int", @"([-+]?)0o?([0-7_]+)", m => {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 8);
                return m.Groups[1].Value == "-" ? -v : v;
            }, null);
            AddRule<int>("!!int", @"([-+]?)0x([0-9a-fA-F_]+)", m => {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 16);
                return m.Groups[1].Value == "-" ? -v : v;
            }, null);
            // Todo: http://yaml.org/type/float.html is wrong  => [0-9.] should be [0-9_]
            AddRule<double>("!!float", @"[-+]?(0|[1-9][0-9_]*)\.[0-9_]*([eE][-+]?[0-9]+)?",
                m => Convert.ToDouble(m.Value.Replace("_", "")), null);
            AddRule<double>("!!float", @"[-+]?\._*[0-9][0-9_]*([eE][-+]?[0-9]+)?",
                m => Convert.ToDouble(m.Value.Replace("_", "")), null);
            AddRule<double>("!!float", @"[-+]?(0|[1-9][0-9_]*)([eE][-+]?[0-9]+)",
                m => Convert.ToDouble(m.Value.Replace("_", "")), null);
            AddRule<double>("!!float", @"\+?(\.inf|\.Inf|\.INF)", m => double.PositiveInfinity, null);
            AddRule<double>("!!float", @"-(\.inf|\.Inf|\.INF)", m => double.NegativeInfinity, null);
            AddRule<double>("!!float", @"\.nan|\.NaN|\.NAN", m => double.NaN, null);
            AddRule<bool>("!!bool", @"y|Y|yes|Yes|YES|true|True|TRUE|on|On|ON", m => true, null);
            AddRule<bool>("!!bool", @"n|N|no|No|NO|false|False|FALSE|off|Off|OFF", m => false, null);
            AddRule<object>("!!null", @"null|Null|NULL|\~|", m => null, null);
            AddRule<string>("!!merge", @"<<", m => "<<", null);
            AddRule<DateTime>("!!timestamp",  // Todo: spec is wrong (([ \t]*)Z|[-+][0-9][0-9]?(:[0-9][0-9])?)? should be (([ \t]*)(Z|[-+][0-9][0-9]?(:[0-9][0-9])?))? to accept "2001-12-14 21:59:43.10 -5"
                @"([0-9]{4})-([0-9]{2})-([0-9]{2})" +
                @"(" +
                    @"([Tt]|[\t ]+)" +
                    @"([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2})(\.([0-9]*))?" +
                    @"(" +
                        @"([ \t]*)" +
                        @"(Z|([-+])([0-9]{1,2})(:([0-9][0-9]))?)" +
                    @")?" +
                @")?", 
                match => DateTime.Parse(match.Value),
                datetime => {
                    var z = datetime.ToString("%K");
                    if ( z != "Z" && z != "" )
                        z = " " + z;
                    if ( datetime.Millisecond == 0 ) {
                        if ( datetime.Hour == 0 && datetime.Minute == 0 && datetime.Second == 0 ) {
                            return datetime.ToString("yyyy-MM-dd" + z);
                        } else {
                            return datetime.ToString("yyyy-MM-dd HH:mm:ss" + z);
                        }
                    } else {
                        return datetime.ToString("yyyy-MM-dd HH:mm:ss.fff" + z);
                    }
                });
            EndUpdate();
        }

        public Type TypeFromTag(string tag)
        {
            tag= YamlNode.ExpandTag(tag);
            if ( types.ContainsKey(tag) )
                return types[tag][0].GetTypeOfValue();
            return null;
        }

        /// <summary>
        /// List of tag resolution rules.
        /// </summary>
        List<YamlTagResolutionRule> Rules = new List<YamlTagResolutionRule>();
        /// <summary>
        /// Add a tag resolution rule that is invoked when <paramref name="regex"/> matches 
        /// the <see cref="YamlScalar.Value">Value of</see> a <see cref="YamlScalar"/> node.
        /// 
        /// The tag is resolved to <paramref name="tag"/> and <paramref name="decode"/> is
        /// invoked when actual value of type <typeparamref name="T"/> is extracted from 
        /// the node text.
        /// </summary>
        /// <remarks>
        /// Surround sequential calls of this function by <see cref="BeginUpdate"/> / <see cref="EndUpdate"/>
        /// pair to avoid invoking slow internal calculation method many times.
        /// </remarks>
        /// <example>
        /// <code>
        /// BeginUpdate(); // to avoid invoking slow internal calculation method many times.
        /// Add( ... );
        /// Add( ... );
        /// Add( ... );
        /// Add( ... );
        /// EndUpdate();   // automaticall invoke internal calculation method 
        /// </code></example>
        /// <param name="tag"></param>
        /// <param name="regex"></param>
        /// <param name="decode"></param>
        /// <param name="encode"></param>
        public void AddRule<T>(string tag, string regex, Func<Match, T> decode, Func<T, string> encode)
        {
            Rules.Add(new YamlTagResolutionRule<T>(tag, regex, decode, encode));
            if ( UpdateCounter == 0 )
                Update();
        }

        public void AddRule<T>(string regex, Func<Match, T> decode, Func<T, string> encode)
        {
            Rules.Add(new YamlTagResolutionRule<T>("!"+typeof(T).FullName, regex, decode, encode));
            if ( UpdateCounter == 0 )
                Update();
        }

        int UpdateCounter = 0;
        /// <summary>
        /// Supress invoking slow internal calculation method when 
        /// <see cref="AddRule&lt;T&gt;(string,string,Func&lt;Match,T&gt;,Func&lt;T,string&gt;)"/> called.
        /// 
        /// BeginUpdate / <see cref="EndUpdate"/> can be called nestedly.
        /// </summary>
        public void BeginUpdate()
        {
            UpdateCounter++;
        }
        /// <summary>
        /// Quit to supress invoking slow internal calculation method when 
        /// <see cref="AddRule&lt;T&gt;(string,string,Func&lt;Match,T&gt;,Func&lt;T,string&gt;)"/> called.
        /// </summary>
        public void EndUpdate()
        {
            if ( UpdateCounter == 0 )
                throw new InvalidOperationException();
            UpdateCounter--;
            if ( UpdateCounter == 0 )
                Update();
        }

        Dictionary<string, Regex> algorithms;
        void Update()
        {
            // Tag to joined regexp source
            var sources = new Dictionary<string, string>();
            foreach ( var rule in Rules ) {
                if ( !sources.ContainsKey(rule.Tag) ) {
                    sources.Add(rule.Tag, rule.PatternSource);
                } else {
                    sources[rule.Tag] += "|" + rule.PatternSource;
                }
            }

            // Tag to joined regexp
            algorithms = new Dictionary<string, Regex>();
            foreach ( var entry in sources ) {
                algorithms.Add(
                    entry.Key,
                    new Regex("^(" + entry.Value + ")$")
                );
            }

            // Tag to decoding methods
            types = new Dictionary<string, List<YamlTagResolutionRule>>();
            foreach ( var rule in Rules ) {
                if ( !types.ContainsKey(rule.Tag) ) 
                    types[rule.Tag] = new List<YamlTagResolutionRule>();
                types[rule.Tag].Add(rule);
            }

            TypeToRule = new Dictionary<Type, YamlTagResolutionRule>();
            foreach ( var rule in Rules ) 
                if(rule.HasEncoder())
                    TypeToRule[rule.GetTypeOfValue()] = rule;
        }

        Dictionary<string, List<YamlTagResolutionRule>> types;
        Dictionary<Type, YamlTagResolutionRule> TypeToRule;

        /// <summary>
        /// Execute tag resolution and returns automatically determined tag value from <paramref name="text"/>.
        /// </summary>
        /// <param name="text">Node text with which automatic tag resolution is done.</param>
        /// <returns>Automatically determined tag value .</returns>
        public string Resolve(string text)
        {
            foreach ( var entry in algorithms )
                if ( entry.Value.IsMatch(text) )
                    return entry.Key;
            return null;
        }

        /// <summary>
        /// Decode <paramref name="text"/> and returns actual value in C# object.
        /// </summary>
        /// <param name="node">Node to be decoded.</param>
        /// <param name="obj">Decoded value.</param>
        /// <returns>True if decoded successfully.</returns>
        public bool Decode(YamlScalar node, out object obj)
        {
            obj = null;
            if ( node.Tag == null || node.Value == null )
                return false;
            var tag= YamlNode.ExpandTag(node.Tag);
            if ( !types.ContainsKey(tag) )
                return false;
            foreach ( var rule in types[tag] ) {
                var m = rule.Pattern.Match(node.Value);
                if ( m.Success ) {
                    obj = rule.Decode(m);
                    return true;
                }
            }
            return false;
        }

        public bool Encode(object obj, out YamlScalar node)
        {
            node = null;
            YamlTagResolutionRule rule;
            if ( !TypeToRule.TryGetValue(obj.GetType(), out rule) )
                return false;
            node = new YamlScalar(rule.Tag, rule.Encode(obj));
            return true;
        }
    }

    internal abstract class YamlTagResolutionRule
    {
        public string Tag { get; protected set; }
        public Regex Pattern { get; protected set; }
        public string PatternSource { get; protected set; }
        public abstract object Decode(Match m);
        public abstract string Encode(object obj);
        public abstract Type GetTypeOfValue();
        public abstract bool HasEncoder();
        public bool IsMatch(string value) { return Pattern.IsMatch(value); }
    }

    internal class YamlTagResolutionRule<T>: YamlTagResolutionRule
    {
        public YamlTagResolutionRule(string tag, string regex, Func<Match, T> decoder, Func<T, string> encoder)
        {
            Tag = YamlNode.ExpandTag(tag);
            PatternSource = regex;
            Pattern = new Regex("^(?:" + regex + ")$");
            Decoder = decoder;
            Encoder = encoder;
        }
        private Func<Match, T> Decoder;
        private Func<T, string> Encoder;
        public override object Decode(Match m)
        {
            return Decoder(m);
        }
        public override string Encode(object obj)
        {
            return Encoder((T)obj);
        }
        public override Type GetTypeOfValue()
        {
            return typeof(T);
        }
        public override bool HasEncoder()
        {
            return Encoder != null;
        }
    }

}
