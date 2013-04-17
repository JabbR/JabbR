using System;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Nancy;
using JabbR.Services;
using JabbR.UploadHandlers;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using Nancy.Authentication.WorldDomination;
using Nancy.Bootstrappers.Ninject;
using Newtonsoft.Json;
using Ninject;
using WorldDomination.Web.Authentication;

namespace JabbR
{
    public partial class Startup
    {
        private static KernelBase SetupNinject(ApplicationSettings settings)
        {
            var kernel = new StandardKernel(new[] { new FactoryModule() });

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            kernel.Bind<IDataProtection>()
                  .To<JabbRDataProtection>();

            kernel.Bind<IFormsAuthenticationProvider>()
                  .To<JabbRFormsAuthenticationProvider>();

            kernel.Bind<ILogger>()
                  .To<RealtimeLogger>();

            // We're doing this manually since we want the chat repository to be shared
            // between the chat service and the chat hub itself
            kernel.Bind<Chat>()
                  .ToMethod(context =>
                  {
                      var resourceProcessor = context.Kernel.Get<ContentProviderProcessor>();
                      var repository = context.Kernel.Get<IJabbrRepository>();
                      var cache = context.Kernel.Get<ICache>();

                      var service = new ChatService(cache, repository);

                      return new Chat(resourceProcessor,
                                      service,
                                      repository,
                                      cache);
                  });

            kernel.Bind<ICryptoService>()
                .To<CryptoService>()
                .InSingletonScope();

            kernel.Bind<IResourceProcessor>()
                .To<ResourceProcessor>()
                .InSingletonScope();

            kernel.Bind<IApplicationSettings>()
                  .ToConstant(settings);

            kernel.Bind<IJavaScriptMinifier>()
                  .To<AjaxMinMinifier>()
                  .InSingletonScope();

            kernel.Bind<IMembershipService>()
                  .To<MembershipService>();

            kernel.Bind<IAuthenticationService>()
                  .ToConstant(new AuthenticationService());

            kernel.Bind<IAuthenticationCallbackProvider>()
                      .To<JabbRAuthenticationCallbackProvider>();

            kernel.Bind<ICache>()
                  .To<DefaultCache>()
                  .InSingletonScope();

            kernel.Bind<IChatNotificationService>()
                  .To<ChatNotificationService>();

            if (String.IsNullOrEmpty(settings.VerificationKey) ||
                String.IsNullOrEmpty(settings.EncryptionKey))
            {
                kernel.Bind<IKeyProvider>()
                      .ToConstant(new FileBasedKeyProvider());
            }
            else
            {
                kernel.Bind<IKeyProvider>()
                      .To<AppSettingKeyProvider>()
                      .InSingletonScope();
            }

            var serializer = new JsonNetSerializer(new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            kernel.Bind<IJsonSerializer>()
                  .ToConstant(serializer);

            kernel.Bind<UploadCallbackHandler>()
                  .ToSelf()
                  .InSingletonScope();

            kernel.Bind<UploadProcessor>()
                  .ToSelf()
                  .InSingletonScope();

            kernel.Bind<ContentProviderProcessor>()
                  .ToConstant(new ContentProviderProcessor(kernel));

            ILuceneFileSystem luceneFileSystem = null;
            if (String.IsNullOrEmpty(settings.AzureblobStorageConnectionString))
            {
                luceneFileSystem = new LuceneLocalFileSystem();
            }
            else
            {
                luceneFileSystem = new LuceneAzureBlobFileSystem(settings);
            }

            kernel.Bind<ILuceneFileSystem>()
                  .ToConstant(luceneFileSystem);

            kernel.Bind<ISearchIndexingService>()
                  .ToMethod(context => new LuceneIndexingService(() => new PersistedRepository(new JabbrContext()), context.Kernel.Get<ILuceneFileSystem>()))
                  .InSingletonScope();

            kernel.Bind<ISearchService>()
                  .To<LuceneSearchService>();

            return kernel;
        }
    }
}