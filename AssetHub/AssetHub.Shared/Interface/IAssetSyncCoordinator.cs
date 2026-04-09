using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public interface IAssetSyncCoordinator {
        Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default);
    }
}
