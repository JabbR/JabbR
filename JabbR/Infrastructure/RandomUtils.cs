using System;
using System.Security.Cryptography;

namespace JabbR.Infrastructure
{
    internal static class RandomUtils
    {
        public static string NextInviteCode()
        {
            // Generate a new invite code
            string code;
            using (var crypto = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[4];
                crypto.GetBytes(data);
                int value = BitConverter.ToInt32(data, 0);
                value = Math.Abs(value) % 1000000;
                code = value.ToString("000000");
            }
            return code;
        }
    }
}