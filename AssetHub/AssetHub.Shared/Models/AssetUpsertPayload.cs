using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Models {
    public class AssetUpsertPayload {
        public string ProjectId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;

        public string AssetId { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Ownership { get; set; } = "Subcontracted";

        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        public string? SiteReference { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public int? YearManufactured { get; set; }
        public decimal? RatePerHour { get; set; }
        public string? Supplier { get; set; }
        public string? ImageUrl { get; set; }

        public DateTimeOffset? CheckInDate { get; set; }
        public DateTimeOffset? CheckOutDate { get; set; }
        public bool? Onsite { get; set; }
    }
}
