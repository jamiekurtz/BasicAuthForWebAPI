using System.Collections.Generic;
using System.Security.Claims;

namespace BasicAuthForWebAPI
{
    public interface IMembershipClaimsHelper
    {
        IEnumerable<Claim> GetAdditionalClaims(MembershipProviderUser user);
    }
}