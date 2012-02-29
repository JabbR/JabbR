using System;
using System.Net;
using System.Threading.Tasks;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders.Core
{
    public class ContentProviderHttpRequest
    {        
        public ContentProviderHttpRequest(Uri url)
        {
            RequestUri = url;
        }

        public Uri RequestUri { get; private set; }
    }
}