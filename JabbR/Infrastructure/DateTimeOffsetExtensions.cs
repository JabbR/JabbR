using System;

namespace JabbR
{
    public static class DateTimeOffsetExtensions
    {
        /// <summary>
        /// Converts a DateTimeOffset into its represntation of milliseconds since 1/1/1970
        /// </summary>
        public static double ToJavaScriptMilliseconds(this DateTimeOffset dateTimeOffset)
        {
            var diff = dateTimeOffset - new DateTimeOffset(1970, 01, 01, 0, 0, 0, dateTimeOffset.Offset);
            return diff.TotalMilliseconds;
        }
    }
}