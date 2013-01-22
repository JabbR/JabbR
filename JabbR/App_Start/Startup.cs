using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Middleware;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.Razor.Owin.IO;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin.Mapping;
using Microsoft.Owin.StaticFiles;
using Nancy.Bootstrappers.Ninject;
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
            var kernel = new StandardKernel(new[] { new FactoryModule() });

            kernel.Bind<JabbrContext>()
                .To<JabbrContext>();

            kernel.Bind<IJabbrRepository>()
                .To<PersistedRepository>();

            kernel.Bind<IChatService>()
                  .To<ChatService>();

            kernel.Bind<IAuthenticationService>()
                  .To<AuthenticationService>();

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
                  .To<ApplicationSettings>()
                  .InSingletonScope();

            kernel.Bind<IJavaScriptMinifier>()
                  .To<AjaxMinMinifier>()
                  .InSingletonScope();

            kernel.Bind<IMembershipService>()
                  .To<MembershipService>();

            try
            {
                if (app.IsRunningUnderSystemWeb())
                {
                    BindSystemWebDependencies(kernel);
                }
                else
                {
                    kernel.Bind<ICache>()
                          .To<DefaultCache>()
                          .InSingletonScope();
                }
            }
            catch (Exception ex)
            {
                // If we were unable to load the system web specific dependencies don't cry about it
                ReportError(ex);
            }

            var serializer = new JsonNetSerializer(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            });

            kernel.Bind<IJsonSerializer>()
                  .ToConstant(serializer);


            var settings = kernel.Get<IApplicationSettings>();
            if (settings.RequireHttps)
            {
                app.Use(typeof(RequireHttpsHandler));
            }

            app.UseShowExceptions();

            // This needs to run before everything
            app.Use(typeof(AuthorizationHandler), kernel);

            SetupSignalR(kernel, app);
            SetupWebApi(kernel, app);
            SetupNancy(kernel, app);
            SetupMiddleware(app);

            // Perform the required migrations
            DoMigrations();

            SetupErrorHandling();
        }

        private static void SetupNancy(IKernel kernel, IAppBuilder app)
        {
            var bootstrapper = new JabbRNinjectNancyBootstrapper(kernel);
            app.MapPath("/auth", subApp => subApp.UseNancy(bootstrapper));
            app.MapPath("/proxy", subApp => subApp.Use(typeof(ImageProxyHandler)));
        }

        private static void SetupMiddleware(IAppBuilder app)
        {
            app.UseStaticFiles("/", ".");
            app.UseRazor(new PhysicalFileSystem(Environment.CurrentDirectory), new AssemblyReferenceLocator(), "/");
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