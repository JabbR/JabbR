using System;
using System.Collections.Generic;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class AduioContentProvider : CollapsibleContentProvider
    {
        private static readonly HashSet<string> _audioMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "audio/mpeg",
            "audio/x-wav",
            "aduio/ogg"
        };

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            return new ContentProviderResultModel()
             {
                 Content = String.Format(@"<audio controls=""controls"" src=""{0}"">Your browser does not support the audio tag.</audio>", response.ResponseUri),
                 Title = response.ResponseUri.AbsoluteUri.ToString()
             };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return !String.IsNullOrEmpty(response.ContentType) &&
                    _audioMimeTypes.Contains(response.ContentType);

        }
    }
}