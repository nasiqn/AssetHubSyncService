using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Dto {
    public class RetrySettings {
        public int MaxRetryAttempts { get; set; } = 4;
        public int BaseDelayMs { get; set; } = 500;
        public bool UseJitter { get; set; } = true;
    }
}
