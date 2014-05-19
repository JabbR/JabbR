using System;
using System.Configuration;
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
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;

using Nancy.Owin;

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
                DoMigrations(configuration);
            }

            var kernel = SetupNinject(configuration);

            app.Use(typeof(DetectSchemeHandler));

            if (configuration.RequireHttps)
            {
                app.Use(typeof(RequireHttpsHandler));
            }

            app.UseErrorPage();

            SetupAuth(app, kernel);
            SetupSignalR(configuration, kernel, app);
            SetupWebApi(kernel, app);
            SetupMiddleware(kernel, app);
            SetupFileUpload(kernel, app);
            SetupNancy(kernel, app);

            SetupErrorHandling();
        }

        private void SetupFileUpload(IKernel kernel, IAppBuilder app)
        {
            app.Map("/upload-file", map =>
            {
                var uploadHandler = kernel.Get<UploadCallbackHandler>();

                map.Run(async context =>
                {
                    if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 404;
                    }
                    else if (!context.Request.User.IsAuthenticated())
                    {
                        context.Response.StatusCode = 403;
                    }
                    else
                    {
                        var form = await context.Request.ReadFormAsync();

                        string roomName = form["room"];
                        string connectionId = form["connectionId"];
                        string file = form["file"];
                        string fileName = form["filename"];
                        string contentType = form["type"];

                        BinaryBlob binaryBlob = BinaryBlob.Parse(file);

                        if (String.IsNullOrEmpty(contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        var stream = new MemoryStream(binaryBlob.Data);

                        await uploadHandler.Upload(context.Request.User.GetUserId(),
                                                   connectionId,
                                                   roomName,
                                                   fileName,
                                                   contentType,
                                                   stream);
                    }
                });
            });
        }

        private static void SetupAuth(IAppBuilder app, IKernel kernel)
        {
            var ticketDataFormat = new TicketDataFormat(kernel.Get<IDataProtector>());

            var type = typeof(CookieAuthenticationOptions)
                .Assembly.GetType("Microsoft.Owin.Security.Cookies.CookieAuthenticationMiddleware");

            app.Use(type, app, new CookieAuthenticationOptions
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

            app.Use(typeof(CustomAuthHandler));

            app.Use(typeof(WindowsPrincipalHandler));
        }

        private static void SetupNancy(IKernel kernel, IAppBuilder app)
        {
            var bootstrapper = new JabbRNinjectNancyBootstrapper(kernel);
            app.UseNancy(new NancyOptions { Bootstrapper = bootstrapper });
        }

        private static void SetupMiddleware(IKernel kernel, IAppBuilder app)
        {
            app.UseStaticFiles();
        }

        private static void SetupSignalR(IJabbrConfiguration jabbrConfig, IKernel kernel, IAppBuilder app)
        {
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            var connectionManager = resolver.Resolve<IConnectionManager>();
            var heartbeat = resolver.Resolve<ITransportHeartbeat>();
            var hubPipeline = resolver.Resolve<IHubPipeline>();
            var configuration = resolver.Resolve<IConfigurationManager>();

            // Enable service bus scale out
            if (!String.IsNullOrEmpty(jabbrConfig.ServiceBusConnectionString) &&
                !String.IsNullOrEmpty(jabbrConfig.ServiceBusTopicPrefix))
            {
                var sbConfig = new ServiceBusScaleoutConfiguration(jabbrConfig.ServiceBusConnectionString,
                                                                   jabbrConfig.ServiceBusTopicPrefix)
                {
                    TopicCount = 5
                };

                resolver.UseServiceBus(sbConfig);
            }

            if (jabbrConfig.ScaleOutSqlServer)
            {
                resolver.UseSqlServer(jabbrConfig.SqlConnectionString.ConnectionString);
            }

            kernel.Bind<IConnectionManager>()
                  .ToConstant(connectionManager);

            // We need to extend this since the inital connect might take a while
            configuration.TransportConnectTimeout = TimeSpan.FromSeconds(30);

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