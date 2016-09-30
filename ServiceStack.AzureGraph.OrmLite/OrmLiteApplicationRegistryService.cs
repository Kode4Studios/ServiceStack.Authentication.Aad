using System;
using ServiceStack.AzureGraph.ServiceModel;
using ServiceStack.AzureGraph.ServiceModel.Entities;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.AzureGraph.OrmLite
{
    public class OrmLiteApplicationRegistryService : IApplicationRegistryService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public OrmLiteApplicationRegistryService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool ApplicationIsRegistered(string directoryName)
        {
            using (var db = _connectionFactory.OpenDbConnection())
            {
                var loweredDomain = directoryName.ToLower();
                return db.Exists<ApplicationRegistration>(d => d.DirectoryName == loweredDomain);
            }
        }

        public ApplicationRegistration GetApplicationByDirectoryName(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;

            var loweredDomain = domain.ToLower();
            using (var db = _connectionFactory.OpenDbConnection())
            {
                return db.Single<ApplicationRegistration>(d => d.DirectoryName == loweredDomain);
            }
        }

        public ApplicationRegistration GetApplicationById(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
                return null;

            using (var db = _connectionFactory.OpenDbConnection())
            {
                return db.Single<ApplicationRegistration>(d => d.ClientId == applicationId);
            }
        }

        public ApplicationRegistration RegisterApplication(ApplicationRegistration registration)
        {
            if (registration == null)
                throw new ArgumentException($"Cannot register null or empty {nameof(ApplicationRegistration)}.");

            return RegisterApplication(registration.ClientId, registration.ClientSecret, registration.DirectoryName,
                registration.RefId, registration.RefIdStr);
        }

        public ApplicationRegistration RegisterApplication(string applicationId, string publicKey, string directoryName, long? refId, string refIdStr)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
                throw new ArgumentException("Parameter cannot be empty.", nameof(applicationId));

            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Parameter cannot be empty.", nameof(publicKey));

            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentException("Parameter cannot be empty.", nameof(directoryName));

            using (var db = _connectionFactory.OpenDbConnection())
            {
                var loweredDomain = directoryName.ToLower();
                if (db.Exists<ApplicationRegistration>(d => d.DirectoryName == loweredDomain))
                    throw new InvalidOperationException($"Aad domain {directoryName} is already registered");

                var id = db.Insert(new ApplicationRegistration
                {
                    ClientId = applicationId,
                    ClientSecret = publicKey,
                    DirectoryName = directoryName,
                    RefId = refId,
                    RefIdStr = refIdStr
                }, true);

                return db.Single<ApplicationRegistration>(d => d.Id == id);
            }
        }

        public void InitSchema()
        {
        }
    }
}