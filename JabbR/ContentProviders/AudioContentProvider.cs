using System;
using System.Collections.Generic;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class AduioContentProvider : IContentProvider
    {
        private static readonly HashSet<string> _audioMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "audio/mpeg",
            "audio/x-wav",
            "aduio/ogg"
        };

        protected bool IsValidContent(Uri uri)
        {
            return uri.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsolutePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);

        }

        public ContentProviderResultModel GetContent(Uri uri)
        {
            if (IsValidContent(uri))
            {
                return new ContentProviderResultModel()
                {
                    Content = String.Format(@"<audio controls=""controls"" src=""{0}"">Your browser does not support the audio tag.</audio>", uri),
                    Title = uri.AbsoluteUri
                };
            }
            return null;
        }
    }
}