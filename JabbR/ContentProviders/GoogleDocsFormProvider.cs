using System;
using System.Collections.Generic;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class GoogleDocsFormProvider : EmbedContentProvider
    {
        public override string MediaFormatString
        {
            get
            {
                return String.Format(@"<iframe src=""https://docs.google.com/spreadsheet/pub?key={{0}}&output=html&widget=true"" style=""width:100%;height:400px;"" frameborder=""0"" marginheight=""0"" marginwidth=""0"">{0}</iframe>", LanguageResources.LoadingMessage);
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "https://docs.google.com/spreadsheet";
                yield return "http://docs.google.com/spreadsheet";
            }
        }

        protected override IList<string> ExtractParameters(Uri responseUri)
        {
            var queryString = new QueryStringCollection(responseUri.Query);
            string formKey = queryString["key"];

            if (!String.IsNullOrEmpty(formKey))
            {
                return new [] { formKey };
            }

            return null;
        }
    }
}