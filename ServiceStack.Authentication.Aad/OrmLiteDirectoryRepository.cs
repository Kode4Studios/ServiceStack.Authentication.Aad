using System;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Authentication.Aad
{
    public class OrmLiteDirectoryRepository : IDirectoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public OrmLiteDirectoryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool DirectoryIsRegistered(string directoryDomain)
        {
            using (var db = _connectionFactory.OpenDbConnection())
            {
                var loweredDomain = directoryDomain.ToLower();
                return db.Exists<DirectoryRegistration>(d => d.DirectoryDomain == loweredDomain);
            }
        }

        public DirectoryRegistration GetDirectoryFromDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;

            var loweredDomain = domain.ToLower();
            using (var db = _connectionFactory.OpenDbConnection())
            {
                return db.Single<DirectoryRegistration>(d => d.DirectoryDomain == loweredDomain);
            }
        }

        public DirectoryRegistration RegisterDirectory(DirectoryRegistration configuration)
        {
            if (configuration == null)
                throw new ArgumentException($"Cannot register null or empty {nameof(DirectoryRegistration)}.");

            return RegisterDirectory(configuration.ClientId, configuration.ClientSecret, configuration.TenantId,
                        configuration.DomainHint, configuration.DirectoryDomain, configuration.RefId, configuration.RefIdStr);
        }

        public DirectoryRegistration RegisterDirectory(string clientId, string clientSecret, string tenantId, string domainHint,
            string directoryDomain, long? refId, string refIdStr)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Parameter cannot be empty.", nameof(clientId));

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("Parameter cannot be empty.", nameof(clientSecret));

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Parameter cannot be empty.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(directoryDomain))
                throw new ArgumentException("Parameter cannot be empty.", nameof(directoryDomain));

            using (var db = _connectionFactory.OpenDbConnection())
            {
                var loweredDomain = directoryDomain.ToLower();
                if (db.Exists<DirectoryRegistration>(d => d.DirectoryDomain == loweredDomain))
                    throw new InvalidOperationException($"Aad domain {directoryDomain} is already registered");

                var id = db.Insert(new DirectoryRegistration
                {
                    ClientSecret = clientSecret,
                    ClientId = clientId,
                    TenantId = tenantId,
                    DirectoryDomain = directoryDomain,
                    RefId = refId,
                    RefIdStr = refIdStr
                }, true);

                return db.Single<DirectoryRegistration>(d => d.Id == id);
            }
        }

        public void InitSchema()
        {
            using (var db = _connectionFactory.OpenDbConnection())
            {
                if (!db.TableExists<DirectoryRegistration>())
                {
                    db.CreateTable<DirectoryRegistration>();
                }
            }
        }
    }
}