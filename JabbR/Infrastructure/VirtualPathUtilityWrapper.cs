using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Infrastructure
{
    public class VirtualPathUtilityWrapper : IVirtualPathUtility
    {
        public string ToAbsolute(string virtualPath)
        {
            return VirtualPathUtility.ToAbsolute(virtualPath);
        }
    }
}
