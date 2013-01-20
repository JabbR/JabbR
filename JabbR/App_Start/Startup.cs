using System.Net.Http.Formatting;
using System.Web.Http;
using JabbR.Auth;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ninject;
using Owin;

namespace JabbR
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var kernel = new StandardKernel();

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            // We're doing this manually since we want the chat repository to be shared
            // between the chat service and the chat hub itself
            kernel.Bind<Chat>()
                  .ToMethod(context =>
                  {
                      var settings = context.Kernel.Get<IApplicationSettings>();
                      var resourceProcessor = context.Kernel.Get<IResourceProcessor>();
                      var repository = context.Kernel.Get<IJabbrRepository>();
                      var cache = context.Kernel.Get<ICache>();
                      var crypto = context.Kernel.Get<ICryptoService>();

                      var service = new ChatService(cache, repository, crypto);

                      return new Chat(settings,
                                      resourceProcessor,
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
                  .To<ApplicationSettings>()
                  .InSingletonScope();

            kernel.Bind<IJavaScriptMinifier>()
                  .To<AjaxMinMinifier>()
                  .InSingletonScope();

            kernel.Bind<ICache>()
                  .To<AspNetCache>()
                  .InSingletonScope();

            var serializer = new JsonNetSerializer(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            });

            kernel.Bind<IJsonSerializer>()
                  .ToConstant(serializer);

            app.UseShowExceptions();

            SetupSignalR(kernel, app);
            SetupWebApi(kernel, app);

            app.UseStaticFiles("/", ".");

            app.Use(typeof(LoginHandler));
            app.Use(typeof(ProxyHandler));

            // Perform the required migrations
            DoMigrations();

            SetupErrorHandling();
        }

        private static void SetupSignalR(StandardKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectSignalRDependencyResolver(kernel);

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
                name: "MessagesV1",
                routeTemplate: "api/v1/{controller}/{room}");

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api",
                defaults: new { controller = "ApiFrontPage" }
            );

            app.UseHttpServer(config);
        }
    }
}