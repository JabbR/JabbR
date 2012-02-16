using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace JabbR.ContentProviders.Core
{
    public abstract class EmbedContentProvider : CollapsibleContentProvider
    {
        public abstract IEnumerable<string> Domains { get; }
        public abstract string MediaFormatString { get; }

       
        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            var args = ExtractParameters(uri);
            if (args == null || !args.Any())
            {
                return null;
            }

            return new ContentProviderResultModel()
            {
                Content = String.Format(MediaFormatString, args.ToArray()),
                Title = uri.AbsoluteUri
            };
        }

        protected override bool IsValidContent(Uri uri)
        {
            return Domains.Any(d => uri.AbsoluteUri.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        }
    }
}