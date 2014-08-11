using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.UploadHandlers
{
    public class UploadResult
    {
        public string Identifier { get; set; }
        public string Url { get; set; }
        public bool UploadTooLarge { get; set; }
        public int MaxUploadSize { get; set; }
    }
}