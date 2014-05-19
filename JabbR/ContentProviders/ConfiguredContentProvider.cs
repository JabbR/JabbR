using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using JabbR.Services;

namespace JabbR.ContentProviders
{
    public class ConfiguredContentProvider : EmbedContentProvider
    {
        private readonly ISettingsManager _settingsManager;

        public ConfiguredContentProvider(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        private ApplicationSettings GetSettings()
        {
            // we must use the settings manager to get the settings instead of injecting the settings
            // directly, because the settings may change at runtime. Normally the special binding 
            // ApplicationSettings has would help, but content providers are loaded via MEF so it doesn't work.
            return _settingsManager.Load();
        }

        private ContentProviderSetting GetMatchingProvider(Uri uri)
        {
            foreach (var provider in GetSettings().ContentProviders)
            {
                if (provider.Enabled)
                {
                    foreach (var domain in provider.GetDomains())
                    {
                        if (uri.AbsoluteUri.StartsWith(domain, StringComparison.OrdinalIgnoreCase))
                        {
                            return provider;
                        }
                    }
                }
            }

            return null;
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                foreach (var provider in GetSettings().ContentProviders)
                {
                    if (provider.Enabled)
                    {
                        foreach (var domain in provider.GetDomains())
                        {
                            yield return domain;
                        }
                    }
                }
            }
        }

        protected override Regex GetParameterExtractionRegex(Uri uri)
        {
            var contentProvider = GetMatchingProvider(uri);
            return contentProvider == null
                ? null
                : new Regex(contentProvider.Extract, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        protected override string GetMediaFormatString(Uri uri)
        {
            var contentProvider = GetMatchingProvider(uri);
            return contentProvider == null
                ? MediaFormatString
                : GenerateContent(contentProvider);
        }

        protected override bool GetIsCollapsible(Uri uri)
        {
            var contentProvider = GetMatchingProvider(uri);
            return contentProvider != null && contentProvider.Collapsible;
        }

        public override string MediaFormatString
        {
            get
            {
                return null;
            }
        }

        protected override bool IsCollapsible { get { return false; } }

        private string GenerateContent(ContentProviderSetting provider)
        {
            return !string.IsNullOrEmpty(provider.Script)
                ? WrapWithCapturedWrite(provider)
                : provider.Output;
        }

        private string WrapWithCapturedWrite(ContentProviderSetting provider)
        {
            var script = provider.Script;
            var title = provider.Title ?? provider.Script;

            var scriptTagId = Guid.NewGuid().ToString();
            return string.Format(@"
                    <div id='{0}'></div>
                    <script type='text/javascript'>
                             captureDocumentWrite('" + SimpleEscape(script) + @"', '" + SimpleEscape(title) + @"', $('#{0}'));
                    </script>", scriptTagId);
        }

        private string SimpleEscape(string s)
        {
            return s
                .Replace("{", "{{")
                .Replace("}", "}}")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"");
        }
    }
}