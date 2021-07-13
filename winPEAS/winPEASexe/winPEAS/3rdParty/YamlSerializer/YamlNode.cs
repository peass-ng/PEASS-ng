using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Globalization;

namespace System.Yaml
{
    /// <summary>
    /// <para>Configuration to customize YAML serialization.</para>
    /// <para>An instance of this class can be passed to the serialization
    /// methods, such as <see cref="YamlNode.ToYaml(YamlConfig)">YamlNode.ToYaml(YamlConfig)</see> and
    /// <see cref="YamlNode.FromYaml(Stream,YamlConfig)">YamlNode.FromYaml(Stream,YamlConfig)</see> or
    /// it can be assigned to <see cref="YamlNode.DefaultConfig">YamlNode.DefaultConfig</see>.
    /// </para>
    /// </summary>
    public class YamlConfig
    {
        /// <summary>
        /// If true, all line breaks in the node value are normalized into "\r\n" 
        /// (= <see cref="LineBreakForOutput"/>) when serialize and line breaks 
        /// that are not escaped in YAML stream are normalized into "\n"
        /// (= <see cref="LineBreakForInput"/>.
        /// If false, the line breaks are preserved. Setting this option false violates 
        /// the YAML specification but sometimes useful. The default is true.
        /// </summary>
        /// <remarks>
        /// <para>The YAML sepcification requires a YAML parser to normalize every line break that 
        /// is not escaped in a YAML stream, into a single line feed "\n" when it parse a YAML stream. 
        /// But this is not convenient in some cases, especially under Windows environment, where 
        /// the system default line break 
        /// is "\r\n" instead of "\n".</para>
        /// 
        /// <para>This library provides two workarounds for this problem.</para>
        /// <para>One is setting <see cref="NormalizeLineBreaks"/> false. It disables the line break
        /// normalization. The line breaks are serialized into a YAML stream as is and 
        /// those in the YAML stream are deserialized as is.</para>
        /// <para>Another is setting <see cref="LineBreakForInput"/> "\r\n". Then, the YAML parser
        /// normalizes all line breaks into "\r\n" instead of "\n".</para>
        /// <para>Note that although these two options are useful in some cases,
        /// they makes the YAML parser violate the YAML specification. </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public bool NormalizeLineBreaks = true;
        /// <summary>
        /// If true, all <see cref="YamlScalar"/>s whose text expression contains line breaks 
        /// will be presented as double quoted texts, where the line break characters are escaped 
        /// by back slash as "\\n" and "\\r". The default is true.
        /// </summary>
        /// <remarks>
        /// <para>The escaped line breaks makes the YAML stream hard to read, but is required to 
        /// prevent the line break characters be normalized by the YAML parser; the YAML 
        /// sepcification requires a YAML parser to normalize all line breaks that are not escaped
        /// into a single line feed "\n" when it parse a YAML source.</para>
        /// 
        /// <para>
        /// If the preservation of line breaks are not required, set this value false.
        /// </para>
        /// 
        /// <para>Then, whenever it is possible, the <see cref="YamlNode"/>s are presented
        /// as literal style text, where the line breaks are not escaped. This results in
        /// a much prettier output in the YAML stream.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public bool ExplicitlyPreserveLineBreaks = true;
        /// <summary>
        /// Line break to be used when <see cref="YamlNode"/> is presented in YAML stream. 
        /// "\r", "\r\n", "\n" are allowed. "\r\n" is defalut.
        /// </summary>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public string LineBreakForOutput = "\r\n";
        /// <summary>
        /// <para>The YAML parser normalizes line breaks in a YAML stream to this value.</para>
        /// 
        /// <para>"\n" is default, and is the only valid value in the YAML specification. "\r" and "\r\n" are
        /// allowed in this library for convenience.</para>
        /// 
        /// <para>To suppress normalization of line breaks by YAML parser, set <see cref="NormalizeLineBreaks"/> 
        /// false, though it is also violate the YAML specification.</para>
        /// </summary>
        /// <remarks>
        /// <para>The YAML sepcification requires a YAML parser to normalize every line break that 
        /// is not escaped in a YAML stream, into a single line feed "\n" when it parse a YAML stream. 
        /// But this is not convenient in some cases, especially under Windows environment, where 
        /// the system default line break 
        /// is "\r\n" instead of "\n".</para>
        /// 
        /// <para>This library provides two workarounds for this problem.</para>
        /// <para>One is setting <see cref="NormalizeLineBreaks"/> false. It disables the line break
        /// normalization. The line breaks are serialized into a YAML stream as is and 
        /// those in the YAML stream are deserialized as is.</para>
        /// <para>Another is setting <see cref="LineBreakForInput"/> "\r\n". Then, the YAML parser
        /// normalizes all line breaks into "\r\n" instead of "\n".</para>
        /// <para>Note that although these two options are useful in some cases,
        /// they makes the YAML parser violate the YAML specification. </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public string LineBreakForInput = "\n";
        /// <summary>
        /// If true, tag for the root node is omitted by <see cref="System.Yaml.Serialization.YamlSerializer"/>.
        /// </summary>
        public bool OmitTagForRootNode = false;
        /// <summary>
        /// If true, the verbatim style of a tag, i.e. !&lt; &gt; is avoided as far as possible.
        /// </summary>
        public bool DontUseVerbatimTag = false;

        /// <summary>
        /// Add a custom tag resolution rule.
        /// </summary>
        /// <example>
        /// <code>
        /// 
        /// </code>
        /// </example>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="tag">Tag for the value.</param>
        /// <param name="regex">Pattern to match the value.</param>
        /// <param name="decode">Method that decode value from <see cref="Match"/> 
        ///     data after matching by <paramref name="regex"/>.</param>
        /// <param name="encode">Method that encode value to <see cref="string"/>.</param>
        public void AddRule<T>(string tag, string regex, Func<Match, T> decode, Func<T, string> encode)
        {
            TagResolver.AddRule<T>(tag, regex, decode, encode);
        }           
        internal YamlTagResolver TagResolver = new YamlTagResolver();

        /// <summary>
        /// Add an ability of instantiating an instance of a class that has no default constructer.
        /// </summary>
        /// <typeparam name="T">Type of the object that is activated by this <paramref name="activator"/>.</typeparam>
        /// <param name="activator">A delegate that creates an instance of <typeparamref name="T"/>.</param>
        /// <example>
        /// <code>
        /// var serializer= new YamlSerializer();
        /// 
        /// var yaml =
        ///   @"%YAML 1.2
        ///   ---
        ///   !System.Drawing.SolidBrush
        ///   Color: Red
        ///   ...
        ///   ";
        /// 
        /// SolidBrush b = null;
        /// try {
        ///   b = (SolidBrush)serializer.Deserialize(yaml)[0];
        /// } catch(MissingMethodException) {
        ///   // SolidBrush has no default constructor!
        /// }
        /// 
        /// YamlNode.DefaultConfig.AddActivator&lt;SolidBrush&gt;(() => new SolidBrush(Color.Black));
        /// 
        /// // Now the serializer knows how to activate an instance of SolidBrush.
        /// b = (SolidBrush)serializer.Deserialize(yaml)[0];
        /// 
        /// Assert.AreEqual(b.Color, Color.Red);
        /// </code>
        /// </example>
        public void AddActivator<T>(Func<object> activator)
            where T: class
        {
            Activator.Add<T>(activator);
        }
        internal System.Yaml.Serialization.ObjectActivator Activator = 
            new System.Yaml.Serialization.ObjectActivator();

        /// <summary>
        /// Gets or sets CultureInfo with which the .NET native values are converted
        /// to / from string. Currently, this is not to be changed from CultureInfo.InvariantCulture.
        /// </summary>
        internal CultureInfo Culture {
            get { return TypeConverter.Culture; }
            set { TypeConverter.Culture = value; }
        }
        internal System.Yaml.Serialization.EasyTypeConverter TypeConverter =
            new System.Yaml.Serialization.EasyTypeConverter();
    }

    /// <summary>
    /// <para>Abstract base class of YAML data nodes.</para>
    /// 
    /// <para>See <see cref="YamlScalar"/>, <see cref="YamlSequence"/> and <see cref="YamlMapping"/> 
    /// for actual data classes.</para>
    /// </summary>
    /// <remarks>
    /// <h3>YAML data model</h3>
    /// <para>See <a href="http://yaml.org/">http://yaml.org/</a> for the official definition of 
    /// Information Models of YAML.</para>
    /// 
    /// <para>YAML data structure is defined as follows. 
    /// Note that this does not represents the text syntax of YAML text 
    /// but does logical data structure.</para>
    /// 
    /// <para>
    /// yaml-stream     ::= yaml-document*<br/>
    /// yaml-document   ::= yaml-directive* yaml-node<br/>
    /// yaml-directive  ::= YAML-directive | TAG-directive | user-defined-directive<br/>
    /// yaml-node       ::= yaml-scalar | yaml-sequence | yaml-mapping<br/>
    /// yaml-scalar     ::= yaml-tag yaml-value<br/>
    /// yaml-sequence   ::= yaml-tag yaml-node*<br/>
    /// yaml-mapping    ::= yaml-tag ( yaml-node yaml-node )*<br/>
    /// yaml-tag        ::= yaml-global-tag yaml-local-tag<br/>
    /// yaml-global-tag ::= "tag:" taggingEntity ":" specific [ "#" fragment ]<br/>
    /// yaml-local-tag  ::= "!" yaml-local-tag-name<br/>
    /// </para>
    /// 
    /// <para>Namely,</para>
    /// 
    /// <para>
    /// A YAML stream consists of zero or more YAML documents.<br/>
    /// A YAML documents have zero or more YAML directives and a root YAML node.<br/>
    /// A YAML directive is either YAML-directive, TAG-directive or user-defined-directive.<br/>
    /// A YAML node is either YAML scalar, YAML sequence or YAML mapping.<br/>
    /// A YAML scalar consists of a YAML tag and a scalar value.<br/>
    /// A YAML sequence consists of a YAML tag and zero or more child YAML nodes.<br/>
    /// A YAML mapping cosists of a YAML tag and zero or more key/value pairs of YAML nodes.<br/>
    /// A YAML tag is either a YAML global tag or a YAML local tag.<br/>
    /// A YAML global tag starts with "tag:" and described in the "tag:" URI scheme defined in RFC4151.<br/>
    /// A YAML local tag starts with "!" with a YAML local tag name<br/>
    /// </para>
    /// 
    /// <code>
    /// // Construct YAML node tree
    /// YamlNode node = 
    ///     new YamlSequence(                           // !!seq node
    ///         new YamlScalar("abc"),                  // !!str node
    ///         new YamlScalar("!!int", "123"),         // !!int node
    ///         new YamlScalar("!!float", "1.23"),      // !!float node
    ///         new YamlSequence(                       // nesting !!seq node
    ///             new YamlScalar("def"),
    ///             new YamlScalar("ghi")
    ///         ),
    ///         new YamlMapping(                        // !!map node
    ///             new YamlScalar("key1"), new YamlScalar("value1"),
    ///             new YamlScalar("key2"), new YamlScalar("value2"),
    ///             new YamlScalar("key3"), new YamlMapping(    // nesting !!map node
    ///                 new YamlScalar("value3key1"), new YamlScalar("value3value1")
    ///             ),
    ///             new YamlScalar("key4"), new YamlScalar("value4")
    ///         )
    ///     );
    ///     
    /// // Convert it to YAML stream
    /// string yaml = node.ToYaml();
    /// 
    /// // %YAML 1.2
    /// // ---
    /// // - abc
    /// // - 123
    /// // - 1.23
    /// // - - def
    /// //   - ghi
    /// // - key1: value1
    /// //   key2: value2
    /// //   key3:
    /// //     value3key1: value3value1
    /// //   key4: value4
    /// // ...
    /// 
    /// // Load the YAML node from the YAML stream.
    /// // Note that a YAML stream can contain several YAML documents each of which
    /// // contains a root YAML node.
    /// YamlNode[] nodes = YamlNode.FromYaml(yaml);
    /// 
    /// // The only one node in the stream is the one we have presented above.
    /// Assert.AreEqual(1, nodes.Length);
    /// YamlNode resotred = nodes[0];
    /// 
    /// // Check if they are equal to each other.
    /// Assert.AreEquel(node, restored);
    /// 
    /// // Extract sub nodes.
    /// var seq = (YamlSequence)restored;
    /// var map = (YamlMapping)seq[4];
    /// var map2 = (YamlMapping)map[new YamlScalar("key3")];
    /// 
    /// // Modify the restored node tree
    /// map2[new YamlScalar("value3key1")] = new YamlScalar("value3value1 modified");
    /// 
    /// // Now they are not equal to each other.
    /// Assert.AreNotEquel(node, restored);
    /// </code>
    /// 
    /// <h3>YamlNode class</h3>
    /// 
    /// <para><see cref="YamlNode"/> is an abstract class that represents a YAML node.</para>
    /// 
    /// <para>In reality, a <see cref="YamlNode"/> is either <see cref="YamlScalar"/>, <see cref="YamlSequence"/> or 
    /// <see cref="YamlMapping"/>.</para>
    /// 
    /// <para>All <see cref="YamlNode"/> has <see cref="YamlNode.Tag"/> property that denotes
    /// the actual data type represented in the YAML node.</para>
    /// 
    /// <para>Default Tag value for <see cref="YamlScalar"/>, <see cref="YamlSequence"/> or <see cref="YamlMapping"/> are
    /// <c>"tag:yaml.org,2002:str"</c>, <c>"tag:yaml.org,2002:seq"</c>, <c>"tag:yaml.org,2002:map"</c>.</para>
    /// 
    /// <para>Global tags that starts with <c>"tag:yaml.org,2002:"</c> ( = <see cref="YamlNode.DefaultTagPrefix">
    /// YamlNode.DefaultTagPrefix</see>) are defined in the YAML tag repository at 
    /// <a href="http://yaml.org/type/">http://yaml.org/type/</a>. In this library, such a tags can be also 
    /// represented in a short form that starts with <c>"!!"</c>, like <c>"!!str"</c>, <c>"!!seq"</c> and <c>"!!map"</c>. 
    /// Tags in the formal style and the shorthand form can be converted to each other by the static methods of 
    /// <see cref="YamlNode.ExpandTag"/> and <see cref="YamlNode.ShorthandTag(string)"/>. 
    /// In addition to these three basic tags, this library uses <c>"!!null"</c>, <c>"!!bool"</c>, <c>"!!int"</c>, 
    /// <c>"!!float"</c> and <c>"!!timestamp"</c> tags, by default.</para>
    /// 
    /// <para><see cref="YamlNode"/>s can be read from a YAML stream with <see cref="YamlNode.FromYaml(string)"/>,
    /// <see cref="YamlNode.FromYaml(Stream)"/>, <see cref="YamlNode.FromYaml(TextReader)"/> and
    /// <see cref="YamlNode.FromYamlFile(string)"/> static methods. Since a YAML stream generally consist of multiple
    /// YAML documents, each of which has a root YAML node, these methods return an array of <see cref="YamlNode"/>
    /// that is contained in the stream.</para>
    /// 
    /// <para><see cref="YamlNode"/>s can be written to a YAML stream with <see cref="YamlNode.ToYaml()"/>,
    /// <see cref="YamlNode.ToYaml(Stream)"/>, <see cref="YamlNode.ToYaml(TextWriter)"/> and
    /// <see cref="YamlNode.ToYamlFile(string)"/>.</para>
    /// 
    /// <para>The way of serialization can be configured in some aspects. The custom settings are specified
    /// by an instance of <see cref="YamlConfig"/> class. The serialization methods introduced above has
    /// overloaded styles that accepts <see cref="YamlConfig"/> instance to customize serialization.
    /// It is also possible to change the default serialization method by modifying <see cref="YamlNode.DefaultConfig">
    /// YamlNode.DefaultConfig</see> static property.</para>
    /// 
    /// <para>A <see cref="YamlScalar"/> has <see cref="YamlScalar.Value"/> property, which holds the string expression
    /// of the node value.</para>
    /// 
    /// <para>A <see cref="YamlSequence"/> implements <see cref="IList&lt;YamlNode&gt;">IList&lt;YamlNode&gt;</see> 
    /// interface to access the child nodes.</para>
    /// 
    /// <para><see cref="YamlMapping"/> implements 
    /// <see cref="IDictionary&lt;YamlNode,YamlNode&gt;">IDictionary&lt;YamlNode,YamlNode&gt;</see> interface
    /// to access the key/value pairs under the node.</para>
    /// 
    /// <h3>Implicit conversion from C# native object to YamlScalar</h3>
    /// 
    /// <para>Implicit cast operators from <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, 
    /// <see cref="double"/> and <see cref="DateTime"/> to <see cref="YamlNode"/> is defined. Thus, anytime 
    /// <see cref="YamlNode"/> is required in C# source, naked scalar value can be written. Namely,
    /// methods of <see cref="YamlSequence"/> and <see cref="YamlMapping"/> accept such C# native types 
    /// as arguments in addition to <see cref="YamlNode"/> types. </para>
    /// 
    /// <code>
    /// var map = new YamlMapping();
    /// map["Time"] = DateTime.Now;                 // implicitly converted to YamlScalar
    /// Assert.IsTrue(map.ContainsKey(new YamlScalar("Time")));
    /// Assert.IsTrue(map.ContainsKey("Time"));     // implicitly converted to YamlScalar
    /// </code>
    /// 
    /// <h3>Equality of YamlNodes</h3>
    /// 
    /// <para>Equality of <see cref="YamlNode"/>s are evaluated on the content base. Different <see cref="YamlNode"/> 
    /// objects that have the same content are evaluated to be equal. Use <see cref="Equals(object)"/> method for 
    /// equality evaluation.</para>
    /// 
    /// <para>In detail, two <see cref="YamlNode"/>s are logically equal to each other when the <see cref="YamlNode"/> 
    /// and its child nodes have the same contents (<see cref="YamlNode.Tag"/> and <see cref="YamlScalar.Value"/>) 
    /// and their node graph topology is exactly same.
    /// </para>
    /// 
    /// <code>
    /// YamlNode a1 = "a";  // implicit conversion
    /// YamlNode a2 = "a";  // implicit conversion
    /// YamlNode a3 = new YamlNode("!char", "a");
    /// YamlNode b  = "b";  // implicit conversion
    /// 
    /// Assert.IsTrue(a1 != a2);        // different objects
    /// Assert.IsTrue(a1.Equals(a2));   // different objects having same content
    /// 
    /// Assert.IsFalse(a1.Equals(a3));  // Tag is different
    /// Assert.IsFalse(a1.Equals(b));   // Value is different
    /// 
    /// var s1 = new YamlMapping(a1, new YamlSequence(a1, a2));
    /// var s2 = new YamlMapping(a1, new YamlSequence(a2, a1));
    /// var s3 = new YamlMapping(a2, new YamlSequence(a1, a2));
    /// 
    /// Assert.IsFalse(s1.Equals(s2)); // node graph topology is different
    /// Assert.IsFalse(s1.Equals(s3)); // node graph topology is different
    /// Assert.IsTrue(s2.Equals(s3));  // different objects having same content and node graph topology
    /// </code>
    /// 
    /// </remarks>
    /// <example>
    /// Example 2.27 in YAML 1.2 specification
    /// 
    /// <code>
    /// // %YAML 1.2
    /// // --- 
    /// // !&lt;tag:clarkevans.com,2002:invoice&gt;
    /// // invoice: 34843
    /// // date   : 2001-01-23
    /// // bill-to: &amp;id001
    /// //     given  : Chris
    /// //     family : Dumars
    /// //     address:
    /// //         lines: |
    /// //             458 Walkman Dr.
    /// //             Suite #292
    /// //         city    : Royal Oak
    /// //         state   : MI
    /// //         postal  : 48046
    /// // ship-to: *id001
    /// // product:
    /// //     - sku         : BL394D
    /// //       quantity    : 4
    /// //       description : Basketball
    /// //       price       : 450.00
    /// //     - sku         : BL4438H
    /// //       quantity    : 1
    /// //       description : Super Hoop
    /// //       price       : 2392.00
    /// // tax  : 251.42
    /// // total: 4443.52
    /// // comments:
    /// //     Late afternoon is best.
    /// //     Backup contact is Nancy
    /// //     Billsmer @ 338-4338.
    /// // ...
    /// 
    /// var invoice = new YamlMapping(
    ///     "invoice", 34843,
    ///     "date", new DateTime(2001, 01, 23),
    ///     "bill-to", new YamlMapping(
    ///         "given", "Chris",
    ///         "family", "Dumars",
    ///         "address", new YamlMapping(
    ///             "lines", "458 Walkman Dr.\nSuite #292\n",
    ///             "city", "Royal Oak",
    ///             "state", "MI",
    ///             "postal", 48046
    ///             )
    ///         ),
    ///     "product", new YamlSequence(
    ///         new YamlMapping(
    ///             "sku",         "BL394D",
    ///             "quantity",    4,
    ///             "description", "Basketball",
    ///             "price",       450.00
    ///             ),
    ///         new YamlMapping(
    ///             "sku",         "BL4438H",
    ///             "quantity",    1,
    ///             "description", "Super Hoop",
    ///             "price",       2392.00
    ///             )
    ///         ),
    ///     "tax", 251.42,
    ///     "total", 4443.52,
    ///     "comments", "Late afternoon is best. Backup contact is Nancy Billsmer @ 338-4338."
    ///     );
    /// invoice["ship-to"] = invoice["bill-to"];
    /// invoice.Tag = "tag:clarkevans.com,2002:invoice";
    /// 
    /// invoice.ToYamlFile("invoice.yaml");
    /// // %YAML 1.2
    /// // ---
    /// // !&lt;tag:clarkevans.com,2002:invoice&gt;
    /// // invoice: 34843
    /// // date: 2001-01-23
    /// // bill-to: &amp;A 
    /// //   given: Chris
    /// //   family: Dumars
    /// //   address: 
    /// //     lines: "458 Walkman Dr.\n\
    /// //       Suite #292\n"
    /// //     city: Royal Oak
    /// //     state: MI
    /// //     postal: 48046
    /// // product: 
    /// //   - sku: BL394D
    /// //     quantity: 4
    /// //     description: Basketball
    /// //     price: !!float 450
    /// //   - sku: BL4438H
    /// //     quantity: 1
    /// //     description: Super Hoop
    /// //     price: !!float 2392
    /// // tax: 251.42
    /// // total: 4443.52
    /// // comments: Late afternoon is best. Backup contact is Nancy Billsmer @ 338-4338.
    /// // ship-to: *A
    /// // ...
    /// 
    /// </code>
    /// </example>
    public abstract class YamlNode: IRehashableKey
    {
        #region Non content values
        /// <summary>
        /// Position in a YAML document, where the node appears. 
        /// Both <see cref="ToYaml()"/> and <see cref="FromYaml(string)"/> sets this property.
        /// When the node appeared multiple times in the document, this property returns the position
        /// where it appeared for the first time.
        /// </summary>
        [DefaultValue(0)]
        public int Raw { get; set; }

        /// <summary>
        /// Position in a YAML document, where the node appears. 
        /// Both <see cref="ToYaml()"/> and <see cref="FromYaml(string)"/> sets this property.
        /// When the node appeared multiple times in the document, this property returns the position
        /// where it appeared for the first time.
        /// </summary>
        [DefaultValue(0)]
        public int Column { get; set; }

        /// <summary>
        /// Temporary data, transfering information between YamlRepresenter and YamlPresenter.
        /// </summary>
        internal Dictionary<string, string> Properties { get; private set; }

        /// <summary>
        /// Initialize a node.
        /// </summary>
        public YamlNode()
        {
            Properties = new Dictionary<string, string>();
        }
        #endregion

        /// <summary>
        /// YAML Tag for this node, which represents the type of node's value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// YAML standard types has tags in a form of "tag:yaml.org,2002:???". Well known tags are
        /// tag:yaml.org,2002:null, tag:yaml.org,2002:bool, tag:yaml.org,2002:int, tag:yaml.org,2002:str,
        /// tag:yaml.org,2002:map, tag:yaml.org,2002:seq, tag:yaml.org,2002:float and tag:yaml.org,2002:timestamp.
        /// </para>
        /// </remarks>
        public string Tag
        { 
            get { return tag; }
            set {
                /* strict tag check
                if ( value.StartsWith("!!") )
                    throw new ArgumentException(
                        "Tag vallue {0} must be resolved to a local or global tag before assignment".DoFormat(value));
                if ( !value.StartsWith("!") && !DefaultTagValidator.IsValid(value) )
                    throw new ArgumentException(
                        "{0} is not a valid global tag.".DoFormat(value));
                */
                tag = value;
                OnChanged();
            }
        }
        string tag;
//        static YamlTagValidator TagValidator = new YamlTagValidator();
        /// <summary>
        /// YAML Tag for this node, which represents the type of node's value.
        /// The <see cref="Tag"/> property is returned in a shorthand style.
        /// </summary>
        public string ShorthandTag()
        {
            return ShorthandTag(Tag);
        }

        #region Hash code
        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// Hash code is calculated using Tag and Value properties.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            // caches hash code
            if ( HashInvalid ) {
                HashCode = GetHashCodeCore();
                HashInvalid = false;
            }
            return HashCode;
        }
        int HashCode;
        bool HashInvalid = true;
        bool ToBeRehash = false;
        /// <summary>
        /// Return the hash code. 
        /// The returned value will be cached until <see cref="OnChanged"/> is called.
        /// </summary>
        /// <returns>Hash code</returns>
        protected abstract int GetHashCodeCore();
        /// <summary>
        /// Call this function when the content of the node is changed.
        /// </summary>
        protected virtual void OnChanged()
        {
            // avoiding inifinite loop
            if ( !ToBeRehash ) {
                try {
                    HashInvalid = true;
                    ToBeRehash = true;
                    if ( Changed != null )
                        Changed(this, EventArgs.Empty);
                } finally {
                    ToBeRehash = false;
                }
            }
        }
        /// <summary>
        /// Invoked when the node's content or its childrens' content was changed.
        /// </summary>
        public event EventHandler Changed;
        #endregion

        /// <summary>
        /// Returns true if <paramref name="obj"/> is of same type as the <see cref="YamlNode"/> and
        /// its content is also logically same.
        /// </summary>
        /// <remarks>
        /// Two <see cref="YamlNode"/>'s are logically equal when the <see cref="YamlNode"/> and its child nodes
        /// have the same contents (<see cref="YamlNode.Tag"/> and <see cref="YamlScalar.Value"/>) 
        /// and their node graph topology is exactly same as the other.
        /// </remarks>
        /// <example>
        /// <code>
        /// var a1 = new YamlNode("a");
        /// var a2 = new YamlNode("a");
        /// var a3 = new YamlNode("!char", "a");
        /// var b  = new YamlNode("b");
        /// 
        /// Assert.IsTrue(a1 != a2);        // different objects
        /// Assert.IsTrue(a1.Equals(a2));   // different objects having same content
        /// 
        /// Assert.IsFalse(a1.Equals(a3));  // Tag is different
        /// Assert.IsFalse(a1.Equals(b));   // Value is different
        /// 
        /// var s1 = new YamlMapping(a1, new YamlSequence(a1, a2));
        /// var s2 = new YamlMapping(a1, new YamlSequence(a2, a1));
        /// var s3 = new YamlMapping(a2, new YamlSequence(a1, a2));
        /// 
        /// Assert.IsFalse(s1.Equals(s2)); // node graph topology is different
        /// Assert.IsFalse(s1.Equals(s3)); // node graph topology is different
        /// Assert.IsTrue(s2.Equals(s3));  // different objects having same content and node graph topology
        /// </code>
        /// </example>
        /// <param name="obj">Object to be compared.</param>
        /// <returns>True if the <see cref="YamlNode"/> logically equals to the <paramref name="obj"/>; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if ( obj == null || !( obj is YamlNode ) )
                return false;
            var repository = new ObjectRepository();
            return Equals((YamlNode)obj, repository);
        }

        /// <summary>
        /// Called when the node is loaded from a document.
        /// </summary>
        internal virtual void OnLoaded()
        {
        }

        /// <summary>
        /// Remember the order of appearance of nodes. It also has ability of rewinding.
        /// </summary>
        internal class ObjectRepository
        {
            Dictionary<YamlNode, int> nodes_a = 
                new Dictionary<YamlNode, int>(TypeUtils.EqualityComparerByRef<YamlNode>.Default);
            Dictionary<YamlNode, int> nodes_b = 
                new Dictionary<YamlNode, int>(TypeUtils.EqualityComparerByRef<YamlNode>.Default);
            Stack<YamlNode> stack_a = new Stack<YamlNode>();
            Stack<YamlNode> stack_b = new Stack<YamlNode>();

            public class Status
            {
                public int count { get; private set; }
                public Status(int c)
                {
                    count= c;
                }
            }

            public bool AlreadyAppeared(YamlNode a, YamlNode b, out bool identity)
            {
                int ai, bi;
                bool ar = nodes_a.TryGetValue(a, out ai);
                bool br = nodes_b.TryGetValue(b, out bi);
                if ( ar && br && ai == bi ) {
                    identity = true;
                    return true;
                }
                if ( ar ^ br ) {
                    identity = false;
                    return true;
                }
                nodes_a.Add(a, nodes_a.Count);
                nodes_b.Add(b, nodes_b.Count);
                stack_a.Push(a);
                stack_b.Push(b);
                if ( a == b ) {
                    identity = true;
                    return true;
                }
                identity = false;
                return false;
            }

            public Status CurrentStatus
            {
                get { return new Status(stack_a.Count); }
                set
                {
                    var count = value.count;
                    while ( stack_a.Count > count ) {
                        var a = stack_a.Pop();
                        nodes_a.Remove(a);
                        var b = stack_b.Pop();
                        nodes_b.Remove(b);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="b"/> is of same type as the <see cref="YamlNode"/> and
        /// its content is also logically same.
        /// </summary>
        /// <param name="b">Node to be compared.</param>
        /// <param name="repository">Node repository holds the nodes that already appeared and 
        /// the corresponding node in the other node tree.</param>
        /// <returns>true if they are equal to each other.</returns>
        internal abstract bool Equals(YamlNode b, ObjectRepository repository);
        /// <summary>
        /// Returns true if <paramref name="b"/> is of same type as the <see cref="YamlNode"/> and
        /// its Tag is same as the node. It returns true for <paramref name="skip"/> if they
        /// both already appeared in the node trees and were compared.
        /// </summary>
        /// <param name="b">Node to be compared.</param>
        /// <param name="repository">Node repository holds the nodes that already appeared and 
        /// the corresponding node in the other node tree.</param>
        /// <param name="skip">true if they already appeared in the node tree and were compared.</param>
        /// <returns>true if they are equal to each other.</returns>
        internal bool EqualsSub(YamlNode b, ObjectRepository repository, out bool skip)
        {
            YamlNode a = this;
            bool identity;
            if ( repository.AlreadyAppeared(a, b, out identity) ) {
                skip = true;
                return identity;
            }
            skip = false;
            if ( a.GetType() != b.GetType() || a.Tag != b.Tag )
                return false;
            return true;
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="Object"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="Object"/></returns>
        public override string ToString()
        {
            var length = 1024;
            return ToString(ref length);
        }
        internal abstract string ToString(ref int length);

        #region ToYaml
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text.
        /// </summary>
        /// <returns>YAML stream.</returns>
        public string ToYaml()
        {
            return ToYaml(DefaultConfig);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text.
        /// </summary>
        /// <returns>YAML stream.</returns>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public string ToYaml(YamlConfig config)
        {
            return DefaultPresenter.ToYaml(this, config);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to <see cref="Stream"/> <paramref name="s"/>.
        /// </summary>
        /// <param name="s"><see cref="Stream"/> to output.</param>
        public void ToYaml(Stream s)
        {
            ToYaml(s, DefaultConfig);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to <see cref="Stream"/> <paramref name="s"/>.
        /// </summary>
        /// <param name="s"><see cref="Stream"/> to output.</param>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public void ToYaml(Stream s, YamlConfig config)
        {
            DefaultPresenter.ToYaml(s, this, config);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to <see cref="TextWriter"/> <paramref name="tw"/>.
        /// </summary>
        /// <param name="tw"><see cref="TextWriter"/> to output.</param>
        public void ToYaml(TextWriter tw)
        {
            ToYaml(tw, DefaultConfig);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to <see cref="TextWriter"/> <paramref name="tw"/>.
        /// </summary>
        /// <param name="tw"><see cref="TextWriter"/> to output.</param>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public void ToYaml(TextWriter tw, YamlConfig config)
        {
            DefaultPresenter.ToYaml(tw, this, config);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to the file.
        /// </summary>
        /// <param name="FileName">Name of the file to output</param>
        public void ToYamlFile(string FileName)
        {
            ToYamlFile(FileName, DefaultConfig);
        }
        /// <summary>
        /// Convert <see cref="YamlNode"/> to a YAML text and save it to the file.
        /// </summary>
        /// <param name="FileName">Name of the file to output</param>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public void ToYamlFile(string FileName, YamlConfig config)
        {
            using ( var s = new FileStream(FileName, FileMode.Create) )
                DefaultPresenter.ToYaml(s, this, config);
        }
        #endregion

        #region static members

        /// <summary>
        /// Gets YAML's default tag prefix.
        /// </summary>
        /// <value>"tag:yaml.org,2002:"</value>
        public static string DefaultTagPrefix { get; private set; }
        /// <summary>
        /// Gets or sets the default configuration to customize serialization of <see cref="YamlNode"/>.
        /// </summary>
        public static YamlConfig DefaultConfig { get; set; }
        internal static YamlParser DefaultParser { get; set; }
        internal static YamlPresenter DefaultPresenter { get; set; }

        static YamlNode()
        {
            // Initializing order matters !
            DefaultTagPrefix = "tag:yaml.org,2002:";
            DefaultConfig = new YamlConfig();
            DefaultParser = new YamlParser();
            DefaultPresenter = new YamlPresenter();
        }

        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text</param>
        /// <returns>YAML nodes</returns>
        public static YamlNode[] FromYaml(string yaml)
        {
            return DefaultParser.Parse(yaml).ToArray();
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text</param>
        /// <returns>YAML nodes</returns>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public static YamlNode[] FromYaml(string yaml, YamlConfig config)
        {
            return DefaultParser.Parse(yaml, config).ToArray();
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="s"><see cref="Stream"/> from which YAML document is read.</param>
        /// <returns>YAML nodes</returns>
        public static YamlNode[] FromYaml(Stream s)
        {
            using ( var sr = new StreamReader(s) )
                return FromYaml(sr);
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="s"><see cref="Stream"/> from which YAML document is read.</param>
        /// <returns>YAML nodes</returns>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public static YamlNode[] FromYaml(Stream s, YamlConfig config)
        {
            using ( var sr = new StreamReader(s) )
                return FromYaml(sr, config);
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="tr"><see cref="TextReader"/> from which YAML document is read.</param>
        /// <returns>YAML nodes</returns>
        public static YamlNode[] FromYaml(TextReader tr)
        {
            var yaml = tr.ReadToEnd();
            return FromYaml(yaml);
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="tr"><see cref="TextReader"/> from which YAML document is read.</param>
        /// <returns>YAML nodes</returns>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public static YamlNode[] FromYaml(TextReader tr, YamlConfig config)
        {
            var yaml = tr.ReadToEnd();
            return FromYaml(yaml, config);
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="FileName">YAML File Name</param>
        /// <returns>YAML nodes</returns>
        public static YamlNode[] FromYamlFile(string FileName)
        {
            using ( var s = new FileStream(FileName, FileMode.Open) )
                return FromYaml(s);
        }
        /// <summary>
        /// Convert YAML text <paramref name="yaml"/> to a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="FileName">YAML File Name</param>
        /// <returns>YAML nodes</returns>
        /// <param name="config"><see cref="YamlConfig">YAML configuration</see> to customize serialization.</param>
        public static YamlNode[] FromYamlFile(string FileName, YamlConfig config)
        {
            using ( var s = new FileStream(FileName, FileMode.Open) )
                return FromYaml(s, config);
        }

        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlNode(string value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlNode(int value)
        {
            return new YamlScalar("!!int", YamlNode.DefaultConfig.TypeConverter.ConvertToString(value));
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlNode(double value)
        {
            return new YamlScalar("!!float", YamlNode.DefaultConfig.TypeConverter.ConvertToString(value));
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlNode(bool value)
        {
            return new YamlScalar("!!bool", YamlNode.DefaultConfig.TypeConverter.ConvertToString(value));
        }
        /// <summary>
        /// Implicit conversion from <see cref="DateTime"/> to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlNode(DateTime value)
        {
            YamlScalar node;
            DefaultConfig.TagResolver.Encode(value, out node);
            return node;
        }

        /// <summary>
        /// Convert shorthand tag starting with "!!" to the formal style that starts with "tag:yaml.org,2002:".
        /// </summary>
        /// <remarks>
        /// When <paramref name="tag"/> starts with "!!", it is converted into formal style.
        /// Otherwise, <paramref name="tag"/> is returned as is.
        /// </remarks>
        /// <example>
        /// <code>
        /// var tag = YamlNode.DefaultTagPrefix + "int";    // -> "tag:yaml.org,2002:int"
        /// tag = YamlNode.ShorthandTag(tag);               // -> "!!int"
        /// tag = YamlNode.ExpandTag(tag);                  // -> "tag:yaml.org,2002:int"
        /// </code>
        /// </example>
        /// <param name="tag">Tag in the shorthand style.</param>
        /// <returns>Tag in formal style.</returns>
        public static string ExpandTag(string tag)
        {
            if ( tag.StartsWith("!!") )
                return DefaultTagPrefix + tag.Substring(2);
            return tag;
        }

        /// <summary>
        /// Convert a formal style tag that starts with "tag:yaml.org,2002:" to 
        /// the shorthand style that starts with "!!".
        /// </summary>
        /// <remarks>
        /// When <paramref name="tag"/> contains YAML standard types, it is converted into !!xxx style.
        /// Otherwise, <paramref name="tag"/> is returned as is.
        /// </remarks>
        /// <example>
        /// <code>
        /// var tag = YamlNode.DefaultTagPrefix + "int";    // -> "tag:yaml.org,2002:int"
        /// tag = YamlNode.ShorthandTag(tag);               // -> "!!int"
        /// tag = YamlNode.ExpandTag(tag);                  // -> "tag:yaml.org,2002:int"
        /// </code>
        /// </example>
        /// <param name="tag">Tag in formal style.</param>
        /// <returns>Tag in compact style.</returns>
        public static string ShorthandTag(string tag)
        {
            if ( tag != null && tag.StartsWith(DefaultTagPrefix) )
                return "!!" + tag.Substring(DefaultTagPrefix.Length);
            return tag;
        }

        #endregion
    }

    /// <summary>
    /// Represents a scalar node in a YAML document.
    /// </summary>
    /// <example>
    /// <code>
    /// var string_node = new YamlNode("abc");
    /// Assert.AreEqual("!!str", string_node.ShorthandTag());
    /// 
    /// var int_node1= new YamlNode(YamlNode.DefaultTagPrefix + "int", "1");
    /// Assert.AreEqual("!!int", int_node1.ShorthandTag());
    /// 
    /// // shorthand tag style can be specified
    /// var int_node2= new YamlNode("!!int", "1");
    /// Assert.AreEqual(YamlNode.DefaultTagPrefix + "int", int_node1.Tag);
    /// Assert.AreEqual("!!int", int_node1.ShorthandTag());
    /// 
    /// // or use implicit conversion
    /// YamlNode int_node3 = 1;
    /// 
    /// // YamlNodes Equals to another node when their values are equal.
    /// Assert.AreEqual(int_node1, int_node2);
    /// 
    /// // Of course, they are different if compaired by references.
    /// Assert.IsTrue(int_node1 != int_node2);
    /// </code>
    /// </example>
    public class YamlScalar: YamlNode
    {
        /// <summary>
        /// String expression of the node value.
        /// </summary>
        public string Value
        {
            get { return value; }
            set { this.value = value; OnChanged(); }
        }
        string value;

        #region constructors
        /// <summary>
        /// Create empty string node.
        /// </summary>
        public YamlScalar() { Tag = ExpandTag("!!str"); Value = ""; }
        /// <summary>
        /// Initialize string node that has <paramref name="value"/> as its content.
        /// </summary>
        /// <param name="value">Value of the node.</param>
        public YamlScalar(string value) { Tag = ExpandTag("!!str"); Value = value; }
        /// <summary>
        /// Create a scalar node with arbitral tag.
        /// </summary>
        /// <param name="tag">Tag to the node.</param>
        /// <param name="value">Value of the node.</param>
        public YamlScalar(string tag, string value) { Tag = ExpandTag(tag); Value = value; }
        /// <summary>
        /// Initialize an integer node that has <paramref name="value"/> as its content.
        /// </summary>
        public YamlScalar(int value)
        {
            Tag = ExpandTag("!!int");
            Value = YamlNode.DefaultConfig.TypeConverter.ConvertToString(value);
        }
        /// <summary>
        /// Initialize a float node that has <paramref name="value"/> as its content.
        /// </summary>
        public YamlScalar(double value)
        {
            Tag = ExpandTag("!!float");
            Value = YamlNode.DefaultConfig.TypeConverter.ConvertToString(value);
        }
        /// <summary>
        /// Initialize a bool node that has <paramref name="value"/> as its content.
        /// </summary>
        public YamlScalar(bool value)
        {
            Tag = ExpandTag("!!bool");
            Value = YamlNode.DefaultConfig.TypeConverter.ConvertToString(value);
        }
        /// <summary>
        /// Initialize a timestamp node that has <paramref name="value"/> as its content.
        /// </summary>
        public YamlScalar(DateTime value)
        {
            YamlScalar node = value;
            Tag = node.Tag;
            Value = node.Value;
        } 

        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlScalar(string value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlScalar(int value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlScalar(double value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Implicit conversion from string to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlScalar(bool value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Implicit conversion from <see cref="DateTime"/> to <see cref="YamlScalar"/>.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static implicit operator YamlScalar(DateTime value)
        {
            YamlScalar node;
            DefaultConfig.TagResolver.Encode(value, out node);
            return node;
        }
        #endregion

        /// <summary>
        /// Call this function when the content of the node is changed.
        /// </summary>
        protected override void OnChanged()
        {
            base.OnChanged();
            UpdateNativeObject();
        }
        
        void UpdateNativeObject()
        {
            object value;
            if ( NativeObjectAvailable = DefaultConfig.TagResolver.Decode(this, out value) ) {
                NativeObject = value;
            } else {
                if ( ( ShorthandTag() == "!!float" ) && ( Value != null ) && new Regex(@"0|[1-9][0-9]*").IsMatch(Value) ) {
                    NativeObject = Convert.ToDouble(Value);
                    NativeObjectAvailable = true;
                }
            }
        }
        /// <summary>
        /// <para>When the node has YAML's standard scalar type, the native object corresponding to
        /// it can be got from this property. To see if this property contains a valid data,
        /// refer to <see cref="NativeObjectAvailable"/>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is not available. See <see cref="NativeObjectAvailable"/>.</exception>
        /// <remarks>
        /// <para>This property is available when <see cref="YamlNode.DefaultConfig"/>.<see cref="YamlConfig.TagResolver"/> contains
        /// an entry for the nodes tag and defines how to decode the <see cref="Value"/> property into native objects.</para>
        /// <para>When this property is available, equality of the scalar node is evaluated by comparing the <see cref="NativeObject"/>
        /// properties by the language default equality operator.</para>
        /// </remarks>
        [Yaml.Serialization.YamlSerialize(System.Yaml.Serialization.YamlSerializeMethod.Never)]
        public object NativeObject {
            get
            {
                if ( !NativeObjectAvailable )
                    throw new InvalidOperationException("NativeObject is not available.");
                return nativeObject;
            }
            private set
            {
                nativeObject = value;
            } 
        }
        object nativeObject;
        /// <summary>
        /// Gets if <see cref="NativeObject"/> contains a valid content.
        /// </summary>
        public bool NativeObjectAvailable { get; private set; }

        internal override bool Equals(YamlNode b, ObjectRepository repository)
        {
            bool skip;
            if(! base.EqualsSub(b, repository, out skip) )
                return false;
            if(skip)
                return true;
            YamlScalar aa = this;
            YamlScalar bb = (YamlScalar)b;
            if ( NativeObjectAvailable ) {
                return bb.NativeObjectAvailable && 
                    (aa.NativeObject == null ? 
                        bb.NativeObject==null :
                        aa.NativeObject.Equals(bb.NativeObject) );
            } else {
                if ( ShorthandTag() == "!!str" ) {
                    return aa.Value == bb.Value;
                } else {
                    // Node with non standard tag is compared by its identity.
                    return false; 
                }
            }
        }
        /// <summary>
        /// Returns the hash code. 
        /// The returned value will be cached until <see cref="YamlNode.OnChanged"/> is called.
        /// </summary>
        /// <returns>Hash code</returns>
        protected override int GetHashCodeCore()
        {
            if ( NativeObjectAvailable ) {
                if ( NativeObject == null ) {
                    return 0;
                } else {
                    return NativeObject.GetHashCode();
                }
            } else {
                if ( ShorthandTag() == "!!str" ) {
                    return ( Value.GetHashCode() * 193 ) ^ Tag.GetHashCode();
                } else {
                    return TypeUtils.HashCodeByRef<YamlScalar>.GetHashCode(this);
                }
            }
        }

        internal override string ToString(ref int length)
        {
            var tag= ShorthandTag() == "!!str" ? "" : ShorthandTag() + " ";
            length -= tag.Length + 1;
            if ( length <= 0 )
                return tag + "\"" + "...";
            if ( Value.Length > length )
                return tag + "\"" + Value.Substring(0, length) + "...";
            length -= Value.Length + 1;
            return tag + "\"" + Value + "\"";
        }
    }

    /// <summary>
    /// Abstract base class of <see cref="YamlNode"/> that have child nodes.
    /// 
    /// <see cref="YamlMapping"/> and <see cref="YamlSequence"/> inherites from this class.
    /// </summary>
    public abstract class YamlComplexNode: YamlNode
    {
        /// <summary>
        /// Calculate hash code from <see cref="YamlNode.Tag"/> property and all child nodes.
        /// The result is cached.
        /// </summary>
        /// <returns>Hash value for the object.</returns>
        protected override int GetHashCodeCore() 
        {
            return GetHashCodeCoreSub(0,
                new Dictionary<YamlNode, int>(
                        TypeUtils.EqualityComparerByRef<YamlNode>.Default));
        }

        /// <summary>
        /// Calculates the hash code for a collection object. This function is called recursively 
        /// on the child objects with the sub cache code repository for the nodes already appeared
        /// in the node tree.
        /// </summary>
        /// <param name="path">The cache code for the path where this node was found.</param>
        /// <param name="dict">Repository of the nodes that already appeared in the node tree.
        /// Sub hash code for the nodes can be refered to from this dictionary.</param>
        /// <returns></returns>
        protected abstract int GetHashCodeCoreSub(int path, Dictionary<YamlNode, int> dict);
    }

    /// <summary>
    /// Represents a mapping node in a YAML document. 
    /// Use <see cref="IDictionary&lt;YamlNode,YamlNode&gt;">IDictionary&lt;YamlNode,YamlNode&gt;</see> interface to
    /// manipulate child key/value pairs.
    /// </summary>
    /// <remarks>
    /// Child items can be accessed via IDictionary&lt;YamlNode, YamlNode&gt; interface.
    /// 
    /// Note that mapping object can not contain multiple keys with same value.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a mapping.
    /// var map1 = new YamlMapping(
    ///     // (key, value) pairs should be written sequential
    ///     new YamlScalar("key1"), new YamlScalar("value1"),
    ///     "key2", "value2" // implicitely converted to YamlScalar
    ///     );
    ///     
    /// // Refer to the mapping.
    /// Assert.AreEqual( map1[new Scalar("key1")], new YamlScalar("value1") );
    /// Assert.AreEqual( map1["key1"], "value1" );
    /// 
    /// // Add an entry.
    /// map1.Add( "key3", new YamlSequence( "value3a", "value3b" ) );
    /// 
    /// // Create another mapping.
    /// var map2 = new YamlMapping(
    ///     "key1", "value1",
    ///     "key2", "value2",
    ///     "key3", new YamlSequence( "value3a", "value3b" )
    ///     );
    ///     
    /// // Mappings are equal when they have objects that are equal to each other.
    /// Assert.IsTrue( map1.Equals( map2 ) );
    /// </code>
    /// </example>
    public class YamlMapping: YamlComplexNode, IDictionary<YamlNode, YamlNode>
    {
        RehashableDictionary<YamlNode, YamlNode> mapping =
            new RehashableDictionary<YamlNode, YamlNode>();

        /// <summary>
        /// Calculates the hash code for a collection object. This function is called recursively 
        /// on the child objects with the sub cache code repository for the nodes already appeared
        /// in the node tree.
        /// </summary>
        /// <param name="path">The cache code for the path where this node was found.</param>
        /// <param name="dict">Repository of the nodes that already appeared in the node tree.
        /// Sub hash code for the nodes can be refered to from this dictionary.</param>
        /// <returns></returns>
        protected override int GetHashCodeCoreSub(int path, Dictionary<YamlNode, int> dict)
        {
            if ( dict.ContainsKey(this) )
                return dict[this].GetHashCode() * 27 + path;
            dict.Add(this, path);

            // Unless !!map, the hash code is based on the node's identity.
            if ( ShorthandTag() != "!!map" )
                return TypeUtils.HashCodeByRef<YamlMapping>.GetHashCode(this);

            var result = Tag.GetHashCode();
            foreach ( var item in this ) {
                int hash_for_key;
                if ( item.Key is YamlComplexNode ) {
                    hash_for_key = GetHashCodeCoreSub(path * 317, dict);
                } else {
                    hash_for_key = item.Key.GetHashCode();
                }
                result += hash_for_key * 971;
                if ( item.Value is YamlComplexNode ) {
                    result += GetHashCodeCoreSub(path * 317 + hash_for_key * 151, dict);
                } else {
                    result += item.Value.GetHashCode() ^ hash_for_key;
                }
            }
            return result;
        }
        
        internal override bool Equals(YamlNode b, ObjectRepository repository)
        {
            YamlNode a = this;

            bool skip;
            if ( !base.EqualsSub(b, repository, out skip) )
                return false;
            if ( skip )
                return true;

            // Unless !!map, the hash equality is evaluated by the node's identity.
            if ( ShorthandTag() != "!!map" )
                return false;

            var aa = this;
            var bb = (YamlMapping)b;
            if ( aa.Count != bb.Count )
                return false;

            var status= repository.CurrentStatus;
            foreach ( var item in this ) {
                var candidates = bb.ItemsFromHashCode(item.Key.GetHashCode());
                KeyValuePair<YamlNode, YamlNode> theone = new KeyValuePair<YamlNode,YamlNode>();
                if ( !candidates.Any(subitem => {
                    if ( item.Key.Equals(subitem.Key, repository) ) {
                        theone = subitem;
                        return true;
                    }
                    repository.CurrentStatus = status;
                    return false;
                }) )
                    return false;
                if(!item.Value.Equals(theone.Value, repository))
                    return false;
            }
            return true;
        }

        internal ICollection<KeyValuePair<YamlNode, YamlNode>> ItemsFromHashCode(int key_hash)
        {
            return mapping.ItemsFromHash(key_hash);
        }

        /// <summary>
        /// Create a YamlMapping that contains <paramref name="nodes"/> in it.
        /// </summary>
        /// <example>
        /// <code>
        /// // Create a mapping.
        /// var map1 = new YamlMapping(
        ///     // (key, value) pairs should be written sequential
        ///     new YamlScalar("key1"), new YamlScalar("value1"),
        ///     new YamlScalar("key2"), new YamlScalar("value2")
        ///     );
        /// </code>
        /// </example>
        /// <exception cref="ArgumentException">Even number of arguments are expected.</exception>
        /// <param name="nodes">(key, value) pairs are written sequential.</param>
        public YamlMapping(params YamlNode[] nodes)
        {
            mapping.Added += ChildAdded;
            mapping.Removed += ChildRemoved;
            if ( nodes.Length / 2 != nodes.Length / 2.0 )
                throw new ArgumentException("Even number of arguments are expected.");
            Tag = DefaultTagPrefix + "map";
            for ( int i = 0; i < nodes.Length; i += 2 )
                Add(nodes[i + 0], nodes[i + 1]);
        }

        void CheckDuplicatedKeys()
        {
            foreach ( var entry in this )
                CheckDuplicatedKeys(entry.Key);
        }

        void CheckDuplicatedKeys(YamlNode key)
        {
            foreach(var k in mapping.ItemsFromHash(key.GetHashCode()))
                if( ( k.Key != key ) && k.Key.Equals(key) )
                    throw new InvalidOperationException("Duplicated key found.");
        }

        void ChildRemoved(object sender, RehashableDictionary<YamlNode, YamlNode>.DictionaryEventArgs e)
        {
            e.Key.Changed -= KeyChanged;
            e.Value.Changed -= ChildChanged;
            OnChanged();
            CheckDuplicatedKeys();
        }

        void ChildAdded(object sender, RehashableDictionary<YamlNode, YamlNode>.DictionaryEventArgs e)
        {
            e.Key.Changed += KeyChanged;
            e.Value.Changed += ChildChanged;
            OnChanged();
            CheckDuplicatedKeys();
        }

        void KeyChanged(object sender, EventArgs e)
        {
            ChildChanged(sender, e);
            CheckDuplicatedKeys((YamlNode)sender);
        }

        void ChildChanged(object sender, EventArgs e)
        {
            OnChanged();
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();
            ProcessMergeKey();
        }
        void ProcessMergeKey()
        {
            // find merge key
            var merge_key = Keys.FirstOrDefault(key => key.Tag == YamlNode.ExpandTag("!!merge"));
            if ( merge_key == null )
                return;

            // merge the value
            var value = this[merge_key];
            if ( value is YamlMapping ) {
                Remove(merge_key);
                Merge((YamlMapping)value);
            } else
            if ( value is YamlSequence ) {
                Remove(merge_key);
                foreach ( var item in (YamlSequence)value )
                    if ( item is YamlMapping )
                        Merge((YamlMapping)item);
            } else {
                // ** ignore
                // throw new InvalidOperationException(
                //     "Can't merge the value into a mapping: " + value.ToString());
            }
        }
        void Merge(YamlMapping map)
        {
            foreach ( var entry in map ) 
                if ( !ContainsKey(entry.Key) )
                    Add(entry.Key, entry.Value);
        }

        /// <summary>
        /// Enumerate child nodes.
        /// </summary>
        /// <returns>Inumerator that iterates child nodes</returns>
        internal override string ToString(ref int length)
        {
            var s = "";
            var t = ( ShorthandTag() == "!!map" ? "" : ShorthandTag() + " " );
            length -= t.Length + 2;
            if ( length < 0 )
                return "{" + t + "...";
            foreach ( var entry in this ) {
                if ( s != "" ) {
                    s += ", ";
                    length -= 2;
                }
                s += entry.Key.ToString(ref length);
                if ( length < 0 )
                    return "{" + t + s;
                s += ": ";
                length -= 2;
                s += entry.Value.ToString(ref length);
                if ( length < 0 )
                    return "{" + t + s;
            }
            return "{" + t + s + "}";
        }

        #region IDictionary<Node,Node> members

        /// <summary>
        /// Adds an element with the provided key and value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is a null reference.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists.</exception>
        /// <param name="key">The node to use as the key of the element to add.</param>
        /// <param name="value">The node to use as the value of the element to add.</param>
        public void Add(YamlNode key, YamlNode value)
        {
            if ( key == null || value == null )
                throw new ArgumentNullException("Key and value must be a valid YamlNode.");
            mapping.Add(key, value);
        }

        /// <summary>
        /// Determines whether the <see cref="YamlMapping"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="YamlMapping"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference </exception>
        /// <returns> true if the <see cref="YamlMapping"/> contains an element with the key that is equal to the specified value; otherwise, false.</returns>
        public bool ContainsKey(YamlNode key)
        {
            return mapping.ContainsKey(key);
        }
        /// <summary>
        /// Gets an ICollection&lt;YamlNode&gt; containing the keys of the <see cref="YamlMapping"/>.
        /// </summary>
        public ICollection<YamlNode> Keys
        {
            get { return mapping.Keys; }
        }
        /// <summary>
        /// Removes the element with the specified key from the <see cref="YamlMapping"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns> true if the element is successfully removed; otherwise, false. This method also returns false if key was not found in the original <see cref="YamlMapping"/>.</returns>
        public bool Remove(YamlNode key)
        {
            return mapping.Remove(key);
        }
        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; 
        /// otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns> true if the object that implements <see cref="YamlMapping"/> contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(YamlNode key, out YamlNode value)
        {
            return mapping.TryGetValue(key, out value);
        }
        /// <summary>
        /// Gets an ICollection&lt;YamlNode&gt; containing the values of the <see cref="YamlMapping"/>.
        /// </summary>
        public ICollection<YamlNode> Values
        {
            get { return mapping.Values; }
        }
        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The element with the specified key.</returns>
        /// <exception cref="ArgumentNullException">key is a null reference</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and key is not found.</exception>
        public YamlNode this[YamlNode key]
        {
            get { return mapping[key]; }
            set { mapping[key] = value; }
        }
        #region ICollection<KeyValuePair<Node,Node>> members
        void ICollection<KeyValuePair<YamlNode, YamlNode>>.Add(KeyValuePair<YamlNode, YamlNode> item)
        {
            ( (ICollection<KeyValuePair<YamlNode, YamlNode>>)mapping ).Add(item);
        }
        /// <summary>
        /// Removes all entries from the <see cref="YamlMapping"/>.
        /// </summary>
        public void Clear()
        {
            mapping.Clear();
        }
        /// <summary>
        /// Determines whether the <see cref="YamlMapping"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="YamlMapping"/>.</param>
        /// <returns>true if item is found in the <see cref="YamlMapping"/> otherwise, false.</returns>
        public bool Contains(KeyValuePair<YamlNode, YamlNode> item)
        {
            return ( (ICollection<KeyValuePair<YamlNode, YamlNode>>)mapping ).Contains(item);
        }
        void ICollection<KeyValuePair<YamlNode, YamlNode>>.CopyTo(KeyValuePair<YamlNode, YamlNode>[] array, int arrayIndex)
        {
            ( (ICollection<KeyValuePair<YamlNode, YamlNode>>)mapping ).CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Returns the number of entries in a <see cref="YamlMapping"/>.
        /// </summary>
        public int Count
        {
            get { return mapping.Count; }
        }
        bool ICollection<KeyValuePair<YamlNode, YamlNode>>.IsReadOnly
        {
            get { return false; }
        }
        bool ICollection<KeyValuePair<YamlNode, YamlNode>>.Remove(KeyValuePair<YamlNode, YamlNode> item)
        {
            return ( (ICollection<KeyValuePair<YamlNode, YamlNode>>)mapping ).Remove(item);
        }
        #endregion
        #region IEnumerable<KeyValuePair<Node,Node>> members
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="YamlMapping"/>.
        /// </summary>
        /// <returns>An enumerator that iterates through the <see cref="YamlMapping"/>.</returns>
        public IEnumerator<KeyValuePair<YamlNode, YamlNode>> GetEnumerator()
        {
            return mapping.GetEnumerator();
        }
        #endregion
        #region IEnumerable members
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mapping.GetEnumerator();
        }
        #endregion
        #endregion
    }

    /// <summary>
    /// Represents a sequence node in a YAML document.
    /// Use <see cref="IList&lt;YamlNode&gt;">IList&lt;YamlNode&gt;</see> interface 
    /// to manipulate child nodes.
    /// </summary>
    public class YamlSequence: YamlComplexNode, IList<YamlNode>, IDisposable
    {
        /// <summary>
        /// Create a sequence node that has <paramref name="nodes"/> as its child.
        /// </summary>
        /// <param name="nodes">Child nodes of the sequence.</param>
        public YamlSequence(params YamlNode[] nodes)
        {
            Tag = DefaultTagPrefix + "seq";
            for ( int i = 0; i < nodes.Length; i++ )
                Add(nodes[i]);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        /// <summary>
        /// Calculates the hash code for a collection object. This function is called recursively 
        /// on the child objects with the sub cache code repository for the nodes already appeared
        /// in the node tree.
        /// </summary>
        /// <param name="path">The cache code for the path where this node was found.</param>
        /// <param name="dict">Repository of the nodes that already appeared in the node tree.
        /// Sub hash code for the nodes can be refered to from this dictionary.</param>
        /// <returns></returns>
        protected override int GetHashCodeCoreSub(int path, Dictionary<YamlNode, int> dict)
        {
            if ( dict.ContainsKey(this) )
                return dict[this].GetHashCode() * 27 + path;
            dict.Add(this, path);

            // Unless !!seq, the hash code is based on the node's identity.
            if ( ShorthandTag() != "!!seq" )
                return TypeUtils.HashCodeByRef<YamlSequence>.GetHashCode(this);

            var result = Tag.GetHashCode();
            for ( int i=0; i<Count; i++) {
                var item= sequence[i];
                if ( item is YamlComplexNode ) {
                    result += GetHashCodeCoreSub(path * 317 ^ i.GetHashCode(), dict);
                } else {
                    result += item.GetHashCode() ^ i.GetHashCode();
                }
            }
            return result;
        }

        internal override bool Equals(YamlNode b, ObjectRepository repository)
        {
            YamlNode a = this;
            bool skip;
            if ( !base.EqualsSub(b, repository, out skip) )
                return false;
            if ( skip )
                return true;

            // Unless !!seq, the hash equality is evaluated by the node's identity.
            if ( ShorthandTag() != "!!seq" )
                return false;

            var aa = this;
            var bb = (YamlSequence)b;
            if ( aa.Count != bb.Count )
                return false;

            var iter_a = aa.GetEnumerator();
            var iter_b = bb.GetEnumerator();
            while ( iter_a.MoveNext() && iter_b.MoveNext() )
                if ( !iter_a.Current.Equals(iter_b.Current, repository) )
                    return false;
            return true;
        }
        
        void OnItemAdded(YamlNode item)
        {
            item.Changed += ItemChanged;
        }
        void OnItemRemoved(YamlNode item)
        {
            item.Changed -= ItemChanged;
        }
        void ItemChanged(object sender, EventArgs e)
        {
            OnChanged();
        }
        
        internal override string ToString(ref int length)
        {
            var t = ( ShorthandTag() == "!!seq" ? "" : ShorthandTag() + " " );
            length -= t.Length + 2;
            if ( length < 0 )
                return "[" + t + "...";
            var s = "";
            foreach ( var item in this ) {
                if ( item != this.First() ) {
                    s += ", ";
                    length -= 2;
                }
                s += item.ToString(ref length);
                if ( length < 0 )
                    return "[" + t + s;
            }
            return "[" + t + s + "]";
        }

        #region IList<Node> members
        List<YamlNode> sequence = new List<YamlNode>();
        /// <summary>
        /// Determines the index of a specific child node in the <see cref="YamlSequence"/>.
        /// </summary>
        /// <remarks>
        /// If an node appears multiple times in the sequence, the IndexOf method always returns the first instance found.
        /// </remarks>
        /// <param name="item">The child node to locate in the <see cref="YamlSequence"/>.</param>
        /// <returns>The index of <paramref name="item"/> if found in the sequence; otherwise, -1.</returns>
        public int IndexOf(YamlNode item)
        {
            return sequence.IndexOf(item);
        }
        /// <summary>
        /// Inserts an item to the <see cref="YamlSequence"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The node to insert into the <see cref="YamlSequence"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the 
        /// <see cref="YamlSequence"/>.</exception>
        /// <remarks>
        /// <para>If <paramref name="index"/> equals the number of items in the <see cref="YamlSequence"/>, 
        /// then <paramref name="item"/> is appended to the sequence.</para>
        /// <para>The nodes that follow the insertion point move down to accommodate the new node.</para>
        /// </remarks>
        public void Insert(int index, YamlNode item)
        {
            sequence.Insert(index, item);
            OnItemAdded(item);
        }
        /// <summary>
        /// Removes the <see cref="YamlSequence"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the node to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="YamlSequence"/>.</exception>
        /// <remarks>
        /// The nodes that follow the removed node move up to occupy the vacated spot. 
        /// </remarks>
        public void RemoveAt(int index)
        {
            var item = sequence[index];
            sequence.RemoveAt(index);
            OnItemRemoved(item);
        }
        /// <summary>
        /// Gets or sets the node at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the node to get or set.</param>
        /// <returns>The node at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="YamlSequence"/>).</exception>
        /// <remarks>
        /// <para>This property provides the ability to access a specific node in the sequence by using the following syntax: mySequence[index].</para>
        /// </remarks>
        public YamlNode this[int index]
        {
            get { return sequence[index]; }
            set {
                if ( index < sequence.Count ) {
                    var item = sequence[index];
                    sequence[index] = value;
                    OnItemRemoved(item);
                } else {
                    sequence[index] = value;
                }
                OnItemAdded(value);
            }
        }
        /// <summary>
        /// Adds an item to the <see cref="YamlSequence"/>.
        /// </summary>
        /// <param name="item">The node to add to the <see cref="YamlSequence"/>.</param>
        public void Add(YamlNode item)
        {
            sequence.Add(item);
            OnItemAdded(item);
        }
        /// <summary>
        /// Removes all nodes from the <see cref="YamlSequence"/>.
        /// </summary>
        public void Clear()
        {
            var old = sequence;
            sequence = new List<YamlNode>();
            foreach ( var item in old )
                OnItemRemoved(item);
        }
        /// <summary>
        /// Determines whether a sequence contains a child node that equals to the specified <paramref name="value"/>
        /// by using the default equality comparer.
        /// </summary>
        /// <param name="value">The node value to locate in the sequence.</param>
        /// <returns>true If the sequence contains an node that has the specified value; otherwise, false.</returns>
        /// <example>
        /// <code>
        /// var seq = new YamlSequence(new YamlScalar("a"));
        /// 
        /// // different object that has same value
        /// Assert.IsTrue(seq.Contains(new YamlScalar("a")));
        /// 
        /// // different value
        /// Assert.IsFalse(s.Contains(str("b")));
        /// </code>
        /// </example>
        public bool Contains(YamlNode value)
        {
            return sequence.Contains(value);
        }
        /// <summary>
        /// Copies the child nodes of the <see cref="YamlSequence"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="YamlSequence"/>.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">
        /// <para>array is multidimensional.</para>
        /// <para>-or-</para>
        /// <para>The number of elements in the source <see cref="YamlSequence"/> is greater than the available space from 
        /// <paramref name="arrayIndex"/> to the end of the destination array.</para>
        /// </exception>
        public void CopyTo(YamlNode[] array, int arrayIndex)
        {
            sequence.CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Gets the number of child nodes of the <see cref="YamlSequence"/>.
        /// </summary>
        /// <value>The number of child nodes of the sequence.</value>
        public int Count
        {
            get { return sequence.Count; }
        }
        bool ICollection<YamlNode>.IsReadOnly
        {
            get { return ( (ICollection<YamlNode>)sequence ).IsReadOnly; }
        }
        /// <summary>
        /// Removes the first occurrence of a specific node from the <see cref="YamlSequence"/>.
        /// </summary>
        /// <param name="node">The node to remove from the <see cref="YamlSequence"/>.</param>
        /// <returns> true if <paramref name="node"/> was successfully removed from the <see cref="YamlSequence"/>; otherwise, false. 
        /// This method also returns false if <paramref name="node"/> is not found in the original <see cref="YamlSequence"/>.</returns>
        /// 
        public bool Remove(YamlNode node)
        {
            var i = sequence.FindIndex(item => item.Equals(node));
            if ( i < 0 )
                return false;
            var item2 = sequence[i];
            sequence.RemoveAt(i);
            OnItemRemoved(item2);
            return true;
        }
        /// <summary>
        /// Returns an enumerator that iterates through the all child nodes.
        /// </summary>
        /// <returns>An enumerator that iterates through the all child nodes.</returns>
        public IEnumerator<YamlNode> GetEnumerator()
        {
            return sequence.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ( (System.Collections.IEnumerable)sequence ).GetEnumerator();
        }
        #endregion
    }

    /// <summary>
    /// Implements utility functions to instantiating YamlNode's
    /// </summary>
    /// <example>
    /// <code>
    /// var node_tree = seq(
    ///     str("abc"),
    ///     str("def"),
    ///     map(
    ///         str("key"), str("value"),
    ///         str("key2"), seq( str("value2a"), str("value2b") )
    ///     ),
    ///     str("2"), // !!str
    ///     str("!!int", "2")
    /// );
    /// 
    /// string yaml = node_tree.ToYaml();
    /// 
    /// // %YAML 1.2
    /// // ---
    /// // - abc
    /// // - def
    /// // - key: value
    /// //   key2: [ value2a, value2b ]
    /// // - "2"         # !!str
    /// // - 2           # !!int
    /// // ...
    /// </code>                                                   
    /// </example>
    public class YamlNodeManipulator
    {
        /// <summary>
        /// Create a scalar node. Tag is set to be "!!str".
        /// </summary>
        /// <example>
        /// <code>
        /// var node_tree = seq(
        ///     str("abc"),
        ///     str("def"),
        ///     map(
        ///         str("key"), str("value"),
        ///         str("key2"), seq( str("value2a"), str("value2b") )
        ///     ),
        ///     str("2"), // !!str
        ///     str("!!int", "2")
        /// );
        /// 
        /// string yaml = node_tree.ToYaml();
        /// 
        /// // %YAML 1.2
        /// // ---
        /// // - abc
        /// // - def
        /// // - key: value
        /// //   key2: [ value2a, value2b ]
        /// // - "2"         # !!str
        /// // - 2           # !!int
        /// // ...
        /// </code>                                                   
        /// </example>
        /// <param name="value">Value for the scalar node.</param>
        /// <returns>Created scalar node.</returns>
        protected static YamlScalar str(string value)
        {
            return new YamlScalar(value);
        }
        /// <summary>
        /// Create a scalar node.
        /// </summary>
        /// <param name="tag">Tag for the scalar node.</param>
        /// <param name="value">Value for the scalar node.</param>
        /// <returns>Created scalar node.</returns>
        protected static YamlScalar str(string tag, string value)
        {
            return new YamlScalar(tag, value);
        }
        /// <summary>
        /// Create a sequence node. Tag is set to be "!!seq".
        /// </summary>
        /// <param name="nodes">Child nodes.</param>
        /// <returns>Created sequence node.</returns>
        protected static YamlSequence seq(params YamlNode[] nodes)
        {
            return new YamlSequence(nodes);
        }
        /// <summary>
        /// Create a sequence node. 
        /// </summary>
        /// <param name="nodes">Child nodes.</param>
        /// <param name="tag">Tag for the seuqnce.</param>
        /// <returns>Created sequence node.</returns>
        protected static YamlSequence seq_tag(string tag, params YamlNode[] nodes)
        {
            var result= new YamlSequence(nodes);
            result.Tag= tag;
            return result;
        }
        /// <summary>
        /// Create a mapping node. Tag is set to be "!!map".
        /// </summary>
        /// <param name="nodes">Sequential list of key/value pairs.</param>
        /// <returns>Created mapping node.</returns>
        protected static YamlMapping map(params YamlNode[] nodes)
        {
            return new YamlMapping(nodes);
        }
        /// <summary>
        /// Create a mapping node. 
        /// </summary>
        /// <param name="nodes">Sequential list of key/value pairs.</param>
        /// <param name="tag">Tag for the mapping.</param>
        /// <returns>Created mapping node.</returns>
        protected static YamlMapping map_tag(string tag, params YamlNode[] nodes)
        {
            var map = new YamlMapping(nodes);
            map.Tag = tag;
            return map;
        }
    }
}
