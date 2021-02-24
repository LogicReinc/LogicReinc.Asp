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
    /// (Decoupled version)
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

            public T UserObjectAs<T>() => (T)UserObject;

            public AuthUser(AuthenticationService service, object user)
            {
                UserObject = user;
                _service = service;
            }
        }
    }

    /// <summary>
    /// Abstract/Interface for authentication, implement and pass to SetAuthentication
    /// (Coupled version)
    /// </summary>
    /// <typeparam name="T">User implementation with GetRoles and GetID</typeparam>
    public abstract class AuthenticationService<T> : AuthenticationService where T : IAuthUser
    {
        private TimeSpan _tokenExpire = TimeSpan.FromHours(24);
        public override TimeSpan Expires => _tokenExpire;


        public AuthenticationService() { }
        public AuthenticationService(TimeSpan expireToken)
        {
            _tokenExpire = expireToken;
        }

        public abstract IAuthUser AuthenticateAuthUser(string user, string pass);
        public abstract IAuthUser GetAuthUser(string id);

        public override object Authenticate(string user, string pass)
        {
            return AuthenticateAuthUser(user, pass);
        }
        public override object GetUser(string id)
        {
            return GetAuthUser(id);
        }

        public override string[] GetRoles(object obj)
        {
            return ((IAuthUser)obj).GetRoles();
        }
        public override string GetUserID(object obj)
        {
            return ((IAuthUser)obj).GetID();
        }
    }

    public interface IAuthUser
    {
        string[] GetRoles();
        string GetID();
    }
}
