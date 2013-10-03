using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.ViewModels
{
    public class StatusViewModel
    {
        private readonly Dictionary<string, Exception> _systems = new Dictionary<string, Exception>();

        public IDictionary<string, Exception> Systems
        {
            get
            {
                return _systems;
            }
        }

        public bool AllOK
        {
            get
            {
                return !_systems.Values.Any(s => s != null);
            }
        }
    }
}