using System;

namespace JabbR.Infrastructure
{
    public class BackplaneChannelRegistration
    {
        public string Name { get; set; }

        public object Instance { get; set; }

        public Type Type { get; set; }
    }
}