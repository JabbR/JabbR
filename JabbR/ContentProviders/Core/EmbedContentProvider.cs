using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace JabbR.ContentProviders.Core
{
    public abstract class EmbedContentProvider : CollapsibleContentProvider
    {
        public abstract IEnumerable<string> Domains { get; }
        public abstract string MediaFormatString { get; }


        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var args = ExtractParameters(request.RequestUri);
            if (args == null || !args.Any())
            {
                return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
            }

            return TaskAsyncHelper.FromResult(new ContentProviderResult()
             {
                 Content = String.Format(MediaFormatString, args.ToArray()),
                 Title = request.RequestUri.AbsoluteUri.ToString()
             });
        }

        public override bool IsValidContent(Uri uri)
        {
            return Domains.Any(d => uri.AbsoluteUri.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        }
    }
}