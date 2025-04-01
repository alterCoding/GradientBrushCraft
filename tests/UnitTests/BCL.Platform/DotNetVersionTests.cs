using System;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace AltCoD.BCL.Platform.Tests
{
    using BCL.Reflection;

    [TestFixture]
    class DotNetVersionTests
    {
        [Test]
        public void BuildFromCurrentFrameworkName()
        {
            string name = AppContext.TargetFrameworkName;
            var framework = new FrameworkName(name);

            var version = new DotNetVersion(framework);

            Assert.That(version.WorldVersion, Is.EqualTo(framework.Version));
            Assert.That(version.WorldVersion, Is.EqualTo(version.InternalVersion));
            Assert.That(version.Target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version.IsStrong, Is.False); //FrameworkName deals only with world version
        }

        [Test]
        public void ParseFrameworkString()
        {
            //1)
            //expected something compatible with FrameworkName (e.g ".NETFramework,Version=v4.8)
            string name = AppContext.TargetFrameworkName;
            var framework = new FrameworkName(name);

            Assert.That(DotNetVersion.TryParseFramework(name, out var target, out var version), Is.True);
            Assert.That(target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version, Is.EqualTo(framework.Version));

            //2)
            //expected something like RuntimeInformation.FrameworkDescription (e.g )
            name = RuntimeInformation.FrameworkDescription;

            Assert.That(DotNetVersion.TryParseFramework(name, out target, out version), Is.True);
            Assert.That(target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version.Major, Is.EqualTo(framework.Version.Major));
            Assert.That(version.Minor, Is.EqualTo(framework.Version.Minor));

            //We CANNOT ASSERT that ! BUT WE SHOULD
            //    Assert.That(version.Build, Is.EqualTo(framework.Version.Build));
            //the RuntimeInformation type returns a full fileversion (not a world version)
            //IT IS A TRUE BUG as we lost 4.8.(1) for example
            //BuildNumber is NOT the release number (which is documented ... and nothing but the registry entries
            //returns the release number)
        }

        [Test]
        public void BuildFromSimpleVersion()
        {
            var ver = new Version("4.8.1");
            var version = DotNetVersion.NetFxWorldSDK(ver);

            Assert.That(version.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.SDK));
            Assert.That(version.IsStrong, Is.False);
            Assert.That(version.Moniker, Is.EqualTo("net481"));
            Assert.That(version.InstallPath, Is.Null);
            Assert.That(version.InternalVersion, Is.EqualTo(ver));
            Assert.That(version.WorldVersion, Is.EqualTo(ver));
            Assert.That(version.VersionTag, Is.EqualTo("4.8.1"));
            Assert.That(version.ServicePack, Is.Null);
        }

        [Test]
        public void BuildFromFrameworkDescription()
        {
            var runver = new Version(4, 8, 3815, 0);
            var framework = $"{DotNetVersion._netFXDisplayName} {runver}"; 

            Assert.That(DotNetVersion.TryParseFramework(framework, out var target, out var ver), Is.True);

            if (target == DotNetTarget.netfx)
            {
                var version = DotNetVersion.NetFxSDK(ver, "/path/to/sdk/");

                Assert.That(version.Target, Is.EqualTo(DotNetTarget.netfx));
                Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.SDK));
                Assert.That(version.IsStrong, Is.True);
                Assert.That(version.Moniker, Is.EqualTo("net48"));
                Assert.That(version.InstallPath, Is.EqualTo("/path/to/sdk/"));
                Assert.That(version.InternalVersion, Is.EqualTo(runver));
                Assert.That(version.WorldVersion, Is.EqualTo(new Version(runver.Major, runver.Minor)));
                Assert.That(version.ServicePack, Is.Null);
            }
            else
            {
                Assert.Fail("to be implemented");
            }
        }

        [Test]
        public void BuildFromAssembly()
        {
            //get full version of current runtime
            var mscorlib = typeof(object).GetTypeInfo().Assembly;
            var file_attrib = mscorlib.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var framework = new FrameworkName(AppContext.TargetFrameworkName);

            if (framework.Identifier == DotNetVersion._netFXName)
            {
                var file_ver = new Version(file_attrib.Version);
                var runpath = Path.GetDirectoryName(mscorlib.Location);

                var version = DotNetVersion.NetFxSDK(file_ver, runpath);

                Assert.That(version.IsStrong, Is.True);
                Assert.That(version.InstallPath, Is.EqualTo(runpath));
                Assert.That(version.InternalVersion, Is.EqualTo(file_ver));
                Assert.That(version.WorldVersion, Is.EqualTo(framework.Version));
                //expected last netFx for dev usage ... so fucking net profiles don't exist anymore since a long time
                Assert.That(version.Profile, Is.EqualTo(DotNetProfile.without)); 
            }
            else
            {
                Assert.Fail("to be implemented");
            }
        }

        [Test]
        public void BuildFromRegistryLegacy()
        {
            var fx35 = new Version("3.5");
            string fx35sz = "3.5.30729.4926";
            var version = DotNetVersion.NetFxSDK(3, 5, fx35sz, sp:1, DotNetProfile.fullFX, "/path/to/sdk/");

            Assert.That(version.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.SDK));
            Assert.That(version.IsStrong, Is.True);
            Assert.That(version.Moniker, Is.EqualTo("net35"));
            Assert.That(version.InstallPath, Is.EqualTo("/path/to/sdk/"));
            Assert.That(version.InternalVersion, Is.EqualTo(new Version(fx35sz)));
            Assert.That(version.WorldVersion, Is.EqualTo(fx35));
            Assert.That(version.ServicePack, Is.EqualTo(1));
            Assert.That(version.Profile, Is.EqualTo(DotNetProfile.fullFX));
        }

        [Test]
        public void BuildFromRegistry()
        {
            var ver = new Version("4.8.1");
            //post 4.5 version seem to have patch numbers .. but where are those version documented ?
            //path-number are fully different from the field release-number
            string versz = "4.8.09999";
            uint release = 533320; //documented ok
            var version = DotNetVersion.NetFxSDK(ver, versz, release, "/path/to/sdk/");

            Assert.That(version.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.SDK));
            Assert.That(version.IsStrong, Is.True);
            Assert.That(version.Moniker, Is.EqualTo("net481"));
            Assert.That(version.InstallPath, Is.EqualTo("/path/to/sdk/"));
            Assert.That(version.InternalVersion, Is.EqualTo(new Version(versz)));
            Assert.That(version.WorldVersion, Is.EqualTo(ver));
            Assert.That(version.VersionTag, Is.EqualTo("4.8.1"));
            Assert.That(version.ServicePack, Is.Null);
            Assert.That(version.Profile, Is.EqualTo(DotNetProfile.without));
        }

        [Test]
        public void BuildCLRVersion()
        {
            var version = DotNetVersion.NetFxCLR();
            //that sucks
            //var clrver = new Version(NETVersionInfo.UnwrapRuntimeSystemVersion());
            var clrver = Environment.Version;

            Assert.That(version.InternalVersion, Is.EqualTo(clrver));
            Assert.That(version.WorldVersion, Is.EqualTo(new Version(clrver.Major, clrver.Minor)));
            Assert.That(version.Moniker, Is.Null);
            Assert.That(version.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.CLR));
        }
    }
}
