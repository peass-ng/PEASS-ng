using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

namespace System.Yaml
{                               
    /// <summary>
    /// <para>When <see cref="Parser&lt;State&gt;"/> reports syntax error by exception, this class is thrown.</para>
    /// 
    /// <para>Sytax errors can also be reported by simply returing false with giving some warnings.</para>
    /// </summary>
    internal class ParseErrorException: Exception
    {
        /// <summary>
        /// Initialize an instance of <see cref="ParseErrorException"/>
        /// </summary>
        /// <param name="message">Error message.</param>
        public ParseErrorException(string message) : base(message) { }
    }

    /// <summary>
    /// <para>Base class to implement a parser class.</para>
    /// 
    /// <para>It allows not very efficient but easy implementation of a text parser along 
    /// with a parameterized BNF productions.</para>
    /// </summary>
    /// <typeparam name="State">Parser specific state structure.</typeparam>
    internal abstract class Parser<State>
        where State: struct
    {
        /// <summary>
        /// Parse the <paramref name="text"/> using the <paramref name="start_rule"/> 
        /// as the starting rule.
        /// </summary>
        /// <param name="start_rule">Starting rule.</param>
        /// <param name="text">Text to be parsed.</param>
        /// <returns></returns>
        protected bool Parse(Func<bool> start_rule, string text)
        {
            this.text = text;
            InitializeParser();
            return start_rule();
        }
        void InitializeParser()
        {
            InitializeLines();
            p = 0;
            stringValue.Length = 0;
            state = new State();
        }

        #region Fields and Properties
        /// <summary>
        /// <para>Gets / sets source text to be parsed.</para>
        /// <para>While parsing, this variable will not be changed.</para>
        /// <para>The current position to be read by parser is represented by the field <see cref="p"/>.</para>
        /// <para>Namely, the next character to be read is <c>text[p]</c>.</para>
        /// </summary>
        protected string text;
        /// <summary>
        /// <para>The current reading position.</para>
        /// 
        /// <para>The next character to be read by the parser is <c>text[p]</c>.</para>
        /// 
        /// <para>Increase <see cref="p"/> to reduce some part of source text <see cref="text"/>.</para>
        /// 
        /// <para>The current position <see cref="p"/> is automatically reverted at rewinding.</para>
        /// </summary>
        /// <example>
        /// Example to show how to reduce BNF reduction rule of ( "t" "e" "x" "t" ).
        /// <code>
        ///   return RewindUnless(()=>
        ///       text[p++] == 't' &amp;&amp;
        ///       text[p++] == 'e' &amp;&amp;
        ///       text[p++] == 'x' &amp;&amp;
        ///       text[p++] == 't'
        ///   );
        /// </code>
        /// </example>
        protected int p;
        /// <summary>
        /// <para>Use this variable to build some string data from source text.</para>
        /// 
        /// <para>It will be automatically reverted at rewinding.</para>
        /// </summary>
        protected StringBuilder stringValue = new StringBuilder();
        /// <summary>
        /// <para>Individual-parser-specific state object.</para>
        /// 
        /// <para>It will be automatically reverted at rewinding.</para>
        /// 
        /// <para>If some action, in addition to simply restore the value of the state object,
        /// is needed to recover the previous state, override <see cref="Rewind"/>
        /// method.</para>
        /// </summary>
        protected State state;
        /// <summary>
        /// Get current position represented by raw and column.
        /// </summary>
        public Position CurrentPosition
        {   
            get
            {
                Position pos = new Position();
                pos.Raw = Lines.BinarySearch(p);
                if ( pos.Raw < 0 ) {
                    pos.Raw = ~pos.Raw;
                    pos.Column = p - Lines[pos.Raw - 1] + 1;
                } else {
                    pos.Raw++; // 1 base
                    pos.Column = 1;
                }
                return pos;
            }
        }
        /// <summary>
        /// Initialize <see cref="Lines"/>, which represents line number to 
        /// start position of each line list.
        /// </summary>
        private void InitializeLines()
        {
            Lines = new List<int>();
            Lines.Add(0);
            for ( var p = 0; p < text.Length; p++ ) {
                if ( text[p] == '\r' ) {
                    if ( p + 1 < text.Length - 1 && text[p + 1] == '\n' )
                        p++;
                    Lines.Add(p + 1);
                } else
                if ( text[p] == '\n' )
                    Lines.Add(p + 1);
            }
        }
        /// <summary>
        /// Line number to start position list.
        /// </summary>
        List<int> Lines = new List<int>();
        /// <summary>
        /// Represents a position in a multiline text.
        /// </summary>
        public struct Position { 
            /// <summary>
            /// Raw in a text.
            /// </summary>
            public int Raw; 
            /// <summary>
            /// Column in a text.
            /// </summary>
            public int Column; 
        }
        #endregion

        #region Error / Warning
        /// <summary>
        /// Reporting syntax error by throwing <see cref="ParseErrorException"/>.
        /// </summary>
        /// <param name="message"><see cref="string.Format(string,object[])"/> template for the error message.</param>
        /// <param name="args"><see cref="string.Format(string,object[])"/> parameters if required</param>
        /// <returns>Because it throw exception, nothing will be returned in reality.</returns>
        public bool Error(string message, params object[] args)
        {
            throw new ParseErrorException(
                string.Format("Syntax error at line {0} column {1}\r\n", CurrentPosition.Raw, CurrentPosition.Column) +
                    string.Format(message, args));
        }
        /// <summary>
        /// <para>Give warning if <paramref name="condition"/> is true.</para>
        /// 
        /// <para>By default, the warning will not be shown / stored to anywhere.
        /// To show or log the warning, override <see cref="StoreWarning"/>.</para>
        /// </summary>
        /// <example>
        /// <code>
        ///   return 
        ///       SomeObsoleteReductionRule() &amp;&amp;
        ///       WarningIf(
        ///           context != Context.IndeedObsolete,
        ///           "Obsolete");
        /// </code>
        /// </example>
        /// <param name="condition">If true, warning is given; otherwize do nothing.</param>
        /// <param name="message"><see cref="string.Format(string,object[])"/> template for the warning message.</param>
        /// <param name="args"><see cref="string.Format(string,object[])"/> parameters if required</param>
        /// <returns>Always true.</returns>
        protected bool WarningIf(bool condition, string message, params object[] args)
        {
            if ( condition )
                Warning(message, args);
            return true;
        }
        /// <summary>
        /// <para>Give warning if <paramref name="condition"/> is false.</para>
        /// 
        /// <para>By default, the warning will not be shown / stored to anywhere.
        /// To show or log the warning, override <see cref="StoreWarning"/>.</para>
        /// </summary>
        /// <example>
        /// <code>
        ///   return 
        ///       SomeObsoleteReductionRule() &amp;&amp;
        ///       WarningUnless(
        ///           context != Context.NotObsolete,
        ///           "Obsolete");
        /// </code>
        /// </example>
        /// <param name="condition">If false, warning is given; otherwize do nothing.</param>
        /// <param name="message"><see cref="string.Format(string,object[])"/> template for the warning message.</param>
        /// <param name="args"><see cref="string.Format(string,object[])"/> parameters if required</param>
        /// <returns>Always true.</returns>
        protected bool WarningUnless(bool condition, string message, params object[] args)
        {
            if ( !condition )
                Warning(message, args);
            return true;
        }
        /// <summary>
        /// <para>Give warning.</para>
        /// 
        /// <para>By default, the warning will not be shown / stored to anywhere.
        /// To show or log the warning, override <see cref="StoreWarning"/>.</para>
        /// </summary>
        /// <example>
        /// <code>
        ///   return 
        ///       SomeObsoleteReductionRule() &amp;&amp;
        ///       Warning("Obsolete");
        /// </code>
        /// </example>
        /// <param name="message"><see cref="string.Format(string,object[])"/> template for the warning message.</param>
        /// <param name="args"><see cref="string.Format(string,object[])"/> parameters if required</param>
        /// <returns>Always true.</returns>
        protected bool Warning(string message, params object[] args)
        {
            message = string.Format( 
                "Warning: {0} at line {1} column {2}.",
                string.Format(message, args),
                CurrentPosition.Raw,
                CurrentPosition.Column
            );
            StoreWarning(message);
            return true;
        }
        /// <summary>
        /// <para>Invoked when warning was given while parsing.</para>
        /// 
        /// <para>Override this method to display / store the warning.</para>
        /// </summary>
        /// <param name="message">Warning message.</param>
        protected virtual void StoreWarning(string message) { }
        #endregion

        #region EBNF operators
        /// <summary>
        /// <para>Represents EBNF operator of "join", i.e. serial appearence of several rules.</para>
        /// </summary>
        /// <remarks>
        /// <para>This recoveres <see cref="p"/>, <see cref="stringValue"/>, <see cref="state"/>
        /// when <paramref name="condition"/> does not return <code>true</code>.</para>
        /// 
        /// <para>If any specific operation is needed for rewinding, in addition to simply
        /// recover the value of <see cref="state"/>, override <see cref="Rewind()"/>.</para>
        /// </remarks>
        /// <param name="rule">If false is returned, the parser status is rewound.</param>
        /// <returns>true if <paramref name="rule"/> returned true; otherwise false.</returns>
        /// <example>
        /// name ::= first-name middle-name? last-name
        /// <code>
        /// bool Name()
        /// {
        ///     return RewindUnless(()=>
        ///         FirstName() &amp;&amp;
        ///         Optional(MiddleName) &amp;&amp;
        ///         LastName()
        ///     );
        /// }
        /// </code>
        /// </example>
        protected bool RewindUnless(Func<bool> rule) // (join) 
        {
            var savedp = p;
            var stringValueLength = stringValue.Length;
            var savedStatus = state;
            if ( rule() )
                return true;
            state = savedStatus; 
            stringValue.Length = stringValueLength;
            p = savedp;
            Rewind();
            return false;
        }
        /// <summary>
        /// This method is called just after <see cref="RewindUnless"/> recovers <see cref="state"/>.
        /// Override it to do any additional operation for rewinding.
        /// </summary>
        protected virtual void Rewind() {}
        /// <summary>
        /// Represents EBNF operator of "*".
        /// </summary>
        /// <param name="rule">Reduction rule to be repeated.</param>
        /// <returns>Always true.</returns>
        /// <example>
        /// lines-or-empty ::= line*
        /// <code>
        /// bool LinesOrEmpty()
        /// {
        ///     return
        ///         Repeat(Line);
        /// }
        /// </code>
        ///
        /// <para>lines-or-empty ::= (text line-break)*</para>
        /// <para>Note: Do not forget <see cref="RewindUnless"/> if several
        /// rules are sequentially appears in <see cref="Repeat(Func&lt;bool&gt;)"/> operator.</para>
        /// <code>
        /// bool LinesOrEmpty()
        /// {
        ///     return
        ///         Repeat(()=>
        ///             RewindUnless(()=>
        ///                 Text() &amp;&amp;
        ///                 LineBreak()
        ///             )
        ///         );
        /// }
        /// </code>
        /// </example>
        protected bool Repeat(Func<bool> rule) // * 
        {
            // repeat while condition() returns true and 
            // it reduces any part of text.
            int start;
            do {
                start = p;
            } while ( rule() && start != p );
            return true;
        }
        /// <summary>
        /// Represents EBNF operator of "+".
        /// </summary>
        /// <param name="rule">Reduction rule to be repeated.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        /// <example>
        /// lines ::= line+
        /// <code>
        /// bool Lines()
        /// {
        ///     return
        ///         Repeat(Line);
        /// }
        /// </code>
        /// </example>
        /// <example>
        /// lines ::= (text line-break)+
        /// 
        /// Note: Do not forget RewindUnless in Repeat operator.
        /// <code>
        /// bool Lines()
        /// {
        ///     return
        ///         Repeat(()=>
        ///             RewindUnless(()=>
        ///                 Text() &amp;&amp;
        ///                 LineBreak()
        ///             )
        ///         );
        /// }
        /// </code>
        /// </example>
        protected bool OneAndRepeat(Func<bool> rule)  // + 
        {
            return rule() && Repeat(rule);
        }
        /// <summary>
        /// Represents <code>n</code> times repeatition.
        /// </summary>
        /// <example>
        /// <para>four-lines ::= (text line-break){4}</para>
        ///
        /// <para>Note: Do not forget <see cref="RewindUnless"/> if several
        /// rules are sequentially appears in <see cref="Repeat(int,Func&lt;bool&gt;)"/> operator.</para>
        /// <code>
        /// bool FourLines()
        /// {
        ///     return
        ///         Repeat(4, ()=>
        ///             RewindUnless(()=>
        ///                 Text() &amp;&amp;
        ///                 LineBreak()
        ///             )
        ///         );
        /// }
        /// </code>
        /// </example>
        /// <param name="n">Repetition count.</param>
        /// <param name="rule">Reduction rule to be repeated.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool Repeat(int n, Func<bool> rule)
        {
            return RewindUnless(() => {
                for ( int i = 0; i < n; i++ )
                    if ( !rule() )
                        return false;
                return true;
            });
        }
        /// <summary>
        /// Represents at least <paramref name="min"/>, at most <paramref name="max"/> times repeatition.
        /// </summary>
        /// <example>
        /// <para>google ::= "g" "o"{2,100} "g" "l" "e"</para>
        /// <para>Note: Do not forget <see cref="RewindUnless"/> if several
        /// rules are sequentially appears in <see cref="Repeat(int,int,Func&lt;bool&gt;)"/> operator.</para>
        /// <code>
        /// bool Google()
        /// {
        ///     return
        ///         RewindUnless(()=>
        ///             text[p++] == 'g' &amp;&amp;
        ///                 Repeat(2, 100,
        ///                     RewindUnless(()=>
        ///                         text[p++] == 'o'
        ///                     )
        ///                 )
        ///             text[p++] == 'g' &amp;&amp;
        ///             text[p++] == 'l' &amp;&amp;
        ///             text[p++] == 'e'
        ///         );
        /// }
        /// </code>
        /// </example>
        /// <param name="min">Minimum repetition count. Negative value is treated as 0.</param>
        /// <param name="max">Maximum repetition count. Negative value is treated as positive infinity.</param>
        /// <param name="rule">Reduction rule to be repeated.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool Repeat(int min, int max, Func<bool> rule)
        {
            return RewindUnless(() => {
                for ( int i = 0; i < min; i++ )
                    if ( !rule() )
                        return false;
                for ( int i = 0; i < max || max < 0; i++ )
                    if ( !rule() )
                        return true;
                return true;
            });
        }
        /// <summary>
        /// Represents BNF operator "?".
        /// </summary>
        /// <example>
        /// <para>file ::= header? body footer?</para>
        /// 
        /// <para>Note: Do not forget <see cref="RewindUnless"/> if several
        /// rules are sequentially appears in <see cref="Optional(bool)"/> operator.</para>
        /// <code>
        /// bool File()
        /// {
        ///     return
        ///         Optional(Header()) &amp;&amp;
        ///         Body() &amp;&amp;
        ///         Optional(Footer());
        /// }
        /// </code>
        /// </example>
        /// <param name="rule">Reduction rule that is optional.</param>
        /// <returns>Always true.</returns>
        protected bool Optional(bool rule) // ? 
        {
            return rule || true;
        }
        /// <summary>
        /// Represents BNF operator "?" (WITH rewinding wrap).
        /// </summary>
        /// <example>
        /// file = header? body footer?
        /// 
        /// <para>Note: Do not forget <see cref="RewindUnless"/> if several
        /// rules are sequentially appears in <see cref="Optional(Func&lt;bool&gt;)"/> operator.</para>
        /// <code>
        /// bool File()
        /// {
        ///     return
        ///         Optional(Header) &amp;&amp;
        ///         Body() &amp;&amp;
        ///         Optional(Footer);
        /// }
        /// </code>
        /// </example>
        /// <param name="rule">Reduction rule that is optional.</param>
        /// <returns>Always true.</returns>
        protected bool Optional(Func<bool> rule) // ? 
        {
            return 
                RewindUnless(()=> rule()) || true;
        }
        #endregion

        #region Chars
        /// <summary>
        /// Reduce one character if it is a member of the specified character set.
        /// </summary>
        /// <param name="charset">Acceptable character set.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        /// <example>
        /// alpha ::= [A-Z][a-z]<br/>
        /// num ::= [0-9]<br/>
        /// alpha-num :: alpha | num<br/>
        /// word ::= alpha ( alpha-num )*<br/>
        /// <code>
        /// Func&lt;char,bool&gt; Alpha = Charset( c =>
        ///     ( 'A' &lt;= c &amp;&amp; c &lt;= 'Z' ) ||
        ///     ( 'a' &lt;= c &amp;&amp; c &lt;= 'z' ) 
        /// );
        /// Func&lt;char,bool&gt; Num = Charset( c =>
        ///       '0' &lt;= c &amp;&amp; c &lt;= '9' 
        /// );
        /// Func&lt;char,bool&gt; AlphaNum = Charset( c =>
        ///     Alpha(c) || Num(c)
        /// );
        /// bool Word()
        /// {
        ///     return 
        ///         Accept(Alpha) &amp;&amp;
        ///         Repeat(AlphaNum);
        ///         // No need for RewindUnless
        /// }
        /// </code>
        /// </example>
        protected bool Accept(Func<char, bool> charset)
        {
            if ( p < text.Length && charset(text[p]) ) {
                p++;
                return true;
            }
            return false;
        }
        /// <summary>
        /// <para>Accepts a character 'c'.</para>
        /// 
        /// <para>It can be also represented by <c>text[p++] == c</c> wrapped by <see cref="RewindUnless"/>.</para>
        /// </summary>
        /// <param name="c">The character to be accepted.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        /// <example>
        /// YMCA ::= "Y" "M" "C" "A"
        /// <code>
        /// bool YMCA()
        /// {
        ///     return
        ///         RewindUnless(()=>
        ///             Accept('Y') &amp;&amp;
        ///             Accept('M') &amp;&amp;
        ///             Accept('C') &amp;&amp;
        ///             Accept('A') 
        ///         );
        /// }
        /// </code>
        /// -or-
        /// <code>
        /// bool YMCA()
        /// {
        ///     return
        ///         RewindUnless(()=>
        ///             text[p++] == 'Y' &amp;&amp;
        ///             text[p++] == 'M' &amp;&amp;
        ///             text[p++] == 'C' &amp;&amp;
        ///             text[p++] == 'A' 
        ///         );
        /// }
        /// </code>
        /// </example>
        protected bool Accept(char c)
        {
            if ( p < text.Length && text[p] == c ) {
                p++;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Accepts a sequence of characters.
        /// </summary>
        /// <param name="s">Sequence of characters to be accepted.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        /// <example>
        /// YMCA ::= "Y" "M" "C" "A"
        /// <code>
        /// bool YMCA()
        /// {
        ///     return
        ///         Accept("YMCA");
        /// }
        /// </code>
        /// </example>
        protected bool Accept(string s)
        {
            if ( p + s.Length >= text.Length )
                return false;
            for ( int i = 0; i < s.Length; i++ )
                if ( s[i] != text[p + i] )
                    return false;
            p += s.Length;
            return true;
        }
        /// <summary>
        /// Represents sequence of characters.
        /// </summary>
        /// <param name="r">Sequence of characters to be accepted.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool Accept(Regex r)
        {
            var m = r.Match(text, p);
            if ( !m.Success )
                return false;
            p += m.Length;
            return true;
        }
        /// <summary>
        /// Represents BNF operator of "*".
        /// </summary>
        /// <param name="charset">Character set to be accepted.</param>
        /// <returns>Always true.</returns>
        protected bool Repeat(Func<char, bool> charset)
        {
            while ( charset(text[p]) )
                p++;
            return true;
        }
        /// <summary>
        /// Represents BNF operator of "+".
        /// </summary>
        /// <param name="charset">Character set to be accepted.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool OneAndRepeat(Func<char, bool> charset)
        {
            if ( !charset(text[p]) )
                return false;
            while ( charset(text[++p]) )
                ;
            return true;
        }
        /// <summary>
        /// Represents <code>n</code> times repetition of characters.
        /// </summary>
        /// <param name="charset">Character set to be accepted.</param>
        /// <param name="n">Repetition count.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool Repeat(Func<char, bool> charset, int n)
        {
            for ( int i = 0; i < n; i++ )
                if ( !charset(text[p + i]) )
                    return false;
            p += n;
            return true;
        }
        /// <summary>
        /// Represents at least <code>min</code> times, at most <code>max</code> times 
        /// repetition of characters.
        /// </summary>
        /// <param name="charset">Character set to be accepted.</param>
        /// <param name="min">Minimum repetition count. Negative value is treated as 0.</param>
        /// <param name="max">Maximum repetition count. Negative value is treated as positive infinity.</param>
        /// <returns>true if the rule matches; otherwise false.</returns>
        protected bool Repeat(Func<char, bool> charset, int min, int max)
        {
            for ( int i = 0; i < min; i++ )
                if ( !charset(text[p + i]) )
                    return false;
            for ( int i = 0; i < max; i++ )
                if ( !charset(text[p + min + i]) ) {
                    p += min + i;
                    return true;
                }
            p += min + max;
            return true;
        }
        /// <summary>
        /// Represents BNF operator "?".
        /// </summary>
        /// <param name="charset">Character set to be accepted.</param>
        /// <returns>Always true.</returns>
        protected bool Optional(Func<char, bool> charset) // ? 
        {
            if ( !charset(text[p]) )
                return true;
            p++;
            return true;
        }
        #endregion

        #region Charset
        /// <summary>
        /// <para>Builds a performance-optimized table-based character set definition from a simple 
        /// but slow comparison-based definition.</para>
        /// 
        /// <para>By default, the character table size is 0x100, namely only the characters of [\0-\xff] are
        /// judged by using a character table and others are by the as-given slow comparisn-based definitions.</para>
        /// 
        /// <para>To have maximized performance, locate the comparison for non-table based judgement first
        /// in the definition as the example below.</para>
        /// 
        /// <para>Use <see cref="Charset(System.Int32, System.Func&lt;char, bool&gt;)"/> form to explicitly 
        /// specify the table size.</para>
        /// </summary>
        /// <example>This sample shows how to build a character set delegate.
        /// <code>
        /// static class YamlCharsets: Charsets
        /// {
        ///     Func&lt;char, bool&gt; cPrintable;
        ///     Func&lt;char, bool&gt; sWhite;
        /// 
        ///     static YamlCharsets()
        ///     {
        ///         cPrintable = CacheResult(c =&gt;
        ///         /*  ( 0x10000 &lt; c &amp;&amp; c &lt; 0x110000 ) || */
        ///             ( 0xe000 &lt;= c &amp;&amp; c &lt;= 0xfffd ) ||
        ///             ( 0xa0 &lt;= c &amp;&amp; c &lt;= 0xd7ff ) ||
        ///             ( c &lt; 0x100 &amp;&amp; ( // to improve performance
        ///                 c == 0x85 ||
        ///                 ( 0x20 &lt;= c &amp;&amp; c &lt;= 0x7e ) ||
        ///                 c == 0x0d ||
        ///                 c == 0x0a ||
        ///                 c == 0x09
        ///             ) )
        ///         );
        ///         sWhite = CacheResult(c =&gt;
        ///             c &lt; 0x100 &amp;&amp; ( // to improve performance
        ///                 c == '\t' ||
        ///                 c == ' '
        ///             )
        ///         );
        ///     }
        /// }
        /// </code></example>
        /// <param name="definition">A simple but slow comparison-based definition of the charsert.</param>
        /// <returns>A performance-optimized table-based delegate built from the given <paramref name="definition"/>.</returns>
        protected static Func<char, bool> Charset(Func<char, bool> definition)
        {
            return Charset(0x100, definition);
        }
        /// <summary>
        /// <para>Builds a performance-optimized table-based character set definition from a simple 
        /// but slow comparison-based definition.</para>
        /// 
        /// <para>Characters out of the table are judged by the as-given slow comparisn-based 
        /// definitions.</para>
        /// 
        /// <para>So, to have maximized performance, locate the comparison for non-table based 
        /// judgement first in the definition as the example below.</para>
        /// </summary>
        /// <example>This sample shows how to build a character set delegate.
        /// <code>
        /// static class YamlCharsets: Charsets
        /// {
        ///     Func&lt;char, bool&gt; cPrintable;
        ///     Func&lt;char, bool&gt; sWhite;
        /// 
        ///     static YamlCharsets()
        ///     {
        ///         cPrintable = CacheResult(c =&gt;
        ///         /*  ( 0x10000 &lt; c &amp;&amp; c &lt; 0x110000 ) || */
        ///             ( 0xe000 &lt;= c &amp;&amp; c &lt;= 0xfffd ) ||
        ///             ( 0xa0 &lt;= c &amp;&amp; c &lt;= 0xd7ff ) ||
        ///             ( c &lt; 0x100 &amp;&amp; ( // to improve performance
        ///                 c == 0x85 ||
        ///                 ( 0x20 &lt;= c &amp;&amp; c &lt;= 0x7e ) ||
        ///                 c == 0x0d ||
        ///                 c == 0x0a ||
        ///                 c == 0x09
        ///             ) )
        ///         );
        ///         sWhite = CacheResult(c =&gt;
        ///             c &lt; 0x100 &amp;&amp; ( // to improve performance
        ///                 c == '\t' ||
        ///                 c == ' '
        ///             )
        ///         );
        ///     }
        /// }
        /// </code></example>
        /// <param name="table_size">Character table size.</param>
        /// <param name="definition">A simple but slow comparison-based definition of the charsert.</param>
        /// <returns>A performance-optimized table-based delegate built from the given <paramref name="definition"/>.</returns>
        protected static Func<char, bool> Charset(
            int table_size, Func<char, bool> definition)
        {
            var table = new bool[table_size];
            for ( char c = '\0'; c < table_size; c++ )
                table[c] = definition(c);
            return c => c < table_size ? table[c] : definition(c);
        }
        #endregion

        #region Actions
        /// <summary>
        /// <para>Saves a part of the source text that is reduced in the <paramref name="rule"/>.</para>
        /// <para>If the rule does not match, nothing happends.</para>
        /// </summary>
        /// <param name="rule">Reduction rule to match.</param>
        /// <param name="value">If the <paramref name="rule"/> matches, 
        /// the part of the source text reduced in the <paramref name="rule"/> is set; 
        /// otherwise String.Empty is set.</param>
        /// <returns>true if <paramref name="rule"/> matches; otherwise false.</returns>
        protected bool Save(Func<bool> rule, ref string value)
        {
            var value_ = "";
            var result = Save(rule, s => value_ = s);
            if ( result )
                value = value_;
            return result;
        }
        /// <summary>
        /// <para>Saves a part of the source text that is reduced in the <paramref name="rule"/>
        /// and append it to <see cref="stringValue"/>.</para>
        /// <para>If the rule does not match, nothing happends.</para>
        /// </summary>
        /// <param name="rule">Reduction rule to match.</param>
        /// <returns>true if <paramref name="rule"/> matches; otherwise false.</returns>
        protected bool Save(Func<bool> rule)
        {
            return 
                Save(rule, s => stringValue.Append(s));
        }
        /// <summary>
        /// <para>Saves a part of the source text that is reduced in the <paramref name="rule"/>.</para>
        /// <para>If the rule does not match, nothing happends.</para>
        /// </summary>
        /// <param name="rule">Reduction rule to match.</param>
        /// <param name="save">If <paramref name="rule"/> matches, this delegate is invoked
        /// with the part of the source text that is reduced in the <paramref name="rule"/>
        /// as the parameter. Do any action in the delegate.</param>
        /// <returns>true if <paramref name="rule"/> matches; otherwise false.</returns>
        /// <example>
        /// <code>
        /// bool SomeRule()
        /// {
        ///     return 
        ///         Save(()=> SubRule(), s => MessageBox.Show(s));
        /// }
        /// </code></example>
        protected bool Save(Func<bool> rule, Action<string> save)
        {
            int start = p;
            var result = rule();
            if ( result )
                save(text.Substring(start, p - start));
            return result;
        }
        /// <summary>
        /// Execute some action.
        /// </summary>
        /// <param name="action">Action to be done.</param>
        /// <returns>Always true.</returns>
        /// <example>
        /// <code>
        /// bool SomeRule()
        /// {
        ///     return 
        ///         SubRule() &amp;&amp;
        ///         Action(()=> do_some_action());
        /// }
        /// </code></example>
        protected bool Action(Action action)
        {
            action();
            return true;
        }
        /// <summary>
        /// Report error by throwing <see cref="ParseErrorException"/> when the <paramref name="rule"/> does not match.
        /// </summary>
        /// <param name="rule">Some reduction rule that must match.</param>
        /// <param name="message">Error message as <see cref="string.Format(string,object[])"/> template</param>
        /// <param name="args">Parameters for <see cref="string.Format(string,object[])"/> template</param>
        /// <returns>Always true; otherwise an exception thrown.</returns>
        protected bool ErrorUnless(bool rule, string message, params object[] args)
        {
            if ( !rule )
                Error(message, args);
            return true;
        }
        /// <summary>
        /// Report error by throwing <see cref="ParseErrorException"/> when the <paramref name="rule"/> does not match.
        /// </summary>
        /// <param name="rule">Some reduction rule that must match.</param>
        /// <param name="message">Error message as <see cref="string.Format(string,object[])"/> template</param>
        /// <param name="args">Parameters for <see cref="string.Format(string,object[])"/> template</param>
        /// <returns>Always true; otherwise an exception is thrown.</returns>
        protected bool ErrorUnless(Func<bool> rule, string message, params object[] args)
        {
            return ErrorUnless(rule(), message);
        }
        /// <summary>
        /// Report error by throwing <see cref="ParseErrorException"/> when the <paramref name="rule"/> does not match
        /// and an additional condition <paramref name="to_be_error"/> is true.
        /// </summary>
        /// <param name="rule">Some reduction rule that must match.</param>
        /// <param name="to_be_error">Additional condition: if this parameter is false, 
        /// rewinding occurs, instead of throwing exception.</param>
        /// <param name="message">Error message as <see cref="string.Format(string,object[])"/> template</param>
        /// <param name="args">Parameters for <see cref="string.Format(string,object[])"/> template</param>
        /// <returns>true if the reduction rule matches; otherwise false.</returns>
        protected bool ErrorUnlessWithAdditionalCondition(Func<bool> rule, bool to_be_error, string message, params object[] args)
        {
            if ( to_be_error ) {
                if ( !rule() ) 
                    Error(message, args);
                return true;
            } else {
                return RewindUnless(rule);
            }
        }
        /// <summary>
        /// Report error by throwing <see cref="ParseErrorException"/> when <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">True to throw exception.</param>
        /// <param name="message">Error message as <see cref="string.Format(string,object[])"/> template</param>
        /// <param name="args">Parameters for <see cref="string.Format(string,object[])"/> template</param>
        /// <returns>Always true.</returns>
        protected bool ErrorIf(bool condition, string message, params object[] args)
        {
            if ( condition )
                Error(message, args);
            return true;
        }
        /// <summary>
        /// Assign <c>var = value</c> and return true;
        /// </summary>
        /// <typeparam name="T">Type of the variable and value.</typeparam>
        /// <param name="var">Variable to be assigned.</param>
        /// <param name="value">Value to be assigned.</param>
        /// <returns>Always true.</returns>
        protected bool Assign<T>(out T var, T value)
        {
            var = value;
            return true;
        }
        #endregion
    }
}
