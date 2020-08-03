using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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

        

        public async Task Invoke(HttpContext context)
        {

            string auth = null;
            //Check for authentication token
            if (context.Request.Headers.ContainsKey("auth"))
                auth = context.Request.Headers["auth"];
            else if (context.Request.Cookies.ContainsKey("auth"))
                auth = context.Request.Cookies["auth"];

            //If found, set authentication
            if(auth != null)
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var claims = tokenHandler.ValidateToken(auth, new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(_service.Secret),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken token);
                    var jwtToken = (JwtSecurityToken)token;
                    var userId = jwtToken.Claims.First(x => x.Type == "id");

                    context.Items["Authentication"] = _service.GetAuthentication(_service.GetUser(userId.Value));
                }
                catch(Exception ex)
                {
                    auth = null;
                }
            }
            await _next(context);
        }
    }
}
