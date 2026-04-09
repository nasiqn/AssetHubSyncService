using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace AssetHub.Shared {
    public sealed class AssetHubRequestHandler : DelegatingHandler {
        private readonly IAssetHubTokenProvider _tokenProvider;
        private readonly ILogger<AssetHubRequestHandler> _logger;
        private readonly AssetHubSettings _settings;

        public AssetHubRequestHandler(IAssetHubTokenProvider assetHubTokenProvider, IOptions<AssetHubSettings> options) {
            _tokenProvider = assetHubTokenProvider;
            _settings = options.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
