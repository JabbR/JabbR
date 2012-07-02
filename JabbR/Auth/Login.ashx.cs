using System;
using System.Net;
using System.Web;
using JabbR.App_Start;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Newtonsoft.Json;
using Ninject;

namespace JabbR.Auth
{
    /// <summary>
    /// Summary description for Login
    /// </summary>
    public class Login : IHttpHandler
    {
        private const string VerifyTokenUrl = "https://rpxnow.com/api/v2/auth_info?apiKey={0}&token={1}";

        public void ProcessRequest(HttpContext context)
        {
            var settings = Bootstrapper.Kernel.Get<IApplicationSettings>();
            string apiKey = settings.AuthApiKey;

            if (String.IsNullOrEmpty(apiKey))
            {
                // Do nothing
                context.Response.Redirect("~/", false);
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            string token = context.Request.Form["token"];

            if (String.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Bad response from login provider - could not find login token.");
            }

            var response = new WebClient().DownloadString(String.Format(VerifyTokenUrl, apiKey, token));

            if (String.IsNullOrEmpty(response))
            {
                throw new InvalidOperationException("Bad response from login provider - could not find user.");
            }

            dynamic j = JsonConvert.DeserializeObject(response);

            if (j.stat.ToString() != "ok")
            {
                throw new InvalidOperationException("Bad response from login provider.");
            }


            string userIdentity = j.profile.identifier.ToString();
            string username = j.profile.preferredUsername.ToString();
            string email = String.Empty;
            if (j.profile.email != null)
            {
                email = j.profile.email.ToString();
            }

            var repository = Bootstrapper.Kernel.Get<IJabbrRepository>();
            var chatService = Bootstrapper.Kernel.Get<IChatService>();

            // Try to get the user by identity
            ChatUser user = repository.GetUserByIdentity(userIdentity);
            string hash = context.Request.QueryString["hash"];

            // No user with this identity
            if (user == null)
            {
                // See if the user is already logged in (via cookie)
                var clientState = GetClientState(context);
                user = repository.GetUserById(clientState.UserId);

                if (user != null)
                {
                    // If they are logged in then assocate the identity
                    user.Identity = userIdentity;
                    user.Email = email;
                    if (!String.IsNullOrEmpty(email) && 
                        String.IsNullOrEmpty(user.Hash))
                    {
                        user.Hash = email.ToMD5();
                    }
                    repository.CommitChanges();
                    context.Response.Redirect(GetUrl(hash), false);
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }
                else
                {
                    // There's no logged in user so create a new user with the associated credentials
                    // but first, let's clean up that username!
                    username = FixUserName(username);
                    user = chatService.AddUser(username, userIdentity, email);
                }
            }
            else
            {
                // Update email and gravatar
                user.Email = email;
                if (!String.IsNullOrEmpty(email) &&
                    String.IsNullOrEmpty(user.Hash))
                {
                    user.Hash = email.ToMD5();
                }
                repository.CommitChanges();
            }

            // Save the cokie state
            var state = JsonConvert.SerializeObject(new { userId = user.Id });
            var cookie = new HttpCookie("jabbr.state", state);
            cookie.Expires = DateTime.Now.AddDays(30);
            context.Response.Cookies.Add(cookie);
            context.Response.Redirect(GetUrl(hash), false);
            context.ApplicationInstance.CompleteRequest();
        }

        private string GetUrl(string hash)
        {
            return HttpRuntime.AppDomainAppVirtualPath + hash;
        }

        private string FixUserName(string username) {
            // simple for now, translate spaces to underscores
            return username.Replace(' ', '_');
        }

        private ClientState GetClientState(HttpContext context)
        {
            // New client state
            var jabbrState = GetCookieValue(context, "jabbr.state");

            ClientState clientState = null;

            if (String.IsNullOrEmpty(jabbrState))
            {
                clientState = new ClientState();
            }
            else
            {
                clientState = JsonConvert.DeserializeObject<ClientState>(jabbrState);
            }

            return clientState;
        }

        private string GetCookieValue(HttpContext context, string key)
        {
            HttpCookie cookie = context.Request.Cookies[key];
            return cookie != null ? HttpUtility.UrlDecode(cookie.Value) : null;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}