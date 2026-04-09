using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AssetHub.Shared.Models {
    public class FieldOpsEnvelope {
        [JsonPropertyName("eventType")]
        public string? EventType { get; init; }

        [JsonPropertyName("eventId")]
        public string? EventId { get; init; }

        [JsonPropertyName("projectId")]
        public string? ProjectId { get; init; }

        [JsonPropertyName("siteRef")]
        public string? SiteRef { get; init; }

        [JsonPropertyName("fields")]
        public RegistrationFields? Fields { get; init; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; init; }

        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; init; }

        [JsonPropertyName("make")]
        public string? Make { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("checkInDate")]
        public string? CheckInDate { get; init; }

        [JsonPropertyName("checkOutDate")]
        public string? CheckOutDate { get; init; }
    }
}
