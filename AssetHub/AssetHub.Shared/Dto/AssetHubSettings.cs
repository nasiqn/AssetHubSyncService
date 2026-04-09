using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AssetHub.Shared.Dto {
    public sealed class AssetHubSettings {
        public const string SectionName = "AssetHub";

        [Required]
        [Url]
        public string BaseUrl { get; init; } = string.Empty;

        [Required]
        [Url]
        public string TokenUrl { get; init; } = string.Empty;

        [Required]
        public string ClientId { get; init; } = string.Empty;

        [Required]
        public string ClientSecret { get; init; } = string.Empty;

        // Refresh a bit early so we do not wait for expiry
        public int RefreshSkewSeconds { get; init; } = 60;
    }
}
