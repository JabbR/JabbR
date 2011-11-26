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
            return String.Join("", MD5.Create()
                         .ComputeHash(Encoding.Default.GetBytes(value))
                         .Select(b => b.ToString("x2")));
        }
    }
}