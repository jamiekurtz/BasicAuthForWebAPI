using System;
using System.Web.Security;

namespace BasicAuthForWebAPI
{
    class MembershipProviderAdapter : IMembershipProvider
    {
        public bool ValidateUser(string username, string password)
        {
            return Membership.ValidateUser(username, password);
        }

        public string[] GetRolesForUser(string username)
        {
            return Roles.GetRolesForUser(username);
        }

        public MembershipProviderUser GetUser(string username)
        {
            var user = Membership.GetUser(username);
            if (user == null)
            {
                throw new ApplicationException(string.Format("User {0} not found", username));
            }

            return new MembershipProviderUser
                {
                    Email = user.Email,
                    UserId = user.ProviderUserKey.ToString(),
                    Username = user.UserName
                };
        }
    }
}
