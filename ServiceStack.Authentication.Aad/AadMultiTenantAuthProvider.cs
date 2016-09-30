using System;
using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack.Authentication.Aad
{
    public class AadMultiTenantAuthProvider : OAuthProvider
    {
        public const string Name = "aad-mt";

//        public AadMultiTenantAuthProvider()
//            : base(new AppSettings(), AadAuthProvider.Realm, Name, "ClientId", "ClientSecret")
//        {
//            
//        }

        public AadMultiTenantAuthProvider(IAppSettings settings)
            : base(settings, AadAuthProvider.Realm, Name, "ClientId", "ClientSecret")
        {
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var z = 2;
            return base.OnAuthenticated(authService, session, tokens, authInfo);
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var directoryRepository = authService.TryResolve<IDirectoryRepository>();
            if (directoryRepository == null)
                throw new UnauthorizedAccessException(
                    $"{nameof(IDirectoryRepository)} was not registered in the container.");

            var directory = ParseDomainFromUserName(request);
            var config = directoryRepository.GetDirectoryByTenantName(directory);
            if (config == null)
                throw new UnauthorizedAccessException($"Directory not found: {directory}");

            var aadProvider = new AadAuthProvider(config.ClientId, config.ClientSecret)
            {
                TenantId = config.TenantId,
                DomainHint = config.DomainHint,
                Provider = Name                
            };
            
            var authTokens = new AuthTokens
            {
                UserName = request.UserName,
                Provider = Name
            };
            authTokens.Items.Add("tenant_id", config.TenantId);
            authTokens.Items.Add("aad_client_id", config.ClientId);
            session.ProviderOAuthAccess.Add(authTokens);
            var authenticated = aadProvider.Authenticate(authService, session, request);
            return authenticated;
        }

        private static string ParseDomainFromUserName(Authenticate request)
        {
            if (string.IsNullOrWhiteSpace(request?.UserName))
                throw new UnauthorizedAccessException("Organization domain not found");

            var idx = request.UserName.LastIndexOf("@", StringComparison.Ordinal);
            if (idx < 0)
                throw new UnauthorizedAccessException("Organization domain not found");

            return request.UserName.Substring(idx);
        }
    }
}