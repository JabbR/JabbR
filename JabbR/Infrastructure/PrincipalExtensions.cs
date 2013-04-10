using System;
using System.Security.Claims;
using System.Security.Principal;

namespace JabbR.Infrastructure
{
    public static class PrincipalExtensions
    {
        public static bool IsAuthenticated(this IPrincipal principal)
        {
            string userId = GetUserId(principal);

            return !String.IsNullOrEmpty(userId);
        }

        public static string GetUserId(this IPrincipal principal)
        {
            if (principal == null)
            {
                return null;
            }

            var claimsPrincipal = principal as ClaimsPrincipal;

            if (claimsPrincipal != null)
            {
                foreach (var identity in claimsPrincipal.Identities)
                {
                    if (identity.AuthenticationType == Constants.JabbRAuthType)
                    {
                        Claim claim = identity.FindFirst(ClaimTypes.NameIdentifier);

                        if (claim != null)
                        {
                            return claim.Value;
                        }
                    }
                }
            }

            return null;
        }

        public static string GetClaimValue(this ClaimsPrincipal principal, string type)
        {
            Claim claim = principal.FindFirst(type);

            return claim != null ? claim.Value : null;
        }
    }
}