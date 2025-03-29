using System;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;
    using BCL.Reflection;

    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();

            this.Text = $"About {AssemblyTitle} ({AssemblyInformationalVersion})";

            this.labelProductName.Text = $"{AssemblyProduct} / {AssemblyDescription}";

            var depencies = App.Instance.GetDependencies();

            this.labelVersion.Text = $"Version Assembly:{AssemblyVersion}, Version File:{AssemblyFileVersion} "+
                $"{Environment.NewLine} libBCL: {depencies.LibBCLVersion} Loaded from embedded resource: {depencies.LibBCLIsEmbedded}";

            this.labelCopyright.Text = AssemblyCopyright;

            string url = "https://github.com/alterCoding";
            string company = $"{AssemblyCompany} - {url}";
            this.labelCompanyName.Text = company;
            labelCompanyName.LinkBehavior = LinkBehavior.HoverUnderline;
            int i = company.IndexOf("https");
            labelCompanyName.LinkArea = new LinkArea(i, company.Length - i);
            labelCompanyName.LinkClicked += async (s, e) => await onLinkClicked(s, e);
            labelCompanyName.Links[0].LinkData = url;
            setTooltipURL(labelCompanyName);

            string message =
@"""Gradient Brushes Craft"" is an unpretentious tiny application that manages and displays a gradient brush while enabling the user to interactively play with some of its parameters.
The brush is an instance of the <PathGradientBrush> or <LinearGradientBrush> classes. As of now, the gradient is a simple @2-colors (using the <PathGradientBrush>.{SurroundColors} or <LinearGradientBrush>.{LinearColors} property), from which a <Blend> object may be attached to and modified (though limited to @3 variable positions/factors).
If you are like me:
 - think that the .Net documention isn't too verbose about the gradient parameters
 - whereas the expected visual gradient result of the <PathGradientBrush>/<LinearGradientBrush>/<Blend> classes properties changes is not always obvious
 - maybe this tool could allow you to save time.

**SCOPE:** this tool targets the GDI+ graphics (and is limited to), thus it may be qualified as quite old; the GDI being heavily used especially with the .Net Framework and/or the Windows Forms.
A second tool (or maybe an update) is needed to target the Presentation Framework (WPF).
";
            this.textBoxDescription.Text = message;
            var parser = new GenericParser(textBoxDescription);
            parser.Parse();

            linkLicense.Text = "Mozilla Public License 2.0";
            linkLicense.LinkClicked += async (s, e) => await onLinkClicked(s, e);
            linkLicense.LinkBehavior = LinkBehavior.HoverUnderline;
            linkLicense.Links[0].LinkData = _licenseFileName;

            var dotnet = App.Instance.DotNetSDKInfo;
            var sdk = dotnet.RuntimeVersion;

            txtInfoDotNet.Text = $"SDK: {sdk.Description(world: false)}";

            //prevent from symbol undefined
            if (sdk.WorldVersion >= new Version(4, 7, 1)) txtInfoSystem.Text = getOSInfo();
            else txtInfoSystem.Text = getOSInfoLegacy();

            txtInfoNetPath.Text = sdk.InstallPath;

            btnCopyEnv.Click += (s, e) => Clipboard.SetText(
                $"{txtInfoDotNet.Text}{Environment.NewLine}{txtInfoSystem.Text}{Environment.NewLine}{txtInfoNetPath.Text}"
                );

            if(dotnet.RuntimeVersionEnforced == false)
            {
                btnNetHelp.Visible = true;
                btnNetHelp.Click += (s, e) => App.Instance.DisplayDotNetIsLegacy();
            }
            else
            {
                btnNetHelp.Visible = false;
            }
        }

        /** suits for net > 4.7.1 */
        private static string getOSInfo()
        {
            return $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}) "+
                $"app.runas: {(Environment.Is64BitProcess ? "x64" : "x86")}";
        }
        /** 
         * suits for net < 4.7.1 ... RuntimeInformation symbol is undefined 
         * symbol is undefined in 4.7 but defined in sub minor version (4.7.1) ... congrats
         */
        private static string getOSInfoLegacy()
        {
            return $"{Environment.OSVersion} app.runas: {(Environment.Is64BitProcess ? "x64" : "x86")}";
        }

        private void setTooltipURL(Control control)
        {
            toolTip.SetToolTip(control, "RIGHT-Click to copy the URL. LEFT-Click to visit the URL");
        }

        private async Task onLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var label = sender as LinkLabel;
            string target = e.Link.LinkData as string;
            if (string.IsNullOrWhiteSpace(target)) return;

            if (label == labelCompanyName)
            {
                e.Link.Visited = true;

                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    Clipboard.SetText(target);
                    toolTip.Show("URL has been copied", label);

                    await Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        BeginInvoke((Action<Control>)setTooltipURL, label);
                    });

                    return;
                }
                else if (e.Button.HasFlag(MouseButtons.Left))
                {
                    Process.Start(target);
                }
            }
            else if(label == linkLicense)
            {
                showLicenseFile();
            }
        }

        private void showLicenseFile()
        {
            if (File.Exists(_licenseFileName))
            {
                //if a local file exists, use it
                Process.Start(_licenseFileName);
            }
            else
            {
                var lic = Properties.Resources.LICENSE;
                var dir = Path.Combine(Path.GetTempPath(), _tmpDir);
                var filename = Path.Combine(dir, _licenseFileName);

                if(File.Exists(filename))
                {
                    //if a local file exists, use it
                    Process.Start(filename);
                }
                else
                {
                    Directory.CreateDirectory(dir);

                    File.WriteAllText(filename, lic);
                    Process.Start(filename);
                }
            }
        }

        #region Assembly  attributes

        public string AssemblyInformationalVersion
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyInformationalVersionAttribute attrib))
                    return attrib.InformationalVersion;
                else
                    return string.Empty;
            }
        }
        public string AssemblyTitle
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyTitleAttribute attrib) && attrib.Title.Length > 0)
                    return attrib.Title;
                else
                    return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public string AssemblyFileVersion
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyFileVersionAttribute attrib))
                    return attrib.Version;
                else
                    return string.Empty;
            }
        }

        public string AssemblyDescription
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyDescriptionAttribute attrib))
                    return attrib.Description;
                else
                    return string.Empty;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyProductAttribute attrib))
                    return attrib.Product;
                else
                    return string.Empty;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyCopyrightAttribute attrib))
                    return attrib.Copyright;
                else
                    return string.Empty;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                if (Assembly.GetExecutingAssembly().TryCustomAttribute(out AssemblyCompanyAttribute attrib))
                    return attrib.Company;
                else
                    return string.Empty;
            }
        }
        #endregion

        private static readonly string _licenseFileName = "LICENSE.txt";
        private static readonly string _tmpDir = "alterCoding/GradientBrushesCraft";
    }
}
