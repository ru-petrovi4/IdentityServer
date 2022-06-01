using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Simcode.IdentityServer
{
    public class ADProfileService : IProfileService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ISystemClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="clock">The clock.</param>
        public ADProfileService(ILogger<ADProfileService> logger, IConfiguration configuration, ISystemClock clock)
        {
            _logger = logger;
            _configuration = configuration;
            _clock = clock;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            //context.LogProfileRequest(_logger);

            // VALTEMP
            //context.AddRequestedClaims(context.Subject.Claims);
            var ret = new HashSet<Claim>(new ClaimComparer());
            ret.Add(new Claim(JwtClaimTypes.Id, "1", ClaimValueTypes.Integer));
            ret.Add(new Claim(JwtClaimTypes.Name, "Name"));
            ret.Add(new Claim(JwtClaimTypes.GivenName, @"FirstName"));
            ret.Add(new Claim(JwtClaimTypes.MiddleName, @"MiddleName"));
            ret.Add(new Claim(JwtClaimTypes.FamilyName, @"LastName"));
            ret.Add(new Claim("pers_number", @"1"));
            ret.Add(new Claim("office", @"Office.Name"));
            ret.Add(new Claim(JwtClaimTypes.Role, @"RoleAdmin"));
            context.AddRequestedClaims(ret);

            //context.LogIssuedClaims(_logger);

            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {            
            context.IsActive = true;            
            return Task.CompletedTask;
        }

        //private Boolean isActive(SearchResult searchResult)
        //{
        //    Attribute userAccountControlAttr = searchResult.getAttributes().get("UserAccountControl");
        //    Integer userAccountControlInt = new Integer((String)userAccoutControlAttr.get());
        //    Boolean disabled = BooleanUtils.toBooleanObject(userAccountControlInt & 0x0002);
        //    return !disabled;
        //}
    }
}


//using System.DirectoryServices;
//using System.DirectoryServices.AccountManagement;
//public class ADProfileService : IProfileService
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;
//    private readonly ISystemClock _clock;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
//    /// </summary>
//    /// <param name="users">The users.</param>
//    /// <param name="clock">The clock.</param>
//    public ADProfileService(ILogger<ADProfileService> logger, IConfiguration configuration, ISystemClock clock)
//    {
//        _logger = logger;
//        _configuration = configuration;
//        _clock = clock;
//    }

//    public Task GetProfileDataAsync(ProfileDataRequestContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress))
//        {
//            var user = context.Subject.GetDisplayName();

//            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, user);

//            var userGroups = userPrincipal.GetGroups();

//            List<Claim> claims = new Claim[]
//            {
//                    new Claim(JwtClaimTypes.Name, userPrincipal.Name),
//                    new Claim(JwtClaimTypes.GivenName, userPrincipal.GivenName),
//                    new Claim(JwtClaimTypes.FamilyName, userPrincipal.DisplayName),
//                    new Claim(JwtClaimTypes.Email, userPrincipal.EmailAddress)
//            }.ToList();

//            foreach (System.DirectoryServices.AccountManagement.Principal principal in userGroups)
//            {
//                // Getting all groups causes JWT to be far too big so just using one as an example.
//                // To see if a user is a "memberOf" a group, use "uPrincipal.IsMemberOf"

//                if (principal.Name == "Domain Users")
//                    claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", principal.Name));
//            }

//            // To get another AD attribute not in "UserPrincipal" e.g. "Department"
//            string department = "";
//            if (userPrincipal.GetUnderlyingObjectType() == typeof(DirectoryEntry))
//            {
//                // Transition to directory entry to get other properties
//                using (var directoryEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject())
//                {
//                    var departmetValue = directoryEntry.Properties["department"];
//                    if (departmetValue != null)
//                        department = departmetValue.Value.ToString();
//                }
//            }
//            // Add custom claims in token here based on user properties or any other source
//            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/department", department));
//            claims.Add(new Claim("upn_custom", userPrincipal.UserPrincipalName));

//            // Filters the claims based on the requested claim types and then adds them to the IssuedClaims collection.
//            //context.AddRequestedClaims(claims);
//            context.IssuedClaims = claims;

//            return Task.CompletedTask;
//        }
//    }

//    public Task IsActiveAsync(IsActiveContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress))
//        {
//            var user = context.Subject;

//            Claim userClaim = user.Claims.FirstOrDefault(claimRecord => claimRecord.Type == "sub");

//            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, userClaim.Value);

//            // To be active, user must be enabled and not locked out

//            var isLocked = userPrincipal.IsAccountLockedOut();

//            context.IsActive = (bool)(userPrincipal.Enabled & !isLocked);

//            return Task.CompletedTask;
//        }
//    }
//}