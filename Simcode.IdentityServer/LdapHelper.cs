using Novell.Directory.Ldap;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Simcode.IdentityServer
{
    public static class LdapHelper
    {
        public static async Task<HashSet<string>> GetGroupsForUser(LdapConnection ldapConnection, string baseDN, string userName)
        {
            var groups = new HashSet<string>();

            foreach (string group in await GetGroups(ldapConnection, baseDN, userName))
            {
                groups.Add(group);

                foreach (string parentGroup in await GetGroups(ldapConnection, baseDN, group))
                    groups.Add(parentGroup);
            }

            return groups;
        }

        private static async Task<IEnumerable<string>> GetGroups(LdapConnection ldapConnection, string baseDN, string user)
        {
            LdapSearchQueue ldapSearchQueue = await ldapConnection.SearchAsync(
                baseDN,
                LdapConnection.ScopeSub,
                $"(sAMAccountName={user})",
                new string[] { "cn", "memberOf" },
                false,
                null as LdapSearchQueue);

            var result = new List<string>();

            LdapMessage ldapMessage;
            while ((ldapMessage = ldapSearchQueue.GetResponse()) != null)
            {
                if (ldapMessage is LdapSearchResult ldapSearchResult)
                {
                    LdapEntry ldapEntry = ldapSearchResult.Entry;
                    foreach (string value in GetGroups(ldapEntry))
                        result.Add(value);
                }
                else
                    continue;
            }

            return result;
        }

        private static IEnumerable<string> GetGroups(LdapEntry ldapEntry)
        {
            ldapEntry.GetAttributeSet().TryGetValue("memberOf", out LdapAttribute? ldapAttribute);

            if (ldapAttribute == null) yield break;

            foreach (string value in ldapAttribute.StringValueArray)
            {
                string? group = GetGroup(value);
                if (!string.IsNullOrEmpty(group))
                    yield return group;
            }
        }

        private static string? GetGroup(string value)
        {
            Match match = Regex.Match(value, "^CN=([^,]*)");

            if (!match.Success) return null;

            return match.Groups[1].Value;
        }
    }
}
