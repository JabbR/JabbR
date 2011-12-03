using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace JabbR.ContentProviders
{
    public class GoogleDocsPresentationsContentProvider : EmbedContentProvider
    {
        public override string MediaFormatString
        {
            get
            {
                return @"<iframe src='https://docs.google.com/presentation/embed?id={0}&start=false&loop=false&delayms=3000' frameborder='0' width='480' height='389' allowfullscreen='true' webkitallowfullscreen='true'></iframe>";
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "https://docs.google.com/presentation";
                yield return "http://docs.google.com/presentation";
            }
        }

        protected override IEnumerable<object> ExtractParameters(Uri responseUri)
        {
            var queryString = HttpUtility.ParseQueryString(responseUri.Query);
            string formId = queryString["id"];
            if (!String.IsNullOrEmpty(formId))
            {
                yield return formId;
            }
        }
    }
}