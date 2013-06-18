using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Infrastructure
{
    public class AuthenticationResult
    {
        public string ProviderName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}