using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Routing;
using System.ComponentModel;

namespace JabbR.Infrastructure
{
    public static class UriExtensions
    {
        /// <summary>
        /// Half arsed model binding implementation, geared for url parameters. 
        /// Returns a value from a dictionary, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="dict">The source dictionary.</param>
        /// <param name="key">The key.</param>
        /// <returns>True if the value does not exist in the dictionary or can be converted to the requested type.
        /// False if the value exists but cannot be converted to the requested type.</returns>
        public static bool TryGetAndConvert<T>(this IDictionary<string,string> dict, string key, out T value)
        {
            value = default(T);
            string valueStr;
            var type = typeof(T);

            if (dict.TryGetValue(key, out valueStr))
            {
                if (String.IsNullOrEmpty(valueStr))
                {
                    if (type.IsValueType && !IsNullableType(type))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null || !converter.IsValid(valueStr))
                {
                    return false;
                }
                value =  (T)converter.ConvertFromString(valueStr);
            }
            return true;
        }

        private static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Splits up url parameters and returns them as a dictionary.
        /// Yes, it's 2012 and we have to tokenize the url parameters manually.
        /// If there is a better way to get these in Web API, please refactor this code via deletion.
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <returns>A dictionary of URL parameter name values</returns>
        public static IDictionary<string, string> QueryString(this Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException("requestUri");
            }

            var query = requestUri.Query;
            var parameters = query.TrimStart(new char[] { '?' }).Split(new char[] { '&' });
            var dict = new Dictionary<string, string>();

            foreach (var param in parameters)
            {
                if (String.IsNullOrEmpty(param))
                {
                    continue;
                }
                string name = null, value = null;

                var nv = param.Split(new char[] { '=' });
                if (nv.Length >= 1)
                {
                    name = nv[0];
                }
                if (nv.Length == 2)
                {
                    value = nv[1];
                }
                dict[name] = value;
            }

            return dict;
        }
    }
}