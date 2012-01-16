using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace JabbR.Infrastructure
{
    public static class RegexExtensions {
        public static string SingleOrDefault(this Regex regex, string value) {
            return regex.Match(value)
                .Groups
                .Cast<Group>()
                .Skip(1)
                .Select(g => g.Value).SingleOrDefault(v => !String.IsNullOrEmpty(v));
        }
        public static string FirstOrDefault(this Regex regex, string value) {
            return regex.Match(value)
                .Groups
                .Cast<Group>()
                .Skip(1)
                .Select(g => g.Value).FirstOrDefault(v => !String.IsNullOrEmpty(v));
        }
    }
}