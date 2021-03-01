using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Task2
{
    internal static class Extensions
    {
        internal static StringBuilder ReplaceAll(this StringBuilder input, IEnumerable<string> stringsToReplace, string replacer = " ")
        {
            return stringsToReplace.Aggregate(input, (current, s) => current.Replace(s, replacer));
        }
    }
}