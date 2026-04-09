using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service {
    public class AssetSyncCoordinator: IAssetSyncCoordinator {
        private readonly IFieldOpsAssetPayloadTransformer _transformer;
        private readonly IAssetHubClient _assetHubClient;
        private readonly ILogger<AssetSyncCoordinator> _logger;

        public AssetSyncCoordinator(
            IFieldOpsAssetPayloadTransformer transformer,
            IAssetHubClient assetHubClient,
            ILogger<AssetSyncCoordinator> logger) {
            _transformer = transformer;
            _assetHubClient = assetHubClient;
            _logger = logger;
        }

        public async Task ProcessAsync(string rawJson, CancellationToken cancellationToken = default) {
            // 1. Transform + validate
            var payload = _transformer.TransformOrThrow(rawJson);

            _logger.LogInformation(
                "Processing event {EventId} ({EventType}) for AssetId {AssetId}",
                payload.EventId,
                payload.EventType,
                payload.AssetId);

            // 2. Dedup check (ALWAYS before create)
            var existing = await _assetHubClient.FindByAssetIdAsync(payload.AssetId, cancellationToken);

            if (payload.EventType == "asset.registration.submitted") {
                if (existing is not null) {
                    // Idempotency: treat as already processed
                    _logger.LogWarning(
                        "Duplicate detected for AssetId {AssetId}. Skipping create.",
                        payload.AssetId);

                    return;
                }

                // 3. Create
                var created = await _assetHubClient.CreateAssetAsync(
                    new CreateAssetRequest(
                        AssetId: payload.AssetId,
                        Name: payload.AssetName,
                        Description: payload.Category),
                    cancellationToken);

                _logger.LogInformation(
                    "Created asset {AssetId} (record id: {RecordId})",
                    payload.AssetId,
                    created.Id);

                // 4. Upload photo (optional)
                if (!string.IsNullOrWhiteSpace(payload.ImageUrl)) {
                    await UploadPhotoAsync(created.Id, payload.ImageUrl!, cancellationToken);
                }
            } else if (payload.EventType == "asset.checkin.updated") {
                if (existing is null) {
                    // Cannot update something that doesn't exist
                    _logger.LogWarning(
                        "Asset {AssetId} not found for check-in update. Skipping.",
                        payload.AssetId);

                    return;
                }

                // 5. Update
                await _assetHubClient.UpdateAssetAsync(
                    existing.Id,
                    new UpdateAssetRequest(
                        Name: payload.AssetName,
                        Description: payload.Onsite == true ? "On Site" : "Checked Out"),
                    cancellationToken);

                _logger.LogInformation(
                    "Updated asset {AssetId} (record id: {RecordId})",
                    payload.AssetId,
                    existing.Id);
            } else {
                throw new InvalidOperationException(
                    $"Unsupported event type '{payload.EventType}'.");
            }
        }

        private async Task UploadPhotoAsync(
            string assetRecordId,
            string imageUrl,
            CancellationToken cancellationToken) {
            try {
                using var http = new HttpClient();
                using var stream = await http.GetStreamAsync(imageUrl, cancellationToken);

                await _assetHubClient.UploadPhotoAsync(
                    assetRecordId,
                    new UploadPhotoRequest(
                        FileName: Path.GetFileName(imageUrl),
                        ContentType: "image/jpeg",
                        Content: stream),
                    cancellationToken);

                _logger.LogInformation(
                    "Uploaded photo for asset record {RecordId}",
                    assetRecordId);
            } catch (Exception ex) {
                _logger.LogWarning(
                    ex,
                    "Photo upload failed for asset record {RecordId}",
                    assetRecordId);
            }
        }
    }
}
