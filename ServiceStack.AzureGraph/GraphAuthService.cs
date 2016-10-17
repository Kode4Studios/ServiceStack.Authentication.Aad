﻿using System;
using System.Net;
using ServiceStack.AzureGraph.ServiceModel;
using ServiceStack.AzureGraph.ServiceModel.Requests;

namespace ServiceStack.AzureGraph
{
    public class GraphAuthService : Service
    {
        private readonly IApplicationRegistryService _registry;

        public GraphAuthService(IApplicationRegistryService registry)
        {
            _registry = registry;
        }

        public object Post(RegisteredDomainCheck request)
        {
            if (string.IsNullOrWhiteSpace(request?.Username))
                return new HttpResult(HttpStatusCode.BadRequest);

            var idx = request.Username.IndexOf("@", StringComparison.Ordinal);
            if (idx < 0)
                return new HttpResult(HttpStatusCode.BadRequest);

            var isRegistered = _registry.ApplicationIsRegistered(request.Username.Substring(idx + 1));

            return new RegisteredDomainCheckResponse
            {
                IsRegistered = isRegistered
            };
        }
    }
}