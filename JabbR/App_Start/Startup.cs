using System.Net.Http.Formatting;
using System.Web.Http;
using JabbR.Infrastructure;
using JabbR.Middleware;
using JabbR.Nancy;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Microsoft.Owin.Mapping;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json.Serialization;
using Ninject;
using Owin;

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
            SetupMiddleware(settings, app);
            SetupNancy(kernel, app);

            SetupErrorHandling();
        }

        private static void SetupNancy(IKernel kernel, IAppBuilder app)
        {
            var bootstrapper = new JabbRNinjectNancyBootstrapper(kernel);
            app.UseNancy(bootstrapper);
        }

        private static void SetupMiddleware(IApplicationSettings settings, IAppBuilder app)
        {
            if (settings.ProxyImages)
            {
                app.MapPath("/proxy", subApp => subApp.Use(typeof(ImageProxyHandler), settings));
            }

            app.UseStaticFiles();
        }

        private static void SetupSignalR(IKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            var connectionManager = resolver.Resolve<IConnectionManager>();

            // Ah well loading system web.
            kernel.Bind<IProtectedData>()
                  .To<MachineKeyProtectedData>();

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

            app.UseWebApi(config);
        }
    }
}