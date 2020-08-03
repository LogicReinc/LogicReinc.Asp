using LogicReinc.Asp.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Controllers
{
    /// <summary>
    /// The controller used when EnabledAuthentication is true in AspServer
    /// 
    /// </summary>
    [Route("[controller]/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private AspServer _server = null;

        private AuthenticationService _service = null;

        public AuthenticationController(AspServer server)
        {
            _server = server;
            _service = server.GetAuthenticationService();
            if (_service == null)
                throw new InvalidOperationException("No Authentication Service set");
        }

        /// <summary>
        /// Logs in with given user using the configured AuthenticationService, This overload returns the authentication as a HTTP-ONLY Cookie
        /// </summary>
        [HttpPost]
        public IActionResult LoginSession([FromBody]AuthenticationRequest user)
        {
            object userObj = _service.Authenticate(user.User, user.Password);
            if (userObj == null)
                return Unauthorized();
            string id = _service.GetUserID(userObj);

            Response.Cookies.Append("auth", generateJwtToken(id), new Microsoft.AspNetCore.Http.CookieOptions()
            {
                HttpOnly = true,
                Expires = DateTimeOffset.Now.Add(_service.Expires)
            });

            return new JsonResult(true);
        }

        /// <summary>
        /// Logs in with given user using the configured AuthenticationService, This overload returns the authentication as a json object.
        /// </summary>
        [HttpPost]
        public IActionResult Login([FromBody] AuthenticationRequest user)
        {
            object userObj = _service.Authenticate(user.User, user.Password);
            if (userObj == null)
                return Unauthorized();
            string id = _service.GetUserID(userObj);

            return new JsonResult(new TokenResponse()
            {
                Token = generateJwtToken(id)
            });
        }

        /// <summary>
        /// Remove authentication cookie
        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("auth");
            return Ok();
        }


        //Private
        private string generateJwtToken(string id)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", id) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_service.Secret), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        private TokenResponse GetTokenResponse(string id)
        {
            return new TokenResponse()
            {
                Token = generateJwtToken(id)
            };
        }
    }

    /// <summary>
    /// Used as model for Login
    /// </summary>
    public class AuthenticationRequest
    {
        public string User { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Used as response for /Login
    /// </summary>
    public class TokenResponse
    {
        public string Token { get; set; }
    }
}
