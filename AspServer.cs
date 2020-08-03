using BlazorApp.Server;
using LogicReinc.Asp.Arguments;
using LogicReinc.Asp.Authentication;
using LogicReinc.Asp.Controllers;
using LogicReinc.Asp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using static LogicReinc.Asp.Authentication.AuthenticationService;

namespace LogicReinc.Asp
{
    /// <summary>
    /// Main class wrapping around the ASP.Net Core framework.
    /// </summary>
    public class AspServer
    {
        private IHost host = null;
        private List<(string, Func<HttpContext, Task>)> _endpoints = new List<(string, Func<HttpContext, Task>)>();
        private AuthenticationService _authService = null;
        protected List<Assembly> _assemblies = new List<Assembly>();
        private Dictionary<string, List<WebSocketClient>> _wsClients = new Dictionary<string, List<WebSocketClient>>();
        
        public List<DirectoryDescriptor> RegisteredDirectories { get; private set; } = new List<DirectoryDescriptor>();
        public List<Type> RegisteredControllers { get; private set; } = new List<Type>();
        public List<WebSocketDescriptor> RegisteredWebSockets { get; private set; } = new List<WebSocketDescriptor>();

        public string[] Urls { get; private set; }

        /// <summary>
        /// Enabling this will add the SyncController on start, providing javascript bindings for your api
        /// </summary>
        public bool EnabledSync { get; set; } = true;
        /// <summary>
        /// Enabling this will add the AuthenticationController on start as well as required middleware, providing authentication for your api
        /// Use SetAuthentication to enable
        /// </summary>
        public bool EnabledAuthentication { get; private set; } = false;
        /// <summary>
        /// Enabling this will enable Controller Discovery, meaning it will automatically add your controllers from assemblies (That are added)
        /// </summary>
        public bool EnabledControllerDiscovery { get; set; } = true;

        public AspServer(int port)
        {
            Urls = new string[] { $"http://0.0.0.0:{port}/" };
            Init();
        }
        public AspServer(string[] urls)
        {
            Urls = urls;
            Init();
        }

        private void Init()
        {
        }

        /// <summary>
        /// Add a static directory of files to given endpoint
        /// </summary>
        /// <param name="url"></param>
        /// <param name="dir"></param>
        public void AddStaticDirectory(string url, string dir)
        {
            RegisteredDirectories.Add(new DirectoryDescriptor()
            {
                Url = url,
                Directory = Path.GetFullPath(dir)
            });
        }

        /// <summary>
        /// Adds assemblies used for auto-discovery
        /// </summary>
        public void AddAssemblies(params Assembly[] assemblies)
        {
            _assemblies.AddRange(assemblies);
        }

        /// <summary>
        /// Add endpoint to Action
        /// </summary>
        public void AddEndpoint(string pattern, Action<HttpContext> handler)
        {
            AddEndpoint(pattern, (x) => Task.Run(()=>handler(x)));
        }
        /// <summary>
        /// Add endpoint to Async action
        /// </summary>
        public void AddEndpoint(string pattern, Func<HttpContext, Task> handler)
        {
            _endpoints.Add((pattern, handler));
        }

        /// <summary>
        /// Registers a controller type
        /// </summary>
        public void AddController(Type controller)
        {
            RegisteredControllers.Add(controller);
        }

        /// <summary>
        /// Register a websocket with a given group name for clients
        /// </summary>
        public void AddWebSocket<T>(string pattern, string name)
        {
            RegisteredWebSockets.Add(new WebSocketDescriptor()
            {
                Type = typeof(T),
                Name = name,
                Path = new PathString(pattern),
                Authenticated = false,
                Roles = new string[] { }
            });
            _wsClients.Add(name, new List<WebSocketClient>());
        }

        /// <summary>
        /// Register a websocket with a given group name for clients with authentication
        /// </summary>
        public void AddWebSocketAuthenticated<T>(string pattern, string name, params string[] roles)
        {
            if (!EnabledAuthentication || _authService == null)
                throw new InvalidOperationException("Authentication not enabled");

            RegisteredWebSockets.Add(new WebSocketDescriptor()
            {
                Type = typeof(T),
                Name = name,
                Path = new PathString(pattern),
                Authenticated = true,
                Roles = roles
            });
            _wsClients.Add(name, new List<WebSocketClient>());
        }

        public List<WebSocketClient> GetWebSocketClients(string name)
        {
            if (!_wsClients.ContainsKey(name))
                throw new ArgumentException($"WebSocket {name} does not exist");
            return _wsClients[name].ToList();
        }

        /// <summary>
        /// Sets an AuthenticationService and enable Authentication
        /// </summary>
        public void SetAuthentication(AuthenticationService service)
        {
            _authService = service;
            EnabledAuthentication = true;
        }

        public async Task Start()
        {
            host = CreateHostBuilder(new string[] { })
                .Build();
            await host.StartAsync();
        }

        public async Task Stop()
        {
            await host.StopAsync();
        }


        //Internal
        internal AuthenticationService GetAuthenticationService()
        {
            return _authService;
        }
        internal void RemoveWebSocketClient(string name, WebSocketClient client)
        {
            if (_wsClients.ContainsKey(name))
                _wsClients[name].Remove(client);
        }


        //Configure (Internal)
        protected void ConfigureServices(IServiceCollection s)
        {
            var c = s.AddControllersWithViews()
                .ConfigureApplicationPartManager((apm) =>
                {
                    apm.FeatureProviders.Add(new FeatureProvider(this));
                });
            foreach (Assembly a in _assemblies)
                c.AddApplicationPart(a);

            s.AddSingleton(new AspServerByPass(Configure));
            s.AddSingleton<RouteManagerService>();
            s.AddSingleton(this);
            

            s.AddRazorPages();

            
        }
        protected void Configure(IApplicationBuilder app, IHostApplicationLifetime cycle)
        {
            //app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();

            foreach(DirectoryDescriptor stat in RegisteredDirectories)
                app.UseStaticFiles(new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(stat.Directory),
                    RequestPath = stat.Url
                });
            
            app.UseRouting();
            
            if(EnabledAuthentication)
                app.UseMiddleware<AuthenticationWare>();

            //Only enable WebSocket if needed
            if (RegisteredWebSockets.Count > 0)
            {
                app.UseWebSockets();

                app.Use(async (context, n) =>
                {
                    foreach (var ws in RegisteredWebSockets)
                    {
                        if (context.Request.Path.StartsWithSegments(ws.Path))
                        {
                            if (!context.WebSockets.IsWebSocketRequest)
                            {
                                context.Response.StatusCode = 400;
                                return;
                            }
                            else
                            {
                                //Do Authentication for WebSocket
                                if (ws.Authenticated)
                                {

                                    AuthUser user = (AuthUser)context.Items["Authentication"];
                                    if (user == null)
                                    {
                                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                        return;
                                    }

                                    string[] roles = user.Roles;
                                    if (ws.Roles.Any(x => !roles.Contains(x)))
                                    {
                                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                        return;
                                    }
                                }

                                //Accept and Handle WebSocket
                                WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                                WebSocketClient client = (WebSocketClient)Activator.CreateInstance(ws.Type, new object[] { ws.Name, this, context, socket });
                                if (_wsClients.ContainsKey(client.Name))
                                {
                                    _wsClients[client.Name].Add(client);
                                    await client.Handle();
                                }
                                else
                                    await client.Socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Group does not exist", CancellationToken.None);
                            }
                            return;
                        }
                    }
                    await n();
                });
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();

                if(EnabledControllerDiscovery)
                    endpoints.MapControllers();

                endpoints.MapFallbackToFile("index.html");

                foreach (var endpoint in _endpoints)
                    endpoints.Map(endpoint.Item1, new RequestDelegate(endpoint.Item2));
            });
        }

        protected virtual IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(s =>
                {
                    ConfigureServices(s);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(Urls);
                    webBuilder.UseStartup<Startup>();
                });




        //Bypass
        internal class FeatureProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            AspServer _server = null;
            public FeatureProvider(AspServer server)
            {
                _server = server;
            }
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                foreach (var controller in _server.RegisteredControllers)
                    feature.Controllers.Add(controller.GetTypeInfo());

                if (_server.EnabledSync)
                    feature.Controllers.Add(typeof(SyncController).GetTypeInfo());
                if (_server.EnabledAuthentication)
                    feature.Controllers.Add(typeof(AuthenticationController).GetTypeInfo());
            }
        }

        public class WebSocketDescriptor
        {
            public Type Type { get; set; }
            public PathString Path { get; set; }
            public string Name { get; set; }
            public bool Authenticated { get; set; }
            public string[] Roles { get; set; }


            public bool CanUse(bool auth, string[] roles)
            {
                if (Authenticated && !auth)
                    return false;
                if (Roles != null && Roles.Length > 0 && roles == null)
                    return !Roles.Any(x => !roles.Contains(x));
                return true;
            }
        }

        public class DirectoryDescriptor
        {
            public string Url { get; set; }
            public string Directory { get; set; }
        }
    }
}
