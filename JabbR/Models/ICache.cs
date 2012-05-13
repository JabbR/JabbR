using System;

namespace JabbR.Models
{
    public interface ICache
    {
        object Get(string key);
        void Set(string key, object value, TimeSpan expiresIn);
        void Remove(string key);
    }
}