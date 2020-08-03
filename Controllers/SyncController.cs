using LogicReinc.Asp.Results;
using LogicReinc.Asp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static LogicReinc.Asp.AspServer;
using static LogicReinc.Asp.Authentication.AuthenticationService;

namespace LogicReinc.Asp.Controllers
{
    /// <summary>
    /// The controller used when EnabledSync is enabled in AspServer
    /// Provides a javascript binding for the API
    /// </summary>
    [Route("[controller]/[action]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        object _lock = new object();

        private AspServer _server = null;
        private List<RouteMeta> _routes = new List<RouteMeta>();
        private static JavascriptResult _script = null;

        private List<SyncControllerDescriptor> _controllers = null;
        public List<SyncControllerDescriptor> Controllers
        {
            get
            {
                if(_controllers == null)
                {
                    lock (_lock)
                    {
                        if(_controllers == null)
                            _controllers = SyncControllerDescriptor.Create(Url, _routes);
                    }
                }
                return _controllers;
            }
        }

        public SyncController(AspServer server, RouteManagerService routeService)
        {
            _server = server;
            _routes = routeService.GetRoutes();
            _script = new JavascriptResult(GetType().Assembly.GetResourceText("LogicReinc.Asp.Scripts.Sync.js"));
        }

        /// <summary>
        /// Returns the Sync base script
        /// </summary>
        [HttpGet]
        public JavascriptResult Script()
        {
            return _script;
        }

        /// <summary>
        /// Returns the configuration containing endpoints (given authentication level)
        /// </summary>
        [HttpGet]
        public SyncDescriptor Get()
        {
            AuthUser user = Request.GetAuthentication();
            List<SyncControllerDescriptor> descs = null;
            if (user != null)
            {
                descs = SyncControllerDescriptor.Create(Url, _routes, user.Roles ?? new string[] { });
            }
            else
                descs = Controllers;
            var result = new SyncDescriptor()
            {
                Authenticated = user != null,
                Controllers = descs,
                WebSockets = _server.RegisteredWebSockets
                    .Where(x => x.CanUse(user != null, user?.Roles ?? new string[] { }))
                    .Select(x=>new SyncWebSocketDescriptor(x))
                    .ToList()
            };
            return result;
        }
        /// <summary>
        /// Returns the configuration containing endpoints (given authentication level) in javascript form.
        /// </summary>
        [HttpGet]
        public JavascriptResult Config()
        {
            Response.ContentType = "application/javascript";
            return new JavascriptResult("var SYNC_CONFIG = " + JsonSerializer.Serialize(Get()));
        }

    }

    public class SyncDescriptor
    {
        [JsonPropertyName("Authenticated")]
        public bool Authenticated { get; set; }

        [JsonPropertyName("Controllers")]
        public List<SyncControllerDescriptor> Controllers { get; set; }

        [JsonPropertyName("WebSockets")]
        public List<SyncWebSocketDescriptor> WebSockets { get; set; }
    }
    /// <summary>
    /// Model for WebSockets
    /// </summary>
    public class SyncWebSocketDescriptor
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Url")]
        public string Url { get; set; }

        public SyncWebSocketDescriptor() { }
        public SyncWebSocketDescriptor(WebSocketDescriptor desc)
        {
            Name = desc.Name;
            Url = desc.Path;
        }
    }
    /// <summary>
    /// Model for Controllers
    /// </summary>
    public class SyncControllerDescriptor
    {
        [JsonPropertyName("ControllerName")]
        public string ControllerName { get; set; }

        [JsonPropertyName("Actions")]
        public List<SyncRouteDescriptor> Actions { get; set; } = new List<SyncRouteDescriptor>();

        public SyncControllerDescriptor() { }
        public SyncControllerDescriptor(RouteMeta meta)
        {
            ControllerName = meta.ControllerName;
        }
        public SyncControllerDescriptor(RouteMeta meta, List<SyncRouteDescriptor> routes)
        {
            ControllerName = meta.ControllerName;
            Actions = routes;
        }

        public static List<SyncControllerDescriptor> Create(IUrlHelper url, List<RouteMeta> metas, string[] roles = null)
        {
            return metas
                .Where(x => x.ControllerName != null)
                .GroupBy(x => x.ControllerName)
                .Select(x =>
                {
                    return new SyncControllerDescriptor(x.FirstOrDefault(),
                    x
                    .Where(x=>x.CanUse(roles != null, roles))
                    .Select(x => 
                        new SyncRouteDescriptor(x, url.Action(x.ActionName, x.ControllerName))
                    ).ToList());
                }).ToList();
        }
    }
    /// <summary>
    /// Model for Controller actions
    /// </summary>
    public class SyncRouteDescriptor
    {
        [JsonPropertyName("Method")]
        public string Method { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Url")]
        public string Url { get; set; }
        [JsonPropertyName("Arguments")]
        public string[] Arguments { get; set; }
        [JsonPropertyName("ArgumentTypes")]
        public string[] ArgumentTypes { get; set; }


        public SyncRouteDescriptor() { }
        public SyncRouteDescriptor(RouteMeta meta, string url)
        {
            Url = url;
            Name = meta.ActionName;
            Method = meta.Method;
            Arguments = meta.ActionMethod.GetParameters()
                .Select(x => x.Name).ToArray();
            ArgumentTypes = meta.ActionMethod.GetParameters()
                .Select(x => x.ParameterType.Name).ToArray();
        }
    }
}
