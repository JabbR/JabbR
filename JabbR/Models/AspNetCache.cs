using System;
using System.Web;
using System.Web.Caching;

namespace JabbR.Models
{
    public class AspNetCache : ICache
    {
        private readonly Cache _cache = HttpRuntime.Cache;

        // Add a prefix since the cache is shared
        private const string CacheKeyPrefix = "JABBR_";

        public object Get(string key)
        {
            return _cache.Get(GetKey(key));
        }

        public void Set(string key, object value, TimeSpan expiresIn)
        {
            _cache.Insert(GetKey(key), value, null, Cache.NoAbsoluteExpiration, expiresIn);
        }

        public void Remove(string key)
        {
            _cache.Remove(GetKey(key));
        }

        private string GetKey(string key)
        {
            return CacheKeyPrefix + key;
        }
    }
}