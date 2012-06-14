using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace JabbR.Infrastructure
{
    /// <summary>
    /// Wraps URL Query string parameters
    /// Yes, it's 2012 and we have to tokenize the url parameters manually.
    /// If there is a better way to get these in Web API, please refactor this code via deletion.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>A dictionary of URL parameter name values</returns>
    public class QueryStringCollection : IEnumerable<KeyValuePair<string,string>>
    {
        private Dictionary<string, string> _queryStringDictionary;

        public QueryStringCollection(Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException("requestUri");
            }

            var query = requestUri.Query;
            var parameters = query.TrimStart(new char[] { '?' }).Split(new char[] { '&' });
            _queryStringDictionary = new Dictionary<string, string>();

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
                _queryStringDictionary[name] = value;
            }
        }

        /// <summary>
        /// Half arsed model binding implementation, geared for url parameters. 
        /// Returns a value from a dictionary, converted to the specified type.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="dict">The source dictionary.</param>
        /// <param name="key">The key.</param>
        /// <returns>True if the value does not exist in the dictionary or can be converted to the requested type.
        /// False if the value exists but cannot be converted to the requested type.</returns>
        public bool TryGetAndConvert<T>(string key, out T value)
        {
            value = default(T);
            string valueStr;
            var type = typeof(T);

            if (_queryStringDictionary.TryGetValue(key, out valueStr))
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
        /// Gets the number of query string parameters.
        /// </summary>
        public int Count { get { return _queryStringDictionary.Count; } }

        /// <summary>
        /// Gets the value of a query string parameter with the name specified by key. Returns the value if it exists in the URI, null otherwise
        /// </summary>
        public string this[string key]
        {
            get
            {
                string value = null;
                _queryStringDictionary.TryGetValue(key, out value);
                return value;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _queryStringDictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _queryStringDictionary.GetEnumerator();
        }
    }
}