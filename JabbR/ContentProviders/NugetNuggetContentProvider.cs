using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    public class NugetNuggetContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _nugetURLRegex = new Regex(@"nuget\.org/packages/(.*)");
        private static readonly string _nugetFeedURL = "http://packages.nuget.org/v1/FeedService.svc/Packages()?$filter=Id eq '{0}'&$orderby=Created desc";
        private static readonly string _nugetBadgeFormat = "<div class=\"nuget-badge\"><div class=\"nuget-pm\">PM></div><code>Install-Package {0}</code><div class=\"nuget-projectinfo\"><div class=\"nuget-title\">{1}</div><div class=\"nuget-summary\">{2}</div><div class=\"nuget-description\">{3}{4}</div>{5}<div style=\"clear:both\"></div></div></div>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            string packageName = ExtractPackageName(response.ResponseUri);
            if (!String.IsNullOrEmpty(packageName))
            {
                dynamic package = FetchPackage(packageName);
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
                            "<div class=\"nuget-ProjectUrl\" ><a href=\"_blank\" src=\"{0}\">{0}</a></div>",
                            packageInfo.ProjectUrl);
                    }

                    return new ContentProviderResultModel()
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
            }
            return null;
        }

        private dynamic FetchPackage(string packageName)
        {
            var webRequest = (HttpWebRequest) WebRequest.Create(String.Format(_nugetFeedURL, packageName));
            webRequest.Accept = "application/json";
            using (var webResponse = webRequest.GetResponse())
            {
                using (var sr = new StreamReader(webResponse.GetResponseStream()))
                {
                    var response = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject(response);
                }
            }
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

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return
                response.ResponseUri.AbsoluteUri.StartsWith("http://nuget.org", StringComparison.OrdinalIgnoreCase) ||
                response.ResponseUri.AbsoluteUri.StartsWith("http://www.nuget.org", StringComparison.OrdinalIgnoreCase);
        }
    }
}