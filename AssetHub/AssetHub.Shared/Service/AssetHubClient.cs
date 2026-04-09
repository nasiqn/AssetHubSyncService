using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssetHub.Shared.Service {
    public class AssetHubClient : IAssetHubClient {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;

        public AssetHubClient(HttpClient httpClient) {
            _httpClient = httpClient;
        }
        public async Task<AssetDto?> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default) {
            ArgumentNullException.ThrowIfNull(request);

            var existing = await FindByAssetIdAsync(request.AssetId, cancellationToken);
            if (existing is not null) {
                throw new InvalidOperationException(
                    $"An asset with Asset ID '{request.AssetId}' already exists (record id: {existing.Id}).");
            }

            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("assets", request, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw BuildException(response.StatusCode, "Asset creation failed.", errorBody);
            }
            return await response.Content.ReadFromJsonAsync<AssetDto>(JsonOptions, cancellationToken)
                   ?? throw new InvalidOperationException("Asset create response was empty.");
        }

        public async Task<AssetDto?> FindByAssetIdAsync(string assetId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(assetId))
                throw new ArgumentException("Asset ID is required.", nameof(assetId));

            var path = $"assets/search?assetId={Uri.EscapeDataString(assetId)}";

            using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw BuildException(response.StatusCode, "Asset search failed.", errorBody);
            }

            AssetSearchResponse result = await response.Content.ReadFromJsonAsync<AssetSearchResponse>(JsonOptions, cancellationToken)
                         ?? new AssetSearchResponse(Array.Empty<AssetDto>());

            return result.Items.FirstOrDefault();
        }

        public async Task<AssetDto?> UpdateAssetAsync(string assetRecordId, UpdateAssetRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(assetRecordId))
                throw new ArgumentException("Asset record ID is required.", nameof(assetRecordId));

            ArgumentNullException.ThrowIfNull(request);

            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                $"assets/{Uri.EscapeDataString(assetRecordId)}",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw BuildException(response.StatusCode, "Asset update failed.", errorBody);
            }

            return await response.Content.ReadFromJsonAsync<AssetDto>(JsonOptions, cancellationToken)
                   ?? throw new InvalidOperationException("Asset update response was empty.");
        }

        public async Task UploadPhotoAsync(string assetRecordId, UploadPhotoRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(assetRecordId))
                throw new ArgumentException("Asset record ID is required.", nameof(assetRecordId));

            ArgumentNullException.ThrowIfNull(request);

            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(request.Content);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);

            form.Add(fileContent, "file", request.FileName);

            using var response = await _httpClient.PostAsync(
                $"assets/{Uri.EscapeDataString(assetRecordId)}/photo",
                form,
                cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw BuildException(response.StatusCode, "Photo upload failed.", errorBody);
            }
        }

        private static AssetHubException BuildException(HttpStatusCode statusCode, string message, string responseBody)
       => new(statusCode, message, responseBody);
    }
}
