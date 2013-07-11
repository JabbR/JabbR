using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JabbR.Infrastructure
{
    public static class StringExtensions
    {
        public static string ToMD5(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            return String.Join("", MD5.Create()
                         .ComputeHash(Encoding.Default.GetBytes(value))
                         .Select(b => b.ToString("x2")));
        }

        public static string ToSha256(this string value, string salt)
        {
            string saltedValue = ((salt ?? "") + value);

            return String.Join("", SHA256.Create()
                         .ComputeHash(Encoding.Default.GetBytes(saltedValue))
                         .Select(b => b.ToString("x2")));
        }

        public static string ToSlug(this string value)
        {
            string result = value;

            // Remove non-ASCII characters
            result = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(result));

            result = result.Trim();

            // Remove Invalid Characters
            result = Regex.Replace(result, @"[^A-z0-9\s-]", string.Empty);

            // Reduce spaces and convert to underscore
            result = Regex.Replace(result, @"\s+", "_");

            return result;
        }

        public static string ToFileNameSlug(this string value)
        {
            string result = value;

            // Trim Slashes
            result = result.TrimEnd('/', '\\', '.');

            // Remove Path (included by IE in Intranet Mode)
            result = result.Contains(@"/") ? result.Substring(result.LastIndexOf(@"/") + 1) : result;
            result = result.Contains(@"\") ? result.Substring(result.LastIndexOf(@"\") + 1) : result;

            if (result.Contains('.'))
            {
                // ToSlug Filename Component
                string fileNameSlug = result.Substring(0, result.LastIndexOf('.')).ToSlug();

                // ToSlug Extension Component
                string fileExtensionSlug = result.Substring(result.LastIndexOf('.') + 1).ToSlug();

                // Combine Filename Slug
                result = string.Concat(fileNameSlug, ".", fileExtensionSlug);
            }
            else
            {
                // No Extension
                result = result.ToSlug();
            }

            return result;
        }
    }
}