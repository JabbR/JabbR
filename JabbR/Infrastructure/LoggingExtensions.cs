using System;

namespace JabbR.Infrastructure
{
    public static class LoggingExtensions
    {
        public static void Log(this ILogger logger, Exception exception)
        {
            logger.Log(LogType.Error, "Exception:\r\n" + exception.ToString());
        }

        public static void LogError(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogType.Error, String.Format(message, args));
        }

        public static void Log(this ILogger logger, string message, params object[] args)
        {
            logger.Log(LogType.Message, String.Format(message, args));
        }
    }
}