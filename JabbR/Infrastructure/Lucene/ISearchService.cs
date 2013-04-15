using System;
using System.Collections.Generic;
using JabbR.Models;

namespace JabbR.Infrastructure
{
    public interface ISearchService
    {
        IList<ChatMessage> Search(string text, int skip, int take, out int totalHits);
    }
}
