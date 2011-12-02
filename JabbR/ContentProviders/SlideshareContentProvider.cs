using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using System.IO;

namespace JabbR.ContentProviders
{
    /// <summary>
    /// Content Provider that provides the necessary functionality to show SlideShare embed code in the chat when someone pastes a
    /// slideshare link (eg. http://www.slideshare.net/verticalmeasures/search-social-and-content-marketing-move-your-business-forward-accelerate). 
    /// Send feedback/issues to Seth Webster (@sethwebsterDanTup on Twitter).
    /// </summary>
    public class SlideShareContentProvider : IContentProvider
    {
        private static readonly String oEmbedUrl = "http://www.slideshare.net/api/oembed/2?url={0}&format=json";

        public string GetContent(HttpWebResponse response)
        {
            if (response.ResponseUri.AbsoluteUri.StartsWith("http://slideshare.net/", StringComparison.OrdinalIgnoreCase)
              || response.ResponseUri.AbsoluteUri.StartsWith("http://www.slideshare.net/", StringComparison.OrdinalIgnoreCase))
            {
                // We have to make a call to the SlideShare api because
                // their embed code request the unique ID of the slide deck
                // where we will only have the url -- this call gets the json information
                // on the slide deck and that package happens to already contain the embed code (.html)
                var webRequest = (HttpWebRequest)HttpWebRequest.Create(
                        String.Format(oEmbedUrl, response.ResponseUri.AbsoluteUri));
                var webResponse = webRequest.GetResponse();
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    dynamic slideShareData = JsonConvert.DeserializeObject(reader.ReadToEnd()
                               );
                    return slideShareData.html;
                }
            }
            return null;
        }
    }
}