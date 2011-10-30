using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SignalR.Samples.Hubs.Chat.ContentProviders
{
    public abstract class EmbedContentProvider : CollapsibleContentProvider
    {
        public virtual Regex MediaUrlRegex
        {
            get
            {
                return null;
            }
        }
        public abstract IEnumerable<string> Domains { get; }
        public abstract string MediaFormatString { get; }

        protected virtual IEnumerable<object> ExtractParameters(Uri responseUri)
        {
            if (MediaUrlRegex != null)
            {
                return MediaUrlRegex.Match(responseUri.AbsoluteUri)
                                    .Groups
                                    .Cast<Group>()
                                    .Skip(1)
                                    .Select(g => g.Value)
                                    .Where(v => !String.IsNullOrEmpty(v));
            }
            return null;
        }

        protected override abstract string GetTitle(HttpWebResponse response);

        protected override string GetCollapsibleContent(HttpWebResponse response)
        {
            var args = ExtractParameters(response.ResponseUri);
            if (args == null || !args.Any())
            {
                return null;
            }

            return String.Format(MediaFormatString, args.ToArray());
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return Domains.Any(d => response.ResponseUri.AbsoluteUri.StartsWith(d, StringComparison.OrdinalIgnoreCase));
        }
    }
}