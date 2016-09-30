using ServiceStack.AzureGraph.ServiceModel.Entities;

namespace ServiceStack.AzureGraph.ServiceModel
{
    public interface IApplicationRegistryService
    {
        bool ApplicationIsRegistered(string directoryName);
        ApplicationRegistration GetApplicationByDirectoryName(string domain);
        ApplicationRegistration GetApplicationById(string tenantId);
        ApplicationRegistration RegisterApplication(ApplicationRegistration registration);

        ApplicationRegistration RegisterApplication(string applicationid, string publicKey, string directoryName,
            long? refId, string refIdStr);

        void InitSchema();
    }
}