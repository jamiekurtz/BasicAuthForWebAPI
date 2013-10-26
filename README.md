BasicAuthForWebAPI
==================

Nuget-deployed library for leveraging HTTP basic authentication in ASP.NET Web API.

By default, will use the [ASP.NET Membership Provider](http://msdn.microsoft.com/en-us/library/yh26yfzy.aspx). But you can modify and extend this behavior using one or more of the following:
- Pass your own IMembershipProvider to the BasicAuthenticationMessageHandler constructor
- Set the GetAdditionalClaims property to a Func that returns a collection of System.Security.Claims.Claim objects
- Set the IssueChallengeResponse to true to allow only authenticated callers and tell browsers to prompt for credentials

This module essentially just sets a principal onto the current request thread. **It is up to you to ensure that your controllers and their actions are protected with the [Authorize] attribute**. If you really don't want to allow access to your site without authentication, you can set the IssueChallengeResponse property to true. This will stop all requests that aren't authenticated - period. When this property is set to true, the [Authorize] and [AllowAnonymous] effectively useless, because authentication will succeed or fail prior to your controllers and their attributes being examined and instantiated.

    
Configuration
-------------

To configure the authentication, place the following line somewhere in your startup code - e.g. the Register() method in /app_start/WebApiConfig.cs:
        
    GlobalConfiguration.Configuration.MessageHandlers.Add(new BasicAuthenticationMessageHandler());

And then make sure you use the [Authorize] attribute to protect your controllers and their actions - either with a global filter or on individual controllers and actions. See below for more details. 

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


IssueChallengeResponse Property
-------------------------------

As mentioned above, the default behavior of this module is to let the [Authorize] and [AllowAnonymous] attributes control access to your controllers and their actions. As such, you should make sure that you either decorate your controllers or actions with the [Authorize] attribute, or add a global filter in the WebApiConfig.Register() method, like this:

    public static void Register(HttpConfiguration config)
    {
        config.Filters.Add(new AuthorizeAttribute());

        // ...
    }


In this "mode", where the IssueChallengeResponse property is set to false (which is its default value), **controller actions not protected with the [Authorize] attribute will be accessible by unauthenticated callers**. Additionally, in this "mode" you can utilize the [AllowAnonymous] attribute to grant unauthenticated access to specfic controllers or controller actions.

If, however, you want to prevent all unauthenticated callers, regardless of the presence of the [Authorize] or [AllowAnonymous] attributes, you can set the IssueChallengeResponse property to true. This will stop all requests that aren't authenticated. Setting this property to true also has the benefit of causing browsers to prompt the caller for a username and password if those credentials are missing from the HTTP Authorization header.


Example Request Using HTTP Basic Authentication
-----------------------------------------------

As I talked about in my [Web API book](http://www.amazon.com/Using-ASP-NET-Web-API-MVC/dp/1430249773), it is rather simple to create a Basic authentication HTTP request in .NET. Here's a little example code:

    const string ApiUrlRoot = "http://localhost:11000";

    const string creds = "jbob" + ":" + "jbob12345"; // username and password
    var bcreds = Encoding.ASCII.GetBytes(creds);
    var base64Creds = Convert.ToBase64String(bcreds);

    var client = new WebClient();
    client.Headers.Add("Authorization", "Basic " + base64Creds); 
    var response = client.DownloadString(ApiUrlRoot + "/api/categories");

The HTTP Basic authentication scheme merely sticks the username and password together and then base64 encodes them.

