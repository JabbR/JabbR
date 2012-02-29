using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    public class NerdDinnerContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _nerdDinnerIdRegex = new Regex(@"(\d+)");
        // 0 Lat, 1 Long, 2 Address, 3 Content Format for Info       
        private static readonly string _nerdDinnerContentFormat = "<div class='nerddinner_map_wrapper'><div id='mapviewer' style='width:500px;float:left;'><iframe id='map' Name='mapFrame' scrolling='no' width='500' height='400' frameborder='0' src='http://www.bing.com/maps/embed/?lvl=16&amp;cp={0}~{1}&amp;sty=r&amp;draggable=true&amp;v=2&amp;dir=0&amp;where1={2}&amp;form=LMLTEW&amp;pp={0}~{1}&amp;mkt=en-us&amp;emid=6eaffd53-d18e-d289-21d6-376c210b7df1&amp;w=500&amp;h=400'></iframe><div id='LME_maplinks' style='line-height:20px;'><a id='LME_largerMap' href='http://www.bing.com/maps/?cp={0}~{1}&amp;sty=r&amp;lvl=16&amp;where1={2}&amp;mm_embed=map&amp;form=LMLTEW' target='_blank'>View Larger Map</a>&nbsp;<a id='LME_directions' href='http://www.bing.com/maps/?cp={0}~{1}&amp;sty=r&amp;lvl=16&amp;rtp=~pos.{0}_{1}_{2}&amp;mm_embed=dir&amp;form=LMLTEW' target='_blank'>Get Directions</a>&nbsp;<a id='LME_birdsEye' href='http://www.bing.com/maps/?cp=pd5yp45rhh8v&amp;sty=b&amp;lvl=18&amp;where1={2}&amp;mm_embed=be&amp;form=LMLTEW' target='_blank'>View Bird's Eye</a></div></div><div class='nerd_dinner_info' style='width:240px;margin-left:5px;float:left'>{3}</div></div>";
        // 0 title, 1 date, 2 time, 3 address, 4 description, 5 dinner id
        private static readonly string _nerdDinnerInfoContentFormat = "<h2>{0}</h2><p><strong>When: </strong>{1} @ {2}</p><p><strong>Where: </strong>{3}</p><p><strong>Description: </strong>{4}</p><p><div id='rsvpmsg'><strong>RSVP for this event:</strong><a href='http://nerddinner.com/RSVP/RsvpTwitterBegin/{5}'><img alt='Twitter' src='http://nerddinner.com/Content/Img/icon-twitter.png' border='0' style='padding:3px;' align='absmiddle'></a><a href='http://nerddinner.com/RSVP/RsvpBegin/{5}?identifier=https%3A%2F%2Fwww.google.com%2Faccounts%2Fo8%2Fid'><img alt='Google' src='http://nerddinner.com/Content/Img/icon-google.png' border='0'  style='padding:3px;' align='absmiddle'></a><a href='http://nerddinner.com/RSVP/RsvpBegin/{5}?identifier=https%3A%2F%2Fme.yahoo.com%2F'><img alt='Yahoo!' src='http://nerddinner.com/Content/Img/icon-yahoo.png' border='0'  style='padding:3px;' align='absmiddle'></a></p></div>";
        private static readonly string _nerdDinnerODdataFeedServiceDinnerQueryFormat = "http://nerddinner.com/Services/OData.svc/Dinners({0})";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            string strDinnerId = ExtractParameter(response.ResponseUri);
            int dinnerId = 0;
            if (!String.IsNullOrEmpty(strDinnerId) && Int32.TryParse(strDinnerId, out dinnerId))
            {
                var dinner = FetchDinner(dinnerId);

                if (dinner != null && dinner.d != null)
                {
                    return new ContentProviderResultModel()
                    {
                        Content = String.Format(_nerdDinnerContentFormat,
                        dinner.d.Latitude,
                        dinner.d.Longitude,
                        dinner.d.Address,
                        String.Format(
                        _nerdDinnerInfoContentFormat,
                        dinner.d.Title,
                        dinner.d.EventDate.Value.Date.ToLongDateString(),
                        dinner.d.EventDate.Value.ToLongTimeString(),
                        dinner.d.Address,
                        dinner.d.Description,
                        dinner.d.DinnerID)),
                        Title = dinner.d.Title
                    };
                }
            }
            return null;
        }

        private static dynamic FetchDinner(int dinnerId)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(
                String.Format(_nerdDinnerODdataFeedServiceDinnerQueryFormat, dinnerId));
            webRequest.Accept = "application/json";
            using (var webResponse = webRequest.GetResponse())
            {
                using (var sr = new StreamReader(webResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject(sr.ReadToEnd());
                }
            }
        }

        protected string ExtractParameter(Uri responseUri)
        {
            return _nerdDinnerIdRegex.Match(responseUri.AbsoluteUri)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .Where(v => !String.IsNullOrEmpty(v))
                                .FirstOrDefault();
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://nerddinner.com/", StringComparison.OrdinalIgnoreCase)
               || response.ResponseUri.AbsoluteUri.StartsWith("http://www.nerddinner.com/", StringComparison.OrdinalIgnoreCase)
               || response.ResponseUri.AbsoluteUri.StartsWith("http://nrddnr.com/", StringComparison.OrdinalIgnoreCase)
               || response.ResponseUri.AbsoluteUri.StartsWith("http://www.nrddnr.com/", StringComparison.OrdinalIgnoreCase);
        }
    }
}