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
                var userIdClaim = claimsPrincipal.FindFirst(JabbRClaimsTypes.UserId);
                if (userIdClaim != null)
                {
                    return userIdClaim.Value;
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