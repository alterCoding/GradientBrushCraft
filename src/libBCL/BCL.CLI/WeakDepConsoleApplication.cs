using System;
using System.Diagnostics;

namespace AltCoD.BCL.CLI
{
    /// <summary>
    /// Minimialistic implemention of the standalone application (0-dep) idea, which targets console application 
    /// </summary>
    /// <remarks>Applies only to console application</remarks>
    /// <seealso cref="Forms.WeakDepFormApplication"/>
    public abstract class WeakDepConsoleApplication : WeakDepApplicationBase
    {
        public WeakDepConsoleApplication(string fontname, int fontsize)
            : base(new ConsoleAsUI(fontname, fontsize))
        {
            _cui = _ui as ConsoleAsUI;
        }
        public WeakDepConsoleApplication()
            : base(new ConsoleAsUI())
        {
            _cui = _ui as ConsoleAsUI;
        }

        /// <summary>
        /// How application exiting is implemented <br/>
        /// [TRUE] a special internal exception is raised and caught at base class level. It enables to have a chance
        /// to do something before exiting <br/>
        /// [FALSE] Environment.Exit(code) is used. No client code can be executed
        /// </summary>
        /// <remarks>Overriding <see cref="applicationExit(int)"/> enables to do something else</remarks>
        protected bool exitRaiseInterruptException { get; set; }

        protected override void showMessageDotNetIsLegacy(bool withURL)
        {
            string caption = $".Net requirement failure ...";

            string help = withURL ? "(Enter [?] to visit URL)" : string.Empty;

            string web_installer = $"{_netRequire.Moniker}-web-installer";
            string url = $"https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/{web_installer}";

            string message =
$@"This application needs {_netRequire.Description()} or later to properly run.
You are currenly running version {_netVerInfo.RuntimeVersion.Description()}. 
Your highest installation is {_netVerInfo.InstalledVersion.Description()}.
***************
You can download the {_netRequire.VersionTag} web installer from the Microsoft web site {help}
***************
{url}
";
            if (withURL)
            {
                MessageAction result = _cui.Show(message, caption, MessageContext.envIssue, 
                    ConsolePrompt.Make().WithKey('g', icase:true).WithText("[G/g] go to URL").Prompt);

                if (result == MessageAction.custom1) Process.Start(url);
            }
            else
            {
                _cui.Error(message, caption);
            }
        }

        protected override void applicationExit(int code)
        {
            //arguable ... but throwing a special exception could be even more arguable ... 
            //finally the best way could be return a code and unwind the stack (though it's manageable only in simple
            //code path cases)
            //In the end, we implement both the special exception way and the brutal way (upon option)

            if(exitRaiseInterruptException) raiseInterrupt(code);
            else Environment.Exit(code);
        }

        protected readonly ConsoleAsUI _cui;
    }
}
