using LogicReinc.Asp.Authentication;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Services
{
    public interface IRouteManagerService
    {
        List<RouteMeta> GetRoutes();
    }

    /// <summary>
    /// Handles API Endpoint extraction for Sync
    /// </summary>
    public class RouteManagerService
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

        public RouteManagerService(IActionDescriptorCollectionProvider ap)
        {
            _actionDescriptorProvider = ap;
        }

        public List<RouteMeta> GetRoutes()
        {
            List<RouteMeta> metas = new List<RouteMeta>();

            foreach(ActionDescriptor desc in _actionDescriptorProvider.ActionDescriptors.Items)
            {
                RouteMeta meta = new RouteMeta();

                // Path and Invocation of Controller/Action
                if (desc is ControllerActionDescriptor)
                {
                    var e = desc as ControllerActionDescriptor;

                    meta.ControllerType = e.ControllerTypeInfo;
                    meta.ActionMethod = e.MethodInfo;
                    meta.ControllerName = e.ControllerName;
                    meta.ActionName = e.ActionName;

                    AuthorizeAttribute attr = e.MethodInfo.GetCustomAttribute<AuthorizeAttribute>();
                    if (attr != null)
                    {
                        meta.RequiresAuthentication = true;
                        meta.Roles = attr.RequiredRoles;
                    }

                    if (meta.Path == "")
                        meta.Path = $"/{e.ControllerName}/{e.ActionName}";
                }

                // Extract HTTP Verb
                if (desc.ActionConstraints != null && desc.ActionConstraints.Select(t => t.GetType()).Contains(typeof(HttpMethodActionConstraint)))
                {
                    HttpMethodActionConstraint httpMethodAction =
                        desc.ActionConstraints.FirstOrDefault(a => a.GetType() == typeof(HttpMethodActionConstraint)) as HttpMethodActionConstraint;

                    if (httpMethodAction != null)
                        meta.Method = string.Join(",", httpMethodAction.HttpMethods);
                }

                // Generating List
                metas.Add(meta);
            }

            return metas;
        }
    }

    /// <summary>
    /// Model for Endpoints
    /// </summary>
    public class RouteMeta
    {
        public Type ControllerType { get; set; }
        public MethodInfo ActionMethod { get; set; }

        public string ControllerName { get; set; }
        public string ActionName { get; set; }

        public string Method { get; set; }

        public bool RequiresAuthentication { get; set; }
        public string[] Roles { get; set; }

        public string Path { get; set; }

        public bool CanUse(bool auth, string[] roles)
        {
            if (RequiresAuthentication && !auth)
                return false;
            if(Roles != null && Roles.Length > 0 && roles == null)
                return !Roles.Any(x => !roles.Contains(x));
            return true;
        }
    }
}
