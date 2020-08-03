using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LogicReinc.Asp.Authentication.AuthenticationService;

namespace LogicReinc.Asp.Authentication
{

    /// <summary>
    /// Used to specify authentication requirement for methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string[] RequiredRoles { get; set; }

        public AuthorizeAttribute(params string[] roles)
        {
            if (roles == null)
                roles = new string[0];
            RequiredRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            AuthUser user = (AuthUser)context.HttpContext.Items["Authentication"];
            if (user == null)
            {
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            string[] roles = user.Roles;

            if (RequiredRoles.Any(x => !roles.Contains(x)))
            {
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
    }
}
