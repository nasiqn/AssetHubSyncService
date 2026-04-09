using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using AssetHub.Shared.Util;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AssetHub.Shared {
    public sealed class AssetHubRequestRetryHandler: DelegatingHandler {
        private const string RetryMarker = "AssetHubRetryAttempted";

        private readonly IAssetHubTokenProvider _tokenProvider;
        private readonly AssetHubSettings _options;

        public AssetHubRequestRetryHandler(
            IAssetHubTokenProvider tokenProvider,
            IOptions<AssetHubSettings> options) {
            _tokenProvider = tokenProvider;
            _options = options.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized) {
                return response;
            }

            if (request.Options.TryGetValue(new HttpRequestOptionsKey<bool>(RetryMarker), out var retried) && retried) {
                return response;
            }

            response.Dispose();

            await _tokenProvider.ForceRefreshAsync(cancellationToken);

            var clonedRequest = await request.CloneAsync(cancellationToken);
            clonedRequest.Options.Set(new HttpRequestOptionsKey<bool>(RetryMarker), true);

            return await base.SendAsync(clonedRequest, cancellationToken);
        }
    }
}
