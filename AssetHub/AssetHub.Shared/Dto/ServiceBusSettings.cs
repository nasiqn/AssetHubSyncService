using System.ComponentModel.DataAnnotations;

namespace AssetHub.Shared.Dto {
    public sealed class ServiceBusSettings {
        public const string SectionName = "ServiceBus";
        // only for local development, should be overridden by environment variables or user secrets in production
        public string ConnectionString { get; set; } = string.Empty;

        [Required]
        public string FullyQualifiedNamespace { get; set; } = string.Empty;

        [Required]
        public string TopicName { get; set; } = string.Empty;

        [Required]
        public string SubscriptionName { get; set; } = string.Empty;

        public int MaxConcurrentCalls { get; set; } = 4;

        public bool AutoCompleteMessages { get; set; } = false;
    }
}
