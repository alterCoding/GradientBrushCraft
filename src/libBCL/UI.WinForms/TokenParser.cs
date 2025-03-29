using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AltCoD.UI.WinForms
{
    /// <summary>
    /// Helper to be used to parse, to highlight and (optionally) to cleanup the richtextbox content, according to
    /// a list of <see cref="TokenSpecifier"/> which define the parsing rules <br/>
    /// Basically, parsing rules are all based on regex pattern matching. <br/>
    /// - the <see cref="TokenSpecifier"/> identifies a list of pre-defined keywords or identifier pattern. In such a
    ///  case, the matched items are simply the text to be styled. <br/>
    /// - the <see cref="TokenSpecifier"/> identifies a pair of token delimiters to match an item. In such a case, the
    ///  delimiters will be removed in the final content (and the enclosed text is styled). Hence, the process relies 
    ///  only on the explicit arbitrary token delimiters that must be inserted prior in the textbox content in the 
    ///  suitable locations
    /// </summary>
    public class TokenParser
    {
        /// <summary>
        /// Wraps a regex pattern
        /// </summary>
        public class TokenSpecifier
        {
            public enum Style
            {
                isHighlighter = 0x1,
                isList = 0x2
            }

            public TokenSpecifier(string pattern) { Pattern = pattern; }

            /// <summary>
            /// [TRUE] when the token implies delimiters, they should be removed in the final text. <br/>
            /// If the token doesn't imply delimiters and the property value is still true, the parsing behavior may be
            /// undefined
            /// </summary>
            public bool RemoveDelimiters { get; set; } = true;

            /// <summary>
            /// [TRUE] when the token implies delimiters, they work by pair, thus enclosing (including) the text to be 
            /// matched
            /// </summary>
            public bool DelimiterIsPair { get; set; } = true;

            public bool MayBeEscaped { get; set; } = false;

            public string Pattern { get; }

            /// <summary>
            /// The color to highlight the token if matched. Meaningful with <see cref="Style.isHighlighter"/>
            /// </summary>
            public Color HighLight
            {
                get => _color;
                set 
                { 
                    _color = value; 
                    if(value.IsEmpty == false)
                        Role |= Style.isHighlighter; 
                }
            }

            /// <summary>
            /// the purpose of the specifier
            /// </summary>
            public Style Role { get; private set; }

            /// <summary>
            /// Get a token specifier based on pattern matching that suits for integral decimal, floating point 
            /// (w/ or w/o postfixes), integral hexadecimal. <br/>
            /// If the number is preceded by the literal '@', the number matching is evinced
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            /// @internal BUG: also detect bad identifier with number at head, e.g 10bad
            public static TokenSpecifier Numbers(Color color)
            {
                //cleanup = false (since the numbers pattern matching is not based on any special delimiter)

                return new TokenSpecifier(_regexNumbers)
                { 
                    HighLight = color, 
                    RemoveDelimiters = false,
                    MayBeEscaped = true
                };
            }

            /// <summary>
            /// Get a token specifier based on pattern matching that identifies a list item. The syntax is simply
            /// a dash at the beginning of line (at least 1 space at head is expected, at least 1 space after the dash,
            /// n spaces allowed)
            /// </summary>
            /// <returns></returns>
            public static TokenSpecifier BulletList() => BulletList(Color.Empty);
            /// <summary>
            /// Same as <see cref="BulletList()"/> but highlight the whole line
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            public static TokenSpecifier BulletList(Color color)
            {
                var spec = new TokenSpecifier(_regexList) 
                { 
                    HighLight = color, 
                    RemoveDelimiters = true, 
                    DelimiterIsPair = false
                };
                spec.Role |= Style.isList;

                return spec;
            }

            private Color _color = Color.Empty;

            /// <summary>
            /// at least 1 space at head, at least 1 space after bullet, the useful part is captured (i.e w/o spaces 
            /// and dash)
            /// </summary>
            private static readonly string _regexList = @"^\s+-\s+(.*)$";

            private static readonly string _regexNumbers =
                @"((\b|[-+\.]\s*)(\d+\.?\d*[fd]?(?![xX])|\d*\.?\d+[fd]?(?![xX]))([eE][-+]?\d+)?)|(0[xX](\d|[aA-fF])*)";
        }

        /// <summary>
        /// </summary>
        /// <param name="textbox">The source/target Textbox. The textbox must be writeable (ReadOnly = false) if one
        /// or more token specifiers state that delimiters must be removed</param>
        /// <param name="tokens"></param>
        public TokenParser(RichTextBox textbox, List<TokenSpecifier> tokens)
        {
            _textbox = textbox;
            _tokens = tokens;

            if (textbox.ReadOnly)
            {
                if(tokens.All(t => t.RemoveDelimiters == false) == false)
                {
                    throw new InvalidOperationException(
$"One or more {nameof(TokenSpecifier)} have specified that token delimiters must be removed but the supplied TextBox is readonly");
                }
            }
        }

        public void IgnoreLines(HashSet<int> toIgnore, bool append = false)
        {
            if (append) _linesToIgnore.UnionWith(toIgnore);
            else _linesToIgnore = toIgnore.ToHashSet();
        }

        /// <summary>
        /// Parse and highlight the textbox content according to the token specifiers that have been defined at
        /// construction time. Token delimiters are removed from the original text depending on the <see cref="TokenSpecifier.RemoveDelimiters"/>
        /// property
        /// </summary>
        /// <remarks>
        /// When overriden, the base implementation must be called (otherwise the core parsing based on the <see cref="TokenSpecifier"/>
        /// set isn't fulfilled)
        /// </remarks>
        public virtual void Parse()
        {
            //save current position
            int actual = _textbox.SelectionStart;

            string content = _textbox.Text;
            _toRemove.Clear();

            //algo: iterate all lines (but lines to ignore)
            //for each line, for each token specifier
            //seek all token instances
            //store positions and highlight the token
            //finally, remove the token delimiter if needed -----

            int position = 0; //content absolute position
            int clines = 0; //current line index 

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (_linesToIgnore.Contains(clines))
                    {
                        clines++;
                        position += line.Length + 1;
                        continue;
                    }

                    foreach (var token in _tokens)
                    {
                        parse(position, line, token);
                    }

                    clines++;
                    position += line.Length +1; //+1 for LF
                }
            }

            if (_toRemove.Any()) cleanup();

            //restore original position
            _textbox.Select(actual, 0);
        }

        private void parse(int start, string line, TokenSpecifier token)
        {
            bool is_coloring = (token.Role & TokenSpecifier.Style.isHighlighter) != 0;
            bool is_list = (token.Role & TokenSpecifier.Style.isList) != 0;

            //the positions to be marked and/or removed
            int pos;
            foreach (Match match in Regex.Matches(line, token.Pattern))
            {
                pos = start + match.Index;

                if(token.MayBeEscaped && match.Index > 0 && line[match.Index-1] == '@')
                {
                    _toRemove.Add(pos - 1);
                    continue;
                }

                if (is_coloring)
                {
                    _textbox.Select(pos, match.Length);
                    _textbox.SelectionColor = token.HighLight;
                }
 
                if(is_list)
                {
                    _textbox.Select(pos, 0);
                    _textbox.SelectionBullet = true;
                }

                if (token.RemoveDelimiters)
                {
                    if (is_list)
                    {
                        //the processing for list is not as easy as a simple enclosing delimiter (i.e removing 1st and
                        //last token) --------

                        //group[1] should contain the captured text w/o spaces/dash at head (referring to the regex)
                        var list_item = match.Groups[1];

                        //eat the heading spaces/dash
                        int begin = pos + list_item.Index;
                        for (int i = pos; i < begin; ++i) _toRemove.Add(i);
                    }
                    else
                    {
                        //trivial case: remove 1 delimeter or a enclosing pair ----

                        _toRemove.Add(pos);

                        if (token.DelimiterIsPair)
                            _toRemove.Add(pos + match.Length - 1);
                    }
                }
            }
        }

        /// <summary>
        /// delete the char at token delimiter positions
        /// </summary>
        void cleanup()
        {
            _toRemove.Sort(); //important

            for (int i = 0; i < _toRemove.Count; i++)
            {
                // ajusting indexes by [-i] because we have removed all prior positions
                _textbox.Select(_toRemove[i] - i, 1);
                _textbox.DeleteSelection();
            }
        }

        void highlight(int start, int length, Color color)
        {
            _textbox.Select(start, length);
            _textbox.SelectionColor = color;
        }

        protected readonly RichTextBox _textbox;

        /// <summary>
        /// positions of the token delimiters to be removed
        /// </summary>
        private readonly List<int> _toRemove = new List<int>();

        private readonly List<TokenSpecifier> _tokens;

        private HashSet<int> _linesToIgnore = new HashSet<int>();
    }

    /// <summary>
    /// A source code (ultra) basic syntax coloring
    /// <para> 
    /// A (really) basic source code syntax coloring is implemented with naive parsing and patterns matching. It's 
    /// DEFINITELY NOT a full syntax coloring which could cover the whole source code tokens range. It couldn't support
    /// long source code and complex use cases such as class typenames and variable names disambiguity. Third party
    /// libraries exist for that purpose but the 1st requirement is zero-dependency for a tiny tool <br/>
    /// The parser doesn't recognise the syntax and grammar by no means. It relies only on the completness of the 
    /// pattern matching. <br/>
    /// In fact, to write directly in RTF would be more comprehensive but it would be definitely not easier
    /// </para>
    /// <para> Features:<br/>
    /// parse and highlights a few tokens <br/>
    /// - all numbers (unquoted) <br/>
    /// - literal strings (double quoted) <br/>
    /// - some keywords <br/> 
    /// - some primitive types <br/>
    /// </para>
    /// </summary>
    public class SourceCodeParser : TokenParser
    {
        public SourceCodeParser(RichTextBox textbox)
            : base(textbox, new List<TokenSpecifier>()
            {
                new TokenSpecifier(_types) { HighLight = Color.Maroon, RemoveDelimiters = false },
                new TokenSpecifier(_keywords) { HighLight = Color.Blue, RemoveDelimiters = false },
                TokenSpecifier.Numbers(Color.Gray),
                new TokenSpecifier(_strings) { HighLight = Color.Purple, RemoveDelimiters = false }
            })
        {
        }

        public override void Parse()
        {
            //step 1: 2n pass for comments based on simple forward iterating
            var comments = highlightComments();

            IgnoreLines(comments);

            //step 2*: parsing based on pattern matching with regex
            base.Parse();
        }

        /// <summary>
        /// parse and highlight comments defined by '//' one liner sequences or multiline '/** .... */' sequences
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private HashSet<int> highlightComments()
        {
            int actual = _textbox.SelectionStart;

            var comments = new HashSet<int>();

            bool commenting = false;
            int position = 0;
            int lines = 0;
            using (var reader = new StringReader(_textbox.Text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string cline = line.TrimStart();
                    if (cline.StartsWith("//")) comment(position, line.Length);
                    else if (cline.StartsWith("/**")) comment(position, line.Length);
                    else if (cline.StartsWith("*") && commenting) comment(position, line.Length);
                    else commenting = false;

                    position += line.Length +1;
                    lines++;
                }

                void comment(int start, int length)
                {
                    commenting = true;
                    _textbox.Select(start, length);
                    _textbox.SelectionColor = Color.Green;
                    comments.Add(lines);
                }
            }

            _textbox.Select(actual, 0);
            return comments;
        }

        //to be continued if needed
        private static readonly string _types = @"\b(var|int|bool|float|double|char|string)\b";

        //to be continued if needed
        private static readonly string _keywords = @"\b(new|unchecked|if|while|do|else|using)\b";

        //double quoted string with inner escape if any
        private static readonly string _strings = @"""([^""\\]|\\.)*""";
    }

    /// <summary>
    /// A general tokens parser that highlight nearly any enclosed text by { } or &lt; &gt; and numbers too.
    /// </summary>
    public class GenericParser : TokenParser
    {
        public GenericParser(RichTextBox textbox)
            : base(textbox, new List<TokenSpecifier>()
            {
                //specified at 1st (the others must overwrite)
                TokenSpecifier.BulletList(),

                //we arbitrarily state that typenames call for the format <typename>
                new TokenSpecifier(_typenames) { HighLight = Color.Purple, RemoveDelimiters = true },

                //we arbitrarily state that any other tokens such as variable or properties call for the format
                //{property} {variableName}
                new TokenSpecifier(_keywords) { HighLight = Color.DarkCyan, RemoveDelimiters = true },

                TokenSpecifier.Numbers(Color.Gray),
            })
        {
        }

        /// <summary>
        /// match any word-based item enclosed by &lt; &gt; (as &lt;typename&gt;)
        /// </summary>
        private static readonly string _typenames = @"<\w+>";

        /// <summary>
        /// match any item enclosed by {} (as {tag})
        /// </summary>
        /// @internal 1st exluding digit ... don't rembember why I had found this useful, problably to prevent from
        /// highlighting numbers which would be enclosed by braces
        private static readonly string _keywords = @"({([\D\s][^{}]*)})";
    }
}
