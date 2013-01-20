using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using Microsoft.AspNet.Razor.Owin.Compilation;
using SquishIt.Framework;

namespace JabbR
{
    /// <summary>
    /// Basic assembly reference locator that hardcodes assemblies needed to compile default.aspx
    /// </summary>
    public class AssemblyReferenceLocator : IReferenceAssemblyLocator
    {
        public IList<string> GetReferenceAssemblies()
        {
            return new[] {
                typeof(ConfigurationManager).Assembly.Location,
                typeof(Bundle).Assembly.Location,
                typeof(NameValueCollection).Assembly.Location
            };
        }
    }
}
