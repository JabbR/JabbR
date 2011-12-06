using System;
using System.Collections.Generic;
using System.Web;
using System.Text.RegularExpressions;

namespace JabbR.ContentProviders
{
    public class GoogleDocsFormProvider : EmbedContentProvider
    {
        private static readonly Regex _formIdExtractRegex = new Regex(@".*formkey=(.+)#|^");

        public override string MediaFormatString
        {
            get
            {
                return @"<iframe src='https://docs.google.com/spreadsheet/embeddedform?formkey={0}' width='500' height='500' frameborder='0' marginheight='0' marginwidth='0'>Loading...</iframe>";
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

        protected override System.Text.RegularExpressions.Regex ParameterExtractionRegex
        {
            get
            {
                return _formIdExtractRegex;
            }
        }
    }
}