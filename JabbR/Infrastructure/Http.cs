using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JabbR.Infrastructure
{
    public static class Http
    {
        private const string _userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0; MAAU)";

        public static Task<dynamic> GetJsonAsync(string url)
        {
            var task = GetAsync(url, webRequest =>
            {
                webRequest.Accept = "application/json";
            });

            return task.Then(response =>
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject(reader.ReadToEnd());
                }
            });
        }

        public static Task<HttpWebResponse> GetAsync(Uri uri, Action<HttpWebRequest> init = null)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.UserAgent = _userAgent;
            if (init != null)
            {
                init(request);
            }

            return Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state), ar => (HttpWebResponse)request.EndGetResponse(ar), null);
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> init = null)
        {
            return GetAsync(new Uri(url), init);
        }

        public static Task<TResult> GetJsonAsync<TResult>(string url)
        {
            var task = GetAsync(url, webRequest =>
            {
                webRequest.Accept = "application/json";
            });

            return task.Then(response =>
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TResult>(reader.ReadToEnd());
                }
            });
        }
    }
}