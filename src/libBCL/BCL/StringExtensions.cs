#if !NET40_OR_GREATER
using System.Linq;
#endif

namespace AltCoD.BCL
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpaces(this string s)
        {
#if NET40_OR_GREATER
            return string.IsNullOrWhiteSpace(s);
#else
            if (string.IsNullOrEmpty(s)) return true;

            if (s.Length == 1) return char.IsWhiteSpace(s[0]);
            else return s.All(char.IsWhiteSpace);
#endif
        }
    }
}
