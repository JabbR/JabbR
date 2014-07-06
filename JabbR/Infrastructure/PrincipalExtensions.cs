using System;
using System.Security.Claims;
using System.Security.Principal;

namespace JabbR.Infrastructure
{
    using System.Text.RegularExpressions;

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
                        Claim idClaim = identity.FindFirst(JabbRClaimTypes.Identifier);

                        if (idClaim != null)
                        {
                            return idClaim.Value;
                        }
                    }
                }
            }

            return null;
        }

        public static bool HasClaim(this ClaimsPrincipal principal, string type)
        {
            return !String.IsNullOrEmpty(principal.GetClaimValue(type));
        }

        public static string GetClaimValue(this ClaimsPrincipal principal, string type)
        {
            Claim claim = principal.FindFirst(type);

            return claim != null ? claim.Value : null;
        }

        public static string GetUsername(this ClaimsPrincipal principal)
        {
            var username = principal.GetClaimValue(ClaimTypes.Name);
            if (username == null)
            {
                return null;
            }

            username = Regex.Replace(username, @"[^\w-_.]", "", RegexOptions.None);
            if (username.Length > 30)
            {
                username = username.Substring(0, 30);
            }

            return username;
        }

        public static string GetIdentityProvider(this ClaimsPrincipal principal)
        {
            return principal.GetClaimValue(ClaimTypes.AuthenticationMethod) ??
                   principal.GetClaimValue(AcsClaimTypes.IdentityProvider);
        }

        public static bool HasRequiredClaims(this ClaimsPrincipal principal)
        {
            return principal.HasClaim(ClaimTypes.NameIdentifier) &&
                   principal.HasClaim(ClaimTypes.Name);
        }

        public static bool HasAllClaims(this ClaimsPrincipal principal)
        {
            return principal.HasRequiredClaims() &&
                   !String.IsNullOrEmpty(principal.GetIdentityProvider());
        }

        public static bool HasPartialIdentity(this ClaimsPrincipal principal)
        {
            return !String.IsNullOrEmpty(principal.GetClaimValue(JabbRClaimTypes.PartialIdentity));
        }
    }
}