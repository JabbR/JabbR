using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace JabbR
{
    public static class JsonSerializer
    {
        public static string Serialize(object item)
        {
            return JsonConvert.SerializeObject(item, new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });
        }
    }
}