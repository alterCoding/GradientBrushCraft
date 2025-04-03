using System;

namespace AltCoD.BCL.CLI
{
    using UI.Win32;

    /// <summary>
    /// Abstract UI implementation for Console target
    /// </summary>
    /// <remarks>
    /// <para>Limitations:<br/>
    /// Do not use for high-frequency outputs due to performance-related topics</para>
    /// </remarks>
    /// @internal PURPOSE: an easier usage than raw Console ... but we must not try to replace third party code which
    /// offer true decent console features (though it's difficult to achieve on windoze platforms)
    public class ConsoleAsUI : IUITextContract
    {
        public ConsoleAsUI(string fontname, int fontsize)
        {
            _native = NativeConsole.StdOut;

            _native.SetFont(fontname, (short)fontsize);
            Console.BackgroundColor = ConsoleColor.Black;
        }
        public ConsoleAsUI()
        {
        }

        #region interface IIOstreamUI

        public void Error(string message, string header)
        {
            WithColor(() =>
            {
                Console.Write("[ERROR] ");
                if (!string.IsNullOrEmpty(header)) Console.WriteLine(header);
                Console.WriteLine(message);
            },
            ConsoleColor.Red);
        }

        public void Info(string message, string header)
        {
            WithColor(() =>
            {
                Console.Write("[INFO] ");
                if (!string.IsNullOrEmpty(header)) Console.WriteLine(header);
                Console.WriteLine(message);
            },
            ConsoleColor.DarkGray);
        }

        public void Out(string message)
        {
            Console.WriteLine(message);
        }

        public void Out(string tag, string message, string header)
        {
            Console.Write(string.Concat("[", tag, "] "));
            if (!string.IsNullOrEmpty(header))
            {
                WithColor(() => Console.WriteLine(header), ConsoleColor.DarkGray);
            }
            Console.WriteLine(message);
        }

        public MessageAction Show(string msg, MessageAction actions, string header, MessageContext ctx, string prompt)
        {
            var cprompt = ConsolePrompt.Make(actions).WithText(prompt).Prompt;
            return Show(msg, header, ctx, cprompt);
        }

        public MessageAction Show(string msg, MessageAction actions, string header)
        {
            var cprompt = ConsolePrompt.Make(actions).Prompt;
            return Show(msg, header: null, MessageContext.none, cprompt);
        }

        #endregion

        /// <summary>
        /// </summary>
        /// @internal to be continued: e.g to not hardcode colors (to be discussed as need may not be monolithic for a
        /// single call so it may not be a good idea)
        [Flags]
        public enum FormatOptions
        {
            none = 0x0,
            disablePrompt = 0x1,
        }

        public static readonly FormatOptions EmptyFormat = new FormatOptions();

        public MessageAction Show(string msg, string header, MessageContext ctx, ConsolePrompt cprompt, FormatOptions fmt = 0) 
        {
            if (!string.IsNullOrEmpty(header))
            {
                WithColor(() =>
                {
                    Console.Write("---- ");
                    Console.Write(header);
                    Console.WriteLine(" ----");
                }, 
                ConsoleColor.DarkGray);
            }

            Console.WriteLine(msg);

            return Cin(cprompt, fmt);
        }

        public MessageAction Cin(ConsolePrompt cprompt, FormatOptions fmt = 0)
        {
            MessageAction input = MessageAction.none;

            if (!cprompt.IsNoOp)
            {
                if ((fmt & FormatOptions.disablePrompt) == 0)
                {
                    WithColor(() =>
                    {
                        string prompt = cprompt.GetPromptText();
                        if (!string.IsNullOrEmpty(prompt)) Console.WriteLine(prompt);
                    }, ConsoleColor.DarkYellow);
                }

                input = waitForInput(cprompt);
            }

            return input;
        }

        public void WithColor(Action output, ConsoleColor color)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            output();
            Console.ForegroundColor = old;
        }

        public void NewLine() => Console.WriteLine();

        private MessageAction waitForInput(ConsolePrompt prompt)
        {
            MessageAction input;
            do
            {
                var key = Console.ReadKey(true); //do not display input
                input = prompt.HandleInput(key);
            }
            while (input == MessageAction.none);

            return input;
        }

        private readonly NativeConsole _native;

    }

    /// <summary>
    /// A functor that returns the <see cref="MessageAction"/> action to be proceeded if a mapping exists for the 
    /// supplied input key <br/>
    /// For example, the default mapping rule returns <see cref="MessageAction.yes"/> if the supplied key is [Y,y]
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public delegate MessageAction ConsoleInputKeyMapper(ConsoleKeyInfo input);

    /// <summary>
    /// Console extensions
    /// </summary>
    public class ConsoleRunner
    {
        public ConsoleRunner(WeakDepConsoleApplication app, ConsoleAsUI console, ConsoleAsUI.FormatOptions options = 0)
        {
            _app = app;
            _console = console;
            Formatting = options;
        }

        /// <summary>
        /// Start the input loop upon the declared supported actions in the prompt object. The behavior depends on those
        /// supported actions and the supplied user action handler
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="proceed">the behavior implementation of every expected action. If an action is not declared
        /// into the prompt object, it will never been delivered to the user handler</param>
        /// <param name="withExit">enables to force the handling of the <see cref="MessageAction.abort"/> even if it
        /// was not declared in the prompt object. If the exit action has been declared in the prompt, passing FALSE
        /// here will be ignored (i.e the action will be delivered to the <paramref name="proceed"/> user handler</param>
        public void Loop(ConsolePrompt prompt, Action<MessageAction> proceed, bool withExit = false)
        {
            //must provide with a way to quit w/o using the prompt active choices
            //is useful when the caller omits to declare or implement an exit condition
            var state = prompt.EnforceBreakCondition(withExit);

            var break_cond = prompt.Actions & (MessageAction.abort | MessageAction.cancel);

            bool first = true;
            MessageAction input;
            do
            {
                var format = Formatting;

                if (prompt.ShouldShowOnce)
                {
                    if (first)
                    {
                        format &= ~ConsoleAsUI.FormatOptions.disablePrompt;
                        first = false;
                    }
                    else
                    {
                        format |= ConsoleAsUI.FormatOptions.disablePrompt;
                    }
                }

                input = _console.Cin(prompt, format);
                if (input == MessageAction.help)
                {
                    _console.WithColor(() => _console.Out(prompt.GetHelp()), ConsoleColor.DarkGray);
                }

                proceed(input);
            }
            while ((input & break_cond) != input);

            state.Restore(prompt);

            if (input == MessageAction.abort) _app.RequireExit(0);
        }

        public ConsoleAsUI.FormatOptions Formatting { get; set; }


        private readonly ConsoleAsUI _console;
        private readonly WeakDepConsoleApplication _app;
    }
}
