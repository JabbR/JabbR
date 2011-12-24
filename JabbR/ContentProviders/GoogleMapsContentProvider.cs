using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class GoogleMapsContentProvider : EmbedContentProvider
    {
        private static readonly Regex _mapQueryRegex = new Regex(@".&hnear=(.*)\&[tg].*");

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _mapQueryRegex;
            }
        }

        public override IEnumerable<string> Domains
        {
            get { yield return "http://maps.google.com"; }
        }

        public override string MediaFormatString
        {
            get
            {
                return @"<a href=""http://maps.google.com?q={0}"">Google Maps</a><br/><img src=""http://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=12&size=500x400&sensor=false""/>";
            }
        }
    }
}