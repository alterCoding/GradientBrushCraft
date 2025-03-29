using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace AltCoD.UI.WinForms
{
    using BCL;
    using BCL.Platform;

    /// <summary>
    /// Implemention base class of the standalone application (0-dep) idea, which targets Win Forms application 
    /// <para>
    /// - implement <see cref="createMainForm"/> to return the concrete application main form <br/>
    /// </para>
    /// </summary>
    /// <remarks>Applies only to Windows Form application</remarks>
    /// <seealso cref="WeakDepConsoleApplication"/>
    public abstract class WeakDepFormApplication : WeakDepApplicationBase
    {
        protected WeakDepFormApplication() : base(new FormAsUI())
        {
        }

        protected abstract Form createMainForm();

        protected sealed override int doRun()
        {
            Application.ThreadException += onUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _mainForm = createMainForm();
            _mainForm.Shown += onFormShown;

            Application.Run(_mainForm);
            return 0;
        }

        protected override void enforceNetEnvironment(bool once)
        {
            if (_netVerInfo.RuntimeVersionEnforced == false)
            {
                //if Form hasn't still be loaded, the URL opening won't work from the MessageBox dialog, thus we
                //give the URL to the user ... and thats'all 

                if (_mainForm.InvokeRequired)
                    _mainForm.Invoke(new Action(() => displayDotNetIsLegacy(withURL:_formShown, disableExit:!once)));
                else
                    displayDotNetIsLegacy(withURL:_formShown, disableExit:!once);
            }
        }

        protected override void applicationExit(int code)
        {
            _mainForm.Close();
        }

        protected override void showMessageDotNetIsLegacy(bool withURL)
        {
            //Native TaskDialog would be better and suits well to display an URL, but the TaskDialog wrapper
            //is not available with .net framework

            string caption = $".Net requirement failure ...";

            string help = withURL ? "(Go to with HELP button)" : string.Empty;

            string web_installer = $"{_netRequire.Moniker}-web-installer";
            string url = $"https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/{web_installer}";
            Clipboard.SetText(url);

            string message =
$@"This application needs {_netRequire.Description()} or later to properly run.
You are currenly running version {_netVerInfo.RuntimeVersion.Description()}. 
Your highest installation is {_netVerInfo.InstalledVersion.Description()}.

You can download the {_netRequire.VersionTag} web installer from the Microsoft web site {help}

{url}

The URL has been copied to the clipboard. You can check it ... ;-)
";
            if (withURL)
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        0, //'0 is default otherwise use MessageBoxOptions Enum
                        url, string.Empty);
            }
            else
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Global exceptions handler in main thread (GUI)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onUnhandledException(object sender, ThreadExceptionEventArgs e)
        {
            showException(e.Exception);
        }

        private void onFormShown(object sender, EventArgs e)
        {
            _formShown = true;

            if (_netVerInfo.RuntimeVersionEnforced == false)
                _mainForm.Text += " [DEGRADED]";

            enforceNetEnvironment(once:true);
        }


        private Form _mainForm;
        private bool _formShown;
    }

    /// <summary>
    /// </summary>
    /// @internal TO BE REMOVED
    abstract class WeakDepApplication
    {
        protected WeakDepApplication()
        {
            _netRequire = NETVersionInfo.GetTargetFrameworkVersion();
            _netVerInfo = new NETVersionInfo(_netRequire.Target, DotNetVersionType.SDK);

            if (_netVerInfo.RuntimeVersionEnforced == false)
            {
                Debug.WriteLine(
                    $"Early .Net version check failure. Target:{_netRequire} but Running:{_netVerInfo.RuntimeVersion}");
            }
        }

        public NETVersionInfo DotNetInfo => _netVerInfo;

        public void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += onUnhandledException;
            Application.ThreadException += onUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _mainForm = createMainForm();
            _mainForm.Shown += onFormShown;

            Application.Run(_mainForm);
        }

        public void DisplayDotNetIsLegacy()
        {
            if (_netVerInfo.RuntimeVersionEnforced != false) return;

            //reset option
            _ignoreNetRequire = null;

            displayDotNetIsLegacy(withURL: true, disableExit:true);
        }

        protected abstract Form createMainForm();


        /// <summary>
        /// Global exceptions handler in non-user interface (other or worker threads origin) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            showException(e.ExceptionObject as Exception);
        }
        
        /// <summary>
        /// Global exceptions handler in main thread (GUI)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onUnhandledException(object sender, ThreadExceptionEventArgs e)
        {
            showException(e.Exception);
        }

        private void showException(Exception e)
        {
            string message = e.InnerException?.Message;
            if (message != null) message = string.Concat(e.Message, Environment.NewLine, "<-- ", message);
            else message = e.Message;

            //catch this nasty context:
            //the client net platform version is lesser than the target one (but not too much ... so it can be launched)
            //at some location, the code references a missed symbol ... exception is thrown by CLR
            //
            if(e is TypeLoadException && _netVerInfo.RuntimeVersionEnforced == false)
            {
                message += @"

TIP: The issue has surely been raised due to running a too old .Net version";
                
                if (_ignoreNetRequire == false)
                    message += @"

Please pay attention to the following message about the .NET requirement";
            }

            MessageBox.Show(message, $"{e.TargetSite} Type:{e.GetType()}", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (_ignoreNetRequire == false) enforceNetEnvironment(once:false);
        }

        private void onFormShown(object sender, EventArgs e)
        {
            _formShown = true;

            if (_netVerInfo.RuntimeVersionEnforced == false)
                _mainForm.Text += " [DEGRADED]";

            enforceNetEnvironment(once:true);
        }

        private void displayDotNetIsLegacy(bool withURL, bool disableExit)
        {
            //Native TaskDialog would be better and suits well to display an URL, but the TaskDialog wrapper
            //is not available with .net framework

            string caption = $".Net requirement failure ...";

            string help = withURL ? "(Go to with HELP button)" : string.Empty;

            string web_installer = $"{_netRequire.Moniker}-web-installer";
            string url = $"https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/{web_installer}";
            Clipboard.SetText(url);

            string message =
$@"This application needs {_netRequire.Description()} or later to properly run.
You are currenly running version {_netVerInfo.RuntimeVersion.Description()}. 
Your highest installation is {_netVerInfo.InstalledVersion.Description()}.

You can download the {_netRequire.VersionTag} web installer from the Microsoft web site {help}

{url}

The URL has been copied to the clipboard. You can check it ... ;-)
";
            if (withURL)
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        0, //'0 is default otherwise use MessageBoxOptions Enum
                        url, string.Empty);
            }
            else
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (_ignoreNetRequire != true)
            {
                message =
$@"You can attempt to go ahead and ignore this warning about .NET requirement, but execution could further crash or miss-work.
Though anyone and anything won't suffer ;-)
[RETRY] Keep moving forward
[{(disableExit ? "CANCEL" : "IGNORE")}] Keep moving forward (and stop suggest)
{(disableExit == false ? "[ABORT] The application will exit now" : string.Empty)}
";
                var buttons = disableExit ? MessageBoxButtons.RetryCancel : MessageBoxButtons.AbortRetryIgnore;
                var result = MessageBox.Show(message, "Please confirm ...", buttons, MessageBoxIcon.Question);
                if (result == DialogResult.Retry) _ignoreNetRequire = false;
                else if (result == DialogResult.Ignore || result == DialogResult.Cancel) _ignoreNetRequire = true;
                else if (result == DialogResult.Abort) Application.Exit();
            }
        }

        private void enforceNetEnvironment(bool once)
        {
            if (_netVerInfo.RuntimeVersionEnforced == false)
            {
                //if Form hasn't still be loaded, the URL opening won't work from the MessageBox dialog, thus we
                //give the URL to the user ... and thats'all 

                if (_mainForm.InvokeRequired)
                    _mainForm.Invoke(new Action(() => displayDotNetIsLegacy(withURL:_formShown, disableExit:!once)));
                else
                    displayDotNetIsLegacy(withURL:_formShown, disableExit:!once);
            }
        }

        private Form _mainForm;
        private bool _formShown;

        private readonly NETVersionInfo _netVerInfo;
        private readonly DotNetVersion _netRequire;
        private bool? _ignoreNetRequire;
    }
}
