using LogicReinc.Asp.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LogicReinc.Asp
{
    public static class Extensions
    {
        public static AuthenticationService.AuthUser GetAuthentication(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey("Authentication"))
                return null;
            return (AuthenticationService.AuthUser)request.HttpContext.Items["Authentication"];
        }
        public static T GetAuthenticationUser<T>(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey("Authentication"))
                return default(T);
            return (T)((AuthenticationService.AuthUser)request.HttpContext.Items["Authentication"]).UserObject;
        }
        public static T GetAuthContext<T>(this ControllerBase controller)
        {
            if (!controller.Request.HttpContext.Items.ContainsKey("Authentication"))
                return default(T);
            return (T)((AuthenticationService.AuthUser)controller.Request.HttpContext.Items["Authentication"]).UserObject;
        }

        public static string GetResourceText(this Assembly asm, string path)
        {
            using (Stream stream = asm.GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
