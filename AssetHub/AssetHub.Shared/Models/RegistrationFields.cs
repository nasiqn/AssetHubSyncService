using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AssetHub.Shared.Models {
    public class RegistrationFields {
        [JsonPropertyName("assetName")]
        public string? AssetName { get; init; }

        [JsonPropertyName("make")]
        public string? Make { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; init; }

        [JsonPropertyName("yearMfg")]
        public string? YearMfg { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("ratePerHour")]
        public string? RatePerHour { get; init; }

        [JsonPropertyName("supplier")]
        public string? Supplier { get; init; }
    }
}
