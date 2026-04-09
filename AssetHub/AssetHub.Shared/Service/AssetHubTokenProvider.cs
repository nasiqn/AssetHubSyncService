using AssetHub.Shared.Dto;
using AssetHub.Shared.Interface;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

public sealed class AssetHubTokenProvider : IAssetHubTokenProvider {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AssetHubSettings _assetHubSettings;
    private readonly ISystemClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private TokenCacheEntry? _cachedToken;

    public AssetHubTokenProvider(
        HttpClient httpClient,
        IOptions<AssetHubSettings> options,
        ISystemClock clock) {
        _httpClient = httpClient;
        _assetHubSettings = options.Value;
        _clock = clock;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) {
        if (HasUsableToken()) {
            return _cachedToken!.AccessToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try {
            if (HasUsableToken()) {
                return _cachedToken!.AccessToken;
            }

            _cachedToken = await RequestTokenAsync(cancellationToken);
            return _cachedToken.AccessToken;
        } finally {
            _gate.Release();
        }
    }

    public async Task<string> ForceRefreshAsync(CancellationToken cancellationToken = default) {
        await _gate.WaitAsync(cancellationToken);
        try {
            _cachedToken = await RequestTokenAsync(cancellationToken);
            return _cachedToken.AccessToken;
        } finally {
            _gate.Release();
        }
    }

    private bool HasUsableToken() {
        if (_cachedToken is null)
            return false;

        return _clock.UtcNow < _cachedToken.RefreshAtUtc;
    }

    private async Task<TokenCacheEntry> RequestTokenAsync(CancellationToken cancellationToken) {
        using var request = new HttpRequestMessage(HttpMethod.Post, _assetHubSettings.TokenUrl);

        var form = new Dictionary<string, string> {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _assetHubSettings.ClientId,
            ["client_secret"] = _assetHubSettings.ClientSecret
        };

        request.Content = new FormUrlEncodedContent(form);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            throw new AssetHubException(
                response.StatusCode,
                "Failed to retrieve OAuth access token.",
                body);
        }

        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Token response was empty or invalid.");

        if (string.IsNullOrWhiteSpace(token.access_token)) {
            throw new InvalidOperationException("Token response did not include access_token.");
        }

        var now = _clock.UtcNow;
        var expiresAt = now.AddSeconds(token.expires_in);
        var refreshAt = expiresAt.AddSeconds(-Math.Max(1, _assetHubSettings.RefreshSkewSeconds));

        if (refreshAt <= now) {
            refreshAt = now;
        }

        return new TokenCacheEntry(token.access_token, expiresAt, refreshAt);
    }

    private sealed record TokenCacheEntry(
        string AccessToken,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset RefreshAtUtc);
}