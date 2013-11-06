using System;
using System.IO;
using System.Text;

namespace JabbR.Infrastructure
{
    internal static class StringReaderExtensions
    {
        public static void SkipWhitespace(this StringReader reader)
        {
            ReadUntil(reader, c => !Char.IsWhiteSpace(c));
        }

        public static string ReadUntilWhitespace(this StringReader reader)
        {
            return ReadUntil(reader, c => Char.IsWhiteSpace(c));
        }

        public static string ReadToEnd(this StringReader reader)
        {
            return ReadUntil(reader, c => false);
        }

        public static void Expect(this StringReader reader, string expectedValue)
        {
            var buffer = new char[expectedValue.Length];
            int read = reader.ReadBlock(buffer, 0, buffer.Length);

            if (read != buffer.Length ||
                !String.Equals(new string(buffer), expectedValue))
            {
                throw new InvalidDataException(String.Format("Expected '{0}' but got '{1}'", expectedValue, new string(buffer)));
            }
        }

        public static string ReadUntil(this StringReader reader, Func<char, bool> predicate)
        {
            var sb = new StringBuilder();
            int ch = -1;
            do
            {
                ch = reader.Peek();
                if (ch == -1 || predicate((char)ch))
                {
                    break;
                }
                sb.Append((char)ch);
                reader.Read();
            }
            while (true);
            return sb.ToString();
        }
    }
}