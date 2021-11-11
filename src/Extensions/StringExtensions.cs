using System;
using System.Text.RegularExpressions;

namespace Our.Umbraco.Skipper.Extensions
{
    public static class StringExtensions
    {
        public static bool IsSkipperDuplicateOf(this string name, string otherName)
        {
            var regex = new Regex(@"^(.+)( \(\d+\))$");
            return regex.IsMatch(otherName) && regex.Replace(otherName, "$1").Equals(name);
        }
    }
}