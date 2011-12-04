using System;
using System.Globalization;
using System.Net;
using JabbR.Models;

namespace JabbR.ContentProviders
{
    public abstract class CollapsibleContentProvider : IContentProvider
    {
        public virtual ContentProviderResultModel GetContent(HttpWebResponse response)
        {
            if (IsValidContent(response))
            {
                return IsCollapsible ? new ContentProviderResultModel()
                {
                    Title = GetTitle(response),
                    Content = String.Format(CultureInfo.InvariantCulture,
                                                      ContentFormat,
                                                      GetTitle(response),
                                                      GetCollapsibleContent(response))
                }
                : new ContentProviderResultModel()
                {
                    Title = GetTitle(response),
                    Content = GetCollapsibleContent(response)
                };
            }

            return null;
        }

        protected virtual string GetTitle(HttpWebResponse response)
        {
            return response.ResponseUri.ToString();
        }

        protected abstract string GetCollapsibleContent(HttpWebResponse response);

        protected abstract bool IsValidContent(HttpWebResponse response);

        protected virtual bool IsCollapsible { get { return true; } }

        private const string ContentFormat = @"<h3 class=""collapsible_title"">{0} (click to show/hide)</h3><div class=""collapsible_box"">{1}</div>";
    }
}