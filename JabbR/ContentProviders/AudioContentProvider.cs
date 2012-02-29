using System;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class AudioContentProvider : IContentProvider
    {
        public bool IsValidContent(Uri uri)
        {
            return uri.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsolutePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsolutePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ContentProviderResult> GetContent(ContentProviderHttpRequest request)
        {
            return TaskAsyncHelper.FromResult(new ContentProviderResult()
            {
                Content = String.Format(@"<audio controls=""controls"" src=""{0}"">Your browser does not support the audio tag.</audio>", request.RequestUri),
                Title = request.RequestUri.AbsoluteUri.ToString()
            });
        }
    }
}