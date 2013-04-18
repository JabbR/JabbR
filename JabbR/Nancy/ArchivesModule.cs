using System;
using JabbR.Infrastructure;
using JabbR.ViewModels;
using Nancy.ModelBinding;
using PagedList;

namespace JabbR.Nancy
{
    public class ArchivesModule : JabbRModule
    {
        public ArchivesModule(ISearchService searchService)
            : base("/archives")
        {
            Get["/"] = _ =>
            {
                var input = this.Bind<SearchRequestModel>();

                IPagedList<SearchMessageViewModel> results = searchService.Search(new SearchRequest()
                {
                    SearchQuery = input.q,
                    CurrentPage = input.page,
                    PerPage = 50,
                });

                var viewModel = new SearchResultsViewModel()
                {
                    Results = results
                };

                return View["index", viewModel];
            };
        }

        private class SearchRequestModel
        {
            public SearchRequestModel()
            {
                page = 1;
            }

            public string q { get; set; }
            public int page { get; set; }
        }
    }
}