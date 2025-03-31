using System;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace AltCoD.DotNetInventory
{
    using EmbeddedAssemblyLoader = BCL.Reflection.INTERNAL.EmbeddedAssemblyLoader;

    using BCL;
    using BCL.Platform;
    using BCL.CLI;
    using BCL.Reflection;

    /// <summary>
    /// ARGUMENTS: (any order)<br/>
    /// - [--no-dynload] Disable dynamic dependencies loading from embedded resource (libBCL.dll). <br/>
    ///   If [true] the lib must be provided with the exe. Anyway if the lib is provided with the exe, the dynamic
    ///   loading won't be used (since the assembly will be resolved from the beginning). The switch might remain useful
    ///   for startup performance taste
    /// - [--wait] pause before exiting
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            //disable dynamic loading of libBCL
            bool no_dynamic_ld = args.Contains("--no-dynload", StringComparer.OrdinalIgnoreCase);

            if (no_dynamic_ld == false)
            {
                var _ = new EmbeddedAssemblyLoader(AppDomain.CurrentDomain);
            }

            //the referenced symbols from the dynamically loaded lib MUST NOT be declared in the current scope
            return doMain(args);
        }

        static int doMain(string[] args)
        {
            bool help = args.Contains("--help", StringComparer.OrdinalIgnoreCase);
            bool wait = args.Contains("--wait", StringComparer.OrdinalIgnoreCase);
            bool custom_font = tryParseFontArg(args, out string font, out int size);

            Application app;

            if(custom_font) app = new Application(font, size);
            else app = new Application();

            if (help)
            {
                app.SayHello(help:true);
                return 0;
            }

            int result = app.Run();

            if (wait) Console.ReadKey();

            return result;

            bool tryParseFontArg(string[] parameters, out string fontname, out int fontsize)
            {
                fontname = null;
                fontsize = -1;

                string p = parameters.FirstOrDefault(a => a.Trim().StartsWith("--font", ignoreCase: true, CultureInfo.InvariantCulture));
                if (string.IsNullOrEmpty(p)) return false;

                int posit = p.IndexOf('=');
                if (posit != -1) p = p.Substring(posit + 1).Trim();
                posit = p.IndexOf(',');
                if (posit != -1)
                {
                    if (!int.TryParse(p.Substring(posit +1), out fontsize)) fontsize = -1;

                    fontname = p.Substring(0, posit);

                    return fontsize != -1;
                }

                fontname = string.Empty;
                return false;
            }
        }
    }

    class Application : WeakDepConsoleApplication
    {
        public const int _runtimeError = -1;

        public Application(string fontname, int size) 
            : base(fontname, size) 
        { 
            exitRaiseInterruptException = true; 
        }
        public Application() 
        { 
            exitRaiseInterruptException = true; 
        }

        public void SayHello(bool help)
        {
            var ass = Assembly.GetExecutingAssembly();
            _cui.WithColor(() => Console.WriteLine(_hello), ConsoleColor.Green);
            _cui.Out($"version: {ass.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion} " +
                $"(file:{ass.GetCustomAttribute<AssemblyFileVersionAttribute>().Version})");
            Console.WriteLine();

            if(help) showUsage();
        }

        private static readonly string _hello =
$@"
       -oOo- DotNet framework inventory -oOo-         
";

        private static readonly string _usage =
$@"
ARGUMENTS:
[--wait] Pause before exiting [optional] [default: false]
[--no-dynload] Disable the dynamic loading of dependencies from embedded resources [optional] [default: false]
[--help] Show this message [optional] [default: do not display]
[--font] Customize font. Spec:= fontname,fontsize such as --font=Consolas,14 (w/o spaces ... otherwise the whole input must be double quoted) [optional]

RETURNS:
 >= 0 means success and < 0 means error
[{_runtimeError}] runtime error has occured
[{ReturnCodes.Error._legacyNet}] met a legacy .net installed package
";
        private void showUsage()
        {
            _cui.WithColor(() => Console.WriteLine(_usage), ConsoleColor.DarkGray);
        }

        protected override int doRun()
        {
            SayHello(help:false);

            NETVersionInfo sdk_info = new NETVersionInfo(DotNetTarget.netfx, DotNetVersionType.SDK);
            NETVersionInfo clr_info = null;
            NETVersionInfo sel_info = null;

            _cui.Out("report", sdk_info.InstalledVersion.ToString(), "Highest installed framework");
            _cui.NewLine();

            var prompt1 = ConsolePrompt.Make(MessageAction.ok | MessageAction.ignore)
                .WithKeys('k', 'r', icase: true)
                .WithText(
@"Input [K/k] to report SD(K) info, input [R/r] to report CL(R) info
[ENTER] => default (SDK), [END] => exit the application
[?] => context info [ESC] => command-line arguments", append:false)
                .WithHelp(
@"SDK versions detection is performed from registry entries.
 - all frameworks versions from 2.0 to 4.8.1 are expected
 - special originalities like client-profile or full-profile are expected for framework 4.0 (detection for earlier version isn't reliable)
 - detection of 1.x versions isn't reliable
CLR versions detection is based on registry and hard-coded rules", inPrompt:false)
                .Prompt;

            var prompt2 =  ConsolePrompt.Make(MessageAction.abort|MessageAction.cancel|MessageAction.ok)
                .WithKeys('l', 'h', 'c', icase: true)
                .WithText(
@"Input [L/l] to (l)ist the detected installed versions
Input [H/h] to report the (h)ighest installed version
Input [C/c] to report the (c)urrent runtime information
[ENTER] => default (show ALL)
[BACKSPACE] => select SDK/CLR, [END] => exit the application", append:false)
                .Prompt;

            var console = new ConsoleRunner(this, _cui);

            console.Loop(prompt1, cin1 =>
            {
                if (cin1 == MessageAction.custom1)
                {
                    selectSDK();
                }
                else if (cin1 == MessageAction.custom2)
                {
                    //CLR info have been selected
                    sel_info = clr_info = clr_info ?? new NETVersionInfo(DotNetTarget.netfx, DotNetVersionType.CLR);
                    _cui.Out("x", string.Empty, "selecting CLR info ...");

                    runSelected(DotNetVersionType.CLR);
                }
                else if(cin1 == MessageAction.ok)
                {
                    selectSDK();
                }
                else if(cin1 == MessageAction.ignore)
                {
                    showUsage();
                }

                void selectSDK()
                {
                    //SDK info have been selected
                    sel_info = sdk_info;
                    _cui.Out("x", string.Empty, "selecting SDK info ...");

                    runSelected(DotNetVersionType.SDK);
                }

            }, withExit:true);

            void runSelected(DotNetVersionType type)
            {
                console.Loop(prompt2, cin2 =>
                {
                    if (cin2 == MessageAction.custom1)
                    {
                        //show list (*) detected versions
                        showAllVersions();
                    }
                    else if (cin2 == MessageAction.custom2)
                    {
                        //show highest
                        _cui.Out("report", sel_info.InstalledVersion.Dump(), $"Highest installed {type} version");
                        _cui.NewLine();
                    }
                    else if (cin2 == MessageAction.custom3)
                    {
                        //show runtime info
                        _cui.Out("report", sel_info.RuntimeVersion.Dump(), $"Running {type} version");
                        _cui.NewLine();
                    }
                    else if (cin2 == MessageAction.ok)
                    {
                        //show all info ---

                        showAllVersions();
                        _cui.Out("report", sel_info.InstalledVersion.Dump(), $"Highest installed {type} version");
                        _cui.NewLine();
                        _cui.Out("report", sel_info.RuntimeVersion.Dump(), $"Running {type} version");
                        _cui.NewLine();
                    }

                    void showAllVersions()
                    {
                        var builder = new StringBuilder();
                        foreach (var package in sel_info.GetAll())
                            builder.AppendLine($"- {package}");

                        _cui.Out("report", builder.ToString(), $"ALL installed {type} versions");
                    }
                });
            }

            if (sdk_info.RuntimeVersionEnforced) return 0;
            else return ReturnCodes.Error._legacyNet;
        }
    }
}
