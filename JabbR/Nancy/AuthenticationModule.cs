using System;
using JabbR.Services;
using Nancy;

namespace JabbR.Nancy
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule(IJabbrRepository repository)
        {
            Post["/"] = _ =>
            {
                string userId;
                if (Request.Cookies.TryGetValue("jabbr.id", out userId) &&
                    !String.IsNullOrEmpty(userId) &&
                    repository.GetUserById(userId) != null)
                {
                    return 200;
                }

                return 403;
            };
        }
    }
}