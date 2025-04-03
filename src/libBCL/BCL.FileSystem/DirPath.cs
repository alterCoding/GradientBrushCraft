using System;
using System.IO;
using System.Linq;

namespace AltCoD.BCL.FileSystem
{
    using BCL.Platform;

    public static class DirPath
    {
        /// <summary>
        /// Directory path equivalence <br/>
        /// - case insensitive [Windoze] <br/>
        /// - cope with alternative delimiter if required <br/>
        /// - trailing delimiter insensitive {0,2}<br/>
        /// - cope with null/empty/whitespaces entries
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="tryAltDir">cope with alternative delimiter <br/>
        /// [true] paths will be normalized before comparison <br/>
        /// [false] any alternative delimiter will be processed as a regular character (thus false negative might arise)
        /// </param>
        /// <remarks>
        /// if presence of alternative delimiter is unknown, it's wise to use <paramref name="tryAltDir"/> set to true
        /// </remarks>
        /// <returns></returns>
        /// @REFACTOR to be reviewed with .netcore and span 
        public static bool Equals(string p1, string p2, bool tryAltDir = false)
        {
            if (ReferenceEquals(p1, p2)) return true;
            if (p1.IsNullOrWhiteSpaces()) return p2.IsNullOrWhiteSpaces();
            if (p2.IsNullOrWhiteSpaces()) return false;

            if (tryAltDir && !OS.IsWin) tryAltDir = false; //useless

            char sep = Path.DirectorySeparatorChar;

            //dir separator normalization --------
            if (tryAltDir)
            {
                char sep1 = (char)0;
                char sep2 = (char)0;

                if (p1.IndexOf(Path.DirectorySeparatorChar) != -1)
                    sep1 = Path.DirectorySeparatorChar; //most case
                else if (tryAltDir && p1.IndexOf(Path.AltDirectorySeparatorChar) != -1)
                    sep1 = Path.AltDirectorySeparatorChar; //alt case

                if (p2.IndexOf(Path.DirectorySeparatorChar) != -1)
                    sep2 = Path.DirectorySeparatorChar; //most case
                else if (tryAltDir && p1.IndexOf(Path.AltDirectorySeparatorChar) != -1)
                    sep2 = Path.AltDirectorySeparatorChar; //alt case

                if (sep1 != 0 || sep2 != 0)
                {
                    if (sep1 != sep2)
                    {
                        //different path level -> path will be unavoidably different
                        if (!tryAltDir) return false;

                        //normalize separators
                        if (sep1 == Path.DirectorySeparatorChar)
                            p2 = p2.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        else
                            p1 = p1.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    }
                    else
                    {
                        sep = sep1;
                    }
                }
            }

            int last1 = p1.Length - 1;
            if (p1[last1] == sep) last1--;
            if (p1[last1] == sep) last1--; //more often than expected
            int last2 = p2.Length - 1;
            if (p2[last2] == sep) last2--;
            if (p2[last2] == sep) last2--;

            if (last1 != last2) return false;

            if (last1 == p1.Length - 1 && last2 == p2.Length) return p1.Equals(p2, _strcmp);
            else return string.Compare(p1, 0, p2, 0, last1, _icase) == 0;
        }

        private static readonly bool _icase = OS.IsWin;
        private static readonly StringComparison _strcmp = _icase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public static class PathName
    {
        public static string Append(string p1, string p2)
        {
#if NET40_OR_GREATER
            return Path.Combine(p1, p2);
#else
            if (p1.EndsWith(_separator)) return string.Concat(p1, p2);
            else return string.Concat(p1, _separator, p2);
#endif
        }
        public static string Append(string p1, string p2, string p3)
        {
#if NET40_OR_GREATER
            return Path.Combine(p1, p2, p3);
#else
            if (p1.EndsWith(_separator))
            {
                if (p2.EndsWith(_separator)) return string.Concat(p1, p2, p3);
                else return string.Concat(p1, p2, _separator, p3);
            }
            else
            {
                if (p2.EndsWith(_separator)) return string.Concat(p1, _separator, p2, p3);
                else return string.Concat(p1, _separator, p2, _separator, p3);
            }
#endif
        }

#if !NET40_OR_GREATER
        public static readonly string _separator = Path.DirectorySeparatorChar.ToString();
#endif
    }
}
