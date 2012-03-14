using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class NugetNuggetContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _nugetURLRegex = new Regex(@"nuget\.org/packages/(.*)");
        private static readonly string _nugetFeedURL = "http://packages.nuget.org/v1/FeedService.svc/Packages()?$filter=Id eq '{0}'&$orderby=Created desc";
        private static readonly string _nugetBadgeFormat = "<div class=\"nuget-badge\"><div class=\"nuget-pm\">PM></div><code>Install-Package {0}</code><div class=\"nuget-projectinfo\"><div class=\"nuget-title\">{1}</div><div class=\"nuget-summary\">{2}</div><div class=\"nuget-description\">{3}{4}</div>{5}<div style=\"clear:both\"></div></div></div>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            string packageName = ExtractPackageName(request.RequestUri);
            if (!String.IsNullOrEmpty(packageName))
            {
                return FetchPackage(packageName).Then(package =>
                {
                    if (package != null && package.d.results.Count > 0)
                    {
                        var packageInfo = package.d.results[0];
                        var projectIcon = String.Empty;
                        if (packageInfo.IconUrl != null)
                        {
                            projectIcon = String.Format("<img class=\"nuget-projecticon\" src=\"{0}\" />",
                                                        packageInfo.IconUrl);
                        }

                        var projectInfo = new StringBuilder();
                        projectInfo.AppendFormat("<div class=\"nuget-authors\" ><span>Authors: </span><div class=\"nuget-authors-entry\">{0}</div></div>",
                                                 packageInfo.Authors);
                        projectInfo.AppendFormat("<div class=\"nuget-downloads\" ><span># Downloads:</span> {0}</div>",
                                                 packageInfo.DownloadCount);

                        if (packageInfo.ProjectUrl != null)
                        {
                            projectInfo.AppendFormat(
                                "<div class=\"nuget-ProjectUrl\" ><a target=\"_blank\" href=\"{0}\">{0}</a></div>",
                                packageInfo.ProjectUrl);
                        }

                        return new ContentProviderResult()
                        {
                            Content = String.Format(_nugetBadgeFormat,
                                                    packageInfo.Id,
                                                    packageInfo.Title,
                                                    packageInfo.Summary,
                                                    projectIcon,
                                                    packageInfo.Description,
                                                    projectInfo),
                            Title = packageInfo.Title + " NuGet package"
                        };
                    }

                    return null;
                });
            }

            return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
        }

        private Task<dynamic> FetchPackage(string packageName)
        {
            var url = String.Format(_nugetFeedURL, packageName);

            return Http.GetJsonAsync(url);
        }

        protected string ExtractPackageName(Uri responseUri)
        {
            var packageDetail =
                _nugetURLRegex.FindMatches(responseUri.AbsoluteUri).FirstOrDefault(v => !String.IsNullOrEmpty(v));
            if (!String.IsNullOrEmpty(packageDetail))
            {
                return packageDetail.IndexOf('/') != -1 ? packageDetail.Split('/')[0] : packageDetail;
            }
            return null;
        }

        public override bool IsValidContent(Uri uri)
        {
            return
                uri.AbsoluteUri.StartsWith("http://nuget.org", StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.StartsWith("http://www.nuget.org", StringComparison.OrdinalIgnoreCase);
        }
    }
}