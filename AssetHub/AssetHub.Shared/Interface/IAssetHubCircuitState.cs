using AssetHub.Shared.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public interface IAssetHubCircuitState {
        CircuitStateView State { get; }
        DateTimeOffset? LastOpenedAtUtc { get; }
        void MarkOpened();
        void MarkClosed();
        void MarkHalfOpen();
        void MarkIsolated();
    }
}
