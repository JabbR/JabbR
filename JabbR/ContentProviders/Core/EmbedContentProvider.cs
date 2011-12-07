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

       
        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var args = ExtractParameters(response.ResponseUri);
            if (args == null || !args.Any())
            {
                return null;
            }

            return new ContentProviderResultModel()
            {
                Content = String.Format(MediaFormatString, args.ToArray()),
                Title = response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return Domains.Any(d => response.ResponseUri.AbsoluteUri.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        }
    }
}