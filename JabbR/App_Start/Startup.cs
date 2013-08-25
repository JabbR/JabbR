using System;
using System.IO;
using System.Net.Http.Formatting;
using System.Web.Http;
using JabbR;
using JabbR.Hubs;
using JabbR.Infrastructure;
using JabbR.Middleware;
using JabbR.Nancy;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Newtonsoft.Json.Serialization;
using Ninject;
using Owin;

[assembly: OwinStartup(typeof(Startup), "Configuration")]

namespace JabbR
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // So that squishit works
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);

            var configuration = new JabbrConfiguration();

            if (configuration.MigrateDatabase)
            {
                // Perform the required migrations
                DoMigrations();
            }

            var kernel = SetupNinject(configuration);

            app.Use(typeof(DetectSchemeHandler));

            if (configuration.RequireHttps)
            {
                app.Use(typeof(RequireHttpsHandler));
            }

            app.UseErrorPage();

            SetupAuth(app, kernel);
            SetupSignalR(kernel, app);
            SetupWebApi(kernel, app);
            SetupMiddleware(kernel, app);
            SetupNancy(kernel, app);

            SetupErrorHandling();
        }

        private static void SetupAuth(IAppBuilder app, IKernel kernel)
        {
            var ticketDataFormat = new TicketDataFormat(kernel.Get<IDataProtector>());

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/account/login"),
                LogoutPath = new PathString("/account/logout"),
                CookieHttpOnly = true,
                AuthenticationType = Constants.JabbRAuthType,
                CookieName = "jabbr.id",
                ExpireTimeSpan = TimeSpan.FromDays(30),
                TicketDataFormat = ticketDataFormat,
                Provider = kernel.Get<ICookieAuthenticationProvider>()
            });

            app.Use(typeof(WindowsPrincipalHandler));

            app.UseStageMarker(PipelineStage.Authenticate);

            //var config = new FederationConfiguration(loadConfig: false);
            //config.WsFederationConfiguration.Issuer = "";
            //config.WsFederationConfiguration.Realm = "http://localhost:16207/";
            //config.WsFederationConfiguration.Reply = "http://localhost:16207/wsfederation";
            //var cbi = new ConfigurationBasedIssuerNameRegistry();
            //cbi.AddTrustedIssuer("", "");
            //config.IdentityConfiguration.AudienceRestriction.AllowedAudienceUris.Add(new Uri("http://localhost:16207/"));
            //config.IdentityConfiguration.IssuerNameRegistry = cbi;
            //config.IdentityConfiguration.CertificateValidationMode = X509CertificateValidationMode.None;
            //config.IdentityConfiguration.CertificateValidator = X509CertificateValidator.None;

            //app.UseFederationAuthentication(new FederationAuthenticationOptions
            //{
            //    ReturnPath = "/wsfederation",
            //    SigninAsAuthenticationType = Constants.JabbRAuthType,
            //    FederationConfiguration = config,
            //    Provider = new FederationAuthenticationProvider()
            //});
        }

        private static void SetupNancy(IKernel kernel, IAppBuilder app)
        {
            var bootstrapper = new JabbRNinjectNancyBootstrapper(kernel);
            app.UseNancy(bootstrapper);
        }

        private static void SetupMiddleware(IKernel kernel, IAppBuilder app)
        {
            app.UseStaticFiles();
        }

        private static void SetupSignalR(IKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            var connectionManager = resolver.Resolve<IConnectionManager>();
            var heartbeat = resolver.Resolve<ITransportHeartbeat>();
            var hubPipeline = resolver.Resolve<IHubPipeline>();

            kernel.Bind<IConnectionManager>()
                  .ToConstant(connectionManager);

            var config = new HubConfiguration
            {
                Resolver = resolver
            };

            hubPipeline.AddModule(kernel.Get<LoggingHubPipelineModule>());

            app.MapSignalR(config);

            var monitor = new PresenceMonitor(kernel, connectionManager, heartbeat);
            monitor.Start();
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