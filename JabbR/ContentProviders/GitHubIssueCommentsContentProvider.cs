using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class GitHubIssueCommentsContentProvider : CollapsibleContentProvider
    {
        //api request url
        //GET /repos/:owner/:repo/issues/comments/:id

        //sample issue comment url
        //https://github.com/:owner/:repoR/issues/:issueid#issuecomment-:commentid
        private static readonly Regex _githubIssuesRegex = new Regex(@"https://github.com(.*)/issues/(\d+)\#issuecomment-(\d+)");
        private static readonly string _gitHubIssuesApiFormat = "https://api.github.com/repos{0}/issues/comments/{1}?callback=addGitHubIssueComment";
        private static readonly string _gitHubIssuesContentFormat = "<div class='git-hub-issue git-hub-issue-{0}'></div><script src='{1}'></script>";


        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var parameters = ExtractParameters(request.RequestUri);

            return TaskAsyncHelper.FromResult(new ContentProviderResult()
            {
                Content = String.Format(_gitHubIssuesContentFormat,
                        parameters[2],
                    String.Format(_gitHubIssuesApiFormat, parameters[0], parameters[2])
                ),
                Title = request.RequestUri.AbsoluteUri
            });
        }

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _githubIssuesRegex;
            }
        }

        public override bool IsValidContent(Uri uri)
        {
            return ExtractParameters(uri).Count == 3;
        }

    }
}