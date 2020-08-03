using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Authentication
{
    /// <summary>
    /// Abstract/Interface for authentication, implement and pass to SetAuthentication
    /// </summary>
    public abstract class AuthenticationService
    {
        internal byte[] Secret { get; } = GetSalt(32);

        public abstract TimeSpan Expires { get; }

        public abstract object Authenticate(string user, string pass);

        public abstract string GetUserID(object obj);
        public abstract string[] GetRoles(object obj);
        
        
        public abstract object GetUser(string id);

        public AuthUser GetAuthentication(object user)
        {
            return new AuthUser(this, user);
        }


        private static byte[] GetSalt(int length)
        {
            var bytes = new byte[length];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Model to interface with User object within framework
        /// </summary>
        public class AuthUser
        {
            private AuthenticationService _service;
            public object UserObject { get; set; }

            public string UserID => _service.GetUserID(UserObject);
            public string[] Roles => _service.GetRoles(UserObject);

            public AuthUser(AuthenticationService service, object user)
            {
                UserObject = user;
                _service = service;
            }
        }
    }
}
