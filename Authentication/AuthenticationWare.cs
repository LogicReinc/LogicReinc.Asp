using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Authentication
{
    /// <summary>
    /// Handles setting authentication state for requests
    /// </summary>
    internal class AuthenticationWare
    {
        private AuthenticationService _service = null;
        private readonly RequestDelegate _next;
        public AuthenticationWare(RequestDelegate next, AspServer server)
        {
            _next = next;
            _service = server.GetAuthenticationService();
            if (_service == null)
                throw new InvalidOperationException("Authentication service not set for middleware");
        }

        public static void HandleAuthentication(AuthenticationService service, HttpContext context)
        {

            string auth = null;
            //Check for authentication token
            if (context.Request.Headers.ContainsKey("auth"))
                auth = context.Request.Headers["auth"];
            else if (context.Request.Cookies.ContainsKey("auth"))
                auth = context.Request.Cookies["auth"];
            else if (context.Request.Headers.ContainsKey("Authorization"))
                auth = context.Request.Headers["Authorization"];
            else if (context.Request.Headers.ContainsKey("Sec-WebSocket-Protocol"))
            {
                string websocketProtocols = context.Request.Headers["Sec-WebSocket-Protocol"];
                string authProtocol = websocketProtocols?.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => x.StartsWith("auth_"))
                    .FirstOrDefault()?.Substring("auth_".Length);
                if (!string.IsNullOrEmpty(authProtocol))
                {
                    auth = authProtocol;
                    List<string> existing = new List<string>();
                    if (context.Response.Headers.ContainsKey("Sec-WebSocket-Protocol"))
                        existing = context.Response.Headers["Sec-WebSocket-Protocol"].ToList();
                    existing.Add("auth_" + authProtocol);
                    context.Response.Headers.Add("Sec-WebSocket-Protocol", new StringValues(existing.ToArray()));
                }
            }

            //If found, set authentication
            if (auth != null)
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var claims = tokenHandler.ValidateToken(auth, new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(service.Secret),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken token);
                    var jwtToken = (JwtSecurityToken)token;
                    var userId = jwtToken.Claims.First(x => x.Type == "id");

                    context.Items["Authentication"] = service.GetAuthentication(service.GetUser(userId.Value));
                }
                catch (Exception ex)
                {
                    auth = null;
                }
            }
        }

        public async Task Invoke(HttpContext context)
        {
            HandleAuthentication(_service, context);
            await _next(context);
        }
    }
}
