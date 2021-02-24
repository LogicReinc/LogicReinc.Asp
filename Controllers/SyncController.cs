using LogicReinc.Asp.Results;
using LogicReinc.Asp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        private List<SyncControllerDescriptor> _controllersDoc = null;
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
        public List<SyncControllerDescriptor> ControllersDocumented
        {
            get
            {
                if (_controllersDoc == null)
                {
                    lock (_lock)
                    {
                        if (_controllersDoc == null)
                            _controllersDoc = SyncControllerDescriptor.Create(Url, _routes, null, true);
                    }
                }
                return _controllersDoc;
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
        public SyncDescriptor Get(bool documentation = false)
        {
            AuthUser user = Request.GetAuthentication();
            List<SyncControllerDescriptor> descs = null;
            if (user != null)
            {
                descs = SyncControllerDescriptor.Create(Url, _routes, user.Roles ?? new string[] { }, documentation && (_server.EnableSyncDocumentation || _server.EnableSyncDocumentationAuthenticated));
            }
            else
                descs = (!(documentation && _server.EnableSyncDocumentation)) ? Controllers : ControllersDocumented;
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
        public JavascriptResult Config(bool documentation = false)
        {
            Response.ContentType = "application/javascript";
            return new JavascriptResult("var SYNC_CONFIG = " + JsonSerializer.Serialize(Get(documentation)));
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

        public static List<SyncControllerDescriptor> Create(IUrlHelper url, List<RouteMeta> metas, string[] roles = null, bool documentation = false)
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
                        new SyncRouteDescriptor(x, url.Action(x.ActionName, x.ControllerName), documentation)
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

        [JsonPropertyName("Documentation")]
        public DocumentationMember Documentation { get; set; }

        public SyncRouteDescriptor() { }
        public SyncRouteDescriptor(RouteMeta meta, string url, bool documentation = false)
        {
            Url = url;
            Name = meta.ActionName;
            Method = meta.Method;
            Arguments = meta.ActionMethod.GetParameters()
                .Select(x => x.Name).ToArray();
            ArgumentTypes = meta.ActionMethod.GetParameters()
                .Select(x => x.ParameterType.Name).ToArray();

            if (documentation)
            {
                DocumentationInfo info = DocumentationInfo.GetDocumentation(Assembly.GetEntryAssembly().GetName().Name);

                if (info != null) {
                    DocumentationMember member = info?.Members.FirstOrDefault(x => {
                        string name = (x.Key.IndexOf('(') > 0) ? x.Key.Substring(0, x.Key.IndexOf('(')) : x.Key;
                        return name == meta.ActionMethod.DeclaringType.FullName + "." + meta.ActionMethod.Name;
                    }).Value;

                    if (member != null && !member.Typed)
                        member.MakeTyped(meta.ActionMethod);

                    Documentation = member;
                }
            }
        }
    }

    /// <summary>
    /// Model for C# Documentation
    /// </summary>
    public class DocumentationInfo
    {
        private static Dictionary<string, DocumentationInfo> _docs = new Dictionary<string, DocumentationInfo>();

        public Dictionary<string, DocumentationMember> Members { get; set; } = new Dictionary<string, DocumentationMember>();

        public static DocumentationInfo Parse(string data)
        {
            XDocument doc = XDocument.Parse(data);

            return new DocumentationInfo()
            {
                Members = doc.Descendants("member")
                .Where(x=>x.Attribute("name") != null && (x.Attribute("name")?.Value?.StartsWith("M:") ?? false))
                .ToDictionary(x => x.Attribute("name").Value.Substring(2), y =>
                {
                     return new DocumentationMember()
                     {
                         Summary = y.Element("summary")?.Value?.Trim(),
                         Return = new DocumentationVariable(null, y.Element("returns")?.Value?.Trim()),
                         Parameters = y.Descendants("param")
                            .Where(x => x.Attribute("name")?.Value != null && x?.Value != null)
                            .ToDictionary(x => x.Attribute("name").Value, y => new DocumentationVariable(null, y?.Value?.Trim()))
                     };
                })
            };
        }
        public static DocumentationInfo GetDocumentation(string assemblyName)
        {
            if(!_docs.ContainsKey(assemblyName))
            {
                string fileName = assemblyName + ".xml";
                if (File.Exists(fileName))
                {
                    try
                    {
                        _docs.Add(assemblyName, Parse(File.ReadAllText(fileName)));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Documentation Parse Exception for [{assemblyName}]:" + ex.Message);
                        _docs.Add(assemblyName, null);
                    }
                }
                else
                    _docs.Add(assemblyName, null);
            }
            return _docs[assemblyName];
        }
    }
    /// <summary>
    /// Model for C# Member Documentation
    /// </summary>
    public class DocumentationMember
    {
        public string Summary { get; set; }
        public DocumentationVariable Return { get; set; }
        public Dictionary<string, DocumentationVariable> Parameters { get; set; } = new Dictionary<string, DocumentationVariable>();

        public bool Typed => (Return == null || Return.Type != null) && !Parameters.Any(x => x.Value.Type == null);

        public void MakeTyped(MethodInfo info)
        {
            if(Return != null)
                Return.Type = info.ReturnType.Name;
            if(Parameters != null)
            {
                ParameterInfo[] infos = info.GetParameters();
                foreach(var kv in Parameters)
                {
                    ParameterInfo para = infos.FirstOrDefault(x => x.Name == kv.Key);
                    kv.Value.Type = (para != null) ? para.ParameterType.Name : "Unknown";
                }
            }
        }
    }
    /// <summary>
    /// Model for C# Variable Documentation
    /// </summary>
    public class DocumentationVariable
    {
        public string Summary { get; set; }
        public string Type { get; set; }

        public DocumentationVariable() { }
        public DocumentationVariable(string type, string description)
        {
            Summary = description;
            Type = type;
        }
    }
}
