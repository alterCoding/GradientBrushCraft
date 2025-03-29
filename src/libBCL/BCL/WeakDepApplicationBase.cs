using System;
using System.Diagnostics;

namespace AltCoD.BCL
{
    using BCL.Platform;

    /// <summary>
    /// A base class that suits for an application aimed to be started as a standalone executable w/o any dependencies
    /// in a zero-deployment spirit (no config files, no install, nothing).
    /// <para>Motivation:<br/>
    /// Dedicated to tiny unpretentious applications where we would like to offer a single executable file. Run it, 
    /// ditch it somewhere, recover it.<br/>
    /// Nothing amazing ! but we need to enable to run possibly under a lower .net version than the targeted one.
    /// Obviously, we must warn the user and offer him to download the famous missing framework. As long as the CLR
    /// enables a decent startup of the application, the user has the choice to take a glance to the application w/o
    /// installing something new. For sure, we must carefully think about a limited feature-set and the full feature-set<br/>
    /// It's very arguable since it's neither .Net idiomatic at all nor modern. But in a way, it could be considered as 
    /// more versatile than relying on the regular behavior based on config-file and manifest (which offers to download 
    /// the target framework and/or aborts the application)
    /// </para>
    /// <para>Features: <br/>
    /// - read the target .net requirement, the runtime environment, as well as the installed environment <br/>
    /// - offer to download the missing .net <br/>
    /// - install unhandled exceptions handler, and especially cope with TypeLoadException since it's the one of the 
    ///   main drawback we can catch with due to a legacy .net version<br/>
    /// </para>
    /// </summary>
    /// <remarks>
    /// Inherit from one of the two mid-implementation instead of inheriting from _this base class  (referring to
    /// the <see cref="WeakDepConsoleApplication"/> or <see cref="AltCoD.Form.WeakDepFormApplication"/> classes)
    /// </remarks>
    public abstract class WeakDepApplicationBase
    {
        protected WeakDepApplicationBase(IUITextContract ui)
        {
            _ui = ui;

            _netRequire = NETVersionInfo.GetTargetFrameworkVersion();
            _netVerInfo = new NETVersionInfo(_netRequire.Target, DotNetVersionType.SDK);

            if (_netVerInfo.RuntimeVersionEnforced == false)
            {
                Debug.WriteLine(
                    $".Net version early check failure. Target:{_netRequire} but Running:{_netVerInfo.RuntimeVersion}");
            }
        }

        public NETVersionInfo DotNetSDKInfo => _netVerInfo;

        public int Run()
        {
            AppDomain.CurrentDomain.UnhandledException += onUnhandledException;

            try
            {
                int result = doRun();
                if (reportApplicationEnd)
                {
                    _ui.Info($"Application terminated with code: ({result})", "Application has stopped ...");
                }
                return result;
            }
            catch (StopException e)
            {
                int result = e.ReturnCode;
                if (reportApplicationEnd)
                {
                    _ui.Info($"Application has been interrupted with code: ({result})", "Application has stopped ...");
                }
                return result;
            }
        }

        public virtual void RequireExit(int code)
        {
            applicationExit(code);
        }

        protected virtual bool reportApplicationEnd => true;

        protected abstract int doRun();

        /// <summary>
        /// must implement application exit
        /// </summary>
        protected abstract void applicationExit(int code);

        protected void raiseInterrupt(int code) => throw new StopException(code);

        public void DisplayDotNetIsLegacy()
        {
            if (_netVerInfo.RuntimeVersionEnforced != false) return;

            //reset option
            _ignoreNetRequire = null;

            displayDotNetIsLegacy(withURL: true, disableExit:true);
        }


        /// <summary>
        /// Global exceptions handler in non-user interface (other or worker threads origin) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            showException(e.ExceptionObject as Exception);
        }

        protected void showException(Exception e)
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

            _ui.Error(message, $"{e.TargetSite} Type:{e.GetType()}");

            if (_ignoreNetRequire == false) enforceNetEnvironment(once:false);
        }

        protected virtual void enforceNetEnvironment(bool once)
        {
            if (_netVerInfo.RuntimeVersionEnforced == false)
            {
                displayDotNetIsLegacy(withURL: true, disableExit: !once);
            }
        }

        protected abstract void showMessageDotNetIsLegacy(bool withURL);

        protected void displayDotNetIsLegacy(bool withURL, bool disableExit)
        {
            //Native TaskDialog would be better and suits well to display an URL, but the TaskDialog wrapper
            //is not available with .net framework

            showMessageDotNetIsLegacy(withURL);

            if (_ignoreNetRequire != true)
            {
                string message =
$@"You can attempt to go ahead and ignore this warning about .NET requirement, but execution could further crash or miss-work.
Though anyone and anything won't suffer ;-)
[RETRY] Keep moving forward
[{(disableExit ? "CANCEL" : "IGNORE")}] Keep moving forward (and stop suggest)
{(disableExit == false ? "[ABORT] The application will exit now" : string.Empty)}
";
                var buttons = disableExit ? MessageActions.RetryCancel : MessageActions.AbortRetryIgnore;
                var result = _ui.Show(message, buttons, "Please confirm ...", MessageContext.envQuestion);
                if (result == MessageAction.retry) _ignoreNetRequire = false;
                else if (result == MessageAction.ignore || result == MessageAction.cancel) _ignoreNetRequire = true;
                else if (result == MessageAction.abort) applicationExit(ReturnCodes.Error._legacyNet);
            }
        }

        protected readonly IUITextContract _ui;

        protected readonly DotNetVersion _netRequire;
        protected readonly NETVersionInfo _netVerInfo;

        private bool? _ignoreNetRequire;

        private class StopException : Exception
        {
            public StopException(int returnCode) { ReturnCode = returnCode;  }
            public int ReturnCode { get; }
        }
    }

    public static class ReturnCodes
    {
        public static class Error
        {
            public const int _legacyNet = -2;
        }
    }
}
