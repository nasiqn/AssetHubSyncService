using AssetHub.Shared.Enum;
using AssetHub.Shared.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service {
    public class AssetHubCircuitState : IAssetHubCircuitState {
        private CircuitStateView _state = CircuitStateView.Closed;
        private DateTimeOffset? _lastOpenedAtUtc;

        public CircuitStateView State => _state;
        public DateTimeOffset? LastOpenedAtUtc => _lastOpenedAtUtc;

        public void MarkOpened() {
            _state = CircuitStateView.Open;
            _lastOpenedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkClosed() => _state = CircuitStateView.Closed;
        public void MarkHalfOpen() => _state = CircuitStateView.HalfOpen;
        public void MarkIsolated() => _state = CircuitStateView.Isolated;
    }
}
