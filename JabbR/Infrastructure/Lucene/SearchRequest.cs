using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Infrastructure
{
    public class SearchRequest
    {
        public string SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int PerPage { get; set; }
    }
}