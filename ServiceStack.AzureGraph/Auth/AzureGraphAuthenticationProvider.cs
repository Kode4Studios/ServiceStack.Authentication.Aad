﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Auth;
using ServiceStack.AzureGraph.ServiceModel;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AzureGraph.Auth
{
    public class AzureGraphAuthenticationProvider : OAuthProvider
    {
        public IAppSettings AppSettings { get; private set; }
        private string _failureRedirectPath;

        public string FailureRedirectPath
        {
            get { return _failureRedirectPath; }
            set
            {
                if (!value.StartsWith("/"))
                    throw new FormatException("FailureRedirectPath should start with '/'");
                _failureRedirectPath = value;
            }
        }

        public AzureGraphAuthenticationProvider()
            : this(new AppSettings())
        {

        }

        public AzureGraphAuthenticationProvider(IAppSettings settings)
            : base(settings, MsGraph.Realm, MsGraph.ProviderName, "ClientId", "ClientSecret")
        {

        }

        public TimeSpan RefreshTokenLifespan { get; set; } = TimeSpan.FromDays(13.9);

        // TODO: Handle dynamic scopes
        // http://graph.microsoft.io/en-us/docs/authorization/permission_scopes
        public string[] Scopes { get; set; }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            // TODO: WARN: Property 'redirect' does not exist on type 'ServiceStack.Authenticate'
            // TODO: WARN: Property 'code' does not exist on type 'ServiceStack.Authenticate'
            // TODO: WARN: Property 'session_state' does not exist on type 'ServiceStack.Authenticate'
            // TODO: The base Init() should strip the query string from the request URL
            if (CallbackUrl.IsNullOrEmpty())
                CallbackUrl = new Uri(authService.Request.AbsoluteUri).GetLeftPart(UriPartial.Path);
            var tokens = Init(authService, ref session, request);
            var httpRequest = authService.Request;
            var query = httpRequest.QueryString.ToNameValueCollection();
            if (HasError(query))
                return RedirectDueToFailure(authService, session, query);

            // 1. The client application starts the flow by redirecting the user agent 
            //    to the Azure AD authorization endpoint. The user authenticates and 
            //    consents, if consent is required.

            // TODO: Can State property be added to IAuthSession to avoid this cast
            var userSession = session as AuthUserSession;
            if (userSession == null)
                throw new NotSupportedException("Concrete dependence on AuthUserSession because of State property");

            var code = query["code"];
            if (code.IsNullOrEmpty())
                return RequestCode(authService, request, session, userSession, tokens);

            var state = query["state"];
            if (state != userSession.State)
            {
                session.IsAuthenticated = false;
                throw new UnauthorizedAccessException("Mismatched state in code response.");
            }

            // 2. The Azure AD authorization endpoint redirects the user agent back 
            //    to the client application with an authorization code. The user 
            //    agent returns authorization code to the client application’s redirect URI.
            // 3. The client application requests an access token from the 
            //    Azure AD token issuance endpoint. It presents the authorization code 
            //    to prove that the user has consented.

            return RequestAccessToken(authService, session, code, tokens);
        }

        private object RequestAccessToken(IServiceBase authService, IAuthSession session, string code,
            IAuthTokens tokens)
        {
            try
            {
                var appDirectory = GetDirectoryNameFromUsername(session.UserName);

                var appRegistry = authService.TryResolve<IApplicationRegistryService>();
                if (appRegistry == null)
                    throw new InvalidOperationException(
                        $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

                var registration = appRegistry.GetApplicationByDirectoryName(appDirectory);
                if (registration == null)
                    throw new UnauthorizedAccessException($"Authorization for directory @{appDirectory} failed.");

                var postData =
                    $"grant_type=authorization_code&redirect_uri={CallbackUrl.UrlEncode()}&code={code}&client_id={registration.ClientId}&client_secret={registration.ClientSecret.UrlEncode()}&scope={BuildScopesFragment()}";
                var result = MsGraph.TokenUrl.PostToUrl(postData);

                var authInfo = JsonObject.Parse(result);
                var authInfoNvc = authInfo.ToNameValueCollection();
                if (HasError(authInfoNvc))
                    return RedirectDueToFailure(authService, session, authInfoNvc);
                tokens.AccessTokenSecret = authInfo["access_token"];
                tokens.RefreshToken = authInfo["refresh_token"];
                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
                       ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));
            }
            catch (WebException webException)
            {
                if (webException.Response == null)
                {
                    return RedirectDueToFailure(authService, session, new NameValueCollection
                    {
                        {"error", webException.GetType().ToString()},
                        {"error_description", webException.Message}
                    });
                }
                Log.Error("Auth Failure", webException);
                var response = ((HttpWebResponse) webException.Response);
                var responseText = Encoding.UTF8.GetString(
                    response.GetResponseStream().ReadFully());
                var errorInfo = JsonObject.Parse(responseText).ToNameValueCollection();
                return RedirectDueToFailure(authService, session, errorInfo);
            }
        }

        private object RequestCode(IServiceBase authService, Authenticate request, IAuthSession session,
            AuthUserSession userSession, IAuthTokens tokens)
        {
            var appDirectory = GetDirectoryNameFromUsername(request.UserName);
            session.UserName = request.UserName;

            var appRegistry = authService.TryResolve<IApplicationRegistryService>();
            if (appRegistry == null)
                throw new InvalidOperationException(
                    $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

            var registration = appRegistry.GetApplicationByDirectoryName(appDirectory);
            if (registration == null)
                throw new UnauthorizedAccessException($"Authorization for directory @{appDirectory} failed.");

            var state = Guid.NewGuid().ToString("N");
            tokens.Items.Add("ClientId", registration.ClientId);
            userSession.State = state;
            var reqUrl =
                $"{MsGraph.AuthorizationUrl}?client_id={registration.ClientId}&response_type=code&redirect_uri={CallbackUrl.UrlEncode()}&scope={BuildScopesFragment()}&state={state}";
            authService.SaveSession(session, SessionExpiry);
            return authService.Redirect(PreAuthUrlFilter(this, reqUrl));

        }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        protected override string GetReferrerUrl(IServiceBase authService, IAuthSession session,
            Authenticate request = null)
        {
            return authService.Request.GetParam("redirect") ??
                   base.GetReferrerUrl(authService, session, request);
            // Note that most auth providers redirect to the referrer url upon failure.
            // This implementation throws a monkey-wrench in that because we are here
            // setting the referrer url to the secure (authentication required) resource.
            // Thus redirecting to the referrer url on auth failure causes a redirect loop.
            // Therefore this auth provider redirects to FailureRedirectPath
            // The bottom line is that the user's destination should be different between success and failure
            // and the base implementation does not naturally support that
        }

        private void FailAndLogError(IAuthSession session, NameValueCollection errorInfo)
        {
            session.IsAuthenticated = false;
            if (HasError(errorInfo))
                Log.Error("{0} OAuth2 Error: '{1}' : \"{2}\" <{3}>".Fmt(
                    Provider,
                    errorInfo["error"],
                    errorInfo["error_description"].UrlDecode(),
                    errorInfo["error_uri"].UrlDecode()));
            else
                Log.Error("Unknown {0} OAuth2 Error".Fmt("Provider"));
        }

        protected IHttpResult RedirectDueToFailure(IServiceBase authService, IAuthSession session,
            NameValueCollection errorInfo)
        {
            FailAndLogError(session, errorInfo);
            var baseUrl = authService.Request.GetBaseUrl();
            var destination = !FailureRedirectPath.IsNullOrEmpty()
                ? baseUrl + FailureRedirectPath
                : session.ReferrerUrl ?? baseUrl;
            var fparam = errorInfo["error"] ?? "Unknown";
            return authService.Redirect(FailedRedirectUrlFilter(this, destination.SetParam("f", fparam)));
        }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        private static bool HasError(NameValueCollection info)
        {
            return !(info["error"] ?? info["error_uri"] ?? info["error_description"]).IsNullOrEmpty();
        }

        private string BuildScopesFragment()
        {
            return
                ((Scopes ?? new string[] {"User.Read", "offline_access", "openid", "profile"}).Select(
                    scope => $"{MsGraph.GraphUrl}/{scope} ").Join(" ")).UrlEncode();
        }

        private static string GetDirectoryNameFromUsername(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new UnauthorizedAccessException("Directory name not found");

            var idx = userName.LastIndexOf("@", StringComparison.Ordinal);
            if (idx < 0)
                throw new UnauthorizedAccessException("Application directory not found");

            return userName.Substring(idx + 1);
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            try
            {
                var me = "https://graph.microsoft.com/v1.0/me".GetStringFromUrl(accept: "application/json",
                    requestFilter: req => req.AddBearerToken(authInfo["access_token"]));

                var meInfo = JsonObject.Parse(me);
                var meInfoNvc = meInfo.ToNameValueCollection();
                tokens.FirstName = meInfoNvc["givenName"];
                tokens.LastName = meInfoNvc["surname"];
                tokens.Email = meInfoNvc["mail"];
                tokens.Language = meInfoNvc["preferredLanguage"];
                tokens.PhoneNumber = meInfoNvc["mobilePhone"];
            }
            catch
            {
                // No user profile related scope
            }
            return base.OnAuthenticated(authService, session, tokens, authInfo);
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            try
            {
                var jwt = new JwtSecurityToken(authInfo["id_token"]);
                var p = jwt.Payload;
                tokens.UserId = (string)p["oid"];
                tokens.UserName = (string)p["preferred_username"];
                tokens.DisplayName = (string)p.GetValueOrDefault("name");
                tokens.Items.Add("TenantId", (string) p["tid"]);
                tokens.RefreshTokenExpiry = jwt.ValidFrom.Add(RefreshTokenLifespan);

                if (SaveExtendedUserInfo)
                    p.Each(x => authInfo[x.Key] = x.Value.ToString());
            }
            catch (KeyNotFoundException ex)
            {
                Log.Error("Reading user auth info from JWT", ex);
                throw;
            }

            LoadUserOAuthProvider(userSession, tokens);
        }
    }
}