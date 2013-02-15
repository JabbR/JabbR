using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Middleware;
using JabbR.Models;
using JabbR.Nancy;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.Mapping;
using Microsoft.Owin.StaticFiles;
using Nancy.Authentication.WorldDomination;
using Nancy.Bootstrappers.Ninject;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ninject;
using Owin;
using WorldDomination.Web.Authentication;

namespace JabbR
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var settings = new ApplicationSettings();

            if (settings.MigrateDatabase)
            {
                // Perform the required migrations
                DoMigrations();
            }

            var kernel = SetupNinject(settings);

            app.Use(typeof(DetectSchemeHandler));

            if (settings.RequireHttps)
            {
                app.Use(typeof(RequireHttpsHandler));
            }

            app.UseShowExceptions();

            // This needs to run before everything
            app.Use(typeof(AuthorizationHandler), kernel.Get<IAuthenticationTokenService>());

            SetupSignalR(kernel, app);
            SetupWebApi(kernel, app);
            SetupMiddleware(app);
            SetupNancy(kernel, app);

            SetupErrorHandling();
        }

        private static KernelBase SetupNinject(ApplicationSettings settings)
        {
            var kernel = new StandardKernel(new[] { new FactoryModule() });

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            kernel.Bind<IAuthenticationTokenService>()
                  .To<AuthenticationTokenService>();

            // We're doing this manually since we want the chat repository to be shared
            // between the chat service and the chat hub itself
            kernel.Bind<Chat>()
                  .ToMethod(context =>
                  {
                      var resourceProcessor = context.Kernel.Get<IResourceProcessor>();
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
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            });

            kernel.Bind<IJsonSerializer>()
                  .ToConstant(serializer);

            return kernel;
        }

        private static void SetupNancy(IKernel kernel, IAppBuilder app)
        {
            var bootstrapper = new JabbRNinjectNancyBootstrapper(kernel);
            app.UseNancy(bootstrapper);
        }

        private static void SetupMiddleware(IAppBuilder app)
        {
            app.MapPath("/proxy", subApp => subApp.Use(typeof(ImageProxyHandler)));
            app.UseStaticFiles();
        }

        private static void SetupSignalR(IKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            var connectionManager = resolver.Resolve<IConnectionManager>();

            kernel.Bind<IConnectionManager>()
                  .ToConstant(connectionManager);

            var config = new HubConfiguration
            {
                Resolver = resolver,
                EnableDetailedErrors = true
            };

            app.MapHubs(config);

            StartBackgroundWork(kernel, resolver);
        }

        private static void SetupWebApi(IKernel kernel, IAppBuilder app)
        {
            var config = new HttpConfiguration();
            var jsonFormatter = new JsonMediaTypeFormatter();

            config.Formatters.Clear();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.Add(jsonFormatter);
            config.DependencyResolver = new NinjectWebApiDependencyResolver(kernel);


            config.Routes.MapHttpRoute(
                name: "LoginV1",
                routeTemplate: "api/v1/authenticate",
                defaults: new { controller = "Authenticate" }
             );

            config.Routes.MapHttpRoute(
                name: "MessagesV1",
                routeTemplate: "api/v1/{controller}/{room}"
            );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api",
                defaults: new { controller = "ApiFrontPage" }
            );

            app.UseHttpServer(config);
        }
    }
}