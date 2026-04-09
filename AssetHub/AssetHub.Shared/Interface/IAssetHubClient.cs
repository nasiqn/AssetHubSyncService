using AssetHub.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public interface IAssetHubClient {
        Task<AssetDto?> FindByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);

        Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);

        Task<AssetDto> UpdateAssetAsync(string assetRecordId, UpdateAssetRequest request, CancellationToken cancellationToken = default);

        Task UploadPhotoAsync(string assetRecordId, UploadPhotoRequest request, CancellationToken cancellationToken = default);
    }
}
