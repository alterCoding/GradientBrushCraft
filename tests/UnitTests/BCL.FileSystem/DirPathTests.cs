using System;
using NUnit.Framework;

namespace AltCoD.BCL.FileSystem.Tests
{
    [TestFixture]
    class DirPathTests
    {
        [Test]
        public void DirectoryPathEquals()
        {
            Assert.That(DirPath.Equals(string.Empty, null), Is.True);
            Assert.That(DirPath.Equals(string.Empty, "  "), Is.True);

            if(OS.IsWin)
            {
                Assert.That(DirPath.Equals(@"c:\dir", @"c:\dir\"), Is.True);
                Assert.That(DirPath.Equals(@"c:\dir", @"c:\dir\\"), Is.True);

                Assert.That(DirPath.Equals(@"C:\dir", @"c:\Dir"), Is.True);

                Assert.That(DirPath.Equals(@"c:\dir", @"c:\dir"), Is.True);
                Assert.That(DirPath.Equals(@"c:/dir", @"c:\dir"), Is.False); 
                Assert.That(DirPath.Equals(@"c:/dir", @"c:/dir"), Is.True); //side effect but ok 
                Assert.That(DirPath.Equals(@"c:/dir", @"c:\dir", tryAltDir:true), Is.True); 
                
                Assert.That(DirPath.Equals(@"c:/dir", @"c:/dir/"), Is.False);
                Assert.That(DirPath.Equals(@"c:/dir", @"c:/dir/", tryAltDir:true), Is.True);
                Assert.That(DirPath.Equals(@"c:/dir", @"c:\dir\", tryAltDir:true), Is.True);
            }

        }
    }
}
