using System;
using System.Reflection;
using System.Globalization;

#if LIBBCL_HAVE_DYNAMIC_LOADING
namespace AltCoD.BCL.Reflection.INTERNAL
#else
namespace AltCoD.BCL.Reflection
#endif
{
    /// <summary>
    /// Helper to load from embedded resource (executing-assembly) an unresolved assembly
    /// </summary>
    /// @internal If the executing assembly needs to dynamically load _this library, it must obviously import this code
    /// prior (by copying it) and not commonly link to it (since the DLL isn't loaded at startup, the symbol will remains
    /// undefined)
#if LIBBCL_HAVE_DYNAMIC_LOADING
    internal 
#else 
    public
#endif
    class EmbeddedAssemblyLoader
    {
        public EmbeddedAssemblyLoader(AppDomain domain)
        {
            domain.AssemblyResolve += onUnresolvedAssembly;
        }

        /// <summary>
        /// Hooks to assembly resolver and tries to load assembly (.dll) from executable resources it CLR can't find it 
        /// locally.
        /// Used for embedding assemblies onto executables.
        /// See: http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
        /// </summary>
        private static Assembly onUnresolvedAssembly(object sender, ResolveEventArgs args)
        {
            var exe = Assembly.GetExecutingAssembly();
            Console.WriteLine($"[INFO] attempt to load from embedded resource the unresolved assembly {args.Name} from {exe}");

            var assname = new AssemblyName(args.Name);

            var path = assname.Name + ".dll";
            if (!assname.CultureInfo.Equals(CultureInfo.InvariantCulture))
            {
                path = $"{assname.CultureInfo}\\${path}";
            }

            byte[] bin;
            using (var stream = exe.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    Console.WriteLine($"[CRITICAL] NULL embedded resource stream for {path}");
                    return null;
                }

                bin = new byte[stream.Length];
                stream.Read(bin, 0, bin.Length);
            }

            var assembly = Assembly.Load(bin);
            Console.WriteLine($"[INFO] Assembly {assname.Name} has been dynamically loaded from embedded resource");
            return assembly;
        }
    }
}
