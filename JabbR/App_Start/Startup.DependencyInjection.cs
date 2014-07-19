using System;
using System.Linq;
using System.Reflection;
using JabbR.ContentProviders;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Nancy;
using JabbR.Services;
using JabbR.UploadHandlers;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Cookies;
using Nancy.Bootstrappers.Ninject;
using Nancy.SimpleAuthentication;
using Newtonsoft.Json;
using Ninject;
using Microsoft.AspNet.SignalR;

namespace JabbR
{
    public partial class Startup
    {
        private static KernelBase SetupNinject(IJabbrConfiguration configuration)
        {
            var kernel = new StandardKernel(new[] { new FactoryModule() });

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IRecentMessageCache>()
                  .To<NoopCache>()
                  .InSingletonScope();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            kernel.Bind<IDataProtector>()
                  .To<JabbRDataProtection>();

            kernel.Bind<ICookieAuthenticationProvider>()
                  .To<JabbRFormsAuthenticationProvider>();

            kernel.Bind<ILogger>()
                  .To<RealtimeLogger>();

            kernel.Bind<IUserIdProvider>()
                  .To<JabbrUserIdProvider>();

            kernel.Bind<IJabbrConfiguration>()
                  .ToConstant(configuration);

            // We're doing this manually since we want the chat repository to be shared
            // between the chat service and the chat hub itself
            kernel.Bind<Chat>()
                  .ToMethod(context =>
                  {
                      var resourceProcessor = context.Kernel.Get<ContentProviderProcessor>();
                      var recentMessageCache = context.Kernel.Get<IRecentMessageCache>();
                      var repository = context.Kernel.Get<IJabbrRepository>();
                      var cache = context.Kernel.Get<ICache>();
                      var logger = context.Kernel.Get<ILogger>();
                      var settings = context.Kernel.Get<ApplicationSettings>();

                      var service = new ChatService(cache, recentMessageCache, repository, settings);

                      return new Chat(resourceProcessor,
                                      service,
                                      recentMessageCache,
                                      repository,
                                      cache,
                                      logger,
                                      settings);
                  });

            kernel.Bind<ICryptoService>()
                .To<CryptoService>();

            kernel.Bind<IJavaScriptMinifier>()
                  .To<AjaxMinMinifier>()
                  .InSingletonScope();

            kernel.Bind<IMembershipService>()
                  .To<MembershipService>();

            kernel.Bind<ApplicationSettings>()
                  .ToMethod(context =>
                  {
                      return context.Kernel.Get<ISettingsManager>().Load();
                  });

            kernel.Bind<ISettingsManager>()
                  .To<SettingsManager>();

            kernel.Bind<IUserAuthenticator>()
                  .To<DefaultUserAuthenticator>();

            kernel.Bind<IAuthenticationService>()
                  .To<AuthenticationService>();

            kernel.Bind<IAuthenticationCallbackProvider>()
                  .To<JabbRAuthenticationCallbackProvider>();

            kernel.Bind<ICache>()
                  .To<DefaultCache>()
                  .InSingletonScope();

            kernel.Bind<IChatNotificationService>()
                  .To<ChatNotificationService>();

            kernel.Bind<IKeyProvider>()
                      .To<SettingsKeyProvider>();

            kernel.Bind<IResourceProcessor>()
                .To<ResourceProcessor>();

            RegisterContentProviders(kernel);

            var serializer = JsonSerializer.Create(new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            kernel.Bind<JsonSerializer>()
                  .ToConstant(serializer);

            kernel.Bind<UploadCallbackHandler>()
                  .ToSelf()
                  .InSingletonScope();

            kernel.Bind<UploadProcessor>()
                  .ToConstant(new UploadProcessor(kernel));

            kernel.Bind<ContentProviderProcessor>()
                  .ToConstant(new ContentProviderProcessor(kernel));

            kernel.Bind<IEmailTemplateContentReader>()
                  .To<RazorEmailTemplateContentReader>();

            kernel.Bind<IEmailTemplateEngine>()
                  .To<RazorEmailTemplateEngine>();

            kernel.Bind<IEmailSender>()
                  .To<SmtpClientEmailSender>();

            kernel.Bind<IEmailService>()
                  .To<EmailService>();

            return kernel;
        }

        private static void RegisterContentProviders(IKernel kernel)
        {
            kernel.Bind<IContentProvider>().To<AudioContentProvider>();
            kernel.Bind<IContentProvider>().To<BashQDBContentProvider>();
            kernel.Bind<IContentProvider>().To<BBCContentProvider>();
            kernel.Bind<IContentProvider>().To<DictionaryContentProvider>();
            kernel.Bind<IContentProvider>().To<GitHubIssueCommentsContentProvider>();
            kernel.Bind<IContentProvider>().To<GitHubIssuesContentProvider>();
            kernel.Bind<IContentProvider>().To<GoogleDocsFormProvider>();
            kernel.Bind<IContentProvider>().To<GoogleDocsPresentationsContentProvider>();
            kernel.Bind<IContentProvider>().To<GoogleMapsContentProvider>();
            kernel.Bind<IContentProvider>().To<ImageContentProvider>();
            kernel.Bind<IContentProvider>().To<ImgurContentProvider>();
            kernel.Bind<IContentProvider>().To<NerdDinnerContentProvider>();
            kernel.Bind<IContentProvider>().To<NugetNuggetContentProvider>();
            kernel.Bind<IContentProvider>().To<ScreencastContentProvider>();
            kernel.Bind<IContentProvider>().To<SlideShareContentProvider>();
            kernel.Bind<IContentProvider>().To<SoundCloudContentProvider>();
            kernel.Bind<IContentProvider>().To<SpotifyContentProvider>();
            kernel.Bind<IContentProvider>().To<UserVoiceContentProvider>();
            kernel.Bind<IContentProvider>().To<UStreamContentProvider>();
            kernel.Bind<IContentProvider>().To<YouTubeContentProvider>();
            kernel.Bind<IContentProvider>().To<ConfiguredContentProvider>();
            kernel.Bind<IContentProvider>().To<XkcdContentProvider>();
            kernel.Bind<IContentProvider>().To<UrbanDictionaryContentProvider>();
        }
    }
}