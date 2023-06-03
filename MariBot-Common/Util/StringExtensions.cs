using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Util
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsAny(this string source, string[] toCheck, StringComparison comp)
        {
            return toCheck.Any(s => source?.IndexOf(s, comp) >= 0);
        }
    }
}
