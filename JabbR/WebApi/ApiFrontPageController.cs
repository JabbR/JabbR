using System.Net;
using System.Net.Http;
using System.Web.Http;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.WebApi.Model;

namespace JabbR.WebApi
{
    public class ApiFrontPageController : ApiController
    {
        private IApplicationSettings _appSettings;

        public ApiFrontPageController(IApplicationSettings appSettings)
        {
            _appSettings = appSettings;
        }

        /// <summary>
        /// Returns an absolute URL (including host and protocol) that corresponds to the relative path passed as an argument.
        /// </summary>
        /// <param name="sitePath">Path within the aplication, may contain ~ to denote the application root</param>
        /// <returns>A URL that corresponds to requested path using host and protocol of the request</returns>
        public string ToAbsoluteUrl(string sitePath)
        {
            return Request.GetAbsoluteUri(sitePath).AbsoluteUri;
        }

        public HttpResponseMessage GetFrontPage()
        {
            var responseData = new ApiFrontpageModel
            {
                MessagesUri = ToAbsoluteUrl(GetMessagesUrl())
            };

            return Request.CreateJabbrSuccessMessage(HttpStatusCode.OK, responseData);
        }

        private string GetMessagesUrl() {
            //hardcoded for now, needs a better place - i.e. some sort of constants.cs. 
            //Alternatively there might be a better way to do that in WebAPI
            return "/api/v1/messages/{room}/{format}";
        }
    }
}