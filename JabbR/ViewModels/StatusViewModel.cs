using System.Collections.Generic;
using System.Linq;

namespace JabbR.ViewModels
{
    public class StatusViewModel
    {
        private readonly List<SystemStatus> _systems = new List<SystemStatus>();

        public IList<SystemStatus> Systems
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
                return _systems.All(s => s.IsOK ?? true);
            }
        }
    }
}