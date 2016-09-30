using System;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Authentication.Aad.Tests
{
    public class OrmLiteDirectoryRepositoryTest
    {
        internal static readonly DirectoryRegistration Directory1 = new DirectoryRegistration
        {
            ClientSecret = "secret",
            ClientId = "clientid",
            TenantId = "ed0dd5aa6f3f4c368a53ede9ea77a140",
            DirectoryDomain = "@foo1.ms.com"
        };

        internal static readonly DirectoryRegistration Directory2 = new DirectoryRegistration
        {
            ClientSecret = "secret2",
            ClientId = "clientid2",
            TenantId = "2b72c902f41f43549f2de8b530d6a803",
            DirectoryDomain = "@foo2.ms.com",
            RefId = 1,
            RefIdStr = "1"
        };

        private IDbConnectionFactory _connectionFactory;
        private OrmLiteDirectoryRepository _repository;

        [OneTimeSetUp]
        public void Init()
        {
            _connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        }

        [SetUp]
        public void Setup()
        {
            _repository = new OrmLiteDirectoryRepository(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<DirectoryRegistration>();
            }
        }

        [Test]
        public void ShouldNotFindDirectory()
        {
            Assert.IsNull(_repository.GetDirectoryByTenantName(Directory1.DirectoryDomain));
        }

        [Test]
        public void ShouldFindDirectory()
        {
            var inserted = _repository.RegisterDirectory(Directory2);

            var result = _repository.GetDirectoryByTenantName(Directory2.DirectoryDomain);

            Assert.IsNotNull(result);
            Assert.AreNotSame(inserted.DirectoryDomain, result.DirectoryDomain);
            Assert.AreEqual(inserted.ClientId, result.ClientId);
            Assert.AreEqual(inserted.ClientSecret, result.ClientSecret);
            Assert.AreEqual(inserted.DomainHint, result.DomainHint);
            Assert.AreEqual(inserted.Id, result.Id);
            Assert.IsNotNull(result.RefId);
            Assert.AreEqual(inserted.RefId, result.RefId);
            Assert.IsNotNull(result.RefIdStr);
            Assert.AreEqual(inserted.RefIdStr, result.RefIdStr);
        }

        [Test]
        public void ShouldCreateDirectory()
        {
            var result = _repository.RegisterDirectory(Directory1);

            Assert.IsNotNull(result);
            Assert.AreNotSame(Directory1, result);
            Assert.Greater(result.Id, 0);
            Assert.AreEqual("clientid", result.ClientId);
            Assert.AreEqual("secret", result.ClientSecret);
            Assert.AreEqual(Directory1.DirectoryDomain, result.DirectoryDomain);
            Assert.AreEqual(Directory1.DirectoryDomain.Substring(1), result.DomainHint);
            Assert.IsNull(result.RefId);
            Assert.IsNull(result.RefIdStr);
        }

        [Test]
        public void ShouldNotCreateDuplicateDirectory()
        {
            var result = _repository.RegisterDirectory(Directory1);
            Assert.Throws<InvalidOperationException>(() => _repository.RegisterDirectory(Directory1));
        }

        [Test]
        public void ShouldCreateMultipleDirectories()
        {
            _repository.RegisterDirectory(Directory1);
            _repository.RegisterDirectory(Directory2);

            var result1 = _repository.DirectoryIsRegistered(Directory1.DirectoryDomain);
            var result2 = _repository.DirectoryIsRegistered(Directory2.DirectoryDomain);

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
        }

        [Test]
        public void ShouldExist()
        {
            _repository.RegisterDirectory(Directory1);

            var isRegistered = _repository.DirectoryIsRegistered(Directory1.DirectoryDomain);

            Assert.IsTrue(isRegistered);
        }

        [Test]
        public void ShouldNotExist()
        {
            _repository.RegisterDirectory(Directory1);

            var isRegistered = _repository.DirectoryIsRegistered(Directory2.DirectoryDomain);

            Assert.IsFalse(isRegistered);
        }
    }
}