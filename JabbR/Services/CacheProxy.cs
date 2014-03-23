using System;

using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class CacheProxy : ICache
    {
        private readonly IBackplaneChannel _backplaneChannel;

        private readonly ICache _cache;

        public CacheProxy(IBackplaneChannel backplaneChannel, ICache cache)
        {
            _backplaneChannel = backplaneChannel;
            _cache = cache;

            _backplaneChannel.Subscribe<ICache>(cache);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _backplaneChannel.Invoke<ICache>("Remove", new object[] { key });
        }

        public object Get(string key)
        {
            return _cache.Get(key);
        }

        public void Set(string key, object value, TimeSpan expiresIn)
        {
            _cache.Set(key, value, expiresIn);
        }

        public void Dispose()
        {
            _backplaneChannel.Unsubscribe(_cache);
        }
    }
}