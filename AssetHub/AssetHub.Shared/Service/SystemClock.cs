using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service {
    public sealed class SystemClock : ISystemClock {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
