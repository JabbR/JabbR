using Microsoft.Ajax.Utilities;
using SignalR.Hubs;

namespace JabbR.Infrastructure
{
    public class AjaxMinMinifier : IJavaScriptMinifier
    {
        public string Minify(string source)
        {
            return new Minifier().MinifyJavaScript(source);
        }
    }
}