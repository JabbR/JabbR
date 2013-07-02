using System;

namespace JabbR.Infrastructure
{
    internal static class HttpServerUtility
    {
        public static string UrlTokenDecode(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            int length = input.Length - 1; // we not consider the last character, because it's the padding number.
            if (length <= 0)
            {
                return String.Empty;
            }

            // Is the last character a number digit?
            int padding = input[length] - '0';
            if ((padding < 0) || (padding > 10))
            {
                return null;
            }

            input = input.Substring(0, length);
            input = input.Replace('-', '+').Replace('_', '/');
            input = input.PadRight(length + padding, '=');

            return input;
        }

        public static string UrlTokenEncode(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length < 1)
            {
                return String.Empty;
            }

            input = input.Replace('+', '-').Replace('/', '_').Replace("=", String.Empty);
            // calculate the padding number, the math is the nearst base 4 number.
            int paddingNumber = (4 - input.Length % 4) % 4;

            return input + paddingNumber.ToString();
        }
    }
}