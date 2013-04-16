using System;
using JabbR.ViewModels;
using PagedList;

namespace JabbR.Infrastructure
{
    public interface ISearchService
    {
        IPagedList<SearchMessageViewModel> Search(SearchRequest request);
    }
}
