using System;
using System.Collections.Generic;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Nancy
{
    public class ArchivesModule : JabbRModule
    {
        public ArchivesModule(ISearchService searchService)
            : base("/archives")
        {
            Get["/"] = _ =>
            {
                int totalHits;
                IList<ChatMessage> results = searchService.Search(Request.Query.q, 0, 50, out totalHits);

                return View["index", results];
            };
        }
    }
}