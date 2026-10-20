using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using DomainLayer;
using System.Web;
using static LinkBoxUI.Context.LinkboxDb;
using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using DomainLayer.ViewModels;
using DataCipher;

namespace LinkBoxUI.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;
        private readonly LinkboxDb _context = new LinkboxDb();
        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }
            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            //var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();
            //Context.LinkboxDb.ApplicationUser user = await userManager.FindAsync(context.UserName, Cryption.GetPassword(context.UserName, context.Password));

            //if (user == null)
            //{
            //    context.SetError("invalid_grant", "The user name or password is incorrect.");
            //    return;
            //}

            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            LoginViewModels.Login login = new LoginViewModels.Login
            {
                userName = context.UserName,
                password = context.Password
            };

            var user = _context.Users.AsEnumerable().Where(a => a.UserName == login.userName && Cryption.Decrypt(a.Password) == login.userName + login.password).FirstOrDefault();
            if (user == null)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }
            else
            {
                //var decrypted = Cryption.Decrypt(user.Password);
                //var userPass = _context.Users.Where(a => a.UserName == login.userName && decrypted == login.userName + login.password).FirstOrDefault();
                identity.AddClaim(new Claim(ClaimTypes.Role, user.UserName));
                identity.AddClaim(new Claim("username", user.UserName));
                identity.AddClaim(new Claim(ClaimTypes.Name, $"Hi {user.UserName}"));
                context.Validated(identity);
            }

            //ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
            //   OAuthDefaults.AuthenticationType);
            //ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
            //    CookieAuthenticationDefaults.AuthenticationType);

            //AuthenticationProperties properties = CreateProperties(user.UserName);
            //AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            //context.Validated(ticket);
            //context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }
    }
}