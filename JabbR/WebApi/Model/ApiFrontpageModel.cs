using System;
using System.Collections.Generic;
using System.Linq;

namespace JabbR.WebApi.Model
{
    public class ApiFrontpageModel
    {
        public AuthApiModel Auth { get; set; }
        public string MessagesUri { get; set; }
    }
}
