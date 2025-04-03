using System;

namespace AltCoD.BCL.Platform
{
    public static class OS
    {
        public static readonly bool IsWin = Environment.OSVersion.IsWindow();
    }

    public static class OperatingSystemExtensions
    {
        public static bool IsWindow(this OperatingSystem os) => os.Platform < PlatformID.Unix;
    }
}
