using System;
using FluentAssertions;
using NUnit.Framework;
using ServiceStack.AzureGraph.OrmLite;
using ServiceStack.AzureGraph.ServiceModel.Entities;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.AzureGraph.Tests
{
    public class OrmLiteApplicationRegistryServiceTest
    {
        internal static readonly ApplicationRegistration Directory1 = new ApplicationRegistration
        {
            ClientSecret = "secret",
            ClientId = "ed0dd5aa6f3f4c368a53ede9ea77a140",
            DirectoryName = "@foo1.ms.com"
        };

        internal static readonly ApplicationRegistration Directory2 = new ApplicationRegistration
        {
            ClientSecret = "secret2",
            ClientId = "2b72c902f41f43549f2de8b530d6a803",
            DirectoryName = "@foo2.ms.com",
            RefId = 1,
            RefIdStr = "1"
        };

        private IDbConnectionFactory _connectionFactory;
        private OrmLiteApplicationRegistryService _service;

        [OneTimeSetUp]
        public void Init()
        {
            _connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        }

        [SetUp]
        public void Setup()
        {
            _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
        }

        [Test]
        public void ShouldNotFindDirectory()
        {
            var dir = _service.GetApplicationByDirectoryName(Directory1.DirectoryName);
            dir.Should().BeNull();
        }

        [Test]
        public void ShouldFindDirectory()
        {
            var inserted = _service.RegisterApplication(Directory2);

            var result = _service.GetApplicationByDirectoryName(Directory2.DirectoryName);

            result.Should().NotBe(null);
            result.Should().NotBeSameAs(inserted);
            result.DirectoryName.Should().Be(inserted.DirectoryName);
            result.ClientId.Should().Be(inserted.ClientId);
            result.ClientSecret.Should().Be(inserted.ClientSecret);
            result.Id.Should().Be(inserted.Id);
            result.RefId.Should().NotBe(null);
            result.RefIdStr.Should().NotBe(null);
            result.RefId.Should().Be(inserted.RefId);
            result.RefIdStr.Should().Be(inserted.RefIdStr);
        }

        [Test]
        public void ShouldCreateDirectory()
        {
            var result = _service.RegisterApplication(Directory1);

            result.Should().NotBe(null);
            result.Should().NotBeSameAs(Directory1);
            result.Id.Should().BeGreaterThan(0);
            result.ClientSecret.Should().Be("secret");
            result.DirectoryName.Should().Be(Directory1.DirectoryName);
            result.RefId.Should().Be(null);
            result.RefIdStr.Should().Be(null);
        }

        [Test]
        public void ShouldNotCreateDuplicateDirectory()
        {
            var result = _service.RegisterApplication(Directory1);
            Action action = () => _service.RegisterApplication(Directory1);
            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ShouldCreateMultipleDirectories()
        {
            _service.RegisterApplication(Directory1);
            _service.RegisterApplication(Directory2);

            var result1 = _service.ApplicationIsRegistered(Directory1.DirectoryName);
            var result2 = _service.ApplicationIsRegistered(Directory2.DirectoryName);

            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Test]
        public void ShouldExist()
        {
            _service.RegisterApplication(Directory1);

            var isRegistered = _service.ApplicationIsRegistered(Directory1.DirectoryName);

            isRegistered.Should().BeTrue();
        }

        [Test]
        public void ShouldNotExist()
        {
            _service.RegisterApplication(Directory1);

            var isRegistered = _service.ApplicationIsRegistered(Directory2.DirectoryName);

            isRegistered.Should().BeFalse();
        }
    }
}