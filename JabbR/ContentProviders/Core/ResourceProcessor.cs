using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace JabbR.ContentProviders.Core
{
    public class ResourceProcessor : IResourceProcessor
    {
        private readonly Lazy<IList<IContentProvider>> _contentProviders = new Lazy<IList<IContentProvider>>(GetContentProviders);

        public Task<ContentProviderResultModel> ExtractResource(string url)
        {
            var uri = new Uri(url);
            return Task.Factory.StartNew(() => ExtractContent(uri));
        }

        private ContentProviderResultModel ExtractContent(Uri uri)
        {
            return _contentProviders.Value.Select(c => c.GetContent(uri))
                                          .FirstOrDefault(content => content != null);
        }


        private static IList<IContentProvider> GetContentProviders()
        {
            // Use MEF to locate the content providers in this assembly
            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(ResourceProcessor).Assembly));
            return compositionContainer.GetExportedValues<IContentProvider>().ToList();
        }
    }
}