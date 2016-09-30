namespace ServiceStack.Authentication.Aad
{
    public interface IDirectoryRepository
    {
        bool DirectoryIsRegistered(string directoryDomain);
        DirectoryRegistration GetDirectoryByTenantName(string domain);
        DirectoryRegistration GetDirectoryByTenantId(string tenantId);
        DirectoryRegistration RegisterDirectory(DirectoryRegistration registration);

        DirectoryRegistration RegisterDirectory(string clientId, string clientSecret, string tenantId,
            string directoryDomain,
            string domainHint, long? refId, string refIdStr);

        void InitSchema();
    }
}