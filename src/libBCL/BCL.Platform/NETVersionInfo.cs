using System;
using System.Linq;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Versioning;
using System.Collections.Generic;

#if BCL_HAVE_GUI
using System.Windows.Forms;
#endif

using System.IO;
using System.Diagnostics;

namespace AltCoD.BCL.Platform
{
    using BCL.Reflection;
    using BCL.FileSystem;

    /// <summary>
    /// A partial implementation of the dotnet system information versioning that enables to inspect and get runtime
    /// information or framework installation details (a little bit ... as far as possible without any too much coding
    /// unpleasant research or windoze internals guess)
    /// </summary>
    /// <remarks>
    /// <para>Motivation:<br/>
    /// One of the main materials of this class is to enable a validation of the runtime environment vs. the target
    /// environment. In regular application development, this use case is useless since the application comes along with
    /// a config file (and if the SKU requirement is not met, .NET offers to download the package. <br/>
    /// Nice ... but for this tiny application, we want zero-deployment (thus no config-file at all). If the CLR is
    /// compatible, the application can be started anyway (w/o any assurance obviously). This permissive behavior
    /// enables us to execute the application which could fail quickly though, but we can offer the user to do a bit more.
    /// That's not cheap and not .net/modern dev idiomatic,  but I've always disliked the default intrusive behavior 
    /// which often leads to the mantra "always install the last version of all things" and you are done.
    /// </para>
    /// <para>Limitations:<br/>
    /// - runtime-only (i.e client-profile) earlier than 4.0 are not considered as their registry entries seem fully
    ///   random (based on unsuccessful trials with XP in VMs ...)<br/>
    /// - original versions of 1.0 should not be detected as they didn't seem to use the NDP registry key
    /// </para>
    /// </remarks>
    /// @internal for future implementation of .Net X : https://learn.microsoft.com/en-us/dotnet/core/install/how-to-detect-installed-versions?pivots=os-windows 
    /// @internal for Net.FX wanderings https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed#
    public class NETVersionInfo
    {
        /// <summary>
        /// Get the current context .Net runtime info and the highest installed NET package
        /// </summary>
        /// <param name="target">retrieve the highest installed .net for the supplied target</param>
        /// <param name="type">Tell if one is interested in CLR or SDK data (involved only the installed items info).
        /// Specifying <see cref="DotNetTargetVersionType.any"/> does not make sense (thus assuming <see cref="DotNetTargetVersionType.SDK"/>)
        /// </param>
        public NETVersionInfo(DotNetTarget target = DotNetTarget.any, DotNetVersionType type = DotNetVersionType.SDK)
        {
            type = type == DotNetVersionType.any ? DotNetVersionType.SDK : type;

            Target = target;
            VersionType = type;

            //@todo when netappcore is implemented, 'any' should become (*) target 

            if (target == DotNetTarget.any || target == DotNetTarget.netfx)
            {
                if (type == DotNetVersionType.SDK) InstalledVersion = getLatestNetFxFromRegistry();
                else InstalledVersion = getCLRFromRegistry();
            }
            else if (target == DotNetTarget.netcore)
                throw new NotImplementedException(
                    "implement the dotnet SDK standard way with dotnet.exe --list-sdks and --list-runtimes");
            else
                throw new NotImplementedException($"implement the target {target}");

            if (type == DotNetVersionType.CLR)
            {
                RuntimeVersion = getCLRRuntimeVersion(target); 
            }
            else
            {
                _sdkRuntimeInfo = getSDKRuntimeInfo(fakeRtti: false);
                RuntimeVersion = _sdkRuntimeInfo;
            }
        }

        public DotNetTarget Target { get; }

        public DotNetVersionType VersionType { get; }

        /// <summary>
        /// Get the highest (installed) .NET available (sdk or clr according to <see cref="Target"/> and <see cref="VersionType"/>)
        /// </summary>
        public DotNetVersion InstalledVersion { get; }

        /// <summary>
        /// Get the current .NET version from _this execution context (SDK or CLR according to <see cref="VersionType"/>)
        /// </summary>
        public DotNetVersion RuntimeVersion { get; }

        /// <summary>
        /// [TRUE] if the runtime SDK .net version is equal or later than the target framework of the current application<br/>
        /// We talk about SDK version, as a CLR requirement failure isn't a subject at all since the execution would't
        /// have been started
        /// </summary>
        public bool RuntimeVersionEnforced => checkRuntimeAccordingToTarget();
        
        /// <summary>
        /// Get the whole list of all installed/upgraded net versions
        /// </summary>
        /// <remarks>ordered by version</remarks>
        /// <param name="rescan"></param>
        /// <returns></returns>
        public List<DotNetVersion> GetAll(bool rescan = false)
        {
            IEnumerable<DotNetVersion> l;
            if (_packages.Any() && !rescan)
            {
                l = _packages.Values;
            }
            else
            {
                l = VersionType == DotNetVersionType.CLR ? getAllCLRFromRegistry() : getAllNetFxFromRegistry();
                l = insert(l, clear:true); //store into _packages
            }

            return l.OrderBy(v => v.WorldVersion).ToList();
        }

        /// <summary>
        /// Retrieve the target version from assembly attribute <see cref="TargetFrameworkAttribute"/> (which should be
        /// mandatory)
        /// </summary>
        /// <param name="assembly">if not supplied, the entry assembly will be used</param>
        /// <returns>A weak version, meaning the version is a so-called world version (i.e a true useable .net version,
        /// but which doesn't reference any patch-number nor build-number)</returns>
        public static DotNetVersion GetTargetFrameworkVersion(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetEntryAssembly();
            var attrib = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            if (attrib == null) return null;

            var framework = new FrameworkName(attrib.FrameworkName);
            return new DotNetVersion(framework);
        }

        public bool CouldExecute(DotNetVersion target)
        {
            if (InstalledVersion.Target != target.Target || InstalledVersion.VersionType != target.VersionType)
                throw new InvalidOperationException($"attempt to compare incompatible {nameof(DotNetVersion)} objects");

            //do we better need to compare world versions
            return InstalledVersion.InternalVersion >= target.InternalVersion;
        }

        /// <summary>
        /// call <see cref="RuntimeEnvironment.GetSystemVersion()"/> and unwraps the version tag
        /// </summary>
        /// <returns></returns>
        /// <remarks>Is this information really useful ? as it misses the rev-number of the version and adds a nasty
        /// prefix 'v' at head. Microsoft warns against use of <see cref="Environment.Version"/> and advises to use the
        /// registry (for netFX) instead. For sure, Environment.Version for netFX 4.5 and later are stuck to a rev-number
        /// of 2000 but at least it enables to get a rev-number, whereas <see cref="RuntimeEnvironment.GetSystemVersion"/>
        /// is limited to major.minor.build (or patch-number)
        /// </remarks>
        internal static string UnwrapRuntimeSystemVersion()
        {
            string v = RuntimeEnvironment.GetSystemVersion();
            if (!char.IsDigit(v[0]))
                //implementation inserts a nasty 'v' ... not confident at all about robustness of this statement
                return v.Substring(1);
            else
                return v;
        }

        #region SDK retrieval

        /// <summary>
        /// Suits for netFX 4.5 and later (till 4.8.1 at now) <br/>
        /// Retrieve the "Version" key value for the long version number (<see cref="DotNetVersion.InternalVersion"/> <br/>
        /// Map the "Release" key value to the understanble version number (<see cref="DotNetVersion.WorldVersion"/>
        /// </summary>
        /// <param name="key">the key such as v4/Full (latest install) or v4/Full/1033 (some prior install)</param>
        /// <param name="defaultPath">the default path that has to be used when one is retrieving an installation stuff
        /// which isn't the latest. Indeed in this case, the 'installPath' value does not exist, then we can safely
        /// take the highest one, since upgrades are expected to have been done in place</param>
        /// <returns></returns>
        private DotNetVersion fromRegistryNetFX45OrLater(RegistryKey key, string defaultPath = null)
        {
            DotNetVersion ver = null;

            var value = key.GetValue("Release");
            if (value != null)
            {
                Version fx45 = netFX45OrLaterVersionFromRelease((int)value);
                if(fx45 != null)
                {
                    var versionsz = key.GetValue("Version") as string;
                    var path = key.GetValue("InstallPath") as string ?? defaultPath;

                    if (!string.IsNullOrWhiteSpace(versionsz) && path != null)
                    {
                        ver = DotNetVersion.NetFxSDK(fx45, versionsz, (uint)(int)value, path);
                    }
                }
            }

            return ver;

            Version netFX45OrLaterVersionFromRelease(int release)
            {
                if (release >= 533320) return new Version(4,8,1);
                if (release >= 528040) return new Version(4,8);
                if (release >= 461808) return new Version(4,7,2);
                if (release >= 461308) return new Version(4,7,1);
                if (release >= 460798) return new Version(4,7);
                if (release >= 394802) return new Version(4,6,2);
                if (release >= 394254) return new Version(4,6,1);
                if (release >= 393295) return new Version(4,6);
                if (release >= 379893) return new Version(4,5,2);
                if (release >= 378675) return new Version(4,5,1);
                if (release >= 378389) return new Version(4,5);

                //a more comprehensive list (depending on OS versioins) on
                //https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies

                return null;
            }
        }

        /// <summary>
        /// seeks for the FX 4 (full/client) or 3.5 <br/>
        /// Retrieve the "Version" key value for the long version number (<see cref="DotNetVersion.InternalVersion"/> <br/>
        /// The understandable version number is by design set to 3.5 or 4.0 (<see cref="DotNetVersion.WorldVersion"/> <br/>
        /// The "SP" key value is read to get the service-pack number
        /// </summary>
        /// <param name="ndp">expect opening from key:NDP</param>
        /// <returns></returns>
        private DotNetVersion fromRegistryNetFX35OrNetFX40(RegistryKey ndp)
        {
            DotNetVersion version = null;

            //try 4.0 full profile
            var key = ndp.OpenSubKey(@"v4\Full");

            //special case for client profile only installation of 4.0 (4.0 only ... since the Client profile has been
            //discontinued from 4.5 and registry entries are not really consistent before 4.0)
            if (key == null) key = ndp.OpenSubKey(@"v4\Client");

            //lack of 4.0 full, lack of 4.0 client ... fallback to 3.5
            //
            //we should don't care about 3.5 client profile as the full fx3.5 is expected to be available on win7 
            //(and it's time to forget all before win7 .. but who knows)
            if (key == null) key = ndp.OpenSubKey(@"v3.5\Full");
            if (key == null) key = ndp.OpenSubKey(@"v3.5\Client");

            if(key != null) version = fromRegistryNetFX40OrEarlier(key);

            key?.Dispose();
            return version;
        }

        /// <summary>
        /// Retrieves netFX from 2.0 to 4.0
        /// </summary>
        /// <param name="ndp">expect opening from key:NDP/v3.5 and its subkeys as [/1033] or NDP/v4[full,client] 
        /// and its subkeys as [/1033]</param>
        /// <param name="defaultPath">the default path that has to be used when one is retrieving an installation stuff
        /// which isn't the latest. Indeed in this case, the 'installPath' value does not exist, then we can safely
        /// take the highest one, since upgrades are expected to have been done in place.<br/>
        /// Notice that versions prior to 3.5 don't seem to have a value "InstallPath", so no way to be properly generic</param>
        /// <returns></returns>
        private DotNetVersion fromRegistryNetFX40OrEarlier(RegistryKey ndp, string defaultPath = null)
        {
            if (!(ndp.GetValue("Version") is string versionsz) || string.IsNullOrEmpty(versionsz)) return null;

            var installed = ndp.GetValue("Install");
            if (installed == null || (int)installed == 0) return null;

            int service_pack = 0;
            var sp = ndp.GetValue("SP");
            if (sp != null) service_pack = (int)sp;

            //we expect 3.5 or 4.0 for actual useable version numbers
            var ver = new Version(versionsz);

            //before 3.5, no InstallPath !!
            var path = ndp.GetValue("InstallPath") as string ?? defaultPath;
            if ((ver.Major >= 4 || (ver.Major == 3 && ver.Minor == 5)) && path == null) return null;

            //the 4.0 is the sole properly detectable client vs. full profiles
            //3.5 client profile (on XP) does not install into the NDP key but in a fucking ..\NDP\DotNetClient\ !!!
            //that's completely unmanageable
            //so we assume that anything prior to v4.0 is a full profile
            //motivation: installating the 3.5 sp1 full profile FIXES the registry entries thus let's assume to
            //unsupport those fucking client profiles

            var profile = ndp.Name.Contains("Full") ? DotNetProfile.fullFX
                : (ndp.Name.Contains("Client") ? DotNetProfile.clientFX : DotNetProfile.fullFX);

            return DotNetVersion.NetFxSDK(ver.Major, ver.Minor, versionsz, service_pack, profile, path);
        }

        private IEnumerable<DotNetVersion> getAllNet35AndEarlier(RegistryKey ndp)
        {
            //get all 2.x and 3.5
            // for each child key "NDP\v(x.x)" excluding the v4x
            //  get at first the highest
            //  and next iterate over the leaf subkeys under v(x.x) ... such as *\1033 *\1036

            RegistryKey regkey = null;
            try
            {
                foreach (var skn in getVersionKeyNames(ndp).Where(n => !n.StartsWith("v4")))
                {
                    regkey = ndp.OpenSubKey(skn);
                    if (regkey == null) continue;

                    DotNetVersion highver = null;

                    //attempt to the latest fx versions < 4.0
                    highver = fromRegistryNetFX40OrEarlier(regkey);

                    if (highver == null) continue;
                    else yield return highver;

                    //get (if any) the prior versions that have been upgraded in place
                    //(notice that we should get as a duplicate the highest version)
                    foreach (var sskn in regkey.GetSubKeyNames())
                    {
                        var subkey = regkey.OpenSubKey(sskn);
                        if (subkey != null && subkey.SubKeyCount == 0) //expected leaf key
                        {
                            var version = fromRegistryNetFX40OrEarlier(subkey, highver.InstallPath);
                            if (version != null) yield return version;
                        }
                        subkey?.Dispose();
                    }
                }
            }
            finally { regkey?.Dispose(); }
        }

        /// <summary>
        /// Get the latest installed netfx 4x version from the registry info
        /// </summary>
        /// <param name="ndp">the root registry key 'NDP'</param>
        /// <returns></returns>
        private DotNetVersion getLatestNet4x(RegistryKey ndp)
        {
            DotNetVersion version = null;
            RegistryKey regkey = null;

            try
            {
                regkey = ndp.OpenSubKey(@"v4\Full");

                if (regkey != null)
                {
                    //attempt to the latest fx versions >= 4.5
                    version = fromRegistryNetFX45OrLater(regkey);

                    //fallback to 4.0 full
                    if (version == null) version = fromRegistryNetFX40OrEarlier(regkey);
                }

                if (version == null)
                {
                    //last chance to 4.0 client
                    regkey.Dispose();
                    regkey = ndp.OpenSubKey(@"v4\Client");

                    if (regkey != null) version = fromRegistryNetFX40OrEarlier(regkey);
                }
            }
            finally { regkey?.Dispose(); }

            return version;
        }

        /// <summary>
        /// Get the latest installed netfx [1.x-3.5] version from the registry info
        /// </summary>
        /// <param name="ndp">the root registry key 'NDP'</param>
        /// <returns></returns>
        private DotNetVersion getLatestNet3x(RegistryKey ndp)
        {
            DotNetVersion version = null;
            RegistryKey regkey = null;
            try
            {
                //a bit ugly and too much ... to retrieve more or less 1 to 4 hives
                var latest = getVersionKeyNames(ndp).Where(n => !n.StartsWith("v4")).SingleOrDefault();

                if(latest != null)
                {
                    regkey = ndp.OpenSubKey(latest);
                    version = fromRegistryNetFX40OrEarlier(regkey);
                }
            }
            finally { regkey?.Dispose(); }

            return version;
        }

        /// <summary>
        /// Get the highest installed netFX framework
        /// </summary>
        private DotNetVersion getLatestNetFxFromRegistry()
        {
            DotNetVersion version;
            RegistryKey rootkey = null;

            try
            {
                //the root key ...\NDP
                rootkey = getNetFXRegistryKey(noThrow:false);

                //seeking 4.0 and later
                version = getLatestNet4x(rootkey);

                //seeking 3.5 and earlier
                if (version == null) version = getLatestNet3x(rootkey);

                return version;
            }
            finally { rootkey?.Dispose(); }
        }

        /// <summary>
        /// Retrieve all installation records of the netFX framework
        /// </summary>
        /// <returns>special pieces of crap like client-profile (only) of 3.5 and earlier are not considered 
        /// (but client profile 4.0 are properly managed)</returns>
        private IEnumerable<DotNetVersion> getAllNetFxFromRegistry()
        {
            RegistryKey rootkey = null;

            try
            {
                //the root key ...\NDP
                rootkey = getNetFXRegistryKey(noThrow:false);

                //1) 4.x
                // get all >= 4.0 versions full-profile
                // get all >= 4.0 client-profile (a lot of duplicates, but the goal is to catch an exotic install of
                // the single 4.0 client-profile)
                // (duplicates are coped with further)
                //-------------------------

                foreach (var a4x in getAllNet4x(rootkey, DotNetProfile.fullFX)) yield return a4x;
                foreach (var a4x in getAllNet4x(rootkey, DotNetProfile.clientFX)) yield return a4x;

                //2) get the 3.5 and earlier versions ---------

                foreach (var a23x in getAllNet35AndEarlier(rootkey)) yield return a23x;
            }
            finally
            {
                rootkey?.Dispose();
            }
        }

        /// <summary>
        /// Get every netfx 4x install (of a given profile) from the registry info
        /// </summary>
        /// <param name="ndp"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private IEnumerable<DotNetVersion> getAllNet4x(RegistryKey ndp, DotNetProfile profile)
        {
            //get all >= 4.0 versions:
            //- get at first the highest 
            //- and next iterate over the leaf subkeys under v4\[Full,Client] ... such as *\1033 *\1036
            //- in each case, we try ver >= 4.5 (with the release info) and try ver = 4.0 as a fallback
            //-------------------------

            RegistryKey regkey = null;
            try
            {
                regkey = ndp.OpenSubKey($@"v4\{profile.AsString()}");
                if (regkey == null) yield break;

                DotNetVersion highver = null;

                //attempt to the latest fx versions >= 4.5
                highver = fromRegistryNetFX45OrLater(regkey);

                //fallback to 4.0 
                if (highver == null) highver = fromRegistryNetFX40OrEarlier(regkey);
                else yield return highver;

                //get (if any) the prior versions that have been upgraded in place
                //(notice that we should get as a duplicate the highest version)
                foreach (var kn in regkey.GetSubKeyNames())
                {
                    var subkey = regkey.OpenSubKey(kn);
                    if (subkey != null && subkey.SubKeyCount == 0) //expected leaf key
                    {
                        var version = fromRegistryNetFX45OrLater(subkey, highver.InstallPath);
                        if (version != null) yield return version;
                    }
                    subkey?.Dispose();
                }
            }
            finally { regkey?.Dispose(); }
        }

        #endregion

        #region CLR retrieval

        /// <summary>
        /// Retrieve all available CLR 
        /// </summary>
        private IEnumerable<DotNetVersion> getAllCLRFromRegistry()
        {
            RegistryKey ndp = null;
            try
            {
                //the root key ...\NDP
                ndp = getNetFXRegistryKey(noThrow:false);

                var version = tryGetCLR40(ndp);
                if (version != null) yield return version;

                version = tryGetCLR20(ndp);
                if (version != null) yield return version;
            }
            finally
            {
                ndp?.Dispose();
            }
        }

        /// <summary>
        /// Get the CLR4 version when installed. CLR4 is detected if: <br/>
        /// - SDK 4.5 and greater is installed <br/>
        /// - or SDK 4.0 full or client profile is installed
        /// </summary>
        /// <returns></returns>
        private DotNetVersion tryGetCLR40(RegistryKey ndp)
        {
            RegistryKey key = null;
            try
            {
                key = ndp.OpenSubKey(@"v4\Full");
                if (key == null) key = ndp.OpenSubKey(@"v4\Client");

                if (key != null) return fromRegistryCLR40(key);
                else return null;
            }
            finally
            {
                key?.Dispose();
            }
        }

        /// <summary>
        /// Get the CLR2 version when installed. CLR2 is detected if: <br/>
        /// - SDK 2.0 or 3.0 or 3.5 have been installed (full profile, i.e normal installation) <br/>
        /// - or the folder v2.0.50727 exists in the framework files root path (which is the last resort in case of
        ///  client runtime-only installation)
        /// </summary>
        /// <returns></returns>
        private DotNetVersion tryGetCLR20(RegistryKey ndp)
        {
            RegistryKey key = null;
            try
            {
                key = ndp.OpenSubKey(_clr20folderOrKey);

                if (key != null)
                {
                    return fromRegistryCLR20(key);
                }
                else
                {
                    //no NDP/v2
                    //last chance ... if we're facing a sucking platform where only one or more client-profile of
                    //3.5 and earlier would have been installed ... meaning **no valuable 'NDP' key info**
                    //The crapy key "DotNetClient" is undocumented and doesn't store any information about 2.0
                    //(moreover, if the 3.5 client has been installed, it has replaced the 2.0 record)
                    //
                    //We are obstinate ... we'll use the filesystem since the 2.0 stuff remains in the 'v2.0.50227'
                    //folder in the framework install root (even if 3.5 or later is installed)
                    //The fileversioin/productversion hold the same version as the (full profile) registry 
                    //We use the 'mscorlib.dll'

                    var path = getGlobalInstallRoot();
                    string mscorlib = Path.Combine(path, _clr20folderOrKey, "mscorlib.dll");
                    if (File.Exists(mscorlib))
                    {
                        var filever = FileVersionInfo.GetVersionInfo(mscorlib);
                        //we take productversion as fileversion may contain some additional info which would
                        //disrupt the version parsing
                        if (Version.TryParse(filever.ProductVersion, out Version libver))
                            return DotNetVersion.NetFxCLR(libver, Path.Combine(path, _clr20folderOrKey));
                    }

                    return null;
                }
            }
            finally
            {
                key?.Dispose();
            }
        }

        /// <summary>
        /// Get the highest available CLR based on the netFX registry entries
        /// </summary>
        /// <returns></returns>
        private DotNetVersion getCLRFromRegistry()
        {
            RegistryKey ndp = null;
            try
            {
                ndp = getNetFXRegistryKey(noThrow:false);

                var version = tryGetCLR40(ndp);
                if (version == null)
                {
                    //CLR 4 is not found
                    //CLR 3 does not exist ... so let's try CLR 2 (we are not quite interested by legacy CLR 2 but some
                    //old frameworks (3.5 and earlier) are unavoidably met on Win7. Indeed, the netFX 3.5 uses ...
                    //CLR-2 ... (and not 3)
                    //so go right away to the NDP/v2 

                    version = tryGetCLR20(ndp);
                }

                //if version is still null, we are not interested by CLR-1 and neolithic topics
                return version;
            }
            finally
            {
                ndp?.Dispose();
            }
        }

        /// <summary>
        /// Get the CLR 4 version from the actual netFX registry entries or file version
        /// </summary>
        /// <param name="v4full">expected the hive NDP/v4/full </param>
        /// <returns></returns>
        private DotNetVersion fromRegistryCLR40(RegistryKey v4full)
        {
            DotNetVersion ver;
            
            ver = fromRegistryNetFX45OrLater(v4full);
            if (ver != null && ver.WorldVersion.Major == 4 && ver.WorldVersion.Minor >= 6)
            {
                //by design, from netFX 4.6 the CLR is the same 4.0.30319.4200
                //note: the registry entries contains only the 4.0.30319 ... but the runtime api always returns 4200
                //as rev-number
                return DotNetVersion.NetFxCLR(new Version(4, 0, 30319, 4200), ver.InstallPath);
            }

            if (ver != null && ver.WorldVersion.Major == 4 && ver.WorldVersion.Minor <= 5)
            {
                //expected 4.0 and 4.5
                //the rev-number may be useful but it isn't retrievable from registry ... that sucks too
                //one more time, thus we need to hack.
                //the CLR version may be gathered from the file-version of the (e.g) clr.dll file

                string clrdll = Path.Combine(ver.InstallPath, "clr.dll");
                if (File.Exists(clrdll))
                {
                    var filever = FileVersionInfo.GetVersionInfo(clrdll);
                    if (Version.TryParse(filever.FileVersion, out Version clrver))
                        ver = DotNetVersion.NetFxCLR(clrver, ver.InstallPath);
                }
            }

            return ver;
        }

        /// <summary>
        /// Get the CLR 2 version from the actual netFX registry entries
        /// </summary>
        /// <param name="v2">expected the hive NDP/v2.0.50727</param>
        /// <returns></returns>
        private DotNetVersion fromRegistryCLR20(RegistryKey v2)
        {
            if(!(v2.GetValue("Version") is string versz) || string.IsNullOrEmpty(versz)) return null;

            //why the fuc..g hive for v2.0.x doesn't contain a key value "installPath" like the others ?
            string path = getGlobalInstallRoot();

            return DotNetVersion.NetFxCLR(new Version(versz), path ?? string.Empty);
        }


        #endregion

        #region runtime info

        /// <summary>
        /// Check that the targeted SDK requirement is satisfied by the current runtime environment
        /// </summary>
        /// <remarks>Obviously it would be useless to try to enforce the CLR version (since the executable wouldn't have
        /// been loaded if the CLR requirement wasn't met) <br/>
        /// POST: <see cref="RuntimeVersionEnforced"/> property is set
        /// </remarks>
        /// <returns></returns>
        /// @internal https://weblog.west-wind.com/posts/2018/Apr/12/Getting-the-NET-Core-Runtime-Version-in-a-Running-Application
        private bool checkRuntimeAccordingToTarget()
        {
            if (_runtimeVersionEnforced.HasValue) return _runtimeVersionEnforced.Value;

            DotNetVersion version;
            try
            {
                version = GetTargetFrameworkVersion();

                if(_sdkRuntimeInfo == null) _sdkRuntimeInfo = getSDKRuntimeInfo(fakeRtti: false);

                bool ok = _sdkRuntimeInfo.InternalVersion >= version.InternalVersion;
                _runtimeVersionEnforced = ok;
                return ok;
            }
            catch (NotSupportedException e)
            {
#if BCL_HAVE_GUI
                MessageBox.Show(e.Message);
#else
                Console.WriteLine(e.Message);
#endif
                return false;
            }
        }

        private static DotNetVersion getCLRRuntimeVersion(DotNetTarget expected)
        {
            //@todo when netappcore is implemented, 'any' should become netcore
            if (!(expected == DotNetTarget.netfx || expected == DotNetTarget.any)) 
                throw new NotImplementedException($"implement get-CLR-version for {expected}");

            //https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed#the-environmentversion-property
            //if the environment-version property returns 42000 whichever the runtime, it's rather unreliable ...
            //we don't thank to Microsoft net wanderings about strong versioning (It looks like the OS version naming ...)
            //
            //They say that we have to seek into the registry ... but such a Microsoft docs remarks is completely useless
            //since we are interested in runtime context NOT in installed inventory context !!!
            //
            //Runtime < fx46 may be consumed as a valuable info, but pay attention to the fact that we get ONLY a
            //CLR version anyway
            //from net.fx 4.6, the property is stuck with a fixed version 4.0.30319.42000 (thus not very useful)
            //
            //In the end ... No way ... we have to use .netX to cope with reliable versioning

            //that sucks
            //Version v = new Version(UnwrapRuntimeSystemVersion());

            return DotNetVersion.NetFxCLR(Environment.Version, RuntimeEnvironment.GetRuntimeDirectory());
        }

        /// <summary>
        /// </summary>
        /// <param name="fakeRtti">[true] do not try to use RuntimeInformation (which needs 4.7.1 and later)</param>
        /// <returns></returns>
        private DotNetVersion getSDKRuntimeInfo(bool fakeRtti)
        {
            if (fakeRtti) return runtimeInfoFromCurrentAssembly();

            //from core 3 and net >= 4.7.1, we should get a full SDK or FX version 
            //BUT how do we map the build number to the effective version ? (for example netfx48 is bound to 4.8.xxxx
            //but fx481 is also bound 4.8.xxxx (and the xxxx of 4.7.xxxx may be greater than the xxxx of 4.8.xxxx)
            //The whole version is actually the file version of mscorlib
            //so that's fucking hell ! that's pretty unusable
            //https://github.com/dotnet/runtime/issues/12124

            var ver = runtimeInfoFromFrameworkDescription();
            if (ver != null) return ver;

            //fallback (but is could be the simplest and why not the best answer ? though slower than parsing RTTI)
            return runtimeInfoFromCurrentAssembly();
        }

        /// <summary>
        /// Trick to wrap the property.get() call
        /// </summary>
        /// <returns></returns>
        /// @internal Since we want to be very conservative for this small application and we would like to allow
        /// startup on a plain old Win.7 with net.fx 4.0, we must pay attention to the Types that are unavailable
        /// before 4.7.1 (e.g RuntimeInformation, notice that the type has been added in a sub-minor version ...
        /// that's incredibily strong build policy from microsoft). 
        /// If a TypeLoadException is thrown into a method, it cannot be caught into she same method level (but only in 
        /// outer frame) since the type is loaded at first.
        /// Then this stupid method ....
        private static string runtimeInformation_getFrameworkDescription()
        {
            return RuntimeInformation.FrameworkDescription;
        }

        /// <summary>
        /// retrieve from <see cref="RuntimeInformation.FrameworkDescription"/> (that doesn't work prior to 4.7.1 since
        /// the type isn't implemented ... raising a TypeLoadException)
        /// </summary>
        /// <returns></returns>
        private static DotNetVersion runtimeInfoFromFrameworkDescription()
        {
            try
            {
                string versz = runtimeInformation_getFrameworkDescription();

                //we get something like ".NET framework 4.8.xxxxx", so parse it ...
                //yeah why .net is still so hacky and dirty
                //
                //NOTE: don't try to feed FrameworkName with the string value (it will fail as it relies on magic string values)
                //FrameworkDescription is ... description ... thus not a strong identifier as itself (but the last part
                //is)

                if (DotNetVersion.TryParseFramework(versz, out var _, out var version))
                    return DotNetVersion.NetFxSDK(version, RuntimeEnvironment.GetRuntimeDirectory());
                else
                    return null;
            }
            catch (TypeLoadException)
            {
                //symbol undefined, too old sdk version
                return null;
            }
        }

        /// <summary>
        /// Get the fileversion of the runtime class lib (mscorlib) <br/>
        /// For NetFX 4.7.1 and later, it's the same version as the <see cref="RuntimeInformation.FrameworkDescription"/>
        /// (which must be parsed however) by design. For 4.6.x where the RuntimeInformation symbol is undefined, the 
        /// retrieved version is still relevant. <br/>
        /// For versions earlier or equal to 4.5.x, where the fileversion contains the useless CLR version, the version 
        /// is deduced from the runtime path, and cross-referenced with the registry entries (in turn, it may be WRONG)
        /// </summary>
        /// <returns>a CLR or a SDK version depending on the version and some other things ... <br/>
        /// Prior to netfx4.6, fileversion of core assemblies hold the CLR version and NOT the SDK 
        /// </returns>
        /// @internal the hack to guess the version for netfx before 4.6 is dirty, but netfx api encourages for that. 
        /// Sorry, but no way, net.fx versioning was a piece of crap
        private DotNetVersion runtimeInfoFromCurrentAssembly()
        {
            var assembly = typeof(object).Assembly;
            var attrib = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

            var runpath = Path.GetDirectoryName(assembly.Location);

            var ver = new Version(attrib.Version);
            if(ver.Major <= 4 && ver.Minor <= 5) //for fx3.5 we get 2.0.xxxxx and for4.x we get 4.0.xxxx
            {
                //in this case, the fileversion is useless, e.g for netfx 4.5, we get 4(.0) -------

                //the nasty hack is going to consider that if the runtime directory is equal to the install path of the
                //highest detected net install, we could assume that we are talking about the same thing
                //then we just have to reuse the full version info from the registry
                //[if the path are different, we keep the CLR short version and get out of there]
                //TODO: To extend the trick, we should iterate over all the .net install to seek the runtime path

                if (DirPath.Equals(runpath, InstalledVersion.InstallPath))
                    return InstalledVersion;
                else
                    return DotNetVersion.NetFxCLR(ver, RuntimeEnvironment.GetRuntimeDirectory());
            }

            return DotNetVersion.NetFxSDK(ver, runpath);
        }

        #endregion


        /// <summary>
        /// Cache the detected versions
        /// </summary>
        /// <param name="versions"></param>
        /// <param name="clear"></param>
        /// <returns>what is actually inserted from <paramref name="versions"/></returns>
        private IEnumerable<DotNetVersion> insert(IEnumerable<DotNetVersion> versions, bool clear = false)
        {
            if (clear) _packages.Clear();

            foreach(var ver in versions)
            {
                var key = new DotNetVersionKey(ver.Target, ver.VersionType, ver.WorldVersion);

                if(_packages.TryGetValue(key, out DotNetVersion curVersion))
                {
                    if (curVersion.Profile == DotNetProfile.clientFX && ver.Profile == DotNetProfile.fullFX)
                    {
                        //throw the duplicates : replacing Client by Full profile
                        _packages[key] = ver;
                        yield return ver;
                    }
                }
                else
                {
                    _packages.Add(key, ver);
                    yield return ver;
                }
            }
        }

        #region registry shortcuts

        /// <summary>
        /// Get the sub key names ordered by version desc
        /// </summary>
        /// <param name="ndp"></param>
        /// <returns></returns>
        private IEnumerable<string> getVersionKeyNames(RegistryKey ndp)
        {
            return ndp.GetSubKeyNames().Where(n => n[0] == 'v' && char.IsDigit(n[1]))
                    .OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase);
        }


        /// <summary>
        /// open the NDP root key
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SystemException">the underlying call exceptions or <see cref="NotSupportedException"/>if
        /// key not found</exception>
        private RegistryKey getNetFXRegistryKey(bool noThrow)
        {
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                                 .OpenSubKey(_regKeyNetFXRegular);

            if (key == null && !noThrow) 
                throw new NotSupportedException("Unable to open registry for the NET Framework 'NDP' key");
            else 
                return key;
        }

        /// <summary>
        /// Open the crapy key (available on legacy installation with only a client-profile of 3.5 and earlier)
        /// </summary>
        /// <returns></returns>
        private RegistryKey getNetFXLegacyKey()
        {
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                                 .OpenSubKey(_regKeyNetFXLegacyRuntime);

            if (key == null)
                throw new NotSupportedException("Unable to open registry for the NET Framework legacy 'DotNetClient' key");
            else 
                return key;
        }

        /// <summary>
        /// Get the global install path root (not very reliable since undocumented ... and doesn't take into account the
        /// installation history ... we get the last written path)
        /// </summary>
        /// <returns></returns>
        private string getGlobalInstallRoot()
        {
            //using the global setting, should be safe for most cases
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(_regKeyNetFX);

            if (key != null) return key.GetValue("InstallRoot") as string;
            else return null;
        }

        #endregion

        /// <summary>
        /// the regular key for NetFX (4.5 and later, 4.0[full,client], 3.5 and earlier full)
        /// </summary>
        private static readonly string _regKeyNetFXRegular = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\";
        /// <summary>
        /// the crapy key for 3.5 and earlier client profile/runtime-only
        /// </summary>
        private static readonly string _regKeyNetFXLegacyRuntime = @"SOFTWARE\Microsoft\NET Framework Setup\DotNetClient\";

        private static readonly string _regKeyNetFX = @"SOFTWARE\Microsoft\.NETFramework\";

        private static readonly string _clr20folderOrKey = "v2.0.50727";

        private bool? _runtimeVersionEnforced;

        /// <summary>
        /// cache the current runtime context
        /// </summary>
        private DotNetVersion _sdkRuntimeInfo;

        /// <summary>
        /// The local installed packages
        /// </summary>
        private readonly Dictionary<DotNetVersionKey, DotNetVersion> _packages = new Dictionary<DotNetVersionKey, DotNetVersion>();
    }
}
