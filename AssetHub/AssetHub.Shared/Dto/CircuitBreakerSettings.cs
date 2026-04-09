using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Dto {
    public class CircuitBreakerSettings {
        public double FailureRatio { get; set; } = 0.5;
        public int MinimumThroughput { get; set; } = 8;
        public int SamplingDurationSeconds { get; set; } = 30;
        public int BreakDurationSeconds { get; set; } = 60;
    }
}
