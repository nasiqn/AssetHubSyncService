using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Dto {
    public class TimeoutSettings {
        public int AttemptTimeoutSeconds { get; set; }
        public int TotalTimeoutSeconds { get; set; }
    }
}
