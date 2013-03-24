using System;
namespace JabbR.Infrastructure
{
    public static class Constants
    {
        public static string UserTokenCookie = "jabbr.userToken";
        public static readonly Version JabbRVersion = typeof(Constants).Assembly.GetName().Version;
    }
}