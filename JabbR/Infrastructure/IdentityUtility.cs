using System;

namespace JabbR.Infrastructure
{
    public static class IdentityUtility
    {
        public static string MakeLegacyIdentity(string providerName, string identity)
        {
            switch (providerName.ToLowerInvariant())
            {
                case "twitter":
                    return String.Format("http://twitter.com/account/profile?user_id={0}", identity);
                case "google":
                    return String.Format("https://www.google.com/profiles/{0}", identity);
                case "facebook":
                    return String.Format("http://www.facebook.com/profile.php?id={0}", identity);
                default:
                    break;
            }

            return null;
        }
    }
}