using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
    }
}