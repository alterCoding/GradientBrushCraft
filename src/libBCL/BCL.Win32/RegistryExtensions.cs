using System;
using Microsoft.Win32;

namespace AltCoD.BCL.Win32
{
    public static class RegistryExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="name">key name or key path</param>
        /// <returns></returns>
        public static RegistryKey OpenKey(this RegistryHive baseKey, string name)
        {
#if !NET40_OR_GREATER
            //the RegistryKey.OpenBaseKey() method has been added from netfx 4.0

            RegistryKey key = null;

            if (baseKey == RegistryHive.LocalMachine) key = Registry.LocalMachine;
            else if (baseKey == RegistryHive.CurrentUser) key = Registry.CurrentUser;
            else if (baseKey == RegistryHive.CurrentConfig) key = Registry.CurrentConfig;
            else if (baseKey == RegistryHive.ClassesRoot) key = Registry.ClassesRoot;
            else if (baseKey == RegistryHive.Users) key = Registry.Users;

            if (key == null) return null;

            return key.OpenSubKey(name);
#else
            return RegistryKey.OpenBaseKey(baseKey, RegistryView.Default).OpenSubKey(name);
#endif
        }
    }

#if !NET40_OR_GREATER
    internal static class RegistryKeyExtensions
    {
        public static void Dispose(this RegistryKey key)
        {
            (key as IDisposable)?.Dispose();
        }
    }
#endif
}
