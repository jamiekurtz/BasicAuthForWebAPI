BasicAuthForWebAPI
==================

Nuget-deployed library for leveraging HTTP basic authentication in ASP.NET Web API.


By default, will use the [ASP.NET Membership Provider](http://msdn.microsoft.com/en-us/library/yh26yfzy.aspx). But you can modify and extend this behavior in either or both of the following ways:
- Pass your own IMembershipProvider to the BasicAuthenticationMessageHandler constructor
- Set the GetAdditionalClaims property to a Func that returns a collection of System.Security.Claims.Claim objects
    
To configure the authentication, place the following line somewhere in your startup code - e.g. the Register() method in /app_start/WebApiConfig.cs:
        
	GlobalConfiguration.Configuration.MessageHandlers.Add(new BasicAuthenticationMessageHandler());
 

The following shows the use of the GetAdditionalClaims property as used with Ninject and NHibernate (contained in the NinjectWebCommon class):       


	private static void RegisterServices(IKernel kernel)
	{
		var authHandler = new BasicAuthenticationMessageHandler
			{
				GetAdditionalClaims = user =>
					{
						var sessionFactory = kernel.Get<ISessionFactory>();
						using (var session = sessionFactory.OpenSession())
						{
							var userId = Guid.Parse(user.UserId);
							var modelUser = session.Get<User>(userId);
                                
							var claims = new List<Claim>
							{
								new Claim(ClaimTypes.GivenName, modelUser.Firstname),
								new Claim(ClaimTypes.Surname, modelUser.Lastname)
							};

							return claims;
						}
					}
			};

		GlobalConfiguration.Configuration.MessageHandlers.Add(authHandler);
	}


You can also implement the IMembershipProvider interface to create your own credential validation, and supply it to the BasicAuthenticationMessageHandler constructor:

    public interface IMembershipProvider
    {
        bool ValidateUser(string username, string password);
        string[] GetRolesForUser(string username);
        MembershipProviderUser GetUser(string username);
    }

Note that the GetAdditionalClaims property will still be invoked if you supply your own IMembershipProvider.