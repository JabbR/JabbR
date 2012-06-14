using System;
using System.Collections.Generic;
using System.Linq;

namespace JabbR.Infrastructure
{
    public interface IVirtualPathUtility
    {
        string ToAbsolute(string relativePath);
    }
}
