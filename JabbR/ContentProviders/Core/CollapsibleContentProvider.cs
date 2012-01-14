using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace JabbR.ContentProviders.Core
{
    public abstract class CollapsibleContentProvider : IContentProvider
    {

        public virtual ContentProviderResultModel GetContent(HttpWebResponse response)
        {
            if (IsValidContent(response))
            {
                var result = GetCollapsibleContent(response);
                if (IsCollapsible && result != null)
                {
                    result.Content = String.Format(CultureInfo.InvariantCulture,
                                                      ContentFormat,
                                                      IsPopOut ? @"<div class=""collapsible_pin""></div>" : "",
                                                      result.Title,
                                                      result.Content);
                }
                return result;
            }

            return null;
        }

        protected virtual Regex ParameterExtractionRegex
        {
            get
            {
                return new Regex(@"(\d+)");

            }
        }

        protected virtual IList<string> ExtractParameters(Uri responseUri)
        {
            return ParameterExtractionRegex.Match(responseUri.AbsoluteUri)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .Where(v => !String.IsNullOrEmpty(v)).ToList();     

        }
        protected abstract ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response);

        protected abstract bool IsValidContent(HttpWebResponse response);

        protected virtual bool IsCollapsible { get { return true; } }
        protected virtual bool IsPopOut { get { return true; } }

        private const string ContentFormat = @"<div class=""collapsible_content"">{0}<h3 class=""collapsible_title"">{1} (click to show/hide)</h3><div class=""collapsible_box"">{2}</div></div>";
    }
}