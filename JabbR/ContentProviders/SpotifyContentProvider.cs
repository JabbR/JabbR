using JabbR.ContentProviders.Core;
using System;
using System.Threading.Tasks;

namespace JabbR.ContentProviders
{
    public class SpotifyContentProvider : CollapsibleContentProvider
    {
        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var spotifyUri = ExtractSpotifyUri(request.RequestUri.AbsolutePath);

            return TaskAsyncHelper.FromResult(new ContentProviderResult()
                                                  {
                                                      Content = String.Format("<iframe src=\"https://embed.spotify.com/?uri=spotify:{0}\" width=\"300\" height=\"380\" frameborder=\"0\" allowtransparency=\"true\"></iframe>", spotifyUri),
                                                      Title = String.Format("spotify:track:{0}", spotifyUri)
                                                  });
        }

        private string ExtractSpotifyUri(string requestUrl)
        {
            return requestUrl.Remove(0, 1).Replace('/', ':');
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://open.spotify.com/", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}