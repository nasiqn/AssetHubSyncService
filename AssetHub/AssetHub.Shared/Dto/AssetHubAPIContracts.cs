using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AssetHub.Shared.Dto {
    public sealed record OAuthTokenResponse(
    string access_token,
    string token_type,
    int expires_in);

    public sealed record AssetDto(
        string Id,
        string AssetId,
        string Name,
        string? Description);

    public sealed record AssetSearchResponse(
        IReadOnlyList<AssetDto> Items);

    public sealed record CreateAssetRequest(
        string AssetId,
        string Name,
        string? Description);

    public sealed record UpdateAssetRequest(
        string Name,
        string? Description);

    public sealed record UploadPhotoRequest(
        string FileName,
        string ContentType,
        Stream Content);

    public sealed record AssetHubError(
        HttpStatusCode StatusCode,
        string Message,
        string? ResponseBody = null);

    public sealed class AssetHubException : Exception {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public AssetHubException(HttpStatusCode statusCode, string message, string? responseBody = null)
            : base(message) {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
