using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JabbR.Services;

namespace JabbR.ContentProviders.Core
{
    public class ResourceProcessor : IResourceProcessor
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IList<IContentProvider> _contentProviders;

        public ResourceProcessor(ISettingsManager settingsManager, IList<IContentProvider> contentProviders)
        {
            _settingsManager = settingsManager;
            _contentProviders = contentProviders;
        }

        public Task<ContentProviderResult> ExtractResource(string url)
        {
            Uri resultUrl;
            if (Uri.TryCreate(url, UriKind.Absolute, out resultUrl))
            {
                var request = new ContentProviderHttpRequest(resultUrl);
                return ExtractContent(request);
            }

            return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
        }

        private Task<ContentProviderResult> ExtractContent(ContentProviderHttpRequest request)
        {
            var validProviders = GetActiveContentProviders().Where(c => c.IsValidContent(request.RequestUri))
                                                  .ToList();

            if (validProviders.Count == 0)
            {
                return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
            }

            var tasks = validProviders.Select(c => c.GetContent(request)).ToArray();

            var tcs = new TaskCompletionSource<ContentProviderResult>();

            Task.Factory.ContinueWhenAll(tasks, completedTasks =>
            {
                var faulted = completedTasks.FirstOrDefault(t => t.IsFaulted);
                if (faulted != null)
                {
                    tcs.SetException(faulted.Exception);
                }
                else if (completedTasks.Any(t => t.IsCanceled))
                {
                    tcs.SetCanceled();
                }
                else
                {
                    ContentProviderResult result = completedTasks.Select(t => t.Result)
                                                                 .FirstOrDefault(content => content != null);
                    tcs.SetResult(result);
                }
            });

            return tcs.Task;
        }

        private IList<IContentProvider> GetActiveContentProviders()
        {
            var applicationSettings = _settingsManager.Load();
            return _contentProviders
                .Where(cp => !applicationSettings.DisabledContentProviders.Contains(cp.GetType().Name))
                .ToList();
        }
    }
}