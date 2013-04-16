using System;
using PagedList;

namespace JabbR.ViewModels
{
    public class SearchResultsViewModel
    {
        public IPagedList<SearchMessageViewModel> Results { get; set; }
    }

    public class SearchMessageViewModel
    {
        public string Id { get; set; }
        public string RoomName { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string HtmlContent { get; set; }
        public bool HtmlEncoded { get; set; }
        public int MessageType { get; set; }
        public string ImageUrl { get; set; }
        public DateTimeOffset When { get; set; }
        public string Source { get; set; }
        public string Content { get; set; }
    }
}