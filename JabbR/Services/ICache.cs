using System;

namespace JabbR.Services
{
    public interface ICache
    {
        object Get(string key);
        void Set(string key, object value, TimeSpan expiresIn);
        void Remove(string key);
    }
}