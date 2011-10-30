using System;
using System.Collections.Generic;
using System.Net;

namespace SignalR.Samples.Hubs.Chat.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        private static readonly HashSet<string> _imageMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "image/png",
            "image/jpg",
            "image/jpeg",
            "image/bmp",
            "image/gif",
        };
               
        protected override string GetTitle(HttpWebResponse response)
        {
            return response.ResponseUri.ToString();
        }

        protected override string GetCollapsibleContent(HttpWebResponse response)
        {
           return String.Format(@"<img src=""{0}"" />", response.ResponseUri);
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return !String.IsNullOrEmpty(response.ContentType) &&
                    _imageMimeTypes.Contains(response.ContentType);
            
        }
    }
}