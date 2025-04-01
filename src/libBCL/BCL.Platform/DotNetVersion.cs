using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AltCoD.BCL.Platform
{
    /// <summary>
    /// NET target types 
    /// </summary>
    /// <remarks>special targets as netstandard (or prior netcoreapp before the NetX) need to be implemented if needed</remarks>
    ///
    /// @internal dotnet internals don't seem to have implemented a strongly typed net target entity. We can see a
    /// FrameworkName class but it's more a wrapper w/o any logic, and relies on magic string resources
    public enum DotNetTarget
    {
        any,
        /// <summary>
        /// stands for the .Net Core and the .Net (w/o 'core'), i.e the .net "now" (all .net naming is sooo confusing)
        /// </summary>
        netcore,
        /// <summary>
        /// stands for the original .NET Framework (the plain old NetFX ...)
        /// </summary>
        netfx
    }

    /// <summary>
    /// DotNET version classifier
    /// </summary>
    public enum DotNetVersionType
    {
        any,
        /// <summary>
        /// stands for the "framework" : FX SDK, .NetCore SDK
        /// </summary>
        SDK,
        /// <summary>
        /// stands for the platform runtime (CLR/desktop for netfx, CoreCLR, Mono)
        /// </summary>
        CLR
    }

    /// <summary>
    /// The legacy split between the client and the full profiles of the NetFX framework (4.0 and earliers only)
    /// </summary>
    /// @internal do not change the value 0,1
    public enum DotNetProfile
    {
        /// <summary>
        /// undefined or irrelevant according to the usage of the <see cref="DotNetVersion"/> object
        /// </summary>
        any = 0,
        /// <summary>
        /// the profile distinction is irrelevant for the target version (e.g netcore or netfx >= 4.5)
        /// </summary>
        without = 1,
        fullFX,
        clientFX
    }

    public static class DotNetEnumExtensions
    {
        /// <summary>
        /// Get a string strong identifier. This is the identifier to be used with <see cref="FrameworkName"/> or as a
        /// value for various sdk build/versioning platform elements
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string ToIdentifier(this DotNetTarget target)
        {
            if (target == DotNetTarget.netfx) return DotNetVersion._netFXName;
            else if (target == DotNetTarget.netcore) return DotNetVersion._netcoreName;
            else throw new NotImplementedException($"The well-known identifier of {target} is undefined");
        }

        public static string AsString(this DotNetProfile profile)
        {
            if (profile == DotNetProfile.fullFX) return "Full";
            else if (profile == DotNetProfile.clientFX) return "Client";
            else return string.Empty; //not interesting to display something
        }
    }


    /// <summary>
    /// A NET version info wrapper <br/>
    /// - support NetFX 3.5 and later  <br/>
    /// - 1th Net core and Net5 and later : to be implemented
    /// </summary>
    /// <remarks>
    /// <para>Motivation: <br/>
    /// A simple version instance is not satisfying as version semantic depends on source context, major version (and
    /// event minor versions), and the versioning scheme doesn't allow us to get a truly understandable version (as netfx
    /// 4.5.2) from a installed framework (e.g 4.5.51209)<br/>
    /// That's the reason why we've introduced a so-called "world-version" to reflect the understandable version number
    /// in addition of the <see cref="InternalVersion"/> property
    /// </para>
    /// Far from perfect ... since NetFX is especially inconsistent about its versioning policy and its deployment.
    /// The next .Net is widely better (but sadly only the CLR follows a strict semantic versioning ... so after the
    /// win32 dll hell, let's dive into the .NET version hell)
    /// </remarks>
    /// @internal Only framework is fully implemented hereafter. Besides, special targets as initial netcoreapp or netstandard 
    /// will probably need implementation twists.
    /// For future NetX implementations, refer to https://learn.microsoft.com/en-us/dotnet/core/versions/. It's stronger
    /// than NetFX versioning, though still confusing between the prior coreapp/standard and the later net x.y
    /// https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies
    /// 
    public class DotNetVersion
    {
        public DotNetVersion(FrameworkName framework)
        {
            Target = string.Compare(framework.Identifier, _netFXName, ignoreCase:true) == 0 ? DotNetTarget.netfx 
                : string.Compare(framework.Identifier, _netcoreName, ignoreCase:true) == 0 ? DotNetTarget.netcore
                : throw new NotSupportedException(
                    $"{nameof(DotNetVersion)} construct failure due to unknown identifier from FrameworkName {framework.Identifier}");

            VersionType = DotNetVersionType.SDK;

            Version ver = framework.Version;
            InternalVersion = ver;
            WorldVersion = ver;
            VersionSZ = ver.ToString();

            Moniker = moniker(ver, Target);
            VersionTag = tag(ver, Target);

            IsStrong = false;

            //lack of profile means "full"
            //https://blog.stephencleary.com/2012/05/framework-profiles-in-net.html
            if (ver.Major >= 4 && ver.Minor > 0) Profile = DotNetProfile.without;
            else if ("Client".Equals(framework.Profile, StringComparison.OrdinalIgnoreCase)) Profile = DotNetProfile.clientFX;
            else Profile = DotNetProfile.fullFX;
        }

        private DotNetVersion(DotNetTarget target, DotNetVersionType type, Version ver, Version worldVer, string verSz, DotNetProfile pfl)
        {
            Target = target;
            VersionType = type;
            Profile = pfl;

            InternalVersion = ver;
            WorldVersion = worldVer;
            VersionSZ = verSz;

            //does not really make sense to affect a TFM for CLR as the targets are framework related to (not CLR)
            Moniker = type == DotNetVersionType.CLR ? null : moniker(worldVer, target);
            VersionTag = tag(worldVer, target);
        }

        /// <summary>
        /// This strong construct suits for netFX 4.5 and laters, especially when the data come from registry info
        /// </summary>
        /// <param name="worldVer"></param>
        /// <param name="verSz"></param>
        /// <param name="release"></param>
        /// <returns></returns>
        /// <remarks>The required strong release number seems to be available only in registry entries. The fileversion
        /// or productinfo attributes offer only a useless build et rev numbers</remarks>
        internal static DotNetVersion NetFxSDK(Version worldVer, string verSz, uint release, string path)
        {
            var version = new DotNetVersion(
                DotNetTarget.netfx, DotNetVersionType.SDK,
                new Version(verSz), worldVer, verSz, DotNetProfile.without)
                {
                    Release = (int)release,
                    IsStrong = true,
                    InstallPath = path
                };
   
            return version;
        }
        /// <summary>
        /// This strong construct suits for netFX lower than 4.5 especially when the data come from registry info
        /// </summary>
        /// <param name="worldMajor">with <paramref name="worldMinor"/> expected to be 3.0, 3.5, 4.0 (earliers are out of scope)</param>
        /// <param name="worldMinor"></param>
        /// <param name="verSz"></param>
        /// <param name="sp">service pack. -1 should be set to express lack of SP (but should be reserved for version that 
        /// are not (or won't be) involved with service pack as 4.0 and later ... i.e initial releases of 2x 3x should 
        /// be SP:0)</param>
        /// <returns></returns>
        /// <remarks>
        /// A strong release number is provided only for version from 4.5 and laters. The drawback related to may be 
        /// mitigated as netfx 2x, 3x, 40 don't admit patch numbers (we got only by design 2.0, 3.0, 3.5 and 4.0)<br/>
        /// Hence the tuple major.minor is the sole expected components for the world version
        /// </remarks>
        internal static DotNetVersion NetFxSDK(int worldMajor, int worldMinor, string verSz, int sp, DotNetProfile pfl, string path)
        {
            if (worldMajor == 4 && worldMinor >= 5) 
                throw new InvalidOperationException(
                    $"netFX >= 4.5 expect a release number (got netFX:{worldMajor}.{worldMinor})");
            if(worldMajor < 4 && sp < 0)
                throw new InvalidOperationException(
                    $"netFX 2x or 3x expect a SP number or 0 for initial release (got netFX:{worldMajor}.{worldMinor}) sp:{sp}");
            if(worldMajor == 4 && worldMinor <= 8 && sp != -1)
                throw new InvalidOperationException(
                    $"netFX 4x don't a SP number (got netFX:{worldMajor}.{worldMinor}) sp:{sp}");

            var ver = new Version(verSz);
            var worldVer = new Version(worldMajor, worldMinor);
            var version = new DotNetVersion(DotNetTarget.netfx, DotNetVersionType.SDK, ver, worldVer, verSz, pfl)
            {
                IsStrong = ver.Build > 0, //could no build number occur ?
                InstallPath = path,
                ServicePack = sp == -1 ? (int?)null : sp
            };

            return version;
        }
        /// <summary>
        /// This strong construct suits for information data coming from a whole file or product version (e.g a parsed 
        /// RuntimeInformation.FrameworkDescription or better read from an assembly fileversion attributes)
        /// </summary>
        /// <param name="ver">the version isn't expected to be a world version</param>
        /// <returns></returns>
        /// <remarks>Pay attention to the fact that <see cref="WorldVersion"/> will be truncated (the 3th part isn't
        /// filled out) as dotnet doesn't give us any way to get it from its build number</remarks>
        ///
        /// @internal The framework seems to have been created to harass programmers ... netfx versioning inconsistencies
        /// seem unbelievable.
        /// Please tell me that I'm wrong but:
        /// - when we get FrameworkDescription: buildnumber is useless to retrieve a true world understandable version
        ///   (we get major.minor, but 4.8 and 4.8.1 aren't distinguishable)
        /// - when we get an assembly, we get only the useless CLR number in the fileversion for old fx below 4.6
        ///    and for later versions, we are trapped into the same issue as framework description
        /// - we should not ignore the patch-number (build-number) as breaking changes have been introduced outside 
        ///   major/minor releases ... the best proof is precisely the RuntimeInformation type introduced in 4.7.1.
        ///   BUG: we cannot enforce the reliability of the <see cref="WorldVersion"/> for the patch-number
        internal static DotNetVersion NetFxSDK(Version ver, string path)
        {
            if (ver.Build < 0) 
                throw new InvalidOperationException(
                    $"A strong NetFX SDK version should include a build/patch number (got {ver})");

            var world = new Version(ver.Major, ver.Minor);
            DotNetProfile profile = DotNetProfile.any;
            if (world.Major >= 4 && world.Minor >= 5) profile = DotNetProfile.without;

            var version = new DotNetVersion(DotNetTarget.netfx, DotNetVersionType.SDK, ver, world, ver.ToString(), profile)
            {
                IsStrong = true, //weak 'true' since patch-number may be crap
                InstallPath = path
            };

            return version;
        }

        /// <summary>
        /// This weak construct suits when we wants to refer to a well-known .NET Framework version w/o referring to
        /// a strong fileversion or release (which is not very useful for all days applications)
        /// </summary>
        /// <param name="ver">the limited version number</param>
        /// <returns></returns>
        internal static DotNetVersion NetFxWorldSDK(Version ver)
        {
            return new DotNetVersion(DotNetTarget.netfx, DotNetVersionType.SDK, ver, ver, ver.ToString(), DotNetProfile.any);
        }

        /// <summary>
        /// Get the current .netFX CLR version
        /// </summary>
        /// <remarks>
        /// Referring to https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.runtimeenvironment.getsystemversion?view=net-9.0 
        /// We could use <see cref="RuntimeEnvironment.GetSystemVersion()"/> instead of <see cref="Environment.Version"/>
        /// but in fact both suck because versioning and naming rules are not consistent across the .net history
        /// </remarks>
        /// <returns></returns>
        internal static DotNetVersion NetFxCLR()
        {
            //that sucks more than Environment.Version
            //Version ver = new Version(NETVersionInfo.UnwrapRuntimeSystemVersion());
            return NetFxCLR(Environment.Version, RuntimeEnvironment.GetRuntimeDirectory());
        }

        internal static DotNetVersion NetFxCLR(Version ver, string path)
        {
            //for user informational purpose, the CLR build number is useless (we mostly talk about CLR 1/2/4 ...)
            //the rev number is useless for CLR as API and fx-infrastructure are unreliable

            var world = new Version(ver.Major, ver.Minor);
            var version = new DotNetVersion(DotNetTarget.netfx, DotNetVersionType.CLR, ver, world, ver.ToString(), DotNetProfile.any);

            if (ver < _runtimeCLRnetFX46) version.IsStrong = true;
            else version.IsStrong = false; //no distinction anymore (stuck on 42000)

            version.InstallPath = path;

            return version;
        }

        /// <summary>
        /// Try to parse a framework identification string (expected to follow the pseudo strong format of <see cref="FrameworkName"/>
        /// or the fully weak format of <see cref="RuntimeInformation.FrameworkDescription"/>
        /// </summary>
        /// <param name="framework"></param>
        /// <param name="target"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static bool TryParseFramework(string framework, out DotNetTarget target, out Version version)
        {
            version = null;
            target = DotNetTarget.any;

            string id;
            framework = framework.Trim();
            if (framework.StartsWith(_netFXName)) { id = _netFXName; target = DotNetTarget.netfx; }
            else if(framework.StartsWith(_netFXDisplayName)) { id = _netFXDisplayName; target = DotNetTarget.netfx; }
            else if (framework.StartsWith(_netcoreName)) { id = _netcoreName; target = DotNetTarget.netcore; }
            else if (framework.StartsWith(_netcoreDisplayName)) { id = _netcoreDisplayName; target = DotNetTarget.netcore; }
            else return false;

            var versionsz = new string(framework.Skip(id.Length).SkipWhile(c => !char.IsDigit(c)).ToArray());
            return Version.TryParse(versionsz, out version);
        }

        public DotNetVersionType VersionType { get; }

        public bool IsStrong { get; private set; }

        /// <summary>
        /// Get the Target Framework Moniker (TFM) according to https://learn.microsoft.com/en-us/dotnet/standard/frameworks 
        /// (partial implementation)
        /// </summary>
        /// <remarks>it's NULL for a CLR version type since TFM are related to framework</remarks>
        public string Moniker { get; }

        /// <summary>
        /// The path is filled out if the version object comes from a system detection or an assembly attribute. It's
        /// undefined (null) if the instance refers to a version number as itself
        /// </summary>
        public string InstallPath { get; private set; } 

        /// <summary>
        /// An internals release number <br/>
        /// Net Framework: the Release REG_DWORD in the NDP registry key) as 0x80ea8 (528040)
        /// </summary>
        /// <remarks>since it's meaningful only for SDK NETFX-4x (else irrelevant NULL), it's better to not rely on this
        /// data for any thing but informational purpose (or internals). Besides, this number seems to be available in
        /// registy entries
        /// </remarks>
        internal int? Release { get; private set; }

        /// <summary>
        /// A plain old "good" service pack number <br/>
        /// Net Framework: the SP REG_DWOrD in the NDP registy key (only for NetfX AND only for earlier than 4.5) <br/>
        ///   note:value 0 means original release for versions than may be affected by service packs (whereas NULL value
        ///   means lack of service pack because those versions cannot be affected by design by service packs)
        /// </summary>
        /// <remarks>it affects only a few old frameworks and we should try to not have to cope with those esoteric
        /// variations ... to stay on the safe side ... or become crazy</remarks>
        public int? ServicePack { get; private set; } 

        public DotNetTarget Target { get; }

        /// <summary>
        /// The full "hard" version number <br/>
        /// Net Framework: <br/>
        /// - [SDK] reflects the Version REG_SZ in the NDP registry key as 4.8.03752. Be aware of the lack of full 
        /// transitivity with the <see cref="WorldVersion"/> property <br/>
        /// - [CLR] reflects the <see cref="Environment.Version"/> property
        /// </summary>
        /// <remarks>Do NOT confuse with the <see cref="WorldVersion"/></remarks>
        public Version InternalVersion { get; }

        /// <summary>
        /// The raw original version string <br/>
        /// Net Framework : the Version REG_SZ in the NDP registry key or part of the <see cref="RuntimeInformation.FrameworkDescription"/>
        /// as "4.8.03752"
        /// </summary>
        public string VersionSZ { get; }

        /// <summary>
        /// Get the simple version major.minor[.xxxxx] as the sole version we would like to cope with for tell about
        /// features. <br/>
        /// We would like to say {patch} instead of {xxxxxx} but .net versioning is not semantic. So "get out of there"
        /// we can't enforce a strong versioning anywhere<br/>
        /// Must reflect the <see cref="VersionTag"/>
        /// </summary>
        /// <remarks>if provided, the [xxxx] isn't the build number of <see cref="InternalVersion"/>, depending on
        /// the way by which the version has been retrieved. The NetFX team has made our life very complicated</remarks>
        public Version WorldVersion { get; }

        /// <summary>
        /// The shortened version as "4.8" (actually suits for most cases)
        /// </summary>
        /// <remarks>excluding service pack info</remarks>
        public string VersionTag { get; }

        /// <summary>
        /// Legacy distinction (NetFX 4.0 and earlier only)
        /// </summary>
        public DotNetProfile Profile { get; }

        /// <summary>
        /// Get a label as ".NET Framework 4.8" or ".NET 5"
        /// </summary>
        /// <param name="world">[true] when 4.8 should be preferred over 4.8.xxxxx </param>
        /// <remarks>not strongly named</remarks>
        /// <returns></returns>
        public string Description(bool world = true)
        {
            return $"{targetSZ(Target, display: true)} {(world ? VersionTag : VersionSZ)}";
        }

        public string Dump()
        {
            return string.Concat(
               ToString(), Environment.NewLine,
               InstallPath);
        }

        public override string ToString()
        {
            string detail = $"{InternalVersion}{(Profile > DotNetProfile.without ? $" {Profile.AsString()}" : string.Empty)}";

            if (ServicePack.HasValue)
                return $"{environmentLabel} {WorldVersion} SP:{ServicePack} ({detail})";
            else
                return $"{environmentLabel} {WorldVersion} ({detail})";
        }

        private static string targetSZ(DotNetTarget target, bool display = false)
        {
            return target == DotNetTarget.netfx ? (display ? _netFXDisplayName : _netFXName)
                : target == DotNetTarget.netcore ? (display ? _netcoreDisplayName : _netcoreName)
                : /**fallback, but will fail miserably somewhere later on*/ target.ToString();
        }

        private string environmentLabel => $"({VersionType}) {targetSZ(Target, display:true)}";

        private static string moniker(Version ver, DotNetTarget target)
        {
            if (target == DotNetTarget.netfx)
                return $"net{ver.Major}{ver.Minor}{(ver.Build > 0 ? ver.Build.ToString() : string.Empty)}";
            else if (target == DotNetTarget.netcore)
                return $"net{ver.Major}.0";
            else
                throw new NotImplementedException();
        }
        private static string tag(Version ver, DotNetTarget target)
        {
            if (target == DotNetTarget.netfx)
                return $"{ver.Major}.{ver.Minor}{(ver.Build > 0 ? $".{ver.Build}" : string.Empty)}";
            else if (target == DotNetTarget.netcore)
                return $"{ver.Major}";
            else
                throw new NotImplementedException();
        }

        public static readonly string _netcoreDisplayName = ".NET";
        public static readonly string _netFXDisplayName = ".NET Framework";

        /// <summary>
        /// the magic string resource
        /// </summary>
        public static readonly string _netcoreName = ".NETCoreApp";
        /// <summary>
        /// the magic string resource 
        /// </summary>
        public static readonly string _netFXName = ".NETFramework";
             
        /// <summary>
        /// the last CLR Net.FX version
        /// </summary>
        public static readonly Version _runtimeCLRnetFX46 = new Version(4, 0, 30319, 42000);
    }

    /// <summary>
    /// A dot net world version wrapper
    /// </summary>
    internal class DotNetVersionKey : IEquatable<DotNetVersionKey>
    {
        public DotNetVersionKey(DotNetTarget target, DotNetVersionType type, Version version)
        {
            Value = version;
            Target = target;
            KeyType = type;
        }
        /// <summary>
        /// The key value (expected to be a world version as only one installation is expected per .net version)
        /// </summary>
        public Version Value { get; }
        public DotNetTarget Target { get; }
        public DotNetVersionType KeyType { get; }

        public override string ToString() => $"{KeyType} {Value}";

        /** @internal @todo poor implementation, should use HashCode.Combine */
        public override int GetHashCode()
        {
            //old way, since targeting old frameworks
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Value.GetHashCode();
                hash = hash * 23 + Target.GetHashCode();
                hash = hash * 23 + KeyType.GetHashCode();
                return hash;
            }
        }

        public bool Equals(DotNetVersionKey o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o is null) return false;

            return Value.Equals(o.Value) && Target == o.Target && KeyType == o.KeyType;
        }

        public override bool Equals(object obj) => obj is DotNetVersionKey o && Equals(o);
        public static bool operator==(DotNetVersionKey k1, DotNetVersionKey k2) => k1 != null ? k1.Equals(k2) : k2 == null;
        public static bool operator !=(DotNetVersionKey k1, DotNetVersionKey k2) => !(k1 == k2);
    }
}
