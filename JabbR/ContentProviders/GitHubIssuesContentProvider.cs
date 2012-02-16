using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    public class GitHubIssuesContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _githubIssuesRegex = new Regex(@"https://github.com(.*)/issues/(\d+)");
        private static readonly string _gitHubIssuesApiFormat = "https://api.github.com/repos{0}/issues/{1}?callback=addGitHubIssue";
        private static readonly string _gitHubIssuesContentFormat = "<div class='git-hub-issue git-hub-issue-{0}'></div><script src='{1}'></script>";

        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            var parameters = ExtractParameters(uri);

            return new ContentProviderResultModel()
            {
                Content = String.Format(_gitHubIssuesContentFormat,
                        parameters[1],
                    String.Format(_gitHubIssuesApiFormat, parameters[0], parameters[1])
                ),
                Title = uri.AbsoluteUri
            };
        }

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _githubIssuesRegex;
            }
        }

        protected override bool IsValidContent(Uri uri)
        {
            return ExtractParameters(uri).Count == 2;
        }
    }
}