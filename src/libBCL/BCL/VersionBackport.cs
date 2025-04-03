using System;

namespace AltCoD.BCL
{
    public static class VersionBackport
    {
#if !NET40_OR_GREATER
        public static bool TryParse(string value, out Version version)
        {
            try
            {
                version = new Version(value);
                return true;
            }
            catch(Exception)
            {
                version = null;
                return false; 
            }
        }
#endif

        /// <summary>
        /// try parsing Version object from <paramref name="value"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>NULL if failed</returns>
        public static Version FromString(string value)
        {
#if NET40_OR_GREATER
            Version.TryParse(value, out Version version);
#else
            TryParse(value, out Version version);
#endif
            return version;
        }

        /// <summary>
        /// This loose equivalence policy compares each version component but lack of component is considered equivalent
        /// to a zero value<br/>
        /// Motivation is to get v3.5 equal to v3.5.0.0, which is valuable for a lot context comparisons
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool WeakEquivalence(Version v1, Version v2)
        {
            if (ReferenceEquals(v1, v2)) return true;
            if (v1 == null || v2 == null) return false;

            if (v1.Major != v2.Major) return false;
            if (v1.Minor != v2.Minor) return false;
            if (v1.Build != v2.Build && (v1.Build + v2.Build != -1)) return false;
            if (v1.Revision != v2.Revision && (v1.Revision + v2.Revision != -1)) return false;

            return true;
        }
    }
}
