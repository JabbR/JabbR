using System;
using System.Globalization;
using System.Net;

namespace JabbR.ContentProviders
{
    public abstract class CollapsibleContentProvider : IContentProvider
    {
        public virtual ContentProviderResultModel GetContent(HttpWebResponse response)
        {
            if (IsValidContent(response))
            {
                var result = GetCollapsibleContent(response);
                if (IsCollapsible)
                {
                    result.Content = String.Format(CultureInfo.InvariantCulture,
                                                      ContentFormat,
                                                      result.Title,
                                                      result.Content);
                }
                return result;
            }

            return null;
        }

        protected abstract ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response);

        protected abstract bool IsValidContent(HttpWebResponse response);

        protected virtual bool IsCollapsible { get { return true; } }

        private const string ContentFormat = @"<h3 class=""collapsible_title"">{0} (click to show/hide)</h3><div class=""collapsible_box"">{1}</div>";
    }
}