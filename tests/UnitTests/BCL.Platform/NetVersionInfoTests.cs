using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NUnit.Framework;

namespace AltCoD.BCL.Platform.Tests
{
    [TestFixture]
    class NetVersionInfoTests
    {
        [Test]
        public void GetCurrentAssemblyVersion()
        {
            string name = AppContext.TargetFrameworkName;
            var framework = new FrameworkName(name);

            //1- get a world(only) version  ----------------

            var version = NETVersionInfo.GetTargetFrameworkVersion();

            Assert.That(version.Target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version.WorldVersion, Is.EqualTo(framework.Version));
            Assert.That(version.InternalVersion, Is.EqualTo(framework.Version));
            Assert.That(version.IsStrong, Is.False);

            //2- get a strong version sdk ---------------

            var netinfo = new NETVersionInfo(DotNetTarget.any, DotNetVersionType.SDK);

            version = netinfo.RuntimeVersion;

            Assert.That(version.Target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version.WorldVersion, Is.EqualTo(framework.Version));
            Assert.That(version.IsStrong, Is.True);

            DotNetVersion.TryParseFramework(RuntimeInformation.FrameworkDescription, out var target, out var ver);
            Assert.That(version.InternalVersion, Is.EqualTo(ver));

            //- get a pseudo-strong/weak version CLR ------------

            netinfo = new NETVersionInfo(DotNetTarget.any, DotNetVersionType.CLR);

            version = netinfo.RuntimeVersion;
            Assert.That(version.Target.ToIdentifier(), Is.EqualTo(framework.Identifier));
            Assert.That(version.VersionType, Is.EqualTo(DotNetVersionType.CLR));
            //CLR has a wrong versioning policy for netFX since the API is stuck to 4.0.30319.42000
            Assert.That(version.IsStrong, Is.False);
            Assert.That(version.Moniker, Is.Null); //does not make sense for CLR
            Assert.That(version.WorldVersion, Is.EqualTo(new Version(4, 0))); //CLR is major.0
            Assert.That(version.InternalVersion, Is.EqualTo(Environment.Version));
        }

        [Test]
        public void EnforceDotNetDependency()
        {
            var netinfo = new NETVersionInfo(DotNetTarget.any, DotNetVersionType.SDK);

            Assert.That(netinfo.RuntimeVersionEnforced, Is.True); //check itself ...

            var framework = new FrameworkName(AppContext.TargetFrameworkName);
            var lower_ver = new Version(framework.Version.Major, framework.Version.Minor - 1);
            var upper_ver = new Version(framework.Version.Major, framework.Version.Minor + 1);

            var current = netinfo.RuntimeVersion;
            if(current.Target == DotNetTarget.netfx)
            {
                DotNetVersion lversion = DotNetVersion.NetFxWorldSDK(lower_ver);
                Assert.That(netinfo.CouldExecute(lversion), Is.True);
                DotNetVersion uversion = DotNetVersion.NetFxWorldSDK(upper_ver);
                Assert.That(netinfo.CouldExecute(uversion), Is.False);
            }
            else if(current.Target == DotNetTarget.netcore)
            {
                Assert.Fail("to be implemented");
            }
        }

        [Test]
        public void DotNetInventory()
        {
            //browse for SDK -----------------

            var netinfo = new NETVersionInfo(DotNetTarget.netfx, DotNetVersionType.SDK);

            DotNetVersion sdk_ver = netinfo.InstalledVersion;
            Assert.That(sdk_ver .IsStrong, Is.True);
            Assert.That(sdk_ver.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(sdk_ver.VersionType, Is.EqualTo(DotNetVersionType.SDK));


            //browse for CLR -----------------

            netinfo = new NETVersionInfo(DotNetTarget.netfx, DotNetVersionType.CLR);
            DotNetVersion clr_ver = netinfo.InstalledVersion;

            //from 4.6 the CLR revNumber doesn't tell anything (stuck on 42000)
            if (clr_ver.InternalVersion < DotNetVersion._runtimeCLRnetFX46) Assert.That(clr_ver.IsStrong, Is.True);
            else Assert.That(clr_ver.IsStrong, Is.False);

            Assert.That(clr_ver.Target, Is.EqualTo(DotNetTarget.netfx));
            Assert.That(clr_ver.VersionType, Is.EqualTo(DotNetVersionType.CLR));

            if (sdk_ver.WorldVersion >= new Version(4, 0))
            {
                Assert.That(clr_ver.WorldVersion, Is.EqualTo(new Version(4, 0)));

                if (sdk_ver.WorldVersion >= new Version(4, 6))
                    Assert.That(clr_ver.InternalVersion, Is.EqualTo(new Version(4, 0, 30319, 42000)));
                else
                    Assert.That(clr_ver.InternalVersion, Is.GreaterThan(new Version(4, 0, 30319, 0)));
            }
            else if(sdk_ver.WorldVersion.Major < 4)
            {
                //expecting SDK 3.5 (earlier is out of scope)

                Assert.That(clr_ver.WorldVersion, Is.EqualTo(new Version(2, 0)));
                Assert.That(clr_ver.InternalVersion, Is.EqualTo(new Version(2, 0, 507277, 5420)));
            }
        }
    }
}
