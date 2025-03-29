using System;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;

    //do not reference BCL.Reflection.EmbeddedAssemblyLoader
    using EmbeddedAssemblyLoader = BCL.Reflection.INTERNAL.EmbeddedAssemblyLoader;

    class App : WeakDepFormApplication
    {
        private App() { }

        public static App Instance { get; } = new App();

        public DependenciesInfo GetDependencies() => new DependenciesInfo();

        protected override bool reportApplicationEnd => false;
        protected override Form createMainForm() => new GradientForm();
    }

    class DependenciesInfo
    {
        public DependenciesInfo()
        {
            var dependencies = AppDomain.CurrentDomain.GetAssemblies();
            var lib = dependencies.Where(ass => ass.GetName().Name.Equals("libBCL", StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();

            if(lib != null)
            {
                LibBCLVersion = lib.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                LibBCLIsEmbedded = string.IsNullOrEmpty(lib.Location);
            }
        }

        /// <summary>
        /// the loaded libBCL version (loaded from embedded resource or regular resolving)
        /// </summary>
        public string LibBCLVersion { get; }
        /// <summary>
        /// The loaded libBCL library has been resolved from embedded resources
        /// </summary>
        public bool LibBCLIsEmbedded { get; }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //disable dynamic loading of libBCL
            bool no_dynamic_ld = args.Contains("--no-dynload", StringComparer.OrdinalIgnoreCase);

            if (no_dynamic_ld == false)
            {
                //enable the dynamic loading of libBCL from embedded resource (referring to the project config entry
                //EmbeddedResource that embeds the dll libBCL into the executable)

                var _ = new EmbeddedAssemblyLoader(AppDomain.CurrentDomain);
            }

            doMain(args);
        }

        static void doMain(string[] args)
        {
            App.Instance.Run();
        }
    }
}
