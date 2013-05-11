namespace BasicAuthForWebAPI
{
    public interface IMembershipProvider
    {
        bool ValidateUser(string username, string password);
        string[] GetRolesForUser(string username);
        MembershipProviderUser GetUser(string username);
    }
}