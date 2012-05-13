using System;
using Newtonsoft.Json;
using SignalR;

namespace JabbR.Infrastructure
{
    public class JsonNetSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonNetSerializer(JsonSerializerSettings settings)
        {
            _settings = settings;
        }

        public string Stringify(object obj)
        {
            return JsonConvert.SerializeObject(obj, _settings);
        }

        public object Parse(string json)
        {
            return JsonConvert.DeserializeObject(json);
        }

        public object Parse(string json, Type targetType)
        {
            return JsonConvert.DeserializeObject(json, targetType);
        }

        public T Parse<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}