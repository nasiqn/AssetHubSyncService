using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Dto {
    public class ResilienceSettings {
        public const string SectionName = "Resilience";
        public RetrySettings Retry { get; set; } = new();
        public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
        public TimeoutSettings Timeout { get; set; } = new();
    }
}
