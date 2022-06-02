using IdentityModel;
using IdentityServer4.Validation;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using IdentityServer4;
using System.Text.Json;

namespace Simcode.IdentityServer
{
    /// <summary>
    /// Resource owner password validator
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.IResourceOwnerPasswordValidator" />
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ISystemClock _clock;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="clock"></param>
        public ResourceOwnerPasswordValidator(ILogger<ResourceOwnerPasswordValidator> logger, IConfiguration configuration, ISystemClock clock)
        {
            _logger = logger;
            _configuration = configuration;
            _clock = clock;
        }

        /// <summary>
        /// Validates the resource owner password credential
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            // search by context.UserName

            // VALTEMP
            // For developers
            if (context.UserName == "mngr" && context.Password == @"mngr1")
            {
                var groups = new HashSet<string>() { "PazCheckAdmins" };

                // VALTEMP
                //var claims = new[]
                //    {
                //        new Claim(JwtClaimTypes.Name, "Сидор Сидоров"),
                //        new Claim(JwtClaimTypes.GivenName, "Сидоров"),
                //        new Claim(JwtClaimTypes.FamilyName, "Сидоров"),
                //        new Claim(JwtClaimTypes.Email, ""),
                //        //new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),                            
                //        new Claim(JwtClaimTypes.Role, JsonSerializer.Serialize(groups), IdentityServerConstants.ClaimValueTypes.Json)
                //    };
                var ret = new HashSet<Claim>(new ClaimComparer());
                ret.Add(new Claim(JwtClaimTypes.Id, "1", ClaimValueTypes.Integer));
                ret.Add(new Claim(JwtClaimTypes.Name, "Александр Сергеевич"));
                ret.Add(new Claim(JwtClaimTypes.GivenName, @"Александр"));
                ret.Add(new Claim(JwtClaimTypes.MiddleName, @"Сергеевич"));
                ret.Add(new Claim(JwtClaimTypes.FamilyName, @"Пушкин"));
                ret.Add(new Claim("pers_number", @"27"));
                ret.Add(new Claim("office", @"Отдел АСУ ТП"));
                ret.Add(new Claim(JwtClaimTypes.Role, @"RoleAdmin"));

                string subjectId = context.UserName;
                context.Result = new GrantValidationResult(
                    subjectId,
                    OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime,
                    ret);
            }

            string activeDirectory_Server = _configuration.GetValue<string>("ActiveDirectory_Server");
            string activeDirectory_UsersDN = _configuration.GetValue<string>("ActiveDirectory_UsersDN");

            int ldapPort = LdapConnection.DefaultPort;
            int ldapVersion = LdapConnection.LdapV3;
            String ldapHost = activeDirectory_Server;
            String loginDN = LdapDn.EscapeRdn("CN=" + context.UserName) + "," + activeDirectory_UsersDN;
            String password = context.Password;            

            using (var ldapConnection = new LdapConnection())
            {
                try
                {                    
                    // connect to the server
                    await ldapConnection.ConnectAsync(ldapHost, ldapPort);

                    // authenticate to the server
                    await ldapConnection.BindAsync(ldapVersion, loginDN, password);

                    var groups = await LdapHelper.GetGroupsForUser(ldapConnection, activeDirectory_UsersDN, context.UserName);

                    // create a new unique subject id
                    string subjectId = context.UserName;
                    string name;
                    string first = @"";
                    string last = @"";
                    
                    LdapSearchQueue ldapSearchQueue = await ldapConnection.SearchAsync(
                        activeDirectory_UsersDN,
                        LdapConnection.ScopeSub,
                        $"(sAMAccountName={context.UserName})",
                        new string[] { "cn", "givenName", "sn" },
                        false,
                        null as LdapSearchQueue);
                        
                    LdapMessage ldapMessage;
                    while ((ldapMessage = ldapSearchQueue.GetResponse()) != null)
                    {
                        if (ldapMessage is LdapSearchResult ldapSearchResult)
                        {
                            LdapEntry ldapEntry = ldapSearchResult.Entry;

                            LdapAttribute? ldapAttribute;
                            ldapEntry.GetAttributeSet().TryGetValue("givenName", out ldapAttribute);
                            if (ldapAttribute != null)
                            {
                                foreach (string value in ldapAttribute.StringValueArray)
                                {
                                    first = value;
                                }
                            }

                            ldapEntry.GetAttributeSet().TryGetValue("sn", out ldapAttribute);
                            if (ldapAttribute != null)
                            {
                                foreach (string value in ldapAttribute.StringValueArray)
                                {
                                    last = value;
                                }
                            }
                            
                            //ldapEntry.GetAttributeSet().TryGetValue("userAccountControl", out ldapAttribute);
                            //if (ldapAttribute != null)
                            //{
                            //    foreach (string value in ldapAttribute.StringValueArray)
                            //    {
                            //        int userAccountControlInt = new Any(value).ValueAsInt32(false);
                            //        isDisabled = (userAccountControlInt & 0x0002) != 0;
                            //    }
                            //}
                        }                            
                    }
                        
                    if (!String.IsNullOrEmpty(first) && !String.IsNullOrEmpty(last))
                    {
                        name = first + " " + last;
                    }
                    else if (!String.IsNullOrEmpty(first))
                    {
                        name = first;
                    }
                    else if (!String.IsNullOrEmpty(last))
                    {
                        name = last;
                    }
                    else 
                    {
                        name = context.UserName;
                    }

                    var claims = new[]
                    {
                        new Claim(JwtClaimTypes.Name, name),
                        new Claim(JwtClaimTypes.GivenName, first),
                        new Claim(JwtClaimTypes.FamilyName, last),
                        new Claim(JwtClaimTypes.Email, ""),
                        //new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),                            
                        new Claim(JwtClaimTypes.Role, JsonSerializer.Serialize(groups), IdentityServerConstants.ClaimValueTypes.Json)
                    };

                    context.Result = new GrantValidationResult(
                        subjectId,
                        OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime,
                        claims);
                }
                catch (LdapException e)
                {
                    if (e.ResultCode == LdapException.InvalidCredentials)
                    {
                        _logger.LogError("Error: Invalid Credentials, User: " + context.UserName);
                    }
                    else if (e.ResultCode == LdapException.NoSuchObject)
                    {
                        _logger.LogError("Error: No such entry");
                    }
                    else if (e.ResultCode == LdapException.NoSuchAttribute)
                    {
                        _logger.LogError("Error: No such attribute");
                    }
                    else
                    {
                        _logger.LogError("Error: " + e.ToString());
                    }
                }
                catch (System.IO.IOException e)
                {
                    _logger.LogError("Error: " + e.ToString());
                }                
            }
        }
    }
}


//using System.DirectoryServices.AccountManagement;

//public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;
//    private readonly ISystemClock _clock;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
//    /// </summary>
//    /// <param name="users">The users.</param>
//    /// <param name="clock">The clock.</param>
//    public ResourceOwnerPasswordValidator(ILogger<ResourceOwnerPasswordValidator> logger, IConfiguration configuration, ISystemClock clock)
//    {
//        _logger = logger;
//        _configuration = configuration;
//        _clock = clock;
//    }

//    /// <summary>
//    /// Validates the resource owner password credential
//    /// </summary>
//    /// <param name="context">The context.</param>
//    /// <returns></returns>
//    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress, "rodc", "1"))
//        {
//            if (adPrincipalContext.ValidateCredentials(context.UserName, context.Password))
//            {
//                // create a new unique subject id
//                var subjectId = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);
//                var claims = new List<Claim>();
//                try
//                {
//                    var userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, context.UserName);
//                    var first = userPrincipal.GivenName;
//                    var last = userPrincipal.Surname;
//                    if (!String.IsNullOrEmpty(first) && !String.IsNullOrEmpty(last))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
//                    }
//                    else if (!String.IsNullOrEmpty(first))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, first));
//                    }
//                    else if (!String.IsNullOrEmpty(last))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, last));
//                    }
//                }
//                catch
//                {
//                    _logger.LogError("adPrincipalContext.ValidateCredentials(context.UserName, context.Password) Error");
//                    claims.Add(new Claim(JwtClaimTypes.Name, context.UserName));
//                }

//                context.Result = new GrantValidationResult(
//                    subjectId,
//                    OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime,
//                    claims);
//            }
//        }

//        return Task.CompletedTask;
//    }
//}
