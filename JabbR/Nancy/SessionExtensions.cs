using System;
using Nancy.Session;
using Newtonsoft.Json;

namespace JabbR.Nancy
{
    public static class SessionExtensions
    {
        public static void SetSessionValue<T>(this ISession session, string key, T entry)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key must have a value", "key");
            }

            session[key] = JsonConvert.SerializeObject(entry);
        }

        public static T GetSessionVaue<T>(this ISession session, string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key must have a value", "key");
            }

            var sessionItem = session[key];

            if (sessionItem == null)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(sessionItem.ToString());
        }
    }
}