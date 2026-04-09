using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public interface IAssetHubTokenProvider {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
        Task<string> ForceRefreshAsync(CancellationToken cancellationToken = default);
    }
}
