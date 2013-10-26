using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BasicAuthForWebAPI
{
    public class BasicAuthenticationMessageHandler : DelegatingHandler
    {
        public const string BasicScheme = "Basic";
        public const string ChallengeAuthenticationHeaderName = "WWW-Authenticate";
        public const char AuthorizationHeaderSeparator = ':';

        private readonly IMembershipProvider _membershipProvider;

        public BasicAuthenticationMessageHandler()
            : this(new MembershipProviderAdapter())
        {
        }

        public BasicAuthenticationMessageHandler(IMembershipProvider membershipProvider)
        {
            _membershipProvider = membershipProvider;
            IssueChallengeResponse = false;
        }

        public Func<MembershipProviderUser, IEnumerable<Claim>> GetAdditionalClaims { get; set; }
 
        public bool IssueChallengeResponse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authHeader = request.Headers.Authorization;
            if (authHeader == null)
            {
                return CreateResponse(request, cancellationToken);
            }

            if (authHeader.Scheme != BasicScheme)
            {
                return CreateResponse(request, cancellationToken);
            }

            var encodedCredentials = authHeader.Parameter;
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.ASCII.GetString(credentialBytes);
            var credentialParts = credentials.Split(AuthorizationHeaderSeparator);

            if (credentialParts.Length != 2)
            {
                return CreateResponse(request, cancellationToken);
            }

            var username = credentialParts[0].Trim();
            var password = credentialParts[1].Trim();

            if (!_membershipProvider.ValidateUser(username, password))
            {
                return CreateResponse(request, cancellationToken);
            }

            SetPrincipal(username);

            return base.SendAsync(request, cancellationToken);
        }

        private Task<HttpResponseMessage> CreateResponse(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return IssueChallengeResponse ? CreateUnauthorizedResponse() : base.SendAsync(request, cancellationToken);
        }

        private void SetPrincipal(string username)
        {
            var roles = _membershipProvider.GetRolesForUser(username);
            var user = _membershipProvider.GetUser(username);

            var identity = CreateIdentity(user);

            var principal = new GenericPrincipal(identity, roles);
            Thread.CurrentPrincipal = principal;
            
            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        private GenericIdentity CreateIdentity(MembershipProviderUser user)
        {
            var identity = new GenericIdentity(user.Username, BasicScheme);
            identity.AddClaim(new Claim(ClaimTypes.Sid, user.UserId));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

            if (GetAdditionalClaims != null)
            {
                try
                {
                    var claims = GetAdditionalClaims(user);
                    identity.AddClaims(claims);
                }
                catch (Exception exception)
                {
                    const string msg = "Error getting additional claims from caller";
                    Debug.WriteLine(msg + ": " + exception);
                    throw new Exception(msg, exception);
                }
            }

            return identity;
        }

        private Task<HttpResponseMessage> CreateUnauthorizedResponse()
        {
            var response = new HttpResponseMessage {StatusCode = HttpStatusCode.Unauthorized};
            response.Headers.Add(ChallengeAuthenticationHeaderName, BasicScheme);
            
            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);
            return taskCompletionSource.Task;
        }
    }
}