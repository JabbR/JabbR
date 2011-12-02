using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    /// <summary>
    /// Content Provider that provides the necessary functionality to show SlideShare embed code in the chat when someone pastes a
    /// slideshare link (eg. http://www.slideshare.net/verticalmeasures/search-social-and-content-marketing-move-your-business-forward-accelerate). 
    /// Send feedback/issues to Seth Webster (@sethwebsterDanTup on Twitter).
    /// </summary>
    public class SlideShareContentProvider : IContentProvider
    {

        private static readonly string slideScript = string.Format(
            "<div style='width:425px' id='__ss_10388027'> " +
            "<strong style='display:block;margin:12px 0 4px'>" +
            "<a href='{0}' target='_blank'></a></strong> " +
           " <iframe src='http://www.slideshare.net/slideshow/embed_code/10388027' width='425' height='355' frameborder='0' marginwidth='0' marginheight='0' scrolling='no'></iframe> <div style='padding:5px 0 12px'> View more <a href='http://www.slideshare.net/' target='_blank'>presentations</a> from <a href='http://www.slideshare.net/verticalmeasures' target='_blank'>Vertical Measures</a> </div> </div>",
           "test"

            );

        private static readonly string oEmbedUrl = "http://www.slideshare.net/api/oembed/2?url={0}&format=json";

        public string GetContent(HttpWebResponse response)
        {
            if (response.ResponseUri.AbsoluteUri.StartsWith("http://slideshare.net/", StringComparison.OrdinalIgnoreCase)
              || response.ResponseUri.AbsoluteUri.StartsWith("http://www.slideshare.net/", StringComparison.OrdinalIgnoreCase))
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        dynamic slideShareData = JsonConvert.DeserializeObject
                                   (
                                       client.DownloadString
                                       (
                                           string.Format(oEmbedUrl, response.ResponseUri.AbsoluteUri)
                                       )
                                   );
                        return slideShareData.html;

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return null;
        }
    }
}