namespace ServiceStack.AzureGraph.ServiceModel.Requests
{
    [Route("/ms-graph/dom-check")]
    public class RegisteredDomainCheck : IReturn<RegisteredDomainCheckResponse>
    {
        public string Username { get; set; }
    }

    public class RegisteredDomainCheckResponse
    {
        public bool IsRegistered { get; set; }
    }
}