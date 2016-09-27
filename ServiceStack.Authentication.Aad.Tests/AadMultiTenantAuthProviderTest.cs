using System;
using System.Collections.Specialized;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Authentication.Aad.Tests
{
    public class AadMultiTenantAuthProviderTest
    {
        private IDirectoryRepository _directoryRepository;
        public AadMultiTenantAuthProvider Subject { get; set; }

        [OneTimeSetUp]
        public void Init()
        {
            var connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            _directoryRepository = new OrmLiteDirectoryRepository(connectionFactory);
            _directoryRepository.InitSchema();
            _directoryRepository.RegisterDirectory(OrmLiteDirectoryRepositoryTest.Directory1);
            _directoryRepository.RegisterDirectory(OrmLiteDirectoryRepositoryTest.Directory2);
        }

        [SetUp]
        public void Setup()
        {
            Subject = new AadMultiTenantAuthProvider(new AppSettings());
        }

        [Test]
        public void ShouldBeAuthProvider()
        {
            Subject.Should().BeAssignableTo<AuthProvider>();
            Subject.Provider.Should().Be("aad-mt");
        }

        [Test]
        public void ShouldNotAttemptAuthenticationAgainstUnRegisteredDirectory()
        {
            using (AadAuthProviderTest.TestAppHost())
            {
                var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
                {
                    {"redirect", "http://localhost/myapp/secure-resource"}
                }, Stream.Null, null);
                var mockAuthService = MockAuthService(_directoryRepository, request);
                var session = new AuthUserSession();

                var username = "user1@notregistered.com";
                var exception = Assert.Throws<UnauthorizedAccessException>(() => Subject.Authenticate(mockAuthService.Object, session, new Authenticate()
                {
                    UserName = username,
                    provider = AadMultiTenantAuthProvider.Name
                }));

                Assert.AreEqual($"Directory not found: @notregistered.com", exception.Message);
            }
        }

        [Test]
        public void ShouldRequestAuthorizationFromUserDomain()
        {

            using (AadAuthProviderTest.TestAppHost())
            {
                var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
                {
                    {"redirect", "http://localhost/myapp/secure-resource"}
                }, Stream.Null, null);
                var mockAuthService = MockAuthService(_directoryRepository, request);
                var session = new AuthUserSession();

                var username = "user1" + OrmLiteDirectoryRepositoryTest.Directory1.DirectoryDomain;
                var response = Subject.Authenticate(mockAuthService.Object, session, new Authenticate()
                {
                    UserName = username,
                    provider = AadMultiTenantAuthProvider.Name
                });
                var result = (IHttpResult) response;
                var codeRequest = new Uri(result.Headers["Location"]);
                var query = PclExportClient.Instance.ParseQueryString(codeRequest.Query);
                var d = OrmLiteDirectoryRepositoryTest.Directory1;

                query["client_id"].Should().Be(d.ClientId);
                query["domain_hint"].Should().Be(d.DomainHint);
                query["login_hint"].Should().Be(username);
                codeRequest.Authority.Should().Be("login.microsoftonline.com");
                codeRequest.LocalPath.Should().Be($"/{d.TenantId}/oauth2/authorize");
                codeRequest.Scheme.Should().Be(Uri.UriSchemeHttps);

                session.ReferrerUrl.Should().Be("http://localhost/myapp/secure-resource");
            }
        }

        private static Mock<IServiceBase> MockAuthService(IDirectoryRepository repository, MockHttpRequest request = null)
        {
            request = request ?? new MockHttpRequest();
            var mockAuthService = new Mock<IServiceBase>();
            mockAuthService.SetupGet(s => s.Request).Returns(request);
            System.Func<IDirectoryRepository> resolveRepo = () => repository;
            mockAuthService.Setup(x => x.TryResolve<IDirectoryRepository>()).Returns(resolveRepo);
            return mockAuthService;
        }
    }
}