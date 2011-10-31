using System.Globalization;
using System.Net;

namespace SignalR.Samples.Hubs.Chat.ContentProviders
{
    public abstract class CollapsibleContentProvider : IContentProvider
    {
        public string GetContent(HttpWebResponse response)
        {
            if (IsValidContent(response))
            {
                return IsCollapsible ? string.Format(CultureInfo.InvariantCulture,
                                                      ContentFormat,
                                                      GetTitle(response),
                                                      GetCollapsibleContent(response)) : GetCollapsibleContent(response);
            }

            return null;
        }

        protected abstract string GetTitle(HttpWebResponse response);

        protected abstract string GetCollapsibleContent(HttpWebResponse response);

        protected abstract bool IsValidContent(HttpWebResponse response);

        protected virtual bool IsCollapsible { get { return true; } }

        private const string ContentFormat = @"<h3 class=""collapsible_title"">{0} (click to show/hide)</h3><div class=""collapsible_box"">{1}</div>";
    }
}