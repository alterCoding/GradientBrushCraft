using System;
using System.Text;

namespace AltCoD.BCL.CLI
{
    /// <summary>
    /// A data behavior holder for console prompt management such as the text to be displayed on the prompt, and/or the 
    /// mapping rules to apply when processing with the user inputs
    /// </summary>
    public class ConsolePrompt
    {
        public class Builder
        {
            /// <summary>
            /// </summary>
            /// <param name="actions">The default actions that may be handled</param>
            internal Builder(MessageAction actions = MessageAction.help)
            {
                _instance = new ConsolePrompt(actions);
            }

            public ConsolePrompt Prompt => _instance;

            /// <summary>
            /// Set the text that will be appended to or set up to the default prompt text
            /// </summary>
            /// <param name="prompt"></param>
            /// <param name="append">[FALSE] the supplied text replaces the default prompt</param>
            /// <returns></returns>
            public Builder WithText(string prompt, bool append = true)
            {
                enable(Options.overwriteTextPrompt, !append);
                _instance._customPromptText = prompt;

                return this;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="handler"></param>
            /// <param name="append"> How the inputs must be handler <br/>
            /// [TRUE] the rules will be applied after the default ones if any matched mapping doesn't exist <br/>
            /// [FALSE] the supplied mapping rules will be the sole processed (i.e default rules aren't considered)
            /// </param>
            /// <param name="actions">
            /// The actions that could be handled with the supplied <paramref name="handler"/> key mapper. The expected
            /// values are the custom ones (i.e <see cref="MessageAction.custom1"/> and so on)
            /// </param>
            /// <returns></returns>
            public Builder WithKeyHandler(ConsoleInputKeyMapper handler, bool append, MessageAction actions)
            {
                if (append) _instance._options &= ~Options.overwriteKeyMap;
                else _instance._options |= Options.overwriteKeyMap;

                _instance.Actions |= actions;
                _instance._customKeyHandler = handler;

                return this;
            }
            /// <summary>
            /// Create a prompt object that maps the supplied key-char to the <see cref="MessageAction.custom1"/> 
            /// custom action
            /// </summary>
            /// <param name="k">expected to be alphanumeric</param>
            /// <param name="append">[FALSE] default mapping won't be used</param>
            /// <param name="icase">[TRUE] case insentive</param>
            /// <remarks>The text prompt should later be appropriately updated with <see cref="WithText(string, bool)"/></remarks>
            public Builder WithKey(char k, bool icase, bool append = true)
            {
                _instance.Actions |= MessageAction.custom1;

                enable(Options.overwriteKeyMap, !append);
                var handler = mapButton1(makeConsoleKey(k, icase), icase);
                _instance._customKeyHandler = handler;

                return this;
            }
            public Builder WithKeys(char k1, char k2, bool icase, bool append = true)
            {
                _instance.Actions |= MessageAction.custom1 | MessageAction.custom2;

                enable(Options.overwriteKeyMap, !append);
                var handler = mapButtons(makeConsoleKey(k1, icase), makeConsoleKey(k2, icase), icase);
                _instance._customKeyHandler = handler;

                return this;
            }
            public Builder WithKeys(char k1, char k2, char k3, bool icase, bool append = true)
            {
                _instance.Actions |= MessageAction.custom1 | MessageAction.custom2 | MessageAction.custom3;

                enable(Options.overwriteKeyMap, !append);
                var handler = mapButtons(makeConsoleKey(k1, icase), makeConsoleKey(k2, icase), makeConsoleKey(k3, icase), icase);
                _instance._customKeyHandler = handler;

                return this;
            }

            public Builder WithHelp(string text, bool inPrompt = true)
            {
                _instance._helpText = text;
                _instance.Actions |= MessageAction.help;
                enable(Options.helpIncludesPrompt, inPrompt);

                return this;
            }

            public Builder ShowOnce(bool once = true)
            {
                enable(Options.showPromptOnce, once);
                return this;
            }

            private void enable(Options property, bool enable = true) 
            {
                //the assignable options
                //property &= Options.overwriteKeyMap | Options.overwriteTextPrompt | Options.showPromptOnce | Options.repeatPromptInHelp;

                if (enable) _instance._options |= property;
                else _instance._options &= ~property;
            }

            private readonly ConsolePrompt _instance;
        }

        private ConsolePrompt(MessageAction actions) 
        {
            Actions = actions;
        }

        public static Builder Make(MessageAction actions) => new Builder(actions);
        public static Builder Make() => new Builder();

        public MessageAction Actions { get; private set; }

        internal bool IsNoOp => Actions <= MessageAction.ok;

        internal bool ShouldShowOnce => _options.HasFlag(Options.showPromptOnce);

        /// <summary>
        /// Get the help text. May contain the prompt text or not (depending on option value)
        /// </summary>
        public string GetHelp()
        {
            if (_options.HasFlag(Options.helpIncludesPrompt))
            {
                if (!string.IsNullOrEmpty(_helpText))
                    return string.Concat(_helpText, Environment.NewLine, GetPromptText());
                else
                    return GetPromptText();
            }
            else
            {
                return _helpText ?? string.Empty;
            }
        }

        public string GetPromptText()
        {
            if (useDefaultPrompt)
                return string.Concat(_customPromptText, '\t', makePrompt(Actions));
            else
                return _customPromptText;
        }

        /// <summary>
        /// Process the supplied input through the default handler and/or the custom handler
        /// </summary>
        /// <param name="input"></param>
        /// <returns>the action related to (or <see cref="MessageAction.none"/> if unhandled)</returns>
        public MessageAction HandleInput(ConsoleKeyInfo input)
        {
            MessageAction result = MessageAction.none;

            if (useDefaultHandler)
            {
                result = defaultInputHandler(input);

                if(result != MessageAction.none)
                {
                    if ((result & Actions) != result) return MessageAction.none; //input unexpected
                    else return result;
                }
            }

            //if unhandled, try the custom handler if any
            if (_customKeyHandler != null) result = _customKeyHandler(input);

            return result;
        }

        internal readonly struct State
        {
            public State(ConsolePrompt prompt)
            {
                _actions = prompt.Actions;
                _options = prompt._options;
            }

            public void Restore(ConsolePrompt prompt)
            {
                prompt.Actions = _actions;
                prompt._options = _options;
            }
            private readonly MessageAction _actions;
            private readonly Options _options;
        }


        /// <summary>
        /// Add if needed the <see cref="MessageAction.abort"/> and <see cref="MessageAction.cancel"/>
        /// </summary>
        /// <returns></returns>
        internal State EnforceBreakCondition(bool withExit = false)
        {
            var state = new State(this);

            Actions |= MessageAction.cancel;
            if (withExit) Actions |= MessageAction.abort;

            _options &= ~Options.overwriteKeyMap;

            return state;
        }

        private static ConsoleKeyInfo makeConsoleKey(char k, bool icase)
        {
            if (char.IsLetterOrDigit(k) == false) throw new InvalidOperationException(
                 $"Console prompt key mapping expects a letter or a digit. Got '{k}'");

            return new ConsoleKeyInfo(k, (ConsoleKey)char.ToUpper(k), !icase && char.IsUpper(k), false, false);
        }

        [Flags]
        private enum Options
        {
            none = 0x0,
            /// <summary>
            /// the key default handler won't be used along with the custom handler
            /// </summary>
            overwriteKeyMap = 0x1,
            /// <summary>
            /// the default text prompt won't be displayed along with the custom text
            /// </summary>
            overwriteTextPrompt = 0x2,
            /// <summary>
            /// the prompt text won't be output at each prompt occurrence
            /// </summary>
            showPromptOnce = 0x4,
            /// <summary>
            /// help text should contain the prompt text too
            /// </summary>
            helpIncludesPrompt = 0x8
        }

        private bool useDefaultHandler => (_options & Options.overwriteKeyMap) == 0;

        private bool useDefaultPrompt => (_options & Options.overwriteTextPrompt) == 0;

        /// <summary>
        /// default handler that covers basic menu/action items
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static MessageAction defaultInputHandler(ConsoleKeyInfo input)
        {
            if (input.Key == ConsoleKey.Enter) return MessageAction.ok;
            else if (input.Key == ConsoleKey.Backspace) return MessageAction.cancel;
            else if (input.Key == ConsoleKey.End) return MessageAction.abort;
            else if (input.Key == ConsoleKey.Spacebar) return MessageAction.retry;
            else if (input.Key == ConsoleKey.Escape) return MessageAction.ignore;
            else if (char.ToUpper(input.KeyChar) == 'Y') return MessageAction.yes;
            else if (char.ToUpper(input.KeyChar) == 'N') return MessageAction.no;
            else if (input.KeyChar == '?') return MessageAction.help;

            return MessageAction.none;
        }

        private string makePrompt(MessageAction actions)
        {
            if (actions == MessageActions.AbortRetryIgnore)
                return string.Concat("[END] => Abort, [SPACE] => Retry, [ESC] => Ignore");
            else if (actions == MessageActions.OkCancel)
                return string.Concat("[ENTER] => OK, [BACKSPACE] => Cancel");
            else if (actions == MessageActions.RetryCancel)
                return string.Concat("[SPACE] => Retry, [BACKSPACE] => Cancel");
            else if (actions == MessageActions.YesNo)
                return string.Concat("[Y/y] => Yes, [N/n] => No");
            else if (actions == MessageActions.YesNoCancel)
                return string.Concat("[Y/y] => Yes, [N/n] => No, [BACKSPACE] => Cancel");
            else
            {
                var builder = new StringBuilder();
                if ((actions & MessageAction.help) != 0)
                    builder.Append("[?] => help, ");
                if ((actions & MessageAction.abort) != 0)
                    builder.Append("[END] => Abort, ");
                if ((actions & MessageAction.retry) != 0)
                    builder.Append("[SPACE] => Retry, ");
                if ((actions & MessageAction.ignore) != 0)
                    builder.Append("[ESC] => Ignore, ");
                if ((actions & MessageAction.ok) != 0)
                    builder.Append("[ENTER] => OK, ");
                if ((actions & MessageAction.cancel) != 0)
                    builder.Append("[BACKSPACE] => Cancel");
                if ((actions & MessageAction.yes) != 0)
                    builder.Append("[Y/y] => Yes, ");
                if ((actions & MessageAction.no) != 0)
                    builder.Append("[N/n] => No, ");

                builder.Remove(builder.Length - 2, 2);
                return builder.ToString();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="k1">KeyChar is expected to be upper case (when case insensitive)</param>
        /// <param name="icase">case insentive</param>
        /// <returns></returns
        private static ConsoleInputKeyMapper mapButton1(ConsoleKeyInfo k1, bool icase) 
        {
            return k => Equals(k, k1, icase) ? MessageAction.custom1 : MessageAction.none;
        }
        private static ConsoleInputKeyMapper mapButtons(ConsoleKeyInfo k1, ConsoleKeyInfo k2, bool icase)
        {
            return k => Equals(k, k1, icase) ? MessageAction.custom1 : 
                        Equals(k, k2, icase) ? MessageAction.custom2 : 
                        MessageAction.none;
        }
        private static ConsoleInputKeyMapper mapButtons(ConsoleKeyInfo k1, ConsoleKeyInfo k2, ConsoleKeyInfo k3, bool icase)
        {
            return k => Equals(k, k1, icase) ? MessageAction.custom1 : 
                        Equals(k, k2, icase) ? MessageAction.custom2 : 
                        Equals(k, k3, icase) ? MessageAction.custom3 : 
                        MessageAction.none;
        }

        internal static bool Equals(ConsoleKeyInfo k1, ConsoleKeyInfo k2, bool icase)
        {
            if (k1.Key != k2.Key) return false;

            if (icase)
            {
                if (char.ToUpper(k1.KeyChar) != char.ToUpper(k2.KeyChar)) return false;

                return (k1.Modifiers & ~ConsoleModifiers.Shift) == (k2.Modifiers & ~ConsoleModifiers.Shift);
            }
            else
            {
                if (k1.KeyChar != k2.KeyChar) return false;

                return k1.Modifiers == k2.Modifiers;
            }
        }

        private Options _options;

        /// <summary>
        /// The key mapping rules to be appended to or to set the default rules up <br/>
        /// May be null if the prompt object only holds the text prompt content
        /// </summary>
        private ConsoleInputKeyMapper _customKeyHandler;

        /// <summary>
        /// The text to be appended to or set up to the prompt<br/>
        /// May be null if the prompt object only defines mapping rules
        /// </summary>
        private string _customPromptText;

        private string _helpText;
    }
}
